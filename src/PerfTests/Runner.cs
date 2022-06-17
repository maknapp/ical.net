using BenchmarkDotNet.Running;

namespace PerfTests
{
    public class Runner
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<ApplicationWorkflows>();
            BenchmarkRunner.Run<OccurencePerfTests>();
            BenchmarkRunner.Run<CalDateTimePerfTests>();
            BenchmarkRunner.Run<SerializationPerfTests>();
            BenchmarkRunner.Run<ThroughputTests>();
        }
    }
}
