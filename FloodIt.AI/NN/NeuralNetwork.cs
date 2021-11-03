using FloodIt.Core;
using FloodIt.Core.Interfaces;
using FloodIt.Core.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FloodIt.AI.NN
{
    public partial class NeuralNetwork : IComparable<NeuralNetwork>, IEquatable<NeuralNetwork>
    {
        public int InputSize => _layersInfo[0];
        public int OutputSize => _layersInfo[^1];
        public float Fitness { get; private set; }

        readonly int[] _layersInfo;
        readonly Layer[] _layers;

        public ReadOnlyCollection<Layer> Layers => new(_layers);
        Trainer NNTrainer { get; }
        Player NNPlayer { get; }

        private NeuralNetwork()
        {
            NNTrainer = new(this);
            NNPlayer = new(this);
            _layersInfo = Array.Empty<int>();
            _layers = Array.Empty<Layer>();
        }

        internal NeuralNetwork(int[] layers, Activation[] activations) : this()
        {
            if (layers.Length < 2)
                throw new ArgumentException($"The Neural Network must have at least 2 different layers (input and output).", nameof(layers));
            if (activations.Length != layers.Length - 1)
                throw new ArgumentException($"Not the good amount of activation functions (received: {activations.Length}, expecting: {layers.Length - 1}).", nameof(activations));

            _layersInfo = layers.ToArray();
            _layers = new Layer[layers.Length - 1];

            InitLayers(activations);
        }

        [JsonConstructor] internal NeuralNetwork(Layer[] layers) : this()
        {
            if (layers.Length < 1)
                throw new ArgumentException($"The Neural Network must have at least 2 different layers.", nameof(layers));

            _layersInfo = layers.Select(l => l.InputSize).Append(layers[^1].OutputSize).ToArray();
            //_layers = layers;
            _layers = new Layer[layers.Length];
            for (int i = 0; i < _layers.Length; i++)
            {
                _layers[i] = layers[i].Clone();
            }
        }

        void InitLayers(Activation[] activations)
        {
            for (int i = 0; i < _layers.Length; i++)
                _layers[i] = new Layer(_layersInfo[i], _layersInfo[i + 1], activations[i]);
        }

        //Pre compile nn for predictions
        public CompiledNeuralNetwork Compile() 
        {
            List<CompiledNeuralNetwork.CompiledLayer> compiledLayers = new();
            foreach (var layer in _layers)
            {
                var func = layer.Compile();
                compiledLayers.Add(new(func));
            }

            return new(compiledLayers.ToArray(), InputSize, OutputSize);
        }


        NeuralNetwork CreateChild()
        {
            NeuralNetwork nn = new(_layers);
            foreach (var l in nn._layers)
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

            foreach (var layer in _layers)
            {
                arr = layer.FeedForward(arr);
            }

            return arr;
        }

        public async Task<float> TrainAsync(int maxIteration = 1_000, GameSettings? settings = null)
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

            await NNTrainer.TrainAsync(g, maxIteration: maxIteration);
            //await g.StartGameAsync(NNTrainer, colorAsync: false);
            return Fitness;
        }

        public Task<bool> PlayAsync(int maxIteration = 1_000, GameSettings? settings = null, CancellationToken cancellationToken = default)
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

            return NNPlayer.GameAsync(g);
            //return g.StartGameAsync(NNPlayer, colorAsync: true, cancellationToken: cancellationToken);
        }

        public override string ToString()
        {
            return $"F: {Fitness}, L: " + string.Join(" -> ", _layersInfo);
        }

        public override bool Equals(object? obj) => Equals(obj as NeuralNetwork);
        public bool Equals(NeuralNetwork? other)
        {
            if (other == null)
                return false;
            if (InputSize != other.InputSize || OutputSize != other.OutputSize || _layers.Length != other._layers.Length)
                return false;
            for (int i = 0; i < _layers.Length; i++)
                if (_layers[i].Equals(other._layers[i]) == false)
                    return false;
            return true;
        }
        public override int GetHashCode() => HashCode.Combine(InputSize, OutputSize, _layers.Length);

        public int CompareTo(NeuralNetwork? other)
        {
            if (other == null || other.Fitness < Fitness)
                return 1;
            if (Fitness < other.Fitness)
                return -1;

            return 0;
        }

        public string Save(string filename, bool writeIndented = false)
        {
            var opt = GetSerializerOptions(writeIndented);
            var json = JsonSerializer.Serialize(this, opt);

            System.IO.File.WriteAllText(filename, json);
            return json;
        }
        public static NeuralNetwork? Load(string filename, bool writeIndented = false)
        {
            var opt = GetSerializerOptions(writeIndented);
            var json = System.IO.File.ReadAllText(filename);
            var ai = JsonSerializer.Deserialize<NeuralNetwork>(json, opt);

            return ai;
        }
        static JsonSerializerOptions GetSerializerOptions(bool writeIndented)
        {
            var opt = new JsonSerializerOptions()
            {
                WriteIndented = writeIndented
            };
            opt.Converters.Add(new JsonConverters.NeuralNetworkConverter());
            opt.Converters.Add(new JsonConverters.LayerConverter());
            return opt;
        }

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

            public void Train(Game g, int maxIteration = 1_000)
            {
                _currentCount = 0;
                _currentMaxIteration = maxIteration;
                g.StartGame(this);
            }

            public async Task TrainAsync(Game g, int maxIteration = 1_000)
            {
                _currentCount = 0;
                _currentMaxIteration = maxIteration;
                await g.StartGameAsync(this, colorAsync: false, cancellationToken: default);
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

        private class Player : IAsyncStrategy
        {
            readonly WeakReference<NeuralNetwork> _parent;
            NeuralNetwork Parent => _parent.TryGetTarget(out var parent) ? parent : throw new NullReferenceException($"{nameof(Parent)} has been collected by GC");

            public Player(NeuralNetwork parent)
            {
                _parent = new(parent);
            }

            public async Task<bool> GameAsync(Game g)
            {
                return await g.StartGameAsync(this);
            }

            Task<Brush?> IAsyncStrategy.PlayAsync(GameState state, CancellationToken cancellationToken)
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

                return Task.FromResult<Brush?>(b);
            }
        }
    }
}
