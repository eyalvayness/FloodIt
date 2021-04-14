using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FloodIt.AI.NN
{
    public class NeuralNetwork : IComparable<NeuralNetwork>
    {
        public int InputSize => _layersInfo[0];
        public int OutputSize => _layersInfo[^1];
        public float Fitness { get; private set; }

        readonly int[] _layersInfo;

        public Layer[] Layers { get; }

        internal NeuralNetwork(int[] layers, Activation[] activations)
        {
            if (layers.Length < 2)
                throw new ArgumentException($"The Neural Network must have at least 2 different layers (input and output).", nameof(layers));
            if (activations.Length != layers.Length - 1)
                throw new ArgumentException($"Not the good amount of activation functions (received: {activations.Length}, expecting: {layers.Length - 1}).", nameof(activations));

            _layersInfo = layers.ToArray();
            Layers = new Layer[layers.Length - 1];

            InitLayers(activations);
        }

        [JsonConstructor] internal NeuralNetwork(Layer[] layers)
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

        internal float[] FeedForward(float[] input)
        {
            var arr = input;

            foreach (var layer in Layers)
            {
                arr = layer.FeedForward(arr);
            }

            return arr;
        }

        internal void AddFitness(float f) => Fitness += f;

        public int CompareTo(NeuralNetwork? other)
        {
            if (other == null || other.Fitness < Fitness)
                return 1;
            if (Fitness < other.Fitness)
                return -1;

            return 0;
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

            public void Evolution()
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
