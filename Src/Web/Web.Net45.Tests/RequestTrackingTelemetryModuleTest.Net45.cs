﻿namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Web;

    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Assert = Xunit.Assert;

    /// <summary>
    /// NET 4.5 specific tests for RequestTrackingTelemetryModule.
    /// </summary>
    public partial class RequestTrackingTelemetryModuleTest
    {
        [TestMethod]
        public void OnBeginSetsOperationContextWithStandardHeaders()
        {
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
            {
                ["Request-Id"] = "|guid1.1",
                ["Correlation-Context"] = "k=v"
            });
            var module = this.RequestTrackingTelemetryModuleFactory(this.CreateDefaultConfig(context));

            module.OnBeginRequest(context);
            var requestTelemetry = context.GetRequestTelemetry();

            // initialize telemetry
            module.OnEndRequest(context);

            Assert.Equal("guid1", requestTelemetry.Context.Operation.Id);
            Assert.Equal("|guid1.1", requestTelemetry.Context.Operation.ParentId);

            Assert.True(requestTelemetry.Id.StartsWith("|guid1.1.", StringComparison.Ordinal));
            Assert.NotEqual("|guid1.1", requestTelemetry.Id);
            Assert.Equal("guid1", this.GetActivityRootId(requestTelemetry.Id));
            Assert.Equal("v", requestTelemetry.Properties["k"]);
        }

        [TestMethod]
        public void OnBeginSetsOperationContextWithStandardHeadersWithNonHierarchialId()
        {
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
            {
                ["Request-Id"] = "guid1",
                ["Correlation-Context"] = "k=v"
            });
            var module = this.RequestTrackingTelemetryModuleFactory(this.CreateDefaultConfig(context));

            module.OnBeginRequest(context);
            var requestTelemetry = context.GetRequestTelemetry();
            module.OnEndRequest(context);

            Assert.Equal("guid1", requestTelemetry.Context.Operation.Id);
            Assert.Equal("guid1", requestTelemetry.Context.Operation.ParentId);

            Assert.True(requestTelemetry.Id.StartsWith("|guid1.", StringComparison.Ordinal));
            Assert.NotEqual("|guid1.1.", requestTelemetry.Id);
            Assert.Equal("guid1", this.GetActivityRootId(requestTelemetry.Id));

            // will initialize telemetry
            module.OnEndRequest(context);
            Assert.Equal("v", requestTelemetry.Properties["k"]);
        }

        [TestMethod]
        public void OnBeginSetsOperationContextWithoutHeaders()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            var module = this.RequestTrackingTelemetryModuleFactory(this.CreateDefaultConfig(context));

            module.OnBeginRequest(context);
            var requestTelemetry = context.GetRequestTelemetry();
            module.OnEndRequest(context);

            var operationId = requestTelemetry.Context.Operation.Id;
            Assert.NotNull(operationId);
            Assert.Null(requestTelemetry.Context.Operation.ParentId);
            Assert.True(requestTelemetry.Id.StartsWith('|' + operationId + '.', StringComparison.Ordinal));
            Assert.NotEqual(operationId, requestTelemetry.Id);

            // This code should go away when Activity is fixed: https://github.com/dotnet/corefx/issues/18418
            // check that Ids are not generated by Activity
            // so they look like OperationTelemetry.Id:
            // length is like default RequestTelemetry.Id length
            Assert.Equal(new RequestTelemetry().Id.Length, operationId.Length);

            // operationId is ulong base64 encoded
            byte[] data = Convert.FromBase64String(operationId);
            Assert.Equal(8, data.Length);
            BitConverter.ToUInt64(data, 0);

            // does not look like root Id generated by Activity
            Assert.Equal(1, operationId.Split('-').Length);

            //// end of workaround test
        }

        [TestMethod]
        public void InitializeFromStandardHeadersAlwaysWinsCustomHeaders()
        {
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
            {
                ["Request-Id"] = "|standard-id.",
                ["x-ms-request-id"] = "legacy-id",
                ["x-ms-request-rooit-id"] = "legacy-root-id"
            });

            var module = this.RequestTrackingTelemetryModuleFactory(this.CreateDefaultConfig(context));
            module.OnBeginRequest(context);

            var requestTelemetry = context.GetRequestTelemetry();

            // initialize telemetry
            module.OnEndRequest(context);
            Assert.Equal("|standard-id.", requestTelemetry.Context.Operation.ParentId);
            Assert.Equal("standard-id", requestTelemetry.Context.Operation.Id);
            Assert.Equal("standard-id", this.GetActivityRootId(requestTelemetry.Id));
            Assert.NotEqual(requestTelemetry.Context.Operation.Id, requestTelemetry.Id);
        }

        [TestMethod]
        public void OnBeginSetsOperationContextWithEnabledLegacyHeaders()
        {
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
            {
                ["x-ms-request-id"] = "guid1",
                ["x-ms-request-root-id"] = "guid2"
            });

            var module = this.RequestTrackingTelemetryModuleFactory(this.CreateDefaultConfig(context, "x-ms-request-root-id", "x-ms-request-id"));

            module.OnBeginRequest(context);
            var requestTelemetry = context.GetRequestTelemetry();
            module.OnEndRequest(context);

            Assert.Equal("guid2", requestTelemetry.Context.Operation.Id);
            Assert.Equal("guid1", requestTelemetry.Context.Operation.ParentId);

            Assert.True(requestTelemetry.Id.StartsWith("|guid2.", StringComparison.Ordinal));
        }

        [TestMethod]
        public void OnBeginSetsOperationContextWithDisabledLegacyHeaders()
        {
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
            {
                ["x-ms-request-id"] = "guid1",
                ["x-ms-request-root-id"] = "guid2"
            });

            var module = this.RequestTrackingTelemetryModuleFactory(this.CreateDefaultConfig(context));

            module.OnBeginRequest(context);
            var requestTelemetry = context.GetRequestTelemetry();
            module.OnEndRequest(context);

            Assert.NotNull(requestTelemetry.Context.Operation.Id);
            Assert.Null(requestTelemetry.Context.Operation.ParentId);
        }

        [TestMethod]
        public void OnBeginReadsRootAndParentIdFromCustomHeader()
        {
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
            {
                ["parentHeaderName"] = "ParentId",
                ["rootHeaderName"] = "RootId"
            });

            var config = this.CreateDefaultConfig(context, rootIdHeaderName: "rootHeaderName", parentIdHeaderName: "parentHeaderName");
            var module = this.RequestTrackingTelemetryModuleFactory(config);
                      
            module.OnBeginRequest(context);

            var requestTelemetry = context.GetRequestTelemetry();

            Assert.Equal("ParentId", requestTelemetry.Context.Operation.ParentId);

            Assert.Equal("RootId", requestTelemetry.Context.Operation.Id);
            Assert.NotEqual("RootId", requestTelemetry.Id);
            Assert.Equal("RootId", this.GetActivityRootId(requestTelemetry.Id));
        }

        [TestMethod]
        public void OnBeginTelemetryCreatedWithinRequestScopeIsRequestChild()
        {
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
            {
                ["Request-Id"] = "|guid1.1",
                ["Correlation-Context"] = "k=v"
            });
            var config = this.CreateDefaultConfig(context);
            var module = this.RequestTrackingTelemetryModuleFactory(this.CreateDefaultConfig(context));

            module.OnBeginRequest(context);

            var requestTelemetry = context.GetRequestTelemetry();
            var telemetryClient = new TelemetryClient(config);
            var exceptionTelemetry = new ExceptionTelemetry();
            telemetryClient.Initialize(exceptionTelemetry);

            module.OnEndRequest(context);

            Assert.Equal("guid1", exceptionTelemetry.Context.Operation.Id);
            Assert.Equal(requestTelemetry.Id, exceptionTelemetry.Context.Operation.ParentId);
            Assert.Equal("v", exceptionTelemetry.Context.Properties["k"]);
        }

        [TestMethod]
        public void OnPreHandlerTelemetryCreatedWithinRequestScopeIsRequestChild()
        {
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
            {
                ["Request-Id"] = "|guid1.1",
                ["Correlation-Context"] = "k=v"
            });
            var config = this.CreateDefaultConfig(context);
            var module = this.RequestTrackingTelemetryModuleFactory(config);
            var telemetryClient = new TelemetryClient(config);

            module.OnBeginRequest(context);

            // simulate losing call context by cleaning up activity
            Assert.NotNull(Activity.Current);
            var activity = Activity.Current;
            activity.Stop();
            Assert.Null(Activity.Current);

            // CallContext was lost after OnBegin, so Asp.NET Http Module will restore it in OnPreRequestHandlerExecute
            new Activity("restored").SetParentId(activity.Id).AddBaggage("k", "v").Start();

            // if OnPreRequestHandlerExecute set a CallContext, child telemetry will be properly filled
            var trace = new TraceTelemetry();
            telemetryClient.TrackTrace(trace);
            var requestTelemetry = context.GetRequestTelemetry();

            Assert.Equal(requestTelemetry.Context.Operation.Id, trace.Context.Operation.Id);

            // we created Activity for request and assigned Id for the request like guid1.1.12345_
            // then we lost it and restored (started a new child activity), so the Id is guid1.1.12345_abc_
            // so the request is grand parent to the trace
            Assert.Equal(Activity.Current.ParentId, requestTelemetry.Id);
            Assert.True(trace.Context.Operation.ParentId.StartsWith(requestTelemetry.Id, StringComparison.Ordinal));
            Assert.Equal(Activity.Current.Id, trace.Context.Operation.ParentId);
            Assert.Equal("v", trace.Context.Properties["k"]);
        }

        [TestMethod]
        public void TelemetryCreatedWithinRequestScopeIsRequestChildWhenActivityIsLost()
        {
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
            {
                ["Request-Id"] = "|guid1.1",
                ["Correlation-Context"] = "k=v"
            });
            var config = this.CreateDefaultConfig(context);
            var module = this.RequestTrackingTelemetryModuleFactory(config);
            var telemetryClient = new TelemetryClient(config);

            module.OnBeginRequest(context);

            // simulate losing call context by cleaning up activity
            Assert.NotNull(Activity.Current);
            Activity.Current.Stop();
            Assert.Null(Activity.Current);

            var trace = new TraceTelemetry();
            telemetryClient.TrackTrace(trace);
            var requestTelemetry = context.GetRequestTelemetry();

            Assert.Equal(requestTelemetry.Context.Operation.Id, trace.Context.Operation.Id);

            // we created Activity for request and assigned Id for the request like guid1.1.12345
            // then we created Activity for request children and assigned it Id like guid1.1.12345_1
            // then we lost it and restored (started a new child activity), so the Id is guid1.1.123_1.abc
            // so the request is grand parent to the trace
            Assert.True(trace.Context.Operation.ParentId.StartsWith(requestTelemetry.Id, StringComparison.Ordinal));
            Assert.Equal("v", trace.Context.Properties["k"]);
        }
    }
}