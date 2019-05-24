using System;
using BenchmarkDotNet.Running;

namespace LiteDB.Benchmarks
{
    class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<PerformanceTests>();
        }
    }
}