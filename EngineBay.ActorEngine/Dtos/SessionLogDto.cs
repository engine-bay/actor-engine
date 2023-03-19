namespace EngineBay.ActorEngine
{
    public class SessionLogDto
    {
        public SessionLogDto(SessionLog sessionLog)
        {
            if (sessionLog is null)
            {
                throw new ArgumentNullException(nameof(sessionLog));
            }

            this.Message = sessionLog.Message;
        }

        public string? Message { get; set; }
    }
}