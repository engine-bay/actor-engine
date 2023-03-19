namespace EngineBay.ActorEngine
{
    public class EvaluationResultDto
    {
        public Guid Id { get; set; }

        public Guid WorkbookId { get; set; }

        public LogLevel LogLevel { get; set; } = LogLevel.Warning;

        public IEnumerable<EvaluationDataVariableStateDto>? DataVariables { get; set; }

        public IEnumerable<SessionLogDto>? Logs { get; set; }
    }
}