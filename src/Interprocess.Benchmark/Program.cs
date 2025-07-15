using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace Cloudtoid.Interprocess.Benchmark;

public sealed class Program
{
    public static void Main()
    {
        var config = DefaultConfig.Instance.AddJob(Job.InProcess);
        _ = BenchmarkRunner.Run(typeof(Program).Assembly, config);
    }
}