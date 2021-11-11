using FloodIt.Core;
using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FloodIt.AI.NN
{
    public partial class CompiledNeuralNetwork : IEquatable<CompiledNeuralNetwork?>
    {
        readonly CompiledDenseLayer[] _compiledLayers;

        public int InputSize { get; }
        public int OutputSize { get; }
        public ReadOnlyCollection<CompiledDenseLayer> CompiledLayers => new(_compiledLayers);
        Player NNPlayer { get; }

        public CompiledNeuralNetwork(CompiledDenseLayer[] compiledLayers, int inputSize, int outputSize)
        {
            _compiledLayers = compiledLayers;
            InputSize = inputSize;
            OutputSize = outputSize;
            NNPlayer = new(this);
        }

        public Task<int> PlayAsync(GameSettings? settings = null, int maxIteration = 1_000, CancellationToken cancellationToken = default)
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

            return NNPlayer.PlayAsync(g, null, maxIteration, cancellationToken);
        }

        public Task<int> PlayAsync(Game g, int maxIteration = 1_000, CancellationToken cancellationToken = default)
        {
            if (g.Settings.Count != InputSize)
                throw new InvalidOperationException($"The board size ({g.Settings.Count}) doesn't match neural network input size ({InputSize}).");
            if (g.Settings.UsedBrushes.Length != OutputSize + 1)
                throw new InvalidOperationException($"The neural network output size ({OutputSize}) must be one less than the total number of brushes ({g.Settings.UsedBrushes.Length}).");

            return NNPlayer.PlayAsync(g, null, maxIteration, cancellationToken);
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

        public string Save(string filename, bool writeIndented = false)
        {
            var opt = GetSerializerOptions(writeIndented);
            var json = JsonSerializer.Serialize(this, opt);

            System.IO.File.WriteAllText(filename, json);
            return json;
        }
        public static CompiledNeuralNetwork? Load(string filename, bool writeIndented = false)
        {
            var opt = GetSerializerOptions(writeIndented);
            var json = System.IO.File.ReadAllText(filename);
            var ai = JsonSerializer.Deserialize<CompiledNeuralNetwork>(json, opt);

            return ai;
        }
        static JsonSerializerOptions GetSerializerOptions(bool writeIndented)
        {
            var opt = new JsonSerializerOptions()
            {
                WriteIndented = writeIndented
            };
            opt.Converters.Add(new JsonConverters.CompiledNeuralNetworkConverter());
            return opt;
        }

        public override bool Equals(object? obj) => Equals(obj as CompiledNeuralNetwork);
        public bool Equals(CompiledNeuralNetwork? other)
        {
            if (other == null)
                return false;
            if (InputSize != other.InputSize || OutputSize != other.OutputSize || _compiledLayers.Length != other._compiledLayers.Length)
                return false;
            for (int k = 0; k < _compiledLayers.Length; k++)
                if (_compiledLayers[k].Equals(other._compiledLayers[k]) == false)
                    return false;

            return true;
        }

        public override int GetHashCode() => HashCode.Combine(InputSize, OutputSize, _compiledLayers.Length);
    }
}