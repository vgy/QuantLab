namespace QuantLab.MarketData.Hub.Models.Domain;

using System.Text.Json;
using System.Text.Json.Serialization;

public class BarIntervalJsonConverter : JsonConverter<BarInterval>
{
    public override BarInterval Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    ) => throw new NotImplementedException();

    public override void Write(
        Utf8JsonWriter writer,
        BarInterval value,
        JsonSerializerOptions options
    ) => writer.WriteStringValue(value.ToShortString());
}
