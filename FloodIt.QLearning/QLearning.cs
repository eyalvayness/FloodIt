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

namespace FloodIt.AI
{
    public class QLearning
    {
        public double Alpha { get; }
        public double Gamma { get; }
        public Dictionary<GameState, Dictionary<Brush, double>> Q { get; }
        Learner QLearner { get; }
        Player QPlayer { get; }

        public QLearning(double alpha, double gamma)
        {
            if (alpha is < 0 or > 1)
                throw new ArgumentOutOfRangeException(nameof(alpha), $"{nameof(alpha)} must be between 0 and 1");
            if (gamma is < 0 or > 1)
                throw new ArgumentOutOfRangeException(nameof(gamma), $"{nameof(gamma)} must be between 0 and 1");

            Alpha = alpha;
            Gamma = gamma;
            Q = new();
            QLearner = new(Alpha, Gamma, Q);
            QPlayer = new();
        }

        internal QLearning(double alpha, double gamma, Dictionary<GameState, Dictionary<Brush, double>> q) : this(alpha, gamma)
        {
            Q = q;
        }

        public double Learn(int batch = 50, GameSettings settings = null)
        {
            double averageR = 0;
            settings ??= new();
            for (int n = 0; n < batch; n++)
            {
                QLearner.UpdateExplorationProb(n);
                Brush[] board = new Brush[settings.Count];
                Brush getter(int i) => board[i];
                void setter(int i, Brush b) => board[i] = b;
                
                Game game = new(getter, setter, settings);

                double r = QLearner.Learn(game);
                averageR += r;
                //Debug.WriteLine($"{n+1}/{batch}: averageR = {r}");
            }

            return averageR / batch;
        }

        public async Task PlayAsync(BrushGetter getBrush, BrushSetter setBrush, GameSettings settings = null, CancellationToken cancellationToken = default)
        {
            var game = new Game(getBrush, setBrush, settings);
            await game.StartGameAsync(QPlayer, colorAsync: true, cancellationToken: cancellationToken);
        }

        private class Learner : IStrategy
        {
            public double Alpha { get; }
            public double Gamma { get; }
            public Dictionary<GameState, Dictionary<Brush, double>> Q { get; }
            public List<List<double>> Rewards { get; }

            readonly double _minExplorationProb = 0.01;
            readonly double explorationDecayProb = 0.03;
            readonly Random _rand;
            double explorationProb;

            public Learner(double alpha, double gamma, Dictionary<GameState, Dictionary<Brush, double>> q)
            {
                Alpha = alpha;
                Gamma = gamma;
                Q = q;
                Rewards = new();
                _rand = new();
            }

            //internal void ResetExplorationProb() => explorationProb = 1;
            internal void UpdateExplorationProb(int n) => explorationProb = Math.Max(_minExplorationProb, Math.Exp(-explorationDecayProb * n));

            public double Learn(Game game)
            {
                Rewards.Add(new());
                game.StartGame(this);
                return Rewards.Last().Average();
            }

            Brush IStrategy.Play(GameState state)
            {
                if (!Q.ContainsKey(state))
                    Q.Add(state, new());
                Dictionary<Brush, double> stateQ = Q[state];

                Brush action = null;
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

                var r = GetRewardForAction(state, action, out GameState newgs);
                Rewards.Last().Add(r);

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

                int b = -1;
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
            Task<Brush> IAsyncStrategy.PlayAsync(GameState state, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                throw new NotImplementedException();
            }
        }
    }
}
