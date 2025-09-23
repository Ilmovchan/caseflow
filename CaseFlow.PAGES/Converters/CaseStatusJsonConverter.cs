using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using CaseFlow.DAL.Enums;
using NpgsqlTypes;

namespace CaseFlow.API.Converters;

public class CaseStatusJsonConverter : JsonConverter<CaseStatus>
{
    private static readonly Dictionary<string, CaseStatus> _fromText;
    private static readonly Dictionary<CaseStatus, string> _toText;

    static CaseStatusJsonConverter()
    {
        _fromText = new();
        _toText = new();

        foreach (var field in typeof(CaseStatus).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var enumValue = (CaseStatus)field.GetValue(null)!;
            var enumMember = field.GetCustomAttribute<EnumMemberAttribute>()?.Value;
            var pgName = field.GetCustomAttribute<PgNameAttribute>()?.PgName;
            var text = enumMember ?? pgName ?? field.Name;

            _fromText[text] = enumValue;
            _toText[enumValue] = text;
        }
    }

    public override CaseStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString();
        if (text != null && _fromText.TryGetValue(text, out var status))
            return status;

        throw new JsonException($"Unknown CaseStatus value: '{text}'");
    }

    public override void Write(Utf8JsonWriter writer, CaseStatus value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(_toText.TryGetValue(value, out var text) ? text : value.ToString());
    }
}