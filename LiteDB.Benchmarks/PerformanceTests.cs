using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace LiteDB.Benchmarks
{
    [MemoryDiagnoser]
    public class PerformanceTests
    {
        private LiteEngine _engine;
        private TempFile _file;

        [GlobalSetup]
        public void Setup()
        {
            _file = new TempFile();
            _engine = new LiteEngine(_file.Filename);
            _engine.InsertBulk("col", GetDocs(1, 10));
        }

        [GlobalCleanup]
        public void CleanUp()
        {
            _engine.Dispose();
            _file.Dispose();
        }

        [Benchmark(OperationsPerInvoke = 100)]
        public void Bulk_Insert_Engine()
        {
            using (var file = new TempFile())
            using (var db = new LiteEngine(file.Filename))
            {
                db.InsertBulk("col", GetDocs(1, 60000));
            }
        }

        [Benchmark(OperationsPerInvoke = 100)]
        public IEnumerable<BsonValue> Read_Engine()
        {
            return _engine.FindAll("col");
        }

        private IEnumerable<BsonDocument> GetDocs(int initial, int count, int type = 1)
        {
            for (var i = initial; i < initial + count; i++)
            {
                yield return new BsonDocument
                {
                    {"_id", i},
                    {"name", Guid.NewGuid().ToString()},
                    {"first", "John"},
                    {"lorem", TempFile.LoremIpsum(3, 5, 2, 3, 3)}
                };
            }
        }
    }
}