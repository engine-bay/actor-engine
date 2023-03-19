namespace EngineBay.ActorEngine
{
    using Proto;
    using Proto.Cluster;
    using Proto.Cluster.Kubernetes;
    using Proto.Cluster.PartitionActivator;
    using Proto.Cluster.Testing;
    using Proto.DependencyInjection;
    using Proto.Remote;
    using Proto.Remote.GrpcNet;

    public static class ActorSystemConfiguration
    {
        public static void AddActorSystem(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddSingleton(provider =>
            {
                // actor system configuration
                var actorSystemConfig = ActorSystemConfig
                    .Setup()
                    .WithMetrics();

                // remote configuration
                var remoteConfig = GrpcNetRemoteConfig
                    .BindToAllInterfaces(advertisedHost: configuration["ProtoActor:AdvertisedHost"])
                    .WithProtoMessages(MessagesReflection.Descriptor)
                    .WithRemoteDiagnostics(true);

                var isKubernetesCluster = Environment.GetEnvironmentVariable("KUBERNETES_CLUSTER") == "True";

                IClusterProvider clusterProvider = isKubernetesCluster ? new KubernetesProvider() : new TestProvider(new TestProviderOptions(), new InMemAgent());

                // TODO clean up actor registration
                // TODO add test/CI environment check for using an in memory provider rather than k8s provider
                // cluster configuration
                var clusterConfig = ClusterConfig
                    .Setup(
                        clusterName: "EngineBay.ActorEngine",
                        clusterProvider: clusterProvider,
                        identityLookup: new PartitionActivatorLookup())

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

                // create the actor system
                return new ActorSystem(actorSystemConfig)
                    .WithServiceProvider(provider)
                    .WithRemote(remoteConfig)
                    .WithCluster(clusterConfig);
            });
        }
    }
}
