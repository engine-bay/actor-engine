namespace EngineBay.ActorEngine
{
    using EngineBay.Core;
    using EngineBay.Persistence;
    using FluentValidation;
    using Proto.Remote.HealthChecks;

    public class ActorEngineModule : IModule
    {
        public IServiceCollection RegisterModule(IServiceCollection services, IConfiguration configuration)
        {
            // Register commands
            services.AddTransient<RunEvaluation>();

            // Register validators
            services.AddTransient<IValidator<CreateEvaluationDto>, CreateEvaluationDtoValidator>();
            services.AddTransient<IValidator<EvaluationDataVariableDto>, EvaluationDataVariableDtoValidator>();

            // register persistence services
            var databaseConfiguration = new CQRSDatabaseConfiguration<ActorEngineDb, ActorEngineQueryDb, ActorEngineWriteDb>();
            databaseConfiguration.RegisterDatabases(services);

            // Register an actor system
            services.AddActorSystem(configuration);

            // start the actor system
            services.AddHostedService<ActorSystemClusterHostedService>();

            services.AddHealthChecks().AddCheck<ActorSystemHealthCheck>("actor-system");

            return services;
        }

        public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapPost("/evaluations", async (RunEvaluation command, CreateEvaluationDto createEvaluationDto, CancellationToken cancellation) =>
            {
                var dto = await command.Handle(createEvaluationDto, cancellation).ConfigureAwait(false);
                return Results.Ok(dto);
            });

            return endpoints;
        }
    }
}