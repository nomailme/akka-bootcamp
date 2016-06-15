using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;
using Akka.Actor;

namespace ChartApp.Actors
{
    public class PerformanceCounterCoordinatorActor: ReceiveActor
    {
        public class Watch
        {
            public ChartingMessages.CounterType Counter { get; set; }

            public Watch(ChartingMessages.CounterType counter)
            {
                Counter = counter;
            }
        }

        public class Unwatch
        {
            public ChartingMessages.CounterType Counter { get; set; }

            public Unwatch(ChartingMessages.CounterType counter)
            {
                Counter = counter;
            }
        }

        private static readonly Dictionary<ChartingMessages.CounterType, Func<PerformanceCounter>> CounterGenerators = new Dictionary<ChartingMessages.CounterType, Func<PerformanceCounter>>()
        {
            { ChartingMessages.CounterType.Cpu, () => new PerformanceCounter("Processor", "% Processor Time", "_Total", true) },
            { ChartingMessages.CounterType.Disk, () => new PerformanceCounter("LogicalDisk", "% Disk Time", "_Total", true) },
            { ChartingMessages.CounterType.Memory, () => new PerformanceCounter("Memory", "% Committed Bytes In Use", true) }
        };

        private static readonly Dictionary<ChartingMessages.CounterType, Func<Series>> CounterSeries = new Dictionary<ChartingMessages.CounterType, Func<Series>>()
        {
            {ChartingMessages.CounterType.Cpu, ()=> new Series(ChartingMessages.CounterType.Cpu.ToString()) {ChartType =  SeriesChartType.SplineArea, Color = Color.DarkGreen} },
            {ChartingMessages.CounterType.Disk, ()=> new Series(ChartingMessages.CounterType.Disk.ToString()) {ChartType =  SeriesChartType.SplineArea, Color = Color.DarkRed} },
            {ChartingMessages.CounterType.Memory, ()=> new Series(ChartingMessages.CounterType.Memory.ToString()) {ChartType =  SeriesChartType.FastLine, Color = Color.MediumBlue} },
        };

        private Dictionary<ChartingMessages.CounterType, IActorRef> counterActors;

        private IActorRef chartingActor;

        public PerformanceCounterCoordinatorActor(IActorRef chartingActor): this(chartingActor, new Dictionary<ChartingMessages.CounterType, IActorRef>())
        {
            
        }

        public PerformanceCounterCoordinatorActor(IActorRef chartingActor, Dictionary<ChartingMessages.CounterType, IActorRef> counterActors)
        {
            this.chartingActor = chartingActor;
            this.counterActors = counterActors;

            Receive<Watch>(watch =>
            {
                if (counterActors.ContainsKey(watch.Counter) == false)
                {
                    var counterActor = Context.ActorOf(Props.Create(() => new PerformanceCounterActor(watch.Counter.ToString(), CounterGenerators[watch.Counter])));
                    counterActors[watch.Counter] = counterActor;
                }

                chartingActor.Tell(new ChartingActor.AddSeries(CounterSeries[watch.Counter]()));

                counterActors[watch.Counter].Tell(new ChartingMessages.SubscribeCounter(watch.Counter, chartingActor));
            });

            Receive<Unwatch>(unwatch =>
            {
                if (counterActors.ContainsKey(unwatch.Counter) == false)
                {
                    return;
                }

                counterActors[unwatch.Counter].Tell(new ChartingMessages.UnsubscribeCounter(unwatch.Counter, chartingActor));
                chartingActor.Tell(new ChartingActor.RemoveSeries(unwatch.Counter.ToString()));

            });
        }
    }
}