namespace EngineBay.ActorEngine
{
    using Microsoft.AspNetCore.DataProtection;

    public class EvaluationDataVariableStateDto
    {
        public EvaluationDataVariableStateDto(DataVariableState dataVariableState, IDataProtectionProvider dataProtectionProvider)
        {
            ArgumentNullException.ThrowIfNull(dataVariableState);

            this.Name = dataVariableState.Name;
            this.Namespace = dataVariableState.Namespace;
            this.Type = dataVariableState.Type;
            this.Value = dataVariableState.GetValue(dataProtectionProvider);
        }

        public string? Name { get; set; }

        public string? Namespace { get; set; }

        public string? Type { get; set; }

        public string? Value { get; set; }
    }
}