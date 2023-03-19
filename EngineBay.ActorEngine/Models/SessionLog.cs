namespace EngineBay.ActorEngine
{
    using System;
    using EngineBay.Core;
    using Humanizer;
    using Microsoft.EntityFrameworkCore;

    public class SessionLog : BaseModel
    {
        public SessionLog(SessionLogMsg sessionLogMsg)
        {
            if (sessionLogMsg is null)
            {
                throw new ArgumentNullException(nameof(sessionLogMsg));
            }

            this.SessionId = new Guid(sessionLogMsg.SessionId);
            this.Message = sessionLogMsg.Message;
            this.LogLevel = (LogLevel)sessionLogMsg.LogLevel;
        }

        public SessionLog()
        {
        }

        public Guid SessionId { get; set; }

        public string? Message { get; set; }

        public LogLevel? LogLevel { get; set; }

        public static new void CreateDataAnnotations(ModelBuilder modelBuilder)
        {
            if (modelBuilder is null)
            {
                throw new ArgumentNullException(nameof(modelBuilder));
            }

            modelBuilder.Entity<SessionLog>().ToTable(typeof(SessionLog).Name.Pluralize());

            modelBuilder.Entity<SessionLog>().HasKey(x => x.Id);

            modelBuilder.Entity<SessionLog>().Property(x => x.CreatedAt).IsRequired();

            modelBuilder.Entity<SessionLog>().Property(x => x.LastUpdatedAt).IsRequired();

            modelBuilder.Entity<SessionLog>().Property(x => x.Message).IsRequired();

            modelBuilder.Entity<SessionLog>().Property(x => x.LogLevel).IsRequired();

            modelBuilder.Entity<SessionLog>().HasIndex(x => x.LogLevel);

            modelBuilder.Entity<SessionLog>().HasIndex(x => x.SessionId);
        }
    }
}