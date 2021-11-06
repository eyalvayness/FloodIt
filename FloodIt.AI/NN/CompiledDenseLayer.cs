using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace FloodIt.AI.NN
{
    public class CompiledDenseLayer : IEquatable<CompiledDenseLayer?>
    {
        readonly Func<float[], float[]> _layer;
        readonly string _formattedLayerInfo;
        public CompiledDenseLayer(Func<float[], float[]> layer, string formattedLayerInfo)
        {
            _formattedLayerInfo = formattedLayerInfo;
            _layer = layer;
        }

        public float[] Compute(float[] xs) => _layer(xs);

        public static CompiledDenseLayer FromFormattedString(string formattedString)
        {
            var lines = formattedString.Split('$');
            int outputSize = lines.Length - 1;

            var param = Expression.Parameter(typeof(float[]), "xs");
            var paramsList = new List<ParameterExpression>() { param };

            var ys = Expression.Variable(typeof(float[]), "ys");
            var activation = Expression.Variable(typeof(Activation), "activation");
            var varsList = new List<ParameterExpression>() { ys, activation };

            var newArray = Expression.NewArrayBounds(typeof(float), Expression.Constant(outputSize));

            var activationAssign = Expression.Assign(activation, Expression.Convert(Expression.Constant(Enum.Parse<Activations>(lines[^1]), typeof(Activations)), typeof(Activation)));
            var ysAssign = Expression.Assign(ys, newArray);
            List<Expression> exprs = new() { ysAssign, activationAssign };

            for (int i = 0; i < outputSize; i++)
            {
                var columns = lines[i].Split(';').Select(s => float.Parse(s)).ToArray();
                int inputSize = columns.Length - 1;
                Expression expr = Expression.Constant(columns[0], typeof(float));

                for (int j = 0; j < inputSize; j++)
                {
                    var weight = Expression.Constant(columns[j + 1], typeof(float));
                    var x = Expression.ArrayAccess(param, Expression.Constant(j, typeof(int)));

                    var right = Expression.Multiply(weight, x);
                    expr = Expression.Add(expr, right);
                }

                var left = Expression.ArrayAccess(ys, Expression.Constant(i));
                var line = Expression.Assign(left, expr);
                exprs.Add(line);
            }

            var activatedExpr = Expression.Assign(ys, Expression.Call(activation, typeof(Activation).GetMethod("Activate")!, new List<ParameterExpression>() { ys }));
            exprs.Add(activatedExpr);

            var returnLabel = Expression.Label(typeof(float[]), "returnYs");
            var returnExpr = Expression.Return(returnLabel, ys, typeof(float[]));
            var returnTarget = Expression.Label(returnLabel, Expression.Constant(Array.Empty<float>(), typeof(float[])));
            exprs.Add(returnExpr);
            exprs.Add(returnTarget);

            var block = Expression.Block(typeof(float[]), varsList, exprs);
            var lambda = Expression.Lambda<Func<float[], float[]>>(block, paramsList);

            return new(lambda.Compile(), formattedString);
        }

        public override bool Equals(object? obj) => Equals(obj as CompiledDenseLayer);
        public bool Equals(CompiledDenseLayer? other) => other != null && _formattedLayerInfo == other._formattedLayerInfo;
        public override int GetHashCode() => HashCode.Combine(_formattedLayerInfo);
        public override string ToString() => _formattedLayerInfo;
    }
}