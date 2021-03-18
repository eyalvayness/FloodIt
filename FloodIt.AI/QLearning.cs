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

namespace FloodIt.AI
{
    public class QLearning
    {
        public double Alpha { get; }
        public double Gamma { get; }
        public Dictionary<GameState, Dictionary<Brush, double>> Q { get; }
        public GameSettings Settings { get; }
        Learner QLearner { get; }
        Player QPlayer { get; }

        public QLearning(double alpha, double gamma, GameSettings? settings)
        {
            if (alpha is < 0 or > 1)
                throw new ArgumentOutOfRangeException(nameof(alpha), $"{nameof(alpha)} must be between 0 and 1");
            if (gamma is < 0 or > 1)
                throw new ArgumentOutOfRangeException(nameof(gamma), $"{nameof(gamma)} must be between 0 and 1");

            Settings = settings ?? new();
            Alpha = alpha;
            Gamma = gamma;
            Q = new();
            QLearner = new(this);
            QPlayer = new(this);
        }

        internal QLearning(double alpha, double gamma, GameSettings? settings, Dictionary<GameState, Dictionary<Brush, double>> q) : this(alpha, gamma, settings)
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
                //Debug.WriteLine($"{n+1}/{batch}: averageR = {r}");
            }

            return averageR / batch;
        }

        public void Save(string filename)
        {
            var opt = new System.Text.Json.JsonSerializerOptions()
            {
                WriteIndented = true
            };
            opt.Converters.Add(new JsonConverters.QLearningConverter());
            opt.Converters.Add(new Core.JsonConverters.GameSettingsConverter());
            var json = System.Text.Json.JsonSerializer.Serialize(this, opt);

            System.IO.File.WriteAllText(filename, json);
        }

        public static QLearning? Load(string filename)
        {
            var opt = new System.Text.Json.JsonSerializerOptions()
            {
                WriteIndented = true
            };
            opt.Converters.Add(new JsonConverters.QLearningConverter());
            opt.Converters.Add(new Core.JsonConverters.GameSettingsConverter());

            var json = System.IO.File.ReadAllText(filename);
            var ai = System.Text.Json.JsonSerializer.Deserialize<QLearning>(json, opt);

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
            public Dictionary<GameState, Dictionary<Brush, double>> Q => Parent.Q;
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

            //internal void ResetExplorationProb() => explorationProb = 1;
            internal void UpdateExplorationProb(int n) => explorationProb = Math.Max(_minExplorationProb, Math.Exp(-explorationDecayProb * n));

            public double Learn(Game game)
            {
                Rewards.Add(new());
                game.StartGame(this);
                return CurrentRewards.Average();
            }

            Brush IStrategy.Play(GameState state)
            {
                if (!Q.ContainsKey(state))
                    Q.Add(state, new());
                Dictionary<Brush, double> stateQ = Q[state];

                Brush? action = null;
                double actionValue = double.MinValue;
                foreach (Brush b in state.PlayableBrushes)
                {
                    if (!stateQ.ContainsKey(b))
                        stateQ.Add(b, 0);
                    double qValue = stateQ[b];

                    if (actionValue <= qValue)
                    {
                        action = b;
                        actionValue = qValue;
                    }
                }
                if (_rand.NextDouble() < explorationProb)
                    action = state.PlayableBrushes.Random();

                action ??= state.PlayableBrushes.Random();
                var r = GetRewardForAction(state, action, out GameState newgs);
                CurrentRewards.Add(r);

                double max = 0;
                if (!Q.ContainsKey(newgs))
                    Q.Add(newgs, new());
                foreach (var val in Q[newgs].Values)
                    if (max <= val)
                        max = val;

                Q[state][action] = (1 - Alpha) * Q[state][action] + Alpha * (r + Gamma * max);
                return action;
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
                double colorsFact = 1.5;
                double endFact = 2;

                double r = (uzl * uzlFact) + (blobs * blobsFact) + (colors * colorsFact) + (end * endFact) + b;
                return r;
            }
        }

        private class Player : IAsyncStrategy
        {
            readonly WeakReference<QLearning> _parent;

            QLearning Parent => _parent.TryGetTarget(out var parent) ? parent : throw new NullReferenceException($"{nameof(Parent)} has been collected by GC");
            public Dictionary<GameState, Dictionary<Brush, double>> Q => Parent.Q;
            
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

                Brush? play = null;
                double maxValue = double.MinValue;
                var options = Q[state];
                foreach (var brush in options.Keys)
                {
                    if (maxValue < options[brush])
                    {
                        maxValue = options[brush];
                        play = brush;
                    }
                }

                return play ?? state.PlayableBrushes.Random();
            }
        }
    }
}
