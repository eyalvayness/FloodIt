using FloodIt.Core;
using FloodIt.Core.Interfaces;
using FloodIt.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FloodIt.AI.NN
{
    public partial class CompiledNeuralNetwork
    {
        private class Player : IStrategy, IAsyncStrategy
        {
            readonly WeakReference<CompiledNeuralNetwork> _parent;
            List<int> _lastStates;
            int _currentULZCount;

            CompiledNeuralNetwork Parent => _parent.TryGetTarget(out var parent) ? parent : throw new NullReferenceException($"{nameof(Parent)} has been collected by GC");


            int _currentCount = 0;
            int _currentMaxIteration = 1_000;

            public Player(CompiledNeuralNetwork parent)
            {
                _parent = new(parent);
                _lastStates = new();
            }

            public int Play(Game g, int maxIteration = 1_000)
            {
                _currentCount = 0;
                _currentMaxIteration = maxIteration;
                _lastStates = new();
                _currentULZCount = -1;
                g.StartGame(this);
                return _currentCount;
            }

            public async Task<int> PlayAsync(Game g, int maxIteration = 1_000, CancellationToken cancellationToken = default)
            {
                _currentCount = 0;
                _currentMaxIteration = maxIteration;
                _lastStates = new();
                _currentULZCount = -1;
                await g.StartGameAsync(this, false, cancellationToken);
                return _currentCount;
            }

            Brush? IStrategy.Play(GameState state) => CommonPlay(state);

            Brush? CommonPlay(GameState state)
            {
                if (++_currentCount >= _currentMaxIteration)
                {
                    return null;
                }
                float[] xs = state.SimplifiedBoard.Select(b => (float)b).ToArray();

                var ys = Parent.Compute(xs);
                var maxV = ys.Max();
                var index = (byte)(ys.ToList().IndexOf(maxV) + 1);

                Brush? b = null;

                bool diffState = NeedUnblock(state);
                if (state.PlayableBytes.Contains(index) && diffState)
                    b = state.GetBrushFromByte(index);
                else
                    b = state.PlayableBrushes.Random();

                return b;
            }

            private bool NeedUnblock(GameState state)
            {
                int k = Array.LastIndexOf<byte>(state.SimplifiedBoard, 0);
                
                if (state.ULZCount != _currentULZCount)
                {
                    _lastStates.Clear();
                    _lastStates.Add(k);
                    _currentULZCount = state.ULZCount;
                    return true;
                }

                if (_lastStates.Contains(k))
                    return false;
                _lastStates.Add(k);
                return true;
            }

            async Task<Brush?> IAsyncStrategy.PlayAsync(GameState state, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(1000, cancellationToken);
                return CommonPlay(state);
            }
        }
    }
}