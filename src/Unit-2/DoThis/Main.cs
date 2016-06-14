using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Akka.Actor;
using Akka.Util.Internal;
using ChartApp.Actors;

namespace ChartApp
{
    public partial class Main : Form
    {
        private IActorRef chartActor;
        private readonly AtomicCounter _seriesCounter = new AtomicCounter(1);
        private IActorRef coordinatorActor;
        private Dictionary<ChartingMessages.CounterType, IActorRef> toggleActors = new Dictionary<ChartingMessages.CounterType, IActorRef>(); 

        public Main()
        {
            InitializeComponent();
        }

        #region Initialization


        private void Main_Load(object sender, EventArgs e)
        {
            chartActor = Program.ChartActors.ActorOf(Props.Create(() => new ChartingActor(sysChart)), "charting");
            chartActor.Tell(new ChartingActor.InitializeChart(null));


            coordinatorActor = Program.ChartActors.ActorOf(Props.Create(()=>new PerformanceCounterCoordinatorActor(chartActor)),"counters");

            toggleActors[ChartingMessages.CounterType.Cpu] = Program.ChartActors.ActorOf(Props.Create(() => new ButtonToggleActor(ChartingMessages.CounterType.Cpu, btnCpu, coordinatorActor, false)).WithDispatcher("akka.actor.synchronized-dispatcher"));
            toggleActors[ChartingMessages.CounterType.Disk] = Program.ChartActors.ActorOf(Props.Create(() => new ButtonToggleActor(ChartingMessages.CounterType.Disk, btnDisk, coordinatorActor, false)).WithDispatcher("akka.actor.synchronized-dispatcher")); ;
            toggleActors[ChartingMessages.CounterType.Memory] = Program.ChartActors.ActorOf(Props.Create(() => new ButtonToggleActor(ChartingMessages.CounterType.Memory, btnMemory, coordinatorActor, false)).WithDispatcher("akka.actor.synchronized-dispatcher")); ;




            toggleActors[ChartingMessages.CounterType.Cpu].Tell(new ButtonToggleActor.Toggle());
            //            var series = ChartDataHelper.RandomSeries("FakeSeries" + _seriesCounter.GetAndIncrement());
            //            chartActor.Tell(new ChartingActor.InitializeChart(new Dictionary<string, Series>()
            //            {
            //                {series.Name, series}
            //            }));
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            //shut down the charting actor
            chartActor.Tell(PoisonPill.Instance);

            //shut down the ActorSystem
            Program.ChartActors.Shutdown();
        }

        #endregion

        private void btnCpu_Click(object sender, EventArgs e)
        {
            toggleActors[ChartingMessages.CounterType.Cpu].Tell(new ButtonToggleActor.Toggle());
        }

        private void btnMemory_Click(object sender, EventArgs e)
        {
            toggleActors[ChartingMessages.CounterType.Memory].Tell(new ButtonToggleActor.Toggle());
        }

        private void btnDisk_Click(object sender, EventArgs e)
        {
            toggleActors[ChartingMessages.CounterType.Disk].Tell(new ButtonToggleActor.Toggle());
        }
    }
}
