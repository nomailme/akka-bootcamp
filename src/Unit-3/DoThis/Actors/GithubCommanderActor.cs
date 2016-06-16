using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Actor.Dsl;
using Akka.Routing;

namespace GithubActors.Actors
{
    /// <summary>
    /// Top-level actor responsible for coordinating and launching repo-processing jobs
    /// </summary>
    public class GithubCommanderActor : ReceiveActor, IWithUnboundedStash
    {
        private IActorRef _canAcceptJobSender;

        private IActorRef _coordinator;
        private int pendingJobReplies;

        private RepoKey repoJob;

        /// <summary>
        /// Gets or sets the stash. This will be automatically populated by the framework AFTER the constructor has been run.
        ///             Implement this as an auto property.
        /// </summary>
        /// <value>
        /// The stash.
        /// </value>
        public IStash Stash { get; set; }

        protected override void PreRestart(Exception reason, object message)
        {
            //kill off the old coordinator so we can recreate it from scratch
            _coordinator.Tell(PoisonPill.Instance);
            base.PreRestart(reason, message);
        }

        protected override void PreStart()
        {
            

            // create a broadcast router who will ask all of them 
            // if they're available for work
            _coordinator =
                Context.ActorOf(Props.Create<GithubCoordinatorActor>().WithRouter(FromConfig.Instance), ActorPaths.GithubCoordinatorActor.Name);
            base.PreStart();
        }

        /* replace the current constructor and Receive<T> handlers
 * in Actors/GithubCommanderActor.cs with the following
 */

        public GithubCommanderActor()
        {
            Ready();
        }

        private void Ready()
        {
            Receive<CanAcceptJob>(job =>
            {
                _coordinator.Tell(job);
                repoJob = job.Repo;
                BecomeAsking();
            });
        }

        private void BecomeAsking()
        {
            _canAcceptJobSender = Sender;
            pendingJobReplies = _coordinator.Ask<Routees>(new GetRoutees()).Result.Members.Count();
            Become(Asking);

            Context.SetReceiveTimeout(TimeSpan.FromSeconds(3));
        }

        private void Asking()
        {
            Receive<ReceiveTimeout>(x =>
            {
                _canAcceptJobSender.Tell(new UnableToAcceptJob(repoJob));
                BecomeReady();
            });

            // stash any subsequent requests
            Receive<CanAcceptJob>(job => Stash.Stash());

            Receive<UnableToAcceptJob>(job =>
            {
                pendingJobReplies--;
                if (pendingJobReplies == 0)
                {
                    _canAcceptJobSender.Tell(job);
                    BecomeReady();
                }
            });

            Receive<AbleToAcceptJob>(job =>
            {
                _canAcceptJobSender.Tell(job);

                // start processing messages
                Sender.Tell(new GithubCoordinatorActor.BeginJob(job.Repo));

                // launch the new window to view results of the processing
                Context.ActorSelection(ActorPaths.MainFormActor.Path).Tell(
                    new MainFormActor.LaunchRepoResultsWindow(job.Repo, Sender));

                BecomeReady();
            });
        }

        private void BecomeReady()
        {
            Become(Ready);
            Stash.UnstashAll();

            Context.SetReceiveTimeout(null);
        }

        #region Message classes

        public class CanAcceptJob
        {
            public CanAcceptJob(RepoKey repo)
            {
                Repo = repo;
            }

            public RepoKey Repo { get; private set; }
        }

        public class AbleToAcceptJob
        {
            public AbleToAcceptJob(RepoKey repo)
            {
                Repo = repo;
            }

            public RepoKey Repo { get; private set; }
        }

        public class UnableToAcceptJob
        {
            public UnableToAcceptJob(RepoKey repo)
            {
                Repo = repo;
            }

            public RepoKey Repo { get; private set; }
        }

        #endregion
    }
}
