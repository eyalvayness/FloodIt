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

namespace FloodIt.AI
{
    public class QLearning
    {
        static readonly JsonSerializerOptions _opt;

        public double Alpha { get; }
        public double Gamma { get; }
        public Dictionary<byte[], double[]> Q { get; }
        public GameSettings Settings { get; }
        Learner QLearner { get; }
        Player QPlayer { get; }

        static QLearning()
        {
            _opt = new JsonSerializerOptions()
            {
                WriteIndented = true
            };
            _opt.Converters.Add(new JsonConverters.QLearningConverter());
            _opt.Converters.Add(new Core.JsonConverters.GameSettingsConverter());
        }

        public QLearning(double alpha, double gamma, GameSettings? settings)
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

        internal QLearning(double alpha, double gamma, GameSettings? settings, Dictionary<byte[], double[]> q) : this(alpha, gamma, settings)
        {
            Q = q;
        }

        public double Learn(int batch = 50)
        {
            double averageR = 0;
            for (int n = 0; n < batch; n++)
            {
                QLearner.UpdateExplorationProb(n);
                Brush[] board = new Brush[Settings.Count];
                Brush getter(int i) => board[i];
                void setter(int i, Brush b) => board[i] = b;
                
                Game game = new(getter, setter, Settings);

                double r = QLearner.Learn(game);
                averageR += r;
            }

            return averageR / batch;
        }

        public string Save(string filename)
        {
            var json = JsonSerializer.Serialize(this, _opt);

            System.IO.File.WriteAllText(filename, json);
            return json;
        }

        public static QLearning? Load(string filename)
        {
            var json = System.IO.File.ReadAllText(filename);
            var ai = JsonSerializer.Deserialize<QLearning>(json, _opt);

            return ai;
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
            public double Alpha => Parent.Alpha;
            public double Gamma => Parent.Gamma;
            public Dictionary<byte[], double[]> Q => Parent.Q;
            public GameSettings Settings => Parent.Settings;
            public List<List<double>> Rewards { get; }
            public List<double> CurrentRewards => Rewards[^1];

            readonly double _minExplorationProb = 0.01;
            readonly double explorationDecayProb = 0.03;
            readonly Random _rand;
            double explorationProb;

            public Learner(QLearning parent)
            {
                _parent = new(parent);
                Rewards = new();
                _rand = new();
            }

            internal void UpdateExplorationProb(int n) => explorationProb = Math.Max(_minExplorationProb, Math.Exp(-explorationDecayProb * n));

            public double Learn(Game game)
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

                if (!Q.ContainsKey(state.SimplifiedBoard))
                {
                    boardCreation = true;
                    Q.Add(state.SimplifiedBoard, new double[Settings.UsedBrushes.Length - 1]);
                }
                var stateQ = Q[state.SimplifiedBoard];

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

                double max = 0;
                if (Q.ContainsKey(newgs.SimplifiedBoard))
                {
                    max = double.MinValue;
                    foreach (var val in Q[newgs.SimplifiedBoard])
                        if (max <= val)
                            max = val;
                }

                stateQ[actionByte - 1] = (1 - Alpha) * stateQ[actionByte - 1] + Alpha * (r + Gamma * max);

                var trace = new { State = stateQ, Board = boardCreation, B = actionByte, R = r, Random = random };
                return actionBrush;
            }

            static double GetRewardForAction(GameState oldState, Brush action, out GameState newState)
            {
                newState = oldState.PlayBrush(action, usingDistance: false);

                int uzl = newState.ULZCount - oldState.ULZCount;
                int blobs = oldState.BlobCount - newState.BlobCount;
                int colors = oldState.PlayableBrushCount - newState.PlayableBrushCount;
                int end = Convert.ToInt32(newState.IsFinished);

                double b = -1;
                double uzlFact = 1;
                double blobsFact = 1;
                double colorsFact = 2;
                double endFact = 3;

                double r = (uzl * uzlFact) + (blobs * blobsFact) + (colors * colorsFact) + (end * endFact) + b;
                return r;
            }
        }

        private class Player : IAsyncStrategy
        {
            readonly WeakReference<QLearning> _parent;

            QLearning Parent => _parent.TryGetTarget(out var parent) ? parent : throw new NullReferenceException($"{nameof(Parent)} has been collected by GC");
            public Dictionary<byte[], double[]> Q => Parent.Q;
            
            public Player(QLearning parent)
            {
                _parent = new(parent);
            }

            async Task<Brush> IAsyncStrategy.PlayAsync(GameState state, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await Task.Delay(250, cancellationToken);
                
                if (!Q.ContainsKey(state.SimplifiedBoard))
                    return state.PlayableBrushes.Random();

                byte? play = null;
                double maxValue = double.MinValue;
                var options = Q[state.SimplifiedBoard];
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
