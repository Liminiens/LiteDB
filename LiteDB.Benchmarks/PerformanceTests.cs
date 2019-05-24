using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace LiteDB.Benchmarks
{
    [MemoryDiagnoser]
    public class PerformanceTests
    {
        [Benchmark(OperationsPerInvoke = 100)]
        public void Bulk_Insert_Engine()
        {
            using (var file = new TempFile())
            using (var db = new LiteEngine(file.Filename))
            {
                db.InsertBulk("col", GetDocs(1, 60000));
            }
        }

        private IEnumerable<BsonDocument> GetDocs(int initial, int count, int type = 1)
        {
            for (var i = initial; i < initial + count; i++)
            {
                yield return new BsonDocument
                {
                    { "_id", i },
                    { "name", Guid.NewGuid().ToString() },
                    { "first", "John" },
                    { "lorem", TempFile.LoremIpsum(3, 5, 2, 3, 3) }
                };
            }
        }
    }
}