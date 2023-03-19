namespace EngineBay.ActorEngine
{
    using Proto;
    using Proto.Cluster;

    public class ActorSystemClusterHostedService : IHostedService
    {
        private readonly ActorSystem actorSystem;

        public ActorSystemClusterHostedService(ActorSystem actorSystem)
        {
            this.actorSystem = actorSystem;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Starting a cluster member");

            await this.actorSystem
                .Cluster()
                .StartMemberAsync().ConfigureAwait(false);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Shutting down a cluster member");

            await this.actorSystem
                .Cluster()
                .ShutdownAsync().ConfigureAwait(false);
        }
    }
}
