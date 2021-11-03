using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FloodIt.AI.NN.JsonConverters
{
    public class CompiledNeuralNetworkConverter : JsonConverter<CompiledNeuralNetwork>
    {
        public override CompiledNeuralNetwork? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            int inputSize = -1, outputSize = -1;
            List<CompiledDenseLayer> layers = new();

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var name = reader.GetString();
                    reader.Read();
                    if (name == nameof(CompiledNeuralNetwork.InputSize))
                        inputSize = reader.GetInt32();
                    else if (name == nameof(CompiledNeuralNetwork.OutputSize))
                        outputSize = reader.GetInt32();
                    else if (name == nameof(CompiledNeuralNetwork.CompiledLayers))
                    {
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        {
                            var s = reader.GetString()!;
                            CompiledDenseLayer layer = CompiledDenseLayer.FromFormattedString(s);
                            layers.Add(layer);
                        }
                    }
                }
            }

            return new(layers.ToArray(), inputSize, outputSize);
        }

        public override void Write(Utf8JsonWriter writer, CompiledNeuralNetwork value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteNumber(nameof(CompiledNeuralNetwork.InputSize), value.InputSize);
            writer.WriteNumber(nameof(CompiledNeuralNetwork.OutputSize), value.OutputSize);

            writer.WriteStartArray(nameof(CompiledNeuralNetwork.CompiledLayers));
            foreach (var layer in value.CompiledLayers)
            {
                writer.WriteStringValue(layer.ToString());
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }
    }
}
