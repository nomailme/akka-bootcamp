using System;
using System.IO;
using Akka.Actor;

namespace WinTail
{
    public class FileValidationActor : UntypedActor
    {
        private readonly IActorRef consoleWriterActor;

        public FileValidationActor(IActorRef consoleWriterActor)
        {
            this.consoleWriterActor = consoleWriterActor;
        }

        /// <summary>
        /// To be implemented by concrete UntypedActor, this defines the behavior of the UntypedActor.
        ///             This method is called for every messageObject received by the actor.
        /// </summary>
        /// <param name="messageObject">The messageObject.</param>
        protected override void OnReceive(object messageObject)
        {
            var message = messageObject as string;

            if (string.IsNullOrEmpty(message))
            {
                consoleWriterActor.Tell(new Messages.NullInputError("No input"));
                Sender.Tell(new Messages.ContinueProcessing());
            }
            else
            {
                bool valid = IsFileExists(message);
                if (valid)
                {
                    consoleWriterActor.Tell(new Messages.InputSuccess("Valid message"));

                    Context.ActorSelection("akka://MyActorSystem/user/tailCoordinatorActor").Tell(new TailCoordinatorActor.StartTail(message, consoleWriterActor));
                }
                else
                {
                    consoleWriterActor.Tell(new Messages.ValidationError("Odd number of characters"));

                    Sender.Tell(new Messages.ContinueProcessing());
                }
            }

            Sender.Tell(new Messages.ContinueProcessing());
        }

        private bool IsFileExists(string message)
        {
            return File.Exists(message);
        }
    }
}
