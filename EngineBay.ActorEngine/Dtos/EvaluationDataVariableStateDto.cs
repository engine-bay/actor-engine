namespace EngineBay.ActorEngine
{
    public class EvaluationDataVariableStateDto
    {
        public EvaluationDataVariableStateDto(DataVariableState dataVariableState)
        {
            if (dataVariableState is null)
            {
                throw new ArgumentNullException(nameof(dataVariableState));
            }

            this.Name = dataVariableState.Name;
            this.Namespace = dataVariableState.Namespace;
            this.Type = dataVariableState.Type;
            this.Value = dataVariableState.Value;
        }

        public string? Name { get; set; }

        public string? Namespace { get; set; }

        public string? Type { get; set; }

        public string? Value { get; set; }
    }
}