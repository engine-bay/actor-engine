namespace EngineBay.ActorEngine
{
    using System;
    using EngineBay.Core;
    using Humanizer;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.EntityFrameworkCore;

    public class SessionLog : BaseModel
    {
        public SessionLog(SessionLogMsg sessionLogMsg, IDataProtectionProvider dataProtectionProvider)
        {
            ArgumentNullException.ThrowIfNull(sessionLogMsg);

            this.SessionId = new Guid(sessionLogMsg.SessionId);
            this.LogLevel = (LogLevel)sessionLogMsg.LogLevel;
            this.SetMessage(sessionLogMsg.Message, dataProtectionProvider);
        }

        public SessionLog()
        {
        }

        public Guid SessionId { get; set; }

        public string? EncryptedMessage { get; set; }

        public LogLevel? LogLevel { get; set; }

        public static new void CreateDataAnnotations(ModelBuilder modelBuilder)
        {
            ArgumentNullException.ThrowIfNull(modelBuilder);

            modelBuilder.Entity<SessionLog>().ToTable(typeof(SessionLog).Name.Pluralize());

            modelBuilder.Entity<SessionLog>().HasKey(x => x.Id);

            modelBuilder.Entity<SessionLog>().Property(x => x.CreatedAt).IsRequired();

            modelBuilder.Entity<SessionLog>().Property(x => x.LastUpdatedAt).IsRequired();

            modelBuilder.Entity<SessionLog>().Property(x => x.EncryptedMessage).IsRequired();

            modelBuilder.Entity<SessionLog>().Property(x => x.LogLevel).IsRequired();

            modelBuilder.Entity<SessionLog>().HasIndex(x => x.LogLevel);

            modelBuilder.Entity<SessionLog>().HasIndex(x => x.SessionId);
        }

        public string GetMessage(IDataProtectionProvider dataProtectionProvider)
        {
            ArgumentNullException.ThrowIfNull(dataProtectionProvider);

            if (this.EncryptedMessage is null)
            {
                throw new InvalidOperationException(nameof(this.EncryptedMessage));
            }

            var dataProtector = dataProtectionProvider.CreateProtector(ProtectedDataConstants.SessionLogMessage);

            return DataProtectionCommonExtensions.Unprotect(dataProtector, this.EncryptedMessage);
        }

        public void SetMessage(string? value, IDataProtectionProvider dataProtectionProvider)
        {
            if (value is null)
            {
                return;
            }

            ArgumentNullException.ThrowIfNull(dataProtectionProvider);

            var dataProtector = dataProtectionProvider.CreateProtector(ProtectedDataConstants.SessionLogMessage);

            this.EncryptedMessage = DataProtectionCommonExtensions.Protect(dataProtector, value);
        }
    }
}