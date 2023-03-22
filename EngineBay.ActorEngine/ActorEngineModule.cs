namespace EngineBay.ActorEngine
{
    using EngineBay.Core;
    using EngineBay.Persistence;
    using FluentValidation;
    using Microsoft.AspNetCore.Identity;
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
            endpoints.MapPost("/evaluations", async (UserManager<ApplicationUser> userManager, HttpContext httpContext, RunEvaluation command, CreateEvaluationDto createEvaluationDto, CancellationToken cancellation) =>
            {
                var user = await userManager.GetUserAsync(httpContext.User).ConfigureAwait(false);

                if (user is null)
                {
                    return Results.Unauthorized();
                }

                var dto = await command.Handle(createEvaluationDto, user, cancellation).ConfigureAwait(false);
                return Results.Ok(dto);
            });

            return endpoints;
        }
    }
}