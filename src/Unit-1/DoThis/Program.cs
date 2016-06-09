using System;
using Akka.Actor;
using Akka.Actor.Dsl;

namespace WinTail
{

    #region Program

    internal class Program
    {
        public static ActorSystem MyActorSystem;

        private static void Main(string[] args)
        {
            MyActorSystem = ActorSystem.Create("MyActorSystem");

            Props consoleWriterProps = Props.Create<ConsoleWriterActor>();
            IActorRef consoleWriterActor = MyActorSystem.ActorOf(consoleWriterProps, "ConsoleWriterActor");

            var tailCoorinatorActor = MyActorSystem.ActorOf<TailCoordinatorActor>("tailCoordinatorActor");

            Props validationProps = Props.Create<FileValidationActor>(consoleWriterActor);
            IActorRef fileValidatorActor = MyActorSystem.ActorOf(validationProps, "FileValidationActor");

            Props consoleReaderProps = Props.Create<ConsoleReaderActor>();
            IActorRef consoleReaderActor = MyActorSystem.ActorOf(consoleReaderProps, "ConsoleReaderActor");

            
            




            // tell console reader to begin
            consoleReaderActor.Tell(ConsoleReaderActor.StartCommand);

            // blocks the main thread from exiting until the actor system is shut down
            MyActorSystem.AwaitTermination();
        }
    }

    #endregion
}
