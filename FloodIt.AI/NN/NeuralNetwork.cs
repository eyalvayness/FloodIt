using FloodIt.Core;
using FloodIt.Core.Interfaces;
using FloodIt.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FloodIt.AI.NN
{
    public class NeuralNetwork : IComparable<NeuralNetwork>
    {
        public int InputSize => _layersInfo[0];
        public int OutputSize => _layersInfo[^1];
        public float Fitness { get; private set; }

        readonly int[] _layersInfo;

        public Layer[] Layers { get; }
        Trainer NNTrainer { get; }
        Player NNPlayer { get; }

        private NeuralNetwork()
        {
            NNTrainer = new(this);
            NNPlayer = new(this);
            _layersInfo = Array.Empty<int>();
            Layers = Array.Empty<Layer>();
        }

        internal NeuralNetwork(int[] layers, Activation[] activations) : this()
        {
            if (layers.Length < 2)
                throw new ArgumentException($"The Neural Network must have at least 2 different layers (input and output).", nameof(layers));
            if (activations.Length != layers.Length - 1)
                throw new ArgumentException($"Not the good amount of activation functions (received: {activations.Length}, expecting: {layers.Length - 1}).", nameof(activations));

            _layersInfo = layers.ToArray();
            Layers = new Layer[layers.Length - 1];

            InitLayers(activations);
        }

        [JsonConstructor] internal NeuralNetwork(Layer[] layers) : this()
        {
            if (layers.Length < 1)
                throw new ArgumentException($"The Neural Network must have at least 2 different layers.", nameof(layers));

            _layersInfo = layers.Select(l => l.InputSize).Append(layers[^1].OutputSize).ToArray();
            Layers = layers;
        }

        void InitLayers(Activation[] activations)
        {
            for (int i = 0; i < Layers.Length; i++)
                Layers[i] = new Layer(_layersInfo[i], _layersInfo[i + 1], activations[i]);
        }

        //Pre compile nn for predictions
        void Compile() { }


        NeuralNetwork CreateChild()
        {
            NeuralNetwork nn = new(this.Layers);
            foreach (var l in nn.Layers)
            {
                l.Evolution();
            }
            return nn;
        }

        public NeuralNetwork[] CreateChildren(int childrenCount)
        {
            List<NeuralNetwork> res = new();

            for (int i = 0; i < childrenCount; i++)
            {
                var child = CreateChild();
                res.Add(child);
            }
            return res.ToArray();
        }

        internal float[] FeedForward(float[] input)
        {
            var arr = input;

            foreach (var layer in Layers)
            {
                arr = layer.FeedForward(arr);
            }

            return arr;
        }

        public async Task<float> TrainAsync(GameSettings? settings = null)
        {
            settings ??= new();

            if (settings.Count != InputSize)
                throw new InvalidOperationException($"The board size ({settings.Count}) doesn't match neural network input size ({InputSize}).");
            if (settings.UsedBrushes.Length != OutputSize + 1)
                throw new InvalidOperationException($"The neural network output size ({OutputSize}) must be one less than the total number of brushes ({settings.UsedBrushes.Length}).");

            Fitness = 0;
            Brush[] board = new Brush[settings.Count];
            Brush getter(int i) => board[i];
            void setter(int i, Brush b) => board[i] = b;
            Game g = new(getter, setter, settings);


            await g.StartGameAsync(NNTrainer, colorAsync: false);
            return Fitness;
        }

        public Task<bool> PlayAsync(GameSettings? settings = null, CancellationToken cancellationToken = default)
        {
            settings ??= new();

            if (settings.Count != InputSize)
                throw new InvalidOperationException($"The board size ({settings.Count}) doesn't match neural network input size ({InputSize}).");
            if (settings.UsedBrushes.Length != OutputSize + 1)
                throw new InvalidOperationException($"The neural network output size ({OutputSize}) must be one less than the total number of brushes ({settings.UsedBrushes.Length}).");

            Brush[] board = new Brush[settings.Count];
            Brush getter(int i) => board[i];
            void setter(int i, Brush b) => board[i] = b;
            Game g = new(getter, setter, settings);


            return g.StartGameAsync(NNPlayer, colorAsync: true, cancellationToken: cancellationToken);
        }

        public int CompareTo(NeuralNetwork? other)
        {
            if (other == null || other.Fitness < Fitness)
                return 1;
            if (Fitness < other.Fitness)
                return -1;

            return 0;
        }

        private class Trainer : IStrategy, IAsyncStrategy
        {
            readonly WeakReference<NeuralNetwork> _parent;
            NeuralNetwork Parent => _parent.TryGetTarget(out var parent) ? parent : throw new NullReferenceException($"{nameof(Parent)} has been collected by GC");

            public Trainer(NeuralNetwork parent)
            {
                _parent = new(parent);
            }

            public void Train(Game g)
            {
                g.StartGame(this);
            }

            Brush IStrategy.Play(GameState state)
            {
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

            Task<Brush> IAsyncStrategy.PlayAsync(GameState state, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
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

                return Task.FromResult(b);
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

        private class Player : IAsyncStrategy
        {
            readonly WeakReference<NeuralNetwork> _parent;
            NeuralNetwork Parent => _parent.TryGetTarget(out var parent) ? parent : throw new NullReferenceException($"{nameof(Parent)} has been collected by GC");

            public Player(NeuralNetwork parent)
            {
                _parent = new(parent);
            }

            Task<Brush> IAsyncStrategy.PlayAsync(GameState state, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                float[] xs = state.SimplifiedBoard.Select(b => (float)b).ToArray();

                var ys = Parent.FeedForward(xs);
                var maxV = ys.Max();
                var index = (byte)(ys.ToList().IndexOf(maxV) + 1);

                Brush? b = null;
                if (state.PlayableBytes.Contains(index))
                    b = state.GetBrushFromByte(index);
                else
                    b = state.PlayableBrushes.Random();

                return Task.FromResult(b);
            }
        }

        public class Layer
        {
            readonly static Random _rand;

            readonly float[] _biaises;
            readonly float[,] _weights;
            readonly Activation _activation;

            public int InputSize { get; }
            public int OutputSize { get; }

            static Layer() => _rand = new();
            internal Layer(int inputOutputSize, Activation activation) : this(inputOutputSize, inputOutputSize, activation) { }
            internal Layer(int inputSize, int outputSize, Activation activation)
            {
                InputSize = inputSize;
                OutputSize = outputSize;
                _weights = new float[inputSize, outputSize];
                _biaises = new float[outputSize];
                _activation = activation;

                InitValues();
            }

            [JsonConstructor] internal Layer(float[,] weights, float[] biaises, Activation activation)
            {
                InputSize = weights.GetLength(1);
                OutputSize = weights.GetLength(0);
                _activation = activation;

                _weights = weights;
                _biaises = biaises;
            }

            static float GetRandom() => GetRandom(-.5f, .5f);
            static float GetRandom(float min, float max) => (float)(_rand.NextDouble() * (max - min) + min);
            static float GetNormalDistribution() => .4f - 1 / MathF.Sqrt(2 * MathF.PI) * MathF.Pow(MathF.E, -(1 / 2) * MathF.Pow(GetRandom(), 2));

            void InitValues()
            {
                for (int i = 0; i < OutputSize; i++)
                {
                    _biaises[i] = GetRandom();
                    for (int j = 0; j < InputSize; j++)
                    {
                        _weights[j, i] = GetRandom();
                    }
                }
            }

            public float GetBiaiseAt(int index) => _biaises[index];
            public float GetWeightAt(int inputIndex, int outputIndex) => _weights[inputIndex, outputIndex];

            public float[] FeedForward(float[] xs)
            {
                if (xs.Length != InputSize)
                    throw new ArgumentOutOfRangeException(nameof(xs), $"The input array must be of length {InputSize}.");

                float[] ys = new float[OutputSize];

                for (int i = 0; i < OutputSize; i++)
                {
                    //ys[i] = _biaises[i];
                    //for (int j = 0; j < InputSize; j++)
                    //{
                    //    ys[i] = ys[i] + xs[j] * _weights[j, i];
                    //}
                    //var val = _biaises[i] + xs.Select((v, j) => v * _weights[j, i]).Sum();
                    ys[i] = _biaises[i] + xs.Select((v, j) => v * _weights[j, i]).Sum();
                }
                ys = _activation.Activate(ys);
                return ys;
            }

            internal void Evolution()
            {
                for (int i = 0; i < _weights.GetLength(0); i++)
                {
                    for (int j = 0; j < _weights.GetLength(1); j++)
                    {
                        _weights[i, j] += GetNormalDistribution();
                    }
                }

                for (int k = 0; k < _biaises.Length; k++)
                {
                    _biaises[k] += GetNormalDistribution();
                }
            }
        }
    }
}
