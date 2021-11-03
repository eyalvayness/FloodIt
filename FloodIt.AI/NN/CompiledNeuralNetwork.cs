using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodIt.AI.NN
{
    public class CompiledNeuralNetwork
    {
        readonly CompiledLayer[] _compiledLayers;

        public int InputSize { get; }
        public int OutputSize { get; }
        public ReadOnlyCollection<CompiledLayer> CompiledLayers => new(_compiledLayers);

        public CompiledNeuralNetwork(CompiledLayer[] compiledLayers, int inputSize, int outputSize)
        {
            _compiledLayers = compiledLayers;
            InputSize = inputSize;
            OutputSize = outputSize;
        }

        public float[] Compute(float[] xs)
        {
            if (xs.Length != InputSize)
                throw new ArgumentOutOfRangeException(nameof(xs), $"The input array must be of length {InputSize}.");

            float[] ys = xs;
            foreach (var cl in _compiledLayers)
            {
                float[] temp = cl.Compute(ys);
                ys = temp;
            }

            return ys;
        }

        public class CompiledLayer
        {
            readonly Func<float[], float[]> _layer;
            public CompiledLayer(Func<float[], float[]> layer)
            {
                _layer = layer;
            }

            public float[] Compute(float[] xs) => _layer(xs);
        }
    }
}
