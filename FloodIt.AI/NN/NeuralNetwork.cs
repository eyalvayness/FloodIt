using FloodIt.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        readonly DenseLayer[] _layers;

        public ReadOnlyCollection<DenseLayer> Layers => new(_layers);
        Trainer NNTrainer { get; }

        private NeuralNetwork()
        {
            NNTrainer = new(this);
            _layersInfo = Array.Empty<int>();
            _layers = Array.Empty<DenseLayer>();
        }

        internal NeuralNetwork(int[] layers, Activation[] activations) : this()
        {
            if (layers.Length < 2)
                throw new ArgumentException($"The Neural Network must have at least 2 different layers (input and output).", nameof(layers));
            if (activations.Length != layers.Length - 1)
                throw new ArgumentException($"Not the good amount of activation functions (received: {activations.Length}, expecting: {layers.Length - 1}).", nameof(activations));

            _layersInfo = layers.ToArray();
            _layers = new DenseLayer[layers.Length - 1];

            InitLayers(activations);
        }

        [JsonConstructor] internal NeuralNetwork(DenseLayer[] layers) : this()
        {
            if (layers.Length < 1)
                throw new ArgumentException($"The Neural Network must have at least 2 different layers.", nameof(layers));

            _layersInfo = layers.Select(l => l.InputSize).Append(layers[^1].OutputSize).ToArray();
            //_layers = layers;
            _layers = new DenseLayer[layers.Length];
            for (int i = 0; i < _layers.Length; i++)
            {
                _layers[i] = layers[i].Clone();
            }
        }

        void InitLayers(Activation[] activations)
        {
            for (int i = 0; i < _layers.Length; i++)
                _layers[i] = new DenseLayer(_layersInfo[i], _layersInfo[i + 1], activations[i]);
        }

        //Pre compile nn for predictions
        public CompiledNeuralNetwork Compile() 
        {
            List<CompiledDenseLayer> compiledLayers = new();
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

        public async Task<(float fitness, int count)> TrainAsync(int maxIteration = 1_000, GameSettings? settings = null)
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

            var count = await NNTrainer.TrainAsync(g, maxIteration: maxIteration);
            //await g.StartGameAsync(NNTrainer, colorAsync: false);
            return (Fitness, count);
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
            opt.Converters.Add(new JsonConverters.DenseLayerConverter());
            return opt;
        }
    }
}
