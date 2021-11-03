using FloodIt.Core;
using FloodIt.Core.Interfaces;
using FloodIt.Core.Utils;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FloodIt.AI.NN
{
    public partial class NeuralNetwork
    {
        private class Trainer : IStrategy, IAsyncStrategy
        {
            readonly WeakReference<NeuralNetwork> _parent;
            NeuralNetwork Parent => _parent.TryGetTarget(out var parent) ? parent : throw new NullReferenceException($"{nameof(Parent)} has been collected by GC");

            int _currentCount = 0;
            int _currentMaxIteration = 1_000;

            public Trainer(NeuralNetwork parent)
            {
                _parent = new(parent);
            }

            public int Train(Game g, int maxIteration = 1_000)
            {
                _currentCount = 0;
                _currentMaxIteration = maxIteration;
                g.StartGame(this);
                return _currentCount;
            }

            public async Task<int> TrainAsync(Game g, int maxIteration = 1_000)
            {
                _currentCount = 0;
                _currentMaxIteration = maxIteration;
                await g.StartGameAsync(this, colorAsync: false, cancellationToken: default);
                return _currentCount;
            }

            Brush? IStrategy.Play(GameState state)
            {
                if (++_currentCount >= _currentMaxIteration)
                {
                    return null;
                }
                float[] xs = state.SimplifiedBoard.Select(b => (float)b).ToArray();

                var ys = Parent.FeedForward(xs);
                var maxV = ys.Max();
                var index = (byte)(ys.ToList().IndexOf(maxV) + 1);

                Brush? b = null;
                if (state.PlayableBytes.Contains(index))
                    b = state.GetBrushFromByte(index);
                else
                    b = state.PlayableBrushes.Random();


                float f = ComputeFitness(state, b);
                Parent.Fitness += f;

                return b;
            }

            Task<Brush?> IAsyncStrategy.PlayAsync(GameState state, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (++_currentCount >= _currentMaxIteration)
                {
                    return Task.FromResult<Brush?>(null);
                }
                float[] xs = state.SimplifiedBoard.Select(b => (float)b).ToArray();

                var ys = Parent.FeedForward(xs);
                var maxV = ys.Max();
                var index = (byte)(ys.ToList().IndexOf(maxV) + 1);

                Brush? b = null;
                if (state.PlayableBytes.Contains(index))
                    b = state.GetBrushFromByte(index);
                else
                    b = state.PlayableBrushes.Random();


                float f = ComputeFitness(state, b);
                Parent.Fitness += f;

                return Task.FromResult<Brush?>(b);
            }

            static float ComputeFitness(GameState oldState, Brush playedBrush)
            {
                GameState newState = oldState.PlayBrush(playedBrush);

                int uzl = newState.ULZCount - oldState.ULZCount;
                int blobs = -(newState.BlobCount - oldState.BlobCount);
                int colors = -(newState.PlayableBrushCount - oldState.PlayableBrushCount);
                int end = Convert.ToInt32(newState.IsFinished);

                float b = -1;// Can be changed to 0
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
    }
}
