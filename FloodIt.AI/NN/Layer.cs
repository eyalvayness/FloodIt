using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json.Serialization;

namespace FloodIt.AI.NN
{
    public partial class NeuralNetwork
    {
        public class Layer : IEquatable<Layer>
        {
            readonly static Random _rand;

            readonly float[] _biaises;
            readonly float[,] _weights;
            readonly Activation _activation;

            public Activations Activation => (Activations)_activation;
            public int InputSize { get; }
            public int OutputSize { get; }

            static Layer() => _rand = new();
            internal Layer(int inputOutputSize, Activation activation) : this(inputOutputSize, inputOutputSize, activation) { }
            internal Layer(int inputSize, int outputSize, Activation activation)
            {
                InputSize = inputSize;
                OutputSize = outputSize;
                _weights = new float[outputSize, inputSize];
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
            static float GetNormalDistribution() => .4f - 1 / MathF.Sqrt(2 * MathF.PI) * MathF.Exp(-(1 / 2) * MathF.Pow(GetRandom(), 2));

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
                    //ys[i] = _biaises[i];
                    //for (int j = 0; j < InputSize; j++)
                    //{
                    //    ys[i] = ys[i] + xs[j] * _weights[j, i];
                    //}
                    //var val = _biaises[i] + xs.Select((v, j) => v * _weights[j, i]).Sum();
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
                        _weights[i, j] += GetNormalDistribution();
                    }
                }

                for (int k = 0; k < OutputSize; k++)
                {
                    _biaises[k] += GetNormalDistribution();
                }
            }

            public override bool Equals(object? obj) => Equals(obj as Layer);
            public bool Equals(Layer? other)
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
            public Func<float[], float[]> Compile()
            {
                var param = Expression.Parameter(typeof(float[]), "xs");
                var paramsList = new List<ParameterExpression>() { param };

                var ys = Expression.Variable(typeof(float[]), "ys");
                var activation = Expression.Variable(typeof(Activation), "activation");
                var varsList = new List<ParameterExpression>() { ys, activation };

                var newArray = Expression.NewArrayBounds(typeof(float), Expression.Constant(OutputSize));

                var activationAssign = Expression.Assign(activation, Expression.Convert(Expression.Constant(Activation, typeof(Activations)), typeof(Activation)));
                var ysAssign = Expression.Assign(ys, newArray);
                List<Expression> exprs = new() { ysAssign, activationAssign };

                for (int i = 0; i < OutputSize; i++)
                {
                    Expression expr = Expression.Constant(_biaises[i], typeof(float));


                    for (int j = 0; j < InputSize; j++)
                    {
                        var weight = Expression.Constant(_weights[i, j], typeof(float));
                        var x = Expression.ArrayAccess(param, Expression.Constant(j, typeof(int)));

                        var right = Expression.Multiply(weight, x);
                        expr = Expression.Add(expr, right);
                    }

                    var left = Expression.ArrayAccess(ys, Expression.Constant(i));
                    var line = Expression.Assign(left, expr);
                    exprs.Add(line);
                }

                //var activatedExpr = Expression.Assign(ys, Expression.Call(activation, typeof(NN.Activation).GetMethod("Activate")!, paramsList));
                //exprs.Add(activatedExpr);

                var returnLabel = Expression.Label(typeof(float[]), "returnYs");
                var returnExpr = Expression.Return(returnLabel, ys, typeof(float[]));
                var returnTarget = Expression.Label(returnLabel, Expression.Constant(Array.Empty<float>(), typeof(float[])));
                exprs.Add(returnExpr);
                exprs.Add(returnTarget);

                var block = Expression.Block(typeof(float[]), varsList, exprs);
                var lambda = Expression.Lambda<Func<float[], float[]>>(block, paramsList);
                return lambda.Compile();
            }

            internal Layer Clone() => new((float[,])_weights.Clone(), (float[])_biaises.Clone(), Activation);
        }
    }
}
