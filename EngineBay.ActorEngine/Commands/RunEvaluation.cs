namespace EngineBay.ActorEngine
{
    using System.Security.Claims;
    using EngineBay.Authentication;
    using EngineBay.Blueprints;
    using EngineBay.Core;
    using EngineBay.Persistence;
    using FluentValidation;
    using Microsoft.EntityFrameworkCore;
    using Proto;
    using Proto.Cluster;

    public class RunEvaluation : ICommandHandler<CreateEvaluationDto, EvaluationResultDto>
    {
        private readonly ActorEngineWriteDbContext actorDb;
        private readonly BlueprintsQueryDbContext blueprintsDb;
        private readonly ActorSystem actorSystem;
        private readonly IValidator<CreateEvaluationDto> validator;

        private readonly GetApplicationUser getApplicationUserQuery;

        public RunEvaluation(GetApplicationUser getApplicationUserQuery, ActorEngineWriteDbContext actorDb, BlueprintsQueryDbContext blueprintsDb, ActorSystem actorSystem, IValidator<CreateEvaluationDto> validator)
        {
            this.getApplicationUserQuery = getApplicationUserQuery;
            this.actorDb = actorDb;
            this.blueprintsDb = blueprintsDb;
            this.actorSystem = actorSystem;
            this.validator = validator;
        }

        public async Task<EvaluationResultDto> Handle(CreateEvaluationDto createEvaluationDto, ClaimsPrincipal claimsPrincipal, CancellationToken cancellation)
        {
            var user = await this.getApplicationUserQuery.Handle(claimsPrincipal, cancellation).ConfigureAwait(false);

            if (createEvaluationDto is null)
            {
                throw new ArgumentNullException(nameof(createEvaluationDto));
            }

            this.validator.ValidateAndThrow(createEvaluationDto);

            var sessionId = Guid.NewGuid();
            var identity = sessionId.ToString();
            var workbookId = createEvaluationDto.WorkbookId;

            var workbook = await this.blueprintsDb.Workbooks
                .Include(x => x.Blueprints)
                    .ThenInclude(blueprint => blueprint.ExpressionBlueprints)
                        .ThenInclude(expressionBlueprint => expressionBlueprint.InputDataTableBlueprints)
                .Include(x => x.Blueprints)
                    .ThenInclude(blueprint => blueprint.ExpressionBlueprints)
                        .ThenInclude(expressionBlueprint => expressionBlueprint.InputDataVariableBlueprints)
                .Include(x => x.Blueprints)
                    .ThenInclude(blueprint => blueprint.ExpressionBlueprints)
                        .ThenInclude(expressionBlueprint => expressionBlueprint.OutputDataVariableBlueprint)
                .Include(x => x.Blueprints)
                    .ThenInclude(blueprint => blueprint.DataVariableBlueprints)
                .Include(x => x.Blueprints)
                    .ThenInclude(x => x.TriggerBlueprints)
                        .ThenInclude(x => x.TriggerExpressionBlueprints)
                            .ThenInclude(x => x.InputDataVariableBlueprint)
                .Include(x => x.Blueprints)
                    .ThenInclude(x => x.TriggerBlueprints)
                        .ThenInclude(x => x.OutputDataVariableBlueprint)
                .Include(x => x.Blueprints)
                        .ThenInclude(x => x.DataTableBlueprints)
                            .ThenInclude(x => x.InputDataVariableBlueprints)
                .Include(x => x.Blueprints)
                    .ThenInclude(x => x.DataTableBlueprints)
                        .ThenInclude(x => x.DataTableColumnBlueprints)
                .Include(x => x.Blueprints)
                    .ThenInclude(x => x.DataTableBlueprints)
                        .ThenInclude(x => x.DataTableRowBlueprints)
                            .ThenInclude(x => x.DataTableCellBlueprints)
                .FirstOrDefaultAsync(x => x.Id == createEvaluationDto.WorkbookId, cancellation)
                .ConfigureAwait(false);

            if (workbook is null)
            {
                throw new ArgumentException(nameof(workbook));
            }

            var workbookMsg = WorkbookToWorkbookMsgMapper.Map(workbook);

            var sessionGrain = this.actorSystem
                .Cluster()
                .GetSessionGrain(identity);

            // Start the session with the workbook
            await sessionGrain.Start(
                    new SessionStartRequest
                    {
                        SessionId = identity,
                        LogLevel = (int)createEvaluationDto.LogLevel,
                        Workbook = workbookMsg,
                    }, cancellation).ConfigureAwait(false);

            // Set the  session's varaibles and then things calculate
            if (createEvaluationDto.DataVariables is not null)
            {
                foreach (var dataVariable in createEvaluationDto.DataVariables)
                {
                    await sessionGrain.UpdateDataVariable(
                    new DataVariable
                    {
                        Name = dataVariable.Name,
                        Value = dataVariable.Value,
                        Namespace = dataVariable.Namespace,
                    }, cancellation).ConfigureAwait(false);
                }
            }

            var sessionLogsResponse = await sessionGrain.GetLogs(cancellation).ConfigureAwait(false);

            if (sessionLogsResponse is null)
            {
                throw new ArgumentException(nameof(sessionLogsResponse));
            }

            var sessionLogs = sessionLogsResponse.Sessionlogs.Select(sessionLogMsg => new SessionLog(sessionLogMsg));

            this.actorDb.SessionLogs.AddRange(sessionLogs);

            var sessionStateResponse = await sessionGrain.GetState(cancellation).ConfigureAwait(false);

            if (sessionStateResponse is null)
            {
                throw new ArgumentException(nameof(sessionStateResponse));
            }

            var dataVariableStates = sessionStateResponse.DataVariableStates.Select(dataVariableStateMsg => new DataVariableState(dataVariableStateMsg));

            this.actorDb.DataVariableStates.AddRange(dataVariableStates);

            await this.actorDb.SaveChangesAsync(user, cancellation).ConfigureAwait(false);

            await sessionGrain.Stop(cancellation).ConfigureAwait(false);

            return new EvaluationResultDto
            {
                Id = sessionId,
                WorkbookId = workbookId,
                DataVariables = dataVariableStates.Select(dataVariableState => new EvaluationDataVariableStateDto(dataVariableState)),
                Logs = sessionLogs.Select(sessionLog => new SessionLogDto(sessionLog)),
            };
        }
    }
}