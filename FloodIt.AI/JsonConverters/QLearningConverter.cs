using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Text.Json;

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
                }
            }

            _ = alpha ?? throw new NullReferenceException();
            _ = gamma ?? throw new NullReferenceException();
            _ = settings ?? throw new NullReferenceException();
            return new QLearning(alpha.Value, gamma.Value, settings);
        }

        public override void Write(Utf8JsonWriter writer, QLearning value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            if (options.GetConverter(typeof(Core.GameSettings)) is not JsonConverter<Core.GameSettings> settingsConverter)
                throw new JsonException($"Impossible to find a converter for {nameof(Core.GameSettings)}");

            writer.WriteNumber(nameof(value.Alpha), value.Alpha);
            writer.WriteNumber(nameof(value.Gamma), value.Gamma);

            writer.WriteStartObject(nameof(value.Settings));
            settingsConverter.Write(writer, value.Settings, options);
            writer.WriteEndObject();

            writer.WriteEndObject();
        }
    }
}
