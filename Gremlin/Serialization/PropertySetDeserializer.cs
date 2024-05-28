using n_ate.Essentials.Serialization;
using ExRam.Gremlinq.Core;
using ExRam.Gremlinq.Core.Deserialization;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace n_ate.Gremlin.Serialization
{
    public class PropertySetDeserializer : IGremlinQueryExecutionResultDeserializer
    {
        public static Regex GremlinTypeRegex = new Regex("^\\s*\\[\\s*\\{([^\\\"]|\\\"type\\\"\\s*\\:\\s*\\\"(?<type>[^\\\"]*)|\\\")*");

        private readonly JsonSerializerOptions _options;

        private readonly IGremlinQueryExecutionResultDeserializer Default;

        public PropertySetDeserializer(IGremlinQueryExecutionResultDeserializer defaultDeserializer, string propertySetName, bool caseInsensitive = true)
        {
            this.Default = defaultDeserializer;
            this.PropertySetName = propertySetName;
            this._options = new JsonSerializerOptions();
            _options.PropertyNameCaseInsensitive = caseInsensitive;
            _options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            _options.Converters.Add(new ValueTupleItemsConverter());
            //_options.Converters.Add(new GremlinEdgeAndVertexConverter());
            _options.Converters.Add(new PropertySetConverter(PropertySetName));
        }

        public string PropertySetName { get; }

        /// <summary></summary>
        public IGremlinQueryExecutionResultDeserializer ConfigureFragmentDeserializer(Func<IGremlinQueryFragmentDeserializer, IGremlinQueryFragmentDeserializer> transformation)
        {
            Default.ConfigureFragmentDeserializer(transformation);
            return this;
        }

        public async IAsyncEnumerable<TElement> Deserialize<TElement>(object executionResult, IGremlinQueryEnvironment environment)
        {
            if (executionResult is Newtonsoft.Json.Linq.JArray jArray)
            {
                foreach (var jToken in jArray)
                {
                    var flattenedToken = FlattenGremlinPropertiesProperty(jToken, _options.PropertyNameCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
                    var json = flattenedToken.ToString(); //get the raw json
                    if (typeof(TElement) == typeof(string)) yield return (TElement)(object)json; //return the results as they become available..
                    else yield return JsonSerializer.Deserialize<TElement>(json, _options)!; //return the results as they become available..
                }
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Crawl the token structure to find each object that has a properties property and then promotes all properties members to the object level. Retain the properties property.
        /// </summary>
        private JToken FlattenGremlinPropertiesProperty(JToken token, StringComparison propertyNameComparison)
        {
            if (token is JObject obj)
            {
                var properties = obj.Properties();
                var propertiesValue = properties.FirstOrDefault(p => p.Name.Equals("properties", propertyNameComparison) && p.Value is JObject)?.Value;
                if (propertiesValue is null)
                {
                    foreach (var property in properties)
                    {
                        if (property.Value is JArray array)
                        {
                            foreach (var item in array)
                            {
                                FlattenGremlinPropertiesProperty(item, propertyNameComparison);
                            }
                        }
                        else FlattenGremlinPropertiesProperty(property.Value, propertyNameComparison);
                    }
                }
                else
                {
                    var flattenedProperties = GetPropertiesDictionary((propertiesValue as JObject)!, propertyNameComparison);
                    foreach (var propertyValue in flattenedProperties)
                    {
                        if (!properties.Any(p => p.Name.Equals(propertyValue.Key, propertyNameComparison))) //No property name conflict..
                        {
                            obj.Add(propertyValue.Key, propertyValue.Value);
                        }
                    }
                }
            }
            return token;
        }

        private Dictionary<string, JToken> GetPropertiesDictionary(JObject obj, StringComparison propertyNameComparison)
        {
            var result = new Dictionary<string, JToken>();
            foreach (var property in obj.Properties())
            {
                var value = property.Value;
                if (property.Value is JArray array)
                {
                    if (array.Count > 0)
                    {
                        if (array.First() is JObject idValuePair)
                        {
                            var idProperty = idValuePair.Property("id", propertyNameComparison);
                            var valueProperty = idValuePair.Property("value", propertyNameComparison);
                            if (idProperty is not null && valueProperty is not null)
                            {
                                value = valueProperty.Value;
                            }
                        }
                    }
                }
                result.Add(property.Name, value);
            }
            return result;
        }
    }
}