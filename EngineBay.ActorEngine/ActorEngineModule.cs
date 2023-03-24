namespace EngineBay.ActorEngine
{
    using System.Security.Claims;
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
            var databaseConfiguration = new CQRSDatabaseConfiguration<ActorEngineDbContext, ActorEngineQueryDbContext, ActorEngineWriteDbContext>();
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
            endpoints.MapPost("/evaluations", async (RunEvaluation command, CreateEvaluationDto createEvaluationDto, ClaimsPrincipal claimsPrincipal, CancellationToken cancellation) =>
            {
                var dto = await command.Handle(createEvaluationDto, claimsPrincipal, cancellation).ConfigureAwait(false);
                return Results.Ok(dto);
            }).RequireAuthorization();

            return endpoints;
        }
    }
}