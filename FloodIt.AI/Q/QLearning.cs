using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using FloodIt.Core;
using FloodIt.Core.Interfaces;
using System.Linq;
using System.Diagnostics;
using FloodIt.Core.Utils;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text;

namespace FloodIt.AI.Q
{
    public class QLearning
    {
        public float Alpha { get; }
        public float Gamma { get; }
        public Dictionary<byte[], float[]> Q { get; }
        public GameSettings Settings { get; }
        Learner QLearner { get; }
        Player QPlayer { get; }

        public QLearning(float alpha, float gamma, GameSettings? settings)
        {
            if (alpha is < 0 or > 1)
                throw new ArgumentOutOfRangeException(nameof(alpha), $"{nameof(alpha)} must be between 0 and 1");
            if (gamma is < 0 or > 1)
                throw new ArgumentOutOfRangeException(nameof(gamma), $"{nameof(gamma)} must be between 0 and 1");

            Settings = settings ?? new();
            Alpha = alpha;
            Gamma = gamma;
            Q = new(new SimplifiedBoardEqualityComparer());
            QLearner = new(this);
            QPlayer = new(this);
        }

        [JsonConstructor]
        internal QLearning(float alpha, float gamma, GameSettings? settings, Dictionary<byte[], float[]> q) : this(alpha, gamma, settings)
        {
            Q = q;
        }

        public float Learn(int batch = 50)
        {
            List<float> rTrace = new();
            float averageR = 0;
            for (int n = 0; n < batch; n++)
            {
                QLearner.UpdateExplorationProb(n);
                Brush[] board = new Brush[Settings.Count];
                Brush getter(int i) => board[i];
                void setter(int i, Brush b) => board[i] = b;

                Game game = new(getter, setter, Settings);

                float r = QLearner.Learn(game);
                averageR += r;
                rTrace.Add(r);
            }

            var ordered = rTrace.OrderBy(d => d).ToArray();
            var negs = rTrace.Where(d => d < 0).ToList();
            var bads = rTrace.Where(d => 0 <= d && d < 1).ToList();
            var goods = rTrace.Where(d => 1 <= d && d < 2).ToList();
            var greats = rTrace.Where(d => 2 <= d).ToList();

            var median = (ordered[(ordered.Length - 1) / 2] + ordered[ordered.Length / 2]) / 2;
            Debug.WriteLine($"N = {negs.Count}, B = {bads.Count}, Go = {goods.Count}, Gr = {greats.Count}, Med = {median}");
            return averageR / batch;
        }

        public string Save(string filename, bool writeIndented = false)
        {
            var opt = GetSerializerOptions(writeIndented);
            var json = JsonSerializer.Serialize(this, opt);

            System.IO.File.WriteAllText(filename, json);
            return json;
        }
        public static QLearning? Load(string filename, bool writeIndented = false)
        {
            var opt = GetSerializerOptions(writeIndented);
            var json = System.IO.File.ReadAllText(filename);
            var ai = JsonSerializer.Deserialize<QLearning>(json, opt);

            return ai;
        }
        static JsonSerializerOptions GetSerializerOptions(bool writeIndented)
        {
            var opt = new JsonSerializerOptions()
            {
                WriteIndented = writeIndented
            };
            opt.Converters.Add(new JsonConverters.QLearningConverter());
            opt.Converters.Add(new Core.JsonConverters.GameSettingsConverter());
            return opt;
        }

        public async Task PlayAsync(BrushGetter getBrush, BrushSetter setBrush, CancellationToken cancellationToken = default)
        {
            var game = new Game(getBrush, setBrush, Settings);
            await game.StartGameAsync(QPlayer, colorAsync: true, cancellationToken: cancellationToken);
        }

        private class Learner : IStrategy
        {
            readonly WeakReference<QLearning> _parent;

            QLearning Parent => _parent.TryGetTarget(out var parent) ? parent : throw new NullReferenceException($"{nameof(Parent)} has been collected by GC");
            public float Alpha => Parent.Alpha;
            public float Gamma => Parent.Gamma;
            public Dictionary<byte[], float[]> Q => Parent.Q;
            public int BrushesCount => Parent.Settings.UsedBrushes.Length;
            public List<List<float>> Rewards { get; }
            public List<float> CurrentRewards => Rewards[^1];

