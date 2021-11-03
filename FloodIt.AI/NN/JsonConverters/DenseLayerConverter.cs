using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FloodIt.AI.NN.JsonConverters
{
    public class DenseLayerConverter : JsonConverter<DenseLayer>
    {
        public const string WeightsPropertyName = "Weights";
        public const string BiaisesPropertyName = "Biaises";

        public override DenseLayer Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            int inputSize = -1, outputSize = -1;
            Activations? activation = null;
            float[,]? weights = null;
            float[]? biaises = null;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var name = reader.GetString();
                    reader.Read();
                    if (name == nameof(DenseLayer.InputSize))
                        inputSize = reader.GetInt32();
                    else if (name == nameof(DenseLayer.OutputSize))
                        outputSize = reader.GetInt32();
                    else if (name == nameof(DenseLayer.Activation))
                        activation = Enum.Parse<Activations>(reader.GetString()!) as Activations?;
                    else if (name == WeightsPropertyName)
                    {
                        _ = inputSize == -1 ? throw new NullReferenceException() : true;
                        _ = outputSize == -1 ? throw new NullReferenceException() : true;
                        weights = new float[outputSize, inputSize];
                        int i = 0, j = 0;
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray && i < outputSize)
                        {
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray && j < inputSize)
                            {
                                weights[i, j] = reader.GetSingle();
                                j++;
                            }
                            j = 0;
                            i++;
                        }
                    }
                    else if (name == BiaisesPropertyName)
                    {
                        _ = outputSize == -1 ? throw new NullReferenceException() : true;
                        biaises = new float[outputSize];
                        int i = 0;
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray && i < outputSize)
                        {
                            biaises[i] = reader.GetSingle();
                            i++;
                        }
                    }
                }
            }

            _ = activation ?? throw new NullReferenceException();
            _ = weights ?? throw new NullReferenceException();
            _ = biaises ?? throw new NullReferenceException();
            return new(weights, biaises, activation);
        }

        public override void Write(Utf8JsonWriter writer, DenseLayer value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteNumber(nameof(DenseLayer.InputSize), value.InputSize);
            writer.WriteNumber(nameof(DenseLayer.OutputSize), value.OutputSize);
            writer.WriteString(nameof(DenseLayer.Activation), value.Activation.ToString());

            writer.WriteStartArray(WeightsPropertyName);
            for (int i = 0; i < value.OutputSize; i++)
            {
                writer.WriteStartArray();
                for (int j = 0; j < value.InputSize; j++)
                {
                    writer.WriteNumberValue(value.GetWeightAt(i, j));
                }
                writer.WriteEndArray();
            }
            writer.WriteEndArray();

            writer.WriteStartArray(BiaisesPropertyName);
            for (int i = 0; i < value.OutputSize; i++)
            {
                writer.WriteNumberValue(value.GetBiaiseAt(i));
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }
    }
}
