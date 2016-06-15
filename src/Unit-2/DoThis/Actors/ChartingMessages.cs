using System;
using Akka.Actor;

namespace ChartApp.Actors
{
    public class ChartingMessages
    {
        public class GatherMetrics
        {
        }

        public class Metric
        {
            public Metric(string series, float counterValue)
            {
                Series = series;
                CounterValue = counterValue;
            }

            public float CounterValue { get; private set; }
            public string Series { get; private set; }
        }

        public enum CounterType
        {
            Cpu,
            Memory,
            Disk
        }

        public class SubscribeCounter
        {
            public CounterType Counter { get; set; }
            public IActorRef Subscriber { get; set; }

            public SubscribeCounter(CounterType counter, IActorRef subscriber)
            {
                Counter = counter;
                Subscriber = subscriber;
            }
        }

        public class UnsubscribeCounter
        {
            public CounterType Counter { get; set; }
            public IActorRef Subscriber { get; set; }

            public UnsubscribeCounter(CounterType counter, IActorRef subscriber)
            {
                Counter = counter;
                Subscriber = subscriber;
            }
        }

        public class TogglePaused
        {
        }
    }
}
