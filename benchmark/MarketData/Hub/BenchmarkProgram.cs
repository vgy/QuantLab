namespace QuantLab.MarketData.Hub.Benchmarks;

using BenchmarkDotNet.Running;

public class BenchmarkProgram
{
    public static void Main(string[] args)
    {
        // Run: dotnet run -c Release --project ./benchmark/MarketData/Hub/QuantLab.MarketData.Hub.Benchmarks.csproj
        var config = new BenchmarkDotNetConfig();
        BenchmarkSwitcher.FromAssembly(typeof(BenchmarkProgram).Assembly).Run(args, config);
    }
}
