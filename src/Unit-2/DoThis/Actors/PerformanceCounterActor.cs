using System;
using System.Collections.Generic;
using System.Diagnostics;
using Akka.Actor;

namespace ChartApp.Actors
{
    public class PerformanceCounterActor : UntypedActor
    {
        private readonly ICancelable cancelPublishing;
        private readonly Func<PerformanceCounter> performanceCounterGenerator;

        private readonly string seriesName;

        private readonly HashSet<IActorRef> subscriptions;
        private PerformanceCounter counter;

        public PerformanceCounterActor(string seriesName, Func<PerformanceCounter> performanceCounterGenerator)
        {
            this.seriesName = seriesName;
            this.performanceCounterGenerator = performanceCounterGenerator;
            subscriptions = new HashSet<IActorRef>();
        }

        protected override void OnReceive(object message)
        {
            if (message is ChartingMessages.GatherMetrics)
            {
                var metric = new ChartingMessages.Metric(seriesName,counter.NextValue());
                foreach (var subscription in subscriptions)
                {
                    subscription.Tell(metric);
                }
            }
            else if (message is ChartingMessages.SubscribeCounter)
            {
                var subscribeMessage = message as ChartingMessages.SubscribeCounter;
                subscriptions.Add(subscribeMessage.Subscriber);
            }
            else if (message is ChartingMessages.UnsubscribeCounter)
            {
                var unsubscribeMessage = message as ChartingMessages.UnsubscribeCounter;
                subscriptions.Remove(unsubscribeMessage.Subscriber);
            }
        }

        protected override void PostStop()
        {
            try
            {
                cancelPublishing.Cancel();
                counter.Dispose();
            }
            catch
            {
                // ignored
            }
            finally
            {
                base.PostStop();
            }
        }

        protected override void PreStart()
        {
            counter = performanceCounterGenerator();

            Context.System.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(250), Self, new ChartingMessages.GatherMetrics(), Self, cancelPublishing);
        }
    }
}
