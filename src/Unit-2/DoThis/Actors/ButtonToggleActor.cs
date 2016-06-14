using System.Drawing.Text;
using System.Windows.Forms;
using Akka.Actor;

namespace ChartApp.Actors
{
    public class ButtonToggleActor: UntypedActor
    {

        public class Toggle { }

        private readonly ChartingMessages.CounterType myCounterType;
        private bool isToggleOn;
        private readonly Button button;
        private readonly IActorRef coordinatorActor;

        public ButtonToggleActor(ChartingMessages.CounterType myCounterType, Button button, IActorRef coordinatorActor, bool isToggleOn = false)
        {
            this.myCounterType = myCounterType;
            this.button = button;
            this.coordinatorActor = coordinatorActor;
            this.isToggleOn = isToggleOn;
        }

        protected override void OnReceive(object message)
        {
            if (message is Toggle && isToggleOn)
            {
                coordinatorActor.Tell(new PerformanceCounterCoordinatorActor.Unwatch(myCounterType));
                FlipToggle();
            }
            else if (message is Toggle && isToggleOn == false)
            {
                coordinatorActor.Tell(new PerformanceCounterCoordinatorActor.Watch(myCounterType));
                FlipToggle();
            }
            else
            {
                Unhandled(message);
            }
        }

        private void FlipToggle()
        {
            isToggleOn = !isToggleOn;

            button.Text = $"{myCounterType.ToString()} ({(isToggleOn ? "ON" : "OFF")})";
        }
    }
}