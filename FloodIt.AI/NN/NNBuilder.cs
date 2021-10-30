using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodIt.AI.NN
{
    public interface INNBuilderInput
    {
        INNBuilder Input(int inputSize);
    }
    public interface INNBuilder
    {
        INNBuilder Dense(int layerSize, Activations activation);

        NeuralNetwork Build();
    }

    public class NNBuilder : INNBuilder, INNBuilderInput
    {
        readonly List<int> _layers;
        readonly List<Activation> _activations;

        int[]? _layersArray;
        Activation[]? _activationsArray;

        public int[] LayersInfo => _layersArray ?? _layers.ToArray();
        
        public NNBuilder()
        {
            _layers = new();
            _activations = new();
        }

        public INNBuilder Input(int inputSize)
        {
            if (_layers.Count > 0)
                throw new InvalidOperationException("An input size has already been set.");
            _layers.Add(inputSize);
            return this;
        }

        public INNBuilder Dense(int layerSize, Activations activation)
        {
            if (_layers.Count < 1)
                throw new InvalidOperationException("No input layer has been set yet.");
            _layers.Add(layerSize);
            _activations.Add(activation);
            return this;
        }

        public NeuralNetwork Build()
        {
            _layersArray = _layers.ToArray();
            _activationsArray = _activations.ToArray();

            return new(_layersArray, _activationsArray);
        }
    }
}