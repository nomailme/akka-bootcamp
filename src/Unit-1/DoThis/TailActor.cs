﻿using System;
using System.IO;
using System.Text;
using Akka.Actor;

namespace WinTail
{
    public class TailActor : UntypedActor
    {
        private string filepath;
        private FileObserver observer;
        private IActorRef reporterActor;
        private FileStream fileStream;
        private StreamReader fileStreamReader;

        public TailActor(IActorRef reporterActor, string filepath)
        {
            this.reporterActor = reporterActor;
            this.filepath = filepath;

            observer = new FileObserver(Self, Path.GetFullPath(filepath));
            observer.Start();

            fileStream = new FileStream(Path.GetFullPath(filepath),
                FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            fileStreamReader = new StreamReader(fileStream, Encoding.UTF8);

            string text = fileStreamReader.ReadToEnd();

            Self.Tell(new InitialRead(filepath, text));
        }

        /// <summary>
        /// To be implemented by concrete UntypedActor, this defines the behavior of the UntypedActor.
        ///             This method is called for every message received by the actor.
        /// </summary>
        /// <param name="message">The message.</param>
        protected override void OnReceive(object message)
        {
            if (message is FileWrite)
            {
                var text = fileStreamReader.ReadToEnd();
                if (string.IsNullOrEmpty(text)==false)
                {
                    reporterActor.Tell(text);
                }
            }
            else if (message is FileError)
            {
                var fileError = message as FileError;
                reporterActor.Tell(string.Format("Error: {0}",fileError.Message));
            }
            else if (message is InitialRead)
            {
                var initialRead = message as InitialRead;
                reporterActor.Tell(initialRead.Text);
            }

        }

        public class FileError
        {
            public FileError(string filename, string message)
            {
                Filename = filename;
                Message = message;
            }

            public string Filename { get; set; }

            public string Message { get; set; }
        }

        public class FileWrite
        {
            public FileWrite(string name)
            {
                Name = name;
            }

            public string Name { get; set; }
        }

        public class InitialRead
        {
            public InitialRead(string filename, string text)
            {
                Filename = filename;
                Text = text;
            }

            public string Filename { get; set; }

            public string Text { get; set; }
        }
    }
}
