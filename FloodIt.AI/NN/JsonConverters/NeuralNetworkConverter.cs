using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FloodIt.AI.NN.JsonConverters
{
    public class NeuralNetworkConverter : JsonConverter<NeuralNetwork>
    {
        public override NeuralNetwork? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (options.GetConverter(typeof(DenseLayer)) is not JsonConverter<DenseLayer> layersConverter)
                throw new JsonException($"Impossible to find a converter for the type {nameof(DenseLayer)}");

            List<DenseLayer> layers = new();

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var name = reader.GetString();
                    if (name == nameof(NeuralNetwork.Layers))
                    {
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        {
                            var layer = layersConverter.Read(ref reader, typeof(DenseLayer), options);
                            _ = layer ?? throw new NullReferenceException();
                            layers.Add(layer);
                        }
                    }
                }
            }

            return new(layers.ToArray());
        }

        public override void Write(Utf8JsonWriter writer, NeuralNetwork value, JsonSerializerOptions options)
        {
            if (options.GetConverter(typeof(DenseLayer)) is not JsonConverter<DenseLayer> layersConverter)
                throw new JsonException($"Impossible to find a converter for the type {nameof(DenseLayer)}");

            writer.WriteStartObject();
            writer.WriteStartArray(nameof(NeuralNetwork.Layers));
            foreach (var layer in value.Layers)
            {
                layersConverter.Write(writer, layer, options);
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }
    }
}
