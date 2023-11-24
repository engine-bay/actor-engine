namespace EngineBay.ActorEngine
{
    using EngineBay.Authentication;
    using EngineBay.Blueprints;
    using EngineBay.Core;
    using FluentValidation;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.EntityFrameworkCore;
    using Proto;
    using Proto.Cluster;

    public class RunEvaluation : ICommandHandler<CreateEvaluationDto, EvaluationResultDto>
    {
        private readonly ActorEngineWriteDbContext actorDb;
        private readonly BlueprintsQueryDbContext blueprintsDb;
        private readonly ActorSystem actorSystem;
        private readonly IValidator<CreateEvaluationDto> validator;
        private readonly IDataProtectionProvider dataProtectionProvider;
        private readonly GetApplicationUser getApplicationUserQuery;

        public RunEvaluation(
            GetApplicationUser getApplicationUserQuery,
            ActorEngineWriteDbContext actorDb,
            BlueprintsQueryDbContext blueprintsDb,
            ActorSystem actorSystem,
            IValidator<CreateEvaluationDto> validator,
            IDataProtectionProvider dataProtectionProvider)
        {
            this.getApplicationUserQuery = getApplicationUserQuery;
            this.actorDb = actorDb;
            this.blueprintsDb = blueprintsDb;
            this.actorSystem = actorSystem;
            this.validator = validator;
            this.dataProtectionProvider = dataProtectionProvider;
        }

        public async Task<EvaluationResultDto> Handle(CreateEvaluationDto createEvaluationDto, CancellationToken cancellation)
        {
            ArgumentNullException.ThrowIfNull(createEvaluationDto);

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
                ;

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
                    }, cancellation);

            // Set the  session's variables and then things calculate
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
                    }, cancellation);
                }
            }

            var sessionLogsResponse = await sessionGrain.GetLogs(cancellation);

            if (sessionLogsResponse is null)
            {
                throw new ArgumentException(nameof(sessionLogsResponse));
            }

            var sessionLogs = sessionLogsResponse.Sessionlogs.Select(sessionLogMsg => new SessionLog(sessionLogMsg, this.dataProtectionProvider));

            this.actorDb.SessionLogs.AddRange(sessionLogs);

            var sessionStateResponse = await sessionGrain.GetState(cancellation);

            if (sessionStateResponse is null)
            {
                throw new ArgumentException(nameof(sessionStateResponse));
            }

            var dataVariableStates = sessionStateResponse.DataVariableStates.Select(dataVariableStateMsg => new DataVariableState(dataVariableStateMsg, this.dataProtectionProvider));

            this.actorDb.DataVariableStates.AddRange(dataVariableStates);

            await this.actorDb.SaveChangesAsync(cancellation);

            await sessionGrain.Stop(cancellation);

            return new EvaluationResultDto
            {
                Id = sessionId,
                WorkbookId = workbookId,
                DataVariables = dataVariableStates.Select(dataVariableState => new EvaluationDataVariableStateDto(dataVariableState, this.dataProtectionProvider)),
                Logs = sessionLogs.Select(sessionLog => new SessionLogDto(sessionLog, this.dataProtectionProvider)),
            };
        }
    }
}