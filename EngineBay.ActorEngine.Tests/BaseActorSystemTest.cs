namespace EngineApi.Tests
{
    using System.Threading.Tasks;
    using EngineBay.ActorEngine;
    using EngineBay.Blueprints;
    using EngineBay.Persistence;
    using Microsoft.EntityFrameworkCore;
    using Moq;
    using Proto;
    using Proto.Cluster;
    using Proto.Cluster.Partition;
    using Proto.Cluster.Testing;
    using Proto.Remote.GrpcNet;

    public abstract class BaseActorSystemTest : IAsyncDisposable
    {
        protected BaseActorSystemTest()
        {
            var mockProvider = new Mock<IServiceProvider>();

            var blueprintsDbOptions = new DbContextOptionsBuilder<ModuleWriteDbContext>()
                .UseInMemoryDatabase(databaseName: "BlueprintsDb")
                .EnableSensitiveDataLogging()
                .Options;

            this.BlueprintsDb = new BlueprintsQueryDbContext(blueprintsDbOptions);

            this.BlueprintsDb.Database.EnsureCreated();

            var actorDbOptions = new DbContextOptionsBuilder<ModuleWriteDbContext>()
                .UseInMemoryDatabase(databaseName: "ActorEngineDb")
                .EnableSensitiveDataLogging()
                .Options;

            this.ActorDb = new ActorEngineWriteDbContext(actorDbOptions);

            this.ActorDb.Database.EnsureCreated();

            // actor system configuration
            var actorSystemConfig = ActorSystemConfig
                .Setup();

            // remote configuration
            var remoteConfig = GrpcNetRemoteConfig
                .BindToLocalhost();

            // cluster configuration
            var clusterConfig = ClusterConfig
                .Setup(
                    clusterName: "UnitTest",
                    clusterProvider: new TestProvider(new TestProviderOptions(), new InMemAgent()),
                    identityLookup: new PartitionIdentityLookup())
                .WithClusterKind(
                        kind: DataVariableGrainActor.Kind,
                        prop: Props.FromProducer(() =>
                            new DataVariableGrainActor(
                                (context, clusterIdentity) => new DataVariableGrain(context, clusterIdentity))))
                                .WithClusterKind(
                        kind: DataTableGrainActor.Kind,
                        prop: Props.FromProducer(() =>
                            new DataTableGrainActor(
                                (context, clusterIdentity) => new DataTableGrain(context, clusterIdentity))))
                                .WithClusterKind(
                        kind: SessionGrainActor.Kind,
                        prop: Props.FromProducer(() =>
                            new SessionGrainActor(
                                (context, clusterIdentity) => new SessionGrain(context, clusterIdentity))))
                                .WithClusterKind(
                        kind: ExpressionGrainActor.Kind,
                        prop: Props.FromProducer(() =>
                            new ExpressionGrainActor(
                                (context, clusterIdentity) => new ExpressionGrain(context, clusterIdentity))))
                                .WithClusterKind(
                        kind: SessionLoggerGrainActor.Kind,
                        prop: Props.FromProducer(() =>
                            new SessionLoggerGrainActor(
                                (context, clusterIdentity) => new SessionLoggerGrain(context, clusterIdentity))))
                                .WithClusterKind(
                        kind: SessionStateGrainActor.Kind,
                        prop: Props.FromProducer(() =>
                            new SessionStateGrainActor(
                                (context, clusterIdentity) => new SessionStateGrain(context, clusterIdentity))));

#pragma warning disable CA2000 //we dispose of the actor system later in the DisposeAsync method

            // create the actor system
            this.ActorSystem = new ActorSystem(actorSystemConfig)
                .WithRemote(remoteConfig)
                .WithCluster(clusterConfig);
#pragma warning restore CA2000
        }

        protected ActorSystem ActorSystem { get; set; }

        protected BlueprintsQueryDbContext BlueprintsDb { get; set; }

        protected ActorEngineWriteDbContext ActorDb { get; set; }

        public ValueTask DisposeAsync()
        {
            this.BlueprintsDb.Database.EnsureDeletedAsync();
            this.ActorDb.Database.EnsureDeletedAsync();
            GC.SuppressFinalize(this);
            this.BlueprintsDb.Dispose();
            this.ActorDb.Dispose();
            this.ActorSystem.ShutdownAsync();
            return this.ActorSystem.DisposeAsync();
        }
    }
}
