namespace EngineBay.ActorEngine
{
    using Microsoft.AspNetCore.DataProtection;

    public class SessionLogDto
    {
        public SessionLogDto(SessionLog sessionLog, IDataProtectionProvider dataProtectionProvider)
        {
            ArgumentNullException.ThrowIfNull(sessionLog);

            this.Message = sessionLog.GetMessage(dataProtectionProvider);
        }

        public string? Message { get; set; }
    }
}