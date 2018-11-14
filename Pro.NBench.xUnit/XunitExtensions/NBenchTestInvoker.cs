﻿#region Using Directives

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using NBench.Reporting.Targets;
using NBench.Sdk;
using NBench.Sdk.Compiler;

using Pro.NBench.xUnit.NBenchExtensions;

using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

#endregion

namespace Pro.NBench.xUnit.XunitExtensions
{
    /// <summary>
    ///     The test invoker for xUnit.net v2 tests.
    /// </summary>
    public class NBenchTestInvoker : TestInvoker<IXunitTestCase>
    {
        #region Fields

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="XunitTestInvoker" /> class.
        /// </summary>
        /// <param name="test">The test that this invocation belongs to.</param>
        /// <param name="messageBus">The message bus to report run status to.</param>
        /// <param name="testClass">The test class that the test method belongs to.</param>
        /// <param name="constructorArguments">The arguments to be passed to the test class constructor.</param>
        /// <param name="testMethod">The test method that will be invoked.</param>
        /// <param name="testMethodArguments">The arguments to be passed to the test method.</param>
        /// <param name="beforeAfterAttributes">The list of <see cref="BeforeAfterTestAttribute" />s for this test invocation.</param>
        /// <param name="aggregator">The exception aggregator used to run code and collect exceptions.</param>
        /// <param name="cancellationTokenSource">The task cancellation token source, used to cancel the test run.</param>
        public NBenchTestInvoker(ITest test, IMessageBus messageBus, Type testClass, object[] constructorArguments, MethodInfo testMethod,
                                 object[] testMethodArguments, IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes, ExceptionAggregator aggregator,
                                 CancellationTokenSource cancellationTokenSource)
            : base(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, aggregator, cancellationTokenSource)
        {
            BeforeAfterAttributes = beforeAfterAttributes;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the list of <see cref="BeforeAfterTestAttribute" />s for this test invocation.
        /// </summary>
        protected IReadOnlyList<BeforeAfterTestAttribute> BeforeAfterAttributes { get; }

        #endregion

        #region Methods

        protected override async Task<decimal> InvokeTestMethodAsync(object testClassInstance)
        {
            var runSummary = await Task.Run(() => RunNBenchTest(testClassInstance));

            return runSummary.Time;
        }


        private void WriteTestOutput(string output)
        {
           Trace.WriteLine(output);
        }
        
        private RunSummary RunNBenchTest(object testClassInstance)
        {
            //TODO: It is not strictly reuired to use a RunSummary at the moment - needs more investigation to see
            //if we can provide more useful information via the standard xUnit mechanism. For now, what we have is sufficient.
            var summary = new RunSummary();

            var discovery = new ReflectionDiscovery(new ActionBenchmarkOutput(report => { }, results =>
                {
                    if (results.Data.Exceptions.Any())
                    {
                        throw new AggregateException(results.Data.Exceptions);
                    }

                    WriteTestOutput("");

                    if (results.AssertionResults.Count > 0)
                    {
                        //TODO: We should determine the accurate elapsed time at this point, to report it in the xUnit runner.
                        summary.Time = (decimal)results.Data.StatsByMetric.Values.First().Runs.First().ElapsedSeconds;

                        foreach (var assertion in results.AssertionResults)
                        {
                            //TODO: Maybe it is bubble to bubble this up, and provide the original line number?
                            Assert.True(assertion.Passed, assertion.Message);

                            summary.Total++;
                            if (!assertion.Passed) { summary.Failed++; }
                            WriteTestOutput(assertion.Message);
                            WriteTestOutput("");
                        }
                    }
                    else
                    {
                         WriteTestOutput("No assertions returned.");
                    }

                     WriteTestOutput("");
                     WriteTestOutput("---------- Measurements ----------");
                     WriteTestOutput("");

                    if (results.Data.StatsByMetric.Count > 0)
                    {
                        foreach (var measurement in results.Data.StatsByMetric)
                        {
                             WriteTestOutput("Metric : " + measurement.Key.ToHumanFriendlyString());
                             WriteTestOutput("");
                             WriteTestOutput($"Per Second ( {measurement.Value.Unit} )");

                             WriteTestOutput($"Average         : {measurement.Value.PerSecondStats.Average}");
                             WriteTestOutput($"Max             : {measurement.Value.PerSecondStats.Max}");
                             WriteTestOutput($"Min             : {measurement.Value.PerSecondStats.Min}");
                             WriteTestOutput($"Std. Deviation  : {measurement.Value.PerSecondStats.StandardDeviation}");
                             WriteTestOutput($"Std. Error      : {measurement.Value.PerSecondStats.StandardError}");
                             WriteTestOutput("");

                             WriteTestOutput($"Per Test ( {measurement.Value.Unit} )");
                             WriteTestOutput($"Average         : {measurement.Value.Stats.Average}");
                             WriteTestOutput($"Max             : {measurement.Value.Stats.Max}");
                             WriteTestOutput($"Min             : {measurement.Value.Stats.Min}");
                             WriteTestOutput($"Std. Deviation  : {measurement.Value.Stats.StandardDeviation}");
                             WriteTestOutput($"Std. Error      : {measurement.Value.Stats.StandardError}");

                             WriteTestOutput("");
                             WriteTestOutput("----------");
                             WriteTestOutput("");
                        }
                    }
                    else
                    {
                         WriteTestOutput("No measurements returned.");
                    }
                }));

            var testClassType = TestClass;

            //TODO: At the moment this is performing work that is not required, but is pragmatic in that a change is not required to the NBench core.
            var benchmarkMetaData = ReflectionDiscovery.CreateBenchmarksForClass(testClassType).First(b => b.Run.InvocationMethod.Name == TestMethod.Name);
            try
            {
                var invoker =
                    new XUnitReflectionBenchmarkInvoker(benchmarkMetaData, testClassInstance, TestMethodArguments);

                var settings = discovery.CreateSettingsForBenchmark(benchmarkMetaData);
                var benchmark = new Benchmark(settings, invoker, discovery.Output, discovery.BenchmarkAssertions);

                Benchmark.PrepareForRun();
                benchmark.Run();
                benchmark.Finish();
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach(var e in ex.LoaderExceptions)
                    WriteTestOutput(e.ToString());

                throw;
            }

            return summary;
        }

        #endregion
    }
}