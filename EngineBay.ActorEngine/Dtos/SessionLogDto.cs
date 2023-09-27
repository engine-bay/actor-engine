namespace EngineBay.ActorEngine
{
    using Microsoft.AspNetCore.DataProtection;

    public class SessionLogDto
    {
        public SessionLogDto(SessionLog sessionLog, IDataProtectionProvider dataProtectionProvider)
        {
            if (sessionLog is null)
            {
                throw new ArgumentNullException(nameof(sessionLog));
            }

            this.Message = sessionLog.GetMessage(dataProtectionProvider);
        }

        public string? Message { get; set; }
    }
}