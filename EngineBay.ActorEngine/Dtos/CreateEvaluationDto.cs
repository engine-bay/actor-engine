namespace EngineBay.ActorEngine
{
    public class CreateEvaluationDto
    {
        public Guid WorkbookId { get; set; }

        public LogLevel LogLevel { get; set; } = LogLevel.Warning;

        public IEnumerable<EvaluationDataVariableDto>? DataVariables { get; set; }
    }
}