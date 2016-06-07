﻿using PerfCounterCollector.FunctionalTests;

namespace FunctionalTests
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Runtime.Versioning;

    using Functional.Helpers;
    using Functional.IisExpress;
    using Microsoft.Developer.Analytics.DataCollection.Model.v2;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class Test45 : SingleWebHostTestBase
    {
        private const int TimeoutInMs = 10000;
        
        private const string TestWebApplicaionSourcePath = @"TestApps\TestApp45\App";
        private const string TestWebApplicaionDestPath = @"TestsPerformanceCollector45";

        [TestInitialize]
        public void TestInitialize()
        {
            var applicationDirectory = Path.Combine(
                Directory.GetCurrentDirectory(),
                TestWebApplicaionDestPath);

            Trace.WriteLine("Application directory:" + applicationDirectory);

            this.StartWebAppHost(
                new SingleWebHostTestConfiguration(
                    new IisExpressConfiguration
                    {
                        ApplicationPool = IisExpressAppPools.Clr4IntegratedAppPool,
                        Path = applicationDirectory,
                        Port = 5678,
                    })
                {
                    TelemetryListenerPort = 4554,
                    // AttachDebugger = Debugger.IsAttached,
                    IKey = "fafa4b10-03d3-4bb0-98f4-364f0bdf5df8",
                });

            this.LaunchAndVerifyApplication();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.StopWebAppHost();
        }

        [TestMethod]
        [Owner("alkaplan")]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
        public void DefaultCounterCollection()
        {
            CommonTests.DefaultCounterCollection(this.Listener);
        }

        [TestMethod]
        [Owner("alkaplan")]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
        public void CustomCounterCollection()
        {
            CommonTests.CustomCounterCollection(this.Listener);
        }

        [TestMethod]
        [Owner("alkaplan")]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
        public void NonExistentCounter()
        {
            CommonTests.NonExistentCounter(this.Listener);
        }

        [TestMethod]
        [Owner("alkaplan")]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
        public void NonParsableCounter()
        {
            CommonTests.NonParsableCounter(this.Listener);
        }

        private void LaunchAndVerifyApplication()
        {
            const string RequestPath = "aspx/TestWebForm.aspx";
            string expectedRequestUrl = this.Config.ApplicationUri + "/" + RequestPath;

            // spin up the application
            var client = new HttpClient();
            var requestMessage = new HttpRequestMessage { RequestUri = new Uri(expectedRequestUrl), Method = HttpMethod.Get, };

            var responseTask = client.SendAsync(requestMessage);
            responseTask.Wait(TimeoutInMs);
            var responseTextTask = responseTask.Result.Content.ReadAsStringAsync();
            responseTextTask.Wait(TimeoutInMs);

            // make sure it's the correct application
            Assert.AreEqual("PerformanceCollector application", responseTextTask.Result);
        }

    }
}