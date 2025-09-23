using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using CaseFlow.DAL.Enums;
using NpgsqlTypes;

namespace CaseFlow.API.Converters;

public class DetectiveStatusJsonConverter : JsonConverter<DetectiveStatus>
{
    private static readonly Dictionary<string, DetectiveStatus> _fromText;
    private static readonly Dictionary<DetectiveStatus, string> _toText;

    static DetectiveStatusJsonConverter()
    {
        _fromText = new();
        _toText = new();

        foreach (var field in typeof(DetectiveStatus).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var enumValue = (DetectiveStatus)field.GetValue(null)!;

            var enumMember = field.GetCustomAttribute<EnumMemberAttribute>()?.Value;
            var pgName = field.GetCustomAttribute<PgNameAttribute>()?.PgName;

            var text = enumMember ?? pgName ?? field.Name;

            _fromText[text] = enumValue;
            _toText[enumValue] = text;
        }
    }

    public override DetectiveStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString();

        if (text != null && _fromText.TryGetValue(text, out var status))
        {
            return status;
        }

        throw new JsonException($"Unknown DetectiveStatus value: '{text}'");
    }

    public override void Write(Utf8JsonWriter writer, DetectiveStatus value, JsonSerializerOptions options)
    {
        if (_toText.TryGetValue(value, out var text))
        {
            writer.WriteStringValue(text);
        }
        else
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}