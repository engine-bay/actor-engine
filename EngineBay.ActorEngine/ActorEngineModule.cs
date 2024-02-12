namespace EngineBay.ActorEngine
{
    using System;
    using EngineBay.Core;
    using EngineBay.Persistence;
    using FluentValidation;
    using Proto;
    using Proto.Remote.HealthChecks;

    public class ActorEngineModule : BaseModule, IDatabaseModule
    {
        public override IServiceCollection RegisterModule(IServiceCollection services, IConfiguration configuration)
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

        public override RouteGroupBuilder MapEndpoints(RouteGroupBuilder endpoints)
        {
            endpoints.MapPost("/evaluations", async (RunEvaluation command, CreateEvaluationDto createEvaluationDto, CancellationToken cancellation) =>
            {
                var dto = await command.Handle(createEvaluationDto, cancellation);
                return Results.Ok(dto);
            }).RequireAuthorization();

            return endpoints;
        }

        public override WebApplication AddMiddleware(WebApplication app)
        {
            ArgumentNullException.ThrowIfNull(app);

            var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
            Log.SetLoggerFactory(loggerFactory);

            return app;
        }

        public IReadOnlyCollection<IModuleDbContext> GetRegisteredDbContexts(IDbContextOptionsFactory dbContextOptionsFactory)
        {
            ArgumentNullException.ThrowIfNull(dbContextOptionsFactory);
            return new IModuleDbContext[] { new ActorEngineDbContext(dbContextOptionsFactory.GetDbContextOptions<ModuleWriteDbContext>()) };
        }
    }
}