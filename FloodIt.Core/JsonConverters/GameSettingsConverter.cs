using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Windows.Media;

namespace FloodIt.Core.JsonConverters
{
    public class GameSettingsConverter : JsonConverter<GameSettings>
    {
        public override GameSettings? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            int? size = null;
            bool? preventSameBrush = null;
            List<Brush> usedBrushes = new();

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var name = reader.GetString();

                    reader.Read();
                    if (name == nameof(GameSettings.Size))
                        size = reader.GetInt32();
                    else if (name == nameof(GameSettings.PreventSameBrush))
                        preventSameBrush = reader.GetBoolean();
                    else if (name == nameof(GameSettings.UsedBrushes))
                    {
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        {
                            var c = reader.GetString();
                            var color = (Color)ColorConverter.ConvertFromString(c);
                            usedBrushes.Add(new SolidColorBrush(color));
                        }
                    }
                }
            }

            _ = size ?? throw new NullReferenceException();
            _ = preventSameBrush ?? throw new NullReferenceException();
            _ = usedBrushes.Count == 0 ? throw new NullReferenceException() : true;

            return new GameSettings()
            {
                Size = size.Value,
                PreventSameBrush = preventSameBrush.Value,
                UsedBrushes = usedBrushes.ToArray()
            };
        }

        public override void Write(Utf8JsonWriter writer, GameSettings value, JsonSerializerOptions options)
        {
            writer.WriteNumber(nameof(GameSettings.Size), value.Size);
            writer.WriteBoolean(nameof(GameSettings.PreventSameBrush), value.PreventSameBrush);

            writer.WriteStartArray(nameof(GameSettings.UsedBrushes));
            foreach (var brush in value.UsedBrushes)
            {
                writer.WriteStringValue(brush.ToString());
            }
            writer.WriteEndArray();
        }
    }
}
