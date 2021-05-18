using System;
using System.IO;
using Mapping_Tools.Classes.MathUtil;
using Newtonsoft.Json;

namespace Mapping_Tools.Classes.JsonConverters {
    public class Vector2Converter : JsonConverter {
        public override bool CanConvert(Type objectType) {
            return objectType == typeof(Vector2) || objectType == typeof(Vector2?);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            var v = (Vector2)value;

            writer.WriteStartObject();
            writer.WritePropertyName("X");
            serializer.Serialize(writer, v.X);
            writer.WritePropertyName("Y");
            serializer.Serialize(writer, v.Y);
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            var x = default(double);
            var y = default(double);
            var gotX = false;
            var gotY = false;
            while (reader.Read()) {
                if (reader.TokenType != JsonToken.PropertyName)
                    break;

                var propertyName = (string)reader.Value;
                if (!reader.Read())
                    continue;

                switch (propertyName) {
                    case "X":
                        x = serializer.Deserialize<double>(reader);
                        gotX = true;
                        break;
                    case "Y":
                        y = serializer.Deserialize<double>(reader);
                        gotY = true;
                        break;
                }
            }

            if (!(gotX && gotY)) {
                throw new InvalidDataException("A Vector2 must contain X and Y properties.");
            }

            return new Vector2(x, y);
        }
    }
}