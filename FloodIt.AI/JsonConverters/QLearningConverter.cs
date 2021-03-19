using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Windows.Media;

namespace FloodIt.AI.JsonConverters
{
    public class QLearningConverter : JsonConverter<QLearning>
    {
        public override QLearning? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (options.GetConverter(typeof(Core.GameSettings)) is not JsonConverter<Core.GameSettings> settingsConverter)
                throw new JsonException($"Impossible to find a converter for {nameof(Core.GameSettings)}");

            double? alpha = null, gamma = null;
            Core.GameSettings? settings = null;
            Dictionary<byte[], Dictionary<byte, double>>? q = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var name = reader.GetString();

                    reader.Read();
                    if (name == nameof(QLearning.Alpha))
                        alpha = reader.GetDouble();
                    else if (name == nameof(QLearning.Gamma))
                        gamma = reader.GetDouble();
                    else if (name == nameof(QLearning.Settings))
                        settings = settingsConverter.Read(ref reader, typeof(Core.GameSettings), options);
                    else if (name == nameof(QLearning.Q))
                    {
                        q = new(new Core.SimplifiedBoardEqualityComparer());

                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        {
                            if (reader.TokenType == JsonTokenType.PropertyName)
                            {
                                var s = reader.GetString() ?? throw new NullReferenceException();
                                var board = Convert.FromBase64String(s);

                                var dict = new Dictionary<byte, double>();
                                q.Add(board, dict);
                                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                                {
                                    if (reader.TokenType == JsonTokenType.PropertyName)
                                    {
                                        var b = reader.GetString() ?? throw new NullReferenceException();
                                        reader.Read();
                                        var d = reader.GetDouble();

                                        dict.Add(byte.Parse(b), d);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            _ = alpha ?? throw new NullReferenceException();
            _ = gamma ?? throw new NullReferenceException();
            _ = settings ?? throw new NullReferenceException();
            _ = q ?? throw new NullReferenceException();
            return new QLearning(alpha.Value, gamma.Value, settings, q);
        }

        public override void Write(Utf8JsonWriter writer, QLearning value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            if (options.GetConverter(typeof(Core.GameSettings)) is not JsonConverter<Core.GameSettings> settingsConverter)
                throw new JsonException($"Impossible to find a converter for {nameof(Core.GameSettings)}");

            writer.WriteNumber(nameof(QLearning.Alpha), value.Alpha);
            writer.WriteNumber(nameof(QLearning.Gamma), value.Gamma);

            writer.WriteStartObject(nameof(QLearning.Settings));
            settingsConverter.Write(writer, value.Settings, options);
            writer.WriteEndObject();

            writer.WriteStartArray(nameof(QLearning.Q));
            foreach (var kvp in value.Q)
            {
                writer.WriteStartObject();
                writer.WriteStartArray(Convert.ToBase64String(kvp.Key));
                foreach (var kvp2 in kvp.Value)
                {
                    writer.WriteStartObject();
                    writer.WriteNumber(kvp2.Key.ToString(), kvp2.Value);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }
    }
}
