using System;
using System.IO;
using Akka.Actor;

namespace WinTail
{
    public class FileObserver : IDisposable
    {
        private readonly string absoluteFilePath;

        private readonly string fileDirectory;

        private readonly string filename;
        private readonly IActorRef tailActor;

        private FileSystemWatcher fileSystemWatcher;

        public FileObserver(IActorRef tailActor, string absoluteFilePath)
        {
            this.tailActor = tailActor;
            this.absoluteFilePath = absoluteFilePath;
            fileDirectory = Path.GetDirectoryName(absoluteFilePath);
            filename = Path.GetFileName(absoluteFilePath);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            fileSystemWatcher.Dispose();
        }

        public void Start()
        {
            fileSystemWatcher = new FileSystemWatcher(fileDirectory, filename);
            fileSystemWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;

            fileSystemWatcher.Changed += FileSystemWatcher_Changed;
            fileSystemWatcher.Error += FileSystemWatcher_Error;

            fileSystemWatcher.EnableRaisingEvents = true;
        }

        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                tailActor.Tell(new TailActor.FileWrite(e.Name), ActorRefs.NoSender);
            }
        }

        private void FileSystemWatcher_Error(object sender, ErrorEventArgs e)
        {
            tailActor.Tell(new TailActor.FileError(filename, e.GetException().Message), ActorRefs.NoSender);
        }
    }
}
