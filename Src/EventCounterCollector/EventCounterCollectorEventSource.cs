﻿namespace Microsoft.ApplicationInsights.Extensibility.EventCounterCollector.Implementation
    {
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Tracing;
    using Microsoft.ApplicationInsights.Common;

    [EventSource(Name = "Microsoft-ApplicationInsights-Extensibility-PerformanceCollector")]
    [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", Justification = "appDomainName is required")]
    internal sealed class EventCounterCollectorEventSource : EventSource
    {
        private readonly ApplicationNameProvider applicationNameProvider = new ApplicationNameProvider();

        private EventCounterCollectorEventSource()
        {
        }

        public static EventCounterCollectorEventSource Log { get; } = new EventCounterCollectorEventSource();

        [Event(1, Level = EventLevel.Informational, Message = @"EventCounterCollectionModule is being initialized. {0}")]
        public void ModuleIsBeingInitializedEvent(
            string message,
            string applicationName = "dummy")
        {
            this.WriteEvent(1, message, this.applicationNameProvider.Name);
        }

        [Event(2, Level = EventLevel.Informational, Message = @"EventCounterCollectionModule has been successfully initialized.")]
        public void ModuleInitializedSuccess(string applicationName = "dummy")
        {
            this.WriteEvent(2, this.applicationNameProvider.Name);
        }

        [Event(3, Level = EventLevel.Error, Message = @"EventCounterCollection - {0} failed with exception: {1}.")]
        public void EventCounterCollectorError(string stage, string exceptionMessage, string applicationName = "dummy")
        {
            this.WriteEvent(3, stage, exceptionMessage, this.applicationNameProvider.Name);
        }

        [Event(4, Level = EventLevel.Warning, Message = @"EventCounter IntervalSec is 0. Using default interval. Counter Name: {0}.")]
        public void EventCounterIntervalZero(
            string counterName,
            string applicationName = "dummy")
        {
            this.WriteEvent(4, counterName, this.applicationNameProvider.Name);
        }

        [Event(5, Level = EventLevel.Informational, Message = @"EventCounterListener initialized successfully.")]
        public void EventCounterInitializeSuccess(
            string applicationName = "dummy")
        {
            this.WriteEvent(5, this.applicationNameProvider.Name);
        }

        [Event(6, Level = EventLevel.Informational, Message = @"EventSource {0} enabled.")]
        public void EnabledEventSource(string eventSourceName, string applicationName = "dummy")
        {
            this.WriteEvent(6, eventSourceName, this.applicationNameProvider.Name);
        }

        [Event(7, Level = EventLevel.Informational, Message = @"EventSource {0} not enabled as not in the list of configured EventSource.")]
        public void NotEnabledEventSource(string eventSourceName, string applicationName = "dummy")
        {
            this.WriteEvent(7, eventSourceName, this.applicationNameProvider.Name);
        }

        [Event(8, Level = EventLevel.Warning, Message = @"Ignoring event from EventSource: {0} as EventCounterListener is not ready yet.")]
        public void IgnoreEventWrittenAsNotInitialized(string eventSourceName, string applicationName = "dummy")
        {
            this.WriteEvent(8, eventSourceName, this.applicationNameProvider.Name);
        }

        [Event(9, Level = EventLevel.Informational, Message = @"Ignoring event written from EventSource: {0} as no counters from this event source are configured to be collected.")]
        public void IgnoreEventWrittenAsEventSourceNotInConfiguredList(string eventSourceName, string applicationName = "dummy")
        {
            this.WriteEvent(9, eventSourceName, this.applicationNameProvider.Name);
        }

        [Event(10, Level = EventLevel.Warning, Message = @"Ignoring event written from EventSource: {0} as payload is not IDictionary to extract metrics.")]
        public void IgnoreEventWrittenAsEventPayloadNotParseable(string eventSourceName, string applicationName = "dummy")
        {
            this.WriteEvent(10, eventSourceName, this.applicationNameProvider.Name);
        }

        [Event(11, Level = EventLevel.Warning, Message = @"EventCounterCollection - {0} failed with exception: {1}.")]
        public void EventCounterCollectorWarning(string stage, string exceptionMessage, string applicationName = "dummy")
        {
            this.WriteEvent(11, stage, exceptionMessage, this.applicationNameProvider.Name);
        }

        public class Keywords
        {
            public const EventKeywords UserActionable = (EventKeywords)0x1;

            public const EventKeywords Diagnostics = (EventKeywords)0x2;
        }
    }
}