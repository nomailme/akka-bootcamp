using System;
using Akka.Actor;

namespace WinTail
{
    public class ValidationActor : UntypedActor
    {
        private readonly IActorRef consoleWriterActor;

        public ValidationActor(IActorRef consoleWriterActor)
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
            }
            else
            {
                bool valid = IsValid(message);
                if (valid)
                {
                    consoleWriterActor.Tell(new Messages.InputSuccess("Valid message"));
                }
                else
                {
                    consoleWriterActor.Tell(new Messages.ValidationError("Odd number of characters"));
                }
            }

            Sender.Tell(new Messages.ContinueProcessing());
        }

        private bool IsValid(string message)
        {
            return message.Length % 2 == 0;
        }
    }
}
