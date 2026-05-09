using System.Text.Json;
using System.Text.Json.Serialization;
using Axe2DEditor.Core.Stats;

namespace Axe2DEditor.Core.Entities;

public abstract class EntityDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public Dictionary<string, double> Stats { get; set; } = [];

    [JsonConverter(typeof(DelimitedStringListConverter))]
    public List<string> Tags { get; set; } = [];

    [JsonConverter(typeof(DelimitedStringListConverter))]
    public List<string> Traits { get; set; } = [];
}

public sealed class DelimitedStringListConverter : JsonConverter<List<string>>
{
    public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.StartArray => ReadArray(ref reader),
            JsonTokenType.String => SplitDelimited(reader.GetString()),
            JsonTokenType.Null => [],
            _ => throw new JsonException($"Unexpected token {reader.TokenType} when reading string list.")
        };
    }

    public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value.Where(v => !string.IsNullOrWhiteSpace(v)))
        {
            writer.WriteStringValue(item);
        }
        writer.WriteEndArray();
    }

    private static List<string> ReadArray(ref Utf8JsonReader reader)
    {
        var items = new List<string>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var value = reader.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    items.Add(value);
                }
            }
            else if (reader.TokenType == JsonTokenType.Null)
            {
                continue;
            }
            else
            {
                throw new JsonException($"Unexpected token {reader.TokenType} inside string list.");
            }
        }

        return items;
    }

    private static List<string> SplitDelimited(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value
            .Split([',', '\r', '\n', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
