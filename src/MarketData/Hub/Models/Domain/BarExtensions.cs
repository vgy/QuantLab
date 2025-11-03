using Google.Protobuf.Collections;

namespace QuantLab.MarketData.Hub.Models.Domain;

using System.Runtime.CompilerServices;
using QuantLab.MarketData.Hub.Models.DTO.Grpc;
using BarGrpc = QuantLab.Protos.MarketData.Bar;

public static class BarExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BarGrpc ToProto(this in Bar b)
    {
        var pb = new BarGrpc
        {
            Symbol = b.Symbol,
            Interval = b.Interval.ToString(),
            Timestamp = b.Timestamp,
            Open = DecimalValue.FromDecimal(b.Open).ToProto(),
            High = DecimalValue.FromDecimal(b.High).ToProto(),
            Low = DecimalValue.FromDecimal(b.Low).ToProto(),
            Close = DecimalValue.FromDecimal(b.Close).ToProto(),
            Volume = b.Volume,
        };
        return pb;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddRangeFast(this RepeatedField<BarGrpc> target, ReadOnlySpan<Bar> source)
    {
        target.Capacity = source.Length;
        foreach (ref readonly var b in source)
            target.Add(b.ToProto());
    }
}
