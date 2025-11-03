namespace QuantLab.MarketData.Hub.Models.DTO.Grpc;

using System.Runtime.CompilerServices;
using DecimalValueGrPc = QuantLab.Protos.MarketData.DecimalValue;

public static class DecimalValueExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DecimalValueGrPc ToProto(this DecimalValue value) =>
        new() { Units = value.Units, Scale = value.Scale };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DecimalValue ToDomain(this DecimalValueGrPc value) =>
        new(value.Units, value.Scale);
}
