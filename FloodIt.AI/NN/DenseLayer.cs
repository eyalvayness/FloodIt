using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json.Serialization;

namespace FloodIt.AI.NN
{
    public class DenseLayer : IEquatable<DenseLayer>
    {
        readonly static Random _rand;

        readonly float[] _biaises;
        readonly float[,] _weights;
        readonly Activation _activation;

        public Activations Activation => (Activations)_activation;
        public int InputSize { get; }
        public int OutputSize { get; }

        static DenseLayer() => _rand = new();
        internal DenseLayer(int inputOutputSize, Activation activation) : this(inputOutputSize, inputOutputSize, activation) { }
        internal DenseLayer(int inputSize, int outputSize, Activation activation)
        {
            InputSize = inputSize;
            OutputSize = outputSize;
            _weights = new float[outputSize, inputSize];
            _biaises = new float[outputSize];
            _activation = activation;

            InitValues();
        }

        [JsonConstructor] internal DenseLayer(float[,] weights, float[] biaises, Activation activation)
        {
            InputSize = weights.GetLength(1);
            OutputSize = weights.GetLength(0);
            _activation = activation;

            _weights = weights;
            _biaises = biaises;
        }

        static float GetRandom() => GetRandom(-1f, 1f);// GetRandom(-.5f, .5f);
        static float GetRandom(float min, float max) => (float)(_rand.NextDouble() * (max - min) + min);
        static readonly float sigma = 2;
        static float GetNormalDistribution()
        {
            var p = 1 / (sigma * MathF.Sqrt(2 * MathF.PI));
            var x = GetRandom();
            return (p - MathF.Exp(-x * x / (2 * sigma)) * p) * MathF.Sign(x);
        }

        void InitValues()
        {
            for (int i = 0; i < OutputSize; i++)
            {
                _biaises[i] = GetRandom();
                for (int j = 0; j < InputSize; j++)
                {
                    _weights[i, j] = GetRandom();
                }
            }
        }

        public float GetBiaiseAt(int index) => _biaises[index];
        public float GetWeightAt(int outputIndex, int inputIndex) => _weights[outputIndex, inputIndex];

        public float[] FeedForward(float[] xs)
        {
            if (xs.Length != InputSize)
                throw new ArgumentOutOfRangeException(nameof(xs), $"The input array must be of length {InputSize}.");

            float[] ys = new float[OutputSize];

            for (int i = 0; i < OutputSize; i++)
            {
                ys[i] = _biaises[i] + xs.Select((v, j) => v * _weights[i, j]).Sum();
            }
            ys = _activation.Activate(ys);
            return ys;
        }

        internal void Evolution()
        {
            for (int i = 0; i < OutputSize; i++)
            {
                for (int j = 0; j < InputSize; j++)
                {
                    var v = GetNormalDistribution();
                    _weights[i, j] += v;
                }
            }

            for (int k = 0; k < OutputSize; k++)
            {
                _biaises[k] += GetNormalDistribution();
            }
        }

        public override bool Equals(object? obj) => Equals(obj as DenseLayer);
        public bool Equals(DenseLayer? other)
        {
            if (other == null)
                return false;
            if (Activation != other.Activation || InputSize != other.InputSize || OutputSize != other.OutputSize)
                return false;

            for (int i = 0; i < OutputSize; i++)
            {
                if (_biaises[i] != other._biaises[i])
                    return false;

                for (int j = 0; j < InputSize; j++)
                {
                    if (_weights[i, j] != other._weights[i, j])
                        return false;
                }
            }

            return true;
        }

        public override int GetHashCode() => HashCode.Combine(Activation, InputSize, OutputSize);

        public override string ToString()
        {
            StringBuilder sb = new();


            for (int i = 0; i < OutputSize; i++)
            {
                sb.Append(_biaises[i]);

                for (int j = 0; j < InputSize; j++)
                {
                    sb.Append(';');
                    sb.Append(_weights[i, j]);
                }
                sb.Append('$');
            }
            sb.Append(Activation);

            return sb.ToString();
        }

        public CompiledDenseLayer Compile()
        {
            var s = ToString();
            return CompiledDenseLayer.FromFormattedString(s);
        }

        internal DenseLayer Clone() => new((float[,])_weights.Clone(), (float[])_biaises.Clone(), Activation);
    }
}
