﻿#region Using Directives

using System.Collections.Generic;
using System.Diagnostics;

using NBench;

using Pro.NBench.xUnit.XunitExtensions;
using Xunit;
using Xunit.Abstractions;
#endregion

//Important - disable test parallelization at assembly or collection level
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Pro.NBench.xUnit.Tests
{
    public class DictionaryMemoryTests
    {
        #region Constructors and Destructors

        public DictionaryMemoryTests(ITestOutputHelper output)
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new XunitTraceListener(output));
        }

            #endregion

        #region Constants

        private const int DictionaryEntrySize = 24;
        private const int MaxExpectedMemory = NumberOfAdds * DictionaryEntrySize;
        private const int NumberOfAdds = 1000000;
        #endregion

        #region Public Methods and Operators

        [PerfBenchmark(RunMode = RunMode.Iterations, TestMode = TestMode.Test, Description = "Dictionary without capacity, add memory test.")]
        [MemoryAssertion(MemoryMetric.TotalBytesAllocated, MustBe.LessThan, MaxExpectedMemory)]
        [NBenchFact]
        public void AddMemory_FailingTest()
        {
            var dictionary = new Dictionary<int, int>();

            Populate(dictionary, NumberOfAdds);
        }

        [NBenchFact]
        [PerfBenchmark(Description = "AddMemoryMeasurement", RunMode = RunMode.Iterations, TestMode = TestMode.Measurement)]
        [MemoryMeasurement(MemoryMetric.TotalBytesAllocated)]
        public void AddMemory_Measurement()
        {
            var dictionary = new Dictionary<int, int>();

            Populate(dictionary, NumberOfAdds);
        }

        [PerfBenchmark(Description = "AddMemoryMeasurement_Theory", RunMode = RunMode.Iterations, TestMode = TestMode.Measurement)]
        [MemoryMeasurement(MemoryMetric.TotalBytesAllocated)]
        [NBenchTheory]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        [InlineData(1000000)]
        public void AddMemory_Measurement_Theory(int numberOfAdds)
        {
            var dictionary = new Dictionary<int, int>(numberOfAdds);

            Populate(dictionary, numberOfAdds);
        }

        [PerfBenchmark(Description = nameof(AddMemory_PassingTest), RunMode = RunMode.Iterations, TestMode = TestMode.Test)]
        [MemoryAssertion(MemoryMetric.TotalBytesAllocated, MustBe.LessThan, MaxExpectedMemory * 2)]
        [NBenchFact]
        public void AddMemory_PassingTest()
        {
            var dictionary = new Dictionary<int, int>(NumberOfAdds);

            Populate(dictionary, NumberOfAdds);
        }

        [PerfBenchmark(Description = nameof(AddMemory_PassingTest), RunMode = RunMode.Iterations, TestMode = TestMode.Test)]
        [MemoryAssertion(MemoryMetric.TotalBytesAllocated, MustBe.LessThan, MaxExpectedMemory * 2)]
        [NBenchTheory]
        [InlineData(1000, "One Thousand")]
        [InlineData(10000, "Ten Thousand")]
        [InlineData(100000, "One Hundred Thousand")]
        [InlineData(1000000, "One Million")]
        public void AddMemory_Test_Theory(int numberOfAdds, string description)
        {
            var dictionary = new Dictionary<int, int>(numberOfAdds);

            Populate(dictionary, numberOfAdds);
        }

#pragma warning disable xUnit1013 // Public method should be marked as test
        public void Populate(Dictionary<int, int> dictionary, int n)
#pragma warning restore xUnit1013 // Public method should be marked as test
        {
            for(var i = 0; i < n; i++) { dictionary.Add(i, i); }
        }
        #endregion
    }
}