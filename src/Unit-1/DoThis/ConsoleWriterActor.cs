using System;
using Akka.Actor;

namespace WinTail
{
    /// <summary>
    /// Actor responsible for serializing messageObject writes to the console.
    /// (write one messageObject at a time, champ :)
    /// </summary>
    class ConsoleWriterActor : UntypedActor
    {
        protected override void OnReceive(object messageObject)
        {
            if (messageObject is Messages.InputError)
            {
                var message = messageObject as Messages.InputError;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(message.Reason);
            } else if (messageObject is Messages.InputSuccess)
            {
                var message = messageObject as Messages.InputSuccess;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(message.Reason);
            }
            else
            {
                Console.WriteLine(messageObject);
            }
            Console.ResetColor();
        }
    }
}