            readonly float _minExplorationProb = 0.0005f;//0.005;//0.01;
            readonly float explorationProbDecay = 0.3f;//0.1;//0.03;
            readonly Random _rand;
            double explorationProb;

            public Learner(QLearning parent)
            {
                _parent = new(parent);
                Rewards = new();
                _rand = new();
            }

            internal void UpdateExplorationProb(int n) => explorationProb = Math.Max(_minExplorationProb, Math.Exp(-explorationProbDecay * n));

            public float Learn(Game game)
            {
                Rewards.Add(new());
                game.StartGame(this);
                var mOne = CurrentRewards.Count(d => d == -1);
                return CurrentRewards.Count > 0 ? CurrentRewards.Average() : 0;
            }

            Brush IStrategy.Play(GameState state)
            {
                bool boardCreation = false;
                bool random = false;
                if (!Q.ContainsKey(state))
                {
                    boardCreation = true;
                    Q.Add(state, new float[BrushesCount - 1]);
                }
                var stateQ = Q[state];

                byte? action = null;
                double actionValue = double.MinValue;
                foreach (byte b in state.PlayableBytes)
                {
                    double qValue = stateQ[b - 1];

                    if (actionValue <= qValue)
                    {
                        action = b;
                        actionValue = qValue;
                    }
                }
                if (_rand.NextDouble() < explorationProb)
                {
                    random = true;
                    action = state.PlayableBytes.Random();
                }

                var actionByte = action ?? state.PlayableBytes.Random();
                var actionBrush = state.GetBrushFromByte(actionByte);
                var r = GetRewardForAction(state, actionBrush, out GameState newgs);
                CurrentRewards.Add(r);

                float max = 0;
                if (Q.ContainsKey(newgs))
                {
                    max = float.MinValue;
                    foreach (var val in Q[newgs])
                        if (max <= val)
                            max = val;
                }

                stateQ[actionByte - 1] = (1 - Alpha) * stateQ[actionByte - 1] + Alpha * (r + Gamma * max);

                var trace = new { State = stateQ, Board = boardCreation, B = actionByte, R = r, Random = random };
                return actionBrush;
            }

            static float GetRewardForAction(GameState oldState, Brush action, out GameState newState)
            {
                newState = oldState.PlayBrush(action, usingDistance: false);

                int uzl = newState.ULZCount - oldState.ULZCount;
                int blobs = -(newState.BlobCount - oldState.BlobCount);
                int colors = -(newState.PlayableBrushCount - oldState.PlayableBrushCount);
                int end = Convert.ToInt32(newState.IsFinished);

                float b = -1;
                float uzlFact = 1;
                float blobsFact = 1;
                float colorsFact = 2;
                float endFact = 3;

                float r = uzl * uzlFact + blobs * blobsFact + colors * colorsFact + end * endFact + b;
                if (r != -1 && oldState == newState)
                {

                }

                return r;
            }
        }

        private class Player : IAsyncStrategy
        {
            readonly WeakReference<QLearning> _parent;

            QLearning Parent => _parent.TryGetTarget(out var parent) ? parent : throw new NullReferenceException($"{nameof(Parent)} has been collected by GC");
            public Dictionary<byte[], float[]> Q => Parent.Q;

            public Player(QLearning parent)
            {
                _parent = new(parent);
            }

            async Task<Brush> IAsyncStrategy.PlayAsync(GameState state, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await Task.Delay(250, cancellationToken);

                if (!Q.ContainsKey(state))
                    return state.PlayableBrushes.Random();

                byte? play = null;
                float maxValue = float.MinValue;
                var options = Q[state];
                for (int i = 0; i < options.Length; i++)
                {
                    if (maxValue < options[i])
                    {
                        maxValue = options[i];
                        play = (byte)(i + 1);
                    }
                }

                return state.GetBrushFromByte(play ?? state.PlayableBytes.Random());
            }
        }
    }
}
