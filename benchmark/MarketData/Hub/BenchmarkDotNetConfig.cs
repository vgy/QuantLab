namespace QuantLab.MarketData.Hub.Benchmarks;

using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;

public class BenchmarkDotNetConfig : ManualConfig
{
    public BenchmarkDotNetConfig()
    {
        // Set a custom folder for all benchmark artifacts
        ArtifactsPath = @"./";

        AddLogger(ConsoleLogger.Default);
        AddExporter(MarkdownExporter.Default);
        AddColumnProvider(DefaultColumnProviders.Instance);
        AddDiagnoser(MemoryDiagnoser.Default);
    }
}
