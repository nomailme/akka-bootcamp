using System;
using Akka.Actor;

namespace WinTail
{
    public class TailCoordinatorActor : UntypedActor
    {
        /// <summary>
        /// To be implemented by concrete UntypedActor, this defines the behavior of the UntypedActor.
        ///             This method is called for every path received by the actor.
        /// </summary>
        /// <param name="message">The path.</param>
        protected override void OnReceive(object message)
        {
            if (message is StartTail)
            {
                var msg = message as StartTail;

                Context.ActorOf(Props.Create<TailActor>(msg.reporterActor, msg.FilePath));
            }
        }

        public class StartTail
        {
            public StartTail(string path, IActorRef reporterActor)
            {
                FilePath = path;
                this.reporterActor = reporterActor;
            }

            public string FilePath { get; set; }
            public IActorRef reporterActor { get; set; }
        }

        public class StopTail
        {
            public StopTail(string filePath)
            {
                FilePath = filePath;
            }

            public string FilePath { get; set; }
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(10,TimeSpan.FromSeconds(30), x =>
            {
                if (x is ArithmeticException) return Directive.Resume;
                if (x is NotSupportedException) return Directive.Stop;
                return Directive.Restart;
            });
        }
    }
}
