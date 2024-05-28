using n_ate.Essentials;
using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace n_ate.Gremlin.Serialization
{
    /// <summary>
    /// Deserializes irregular Gremlin response JSON.
    /// </summary>
    public class GremlinEdgeAndVertexConverter : JsonConverterFactory
    {
        public GremlinEdgeAndVertexConverter() : base()
        {
        }

        public override bool CanConvert(Type typeToConvert)
        {
            var canConvert = typeToConvert.IsPrimitive || typeToConvert.IsEnum || typeToConvert.IsValueType || typeToConvert == typeof(string);
            return canConvert;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var innerType = typeToConvert.GetInnerType();
            var method = this.GetType().GetMethod(nameof(GetGremlinEdgeAndVertexConverter), BindingFlags.Instance | BindingFlags.NonPublic)!;
            var genericMethod = method.MakeGenericMethod(typeToConvert);
            var converter = (JsonConverter)genericMethod.Invoke(this, null)!;
            return converter;
        }

        private GremlinEdgeAndVertexConverter<T> GetGremlinEdgeAndVertexConverter<T>()
        {
            return new GremlinEdgeAndVertexConverter<T>(this);
        }
    }

    internal class GremlinEdgeAndVertexConverter<T> : JsonConverter<T>
    {
        internal GremlinEdgeAndVertexConverter(GremlinEdgeAndVertexConverter factory) : base()
        {
            this.ConverterFactory = factory;
        }

        public GremlinEdgeAndVertexConverter ConverterFactory { get; }

        public override bool CanConvert(Type typeToConvert)
        {
            var canConvert = typeToConvert == typeof(T);
            return canConvert;
        }

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            T? result;
            var updatedOptions = new JsonSerializerOptions(options);
            updatedOptions.Converters.Remove(this);
            updatedOptions.Converters.Remove(this.ConverterFactory);
            if (reader.TokenType == JsonTokenType.StartArray) //long-form gremlin property
            {
                var properties = JsonSerializer.Deserialize<GremlinExtendedProperty<T>[]>(ref reader, updatedOptions);
                var property = properties?.FirstOrDefault();
                if (property is null) throw new NotImplementedException();
                else result = property.Value;
            }
            else
            {
                result = JsonSerializer.Deserialize<T>(ref reader, updatedOptions)!;
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value?.GetType() ?? typeof(T), options);
        }
    }
}