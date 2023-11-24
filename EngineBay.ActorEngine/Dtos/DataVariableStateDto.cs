namespace EngineBay.ActorEngine
{
    using Microsoft.AspNetCore.DataProtection;

    public class DataVariableStateDto
    {
        public DataVariableStateDto(DataVariableState dataVariableState, IDataProtectionProvider dataProtectionProvider)
        {
            ArgumentNullException.ThrowIfNull(dataVariableState);

            this.Id = dataVariableState.Id;
            this.Identity = dataVariableState.Identity;
            this.SessionId = dataVariableState.SessionId;
            this.Name = dataVariableState.Name;
            this.Type = dataVariableState.Type;
            this.Value = dataVariableState.GetValue(dataProtectionProvider);
        }

        public Guid Id { get; set; }

        public Guid Identity { get; set; }

        public Guid SessionId { get; set; }

        public string? Name { get; set; }

        public string? Type { get; set; }

        public string? Value { get; set; }
    }
}