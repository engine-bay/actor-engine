namespace EngineBay.ActorEngine
{
    using System;
    using EngineBay.Core;
    using Humanizer;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.EntityFrameworkCore;

    public class DataVariableState : BaseModel
    {
        public DataVariableState(DataVariableStateMsg dataVariableStateMsg, IDataProtectionProvider dataProtectionProvider)
        {
            ArgumentNullException.ThrowIfNull(dataVariableStateMsg);

            this.Identity = new Guid(dataVariableStateMsg.Identity);
            this.SessionId = new Guid(dataVariableStateMsg.SessionId);
            this.Name = dataVariableStateMsg.Name;
            this.Namespace = dataVariableStateMsg.Namespace;
            this.Type = dataVariableStateMsg.Type;
            this.SetValue(dataVariableStateMsg.Value, dataProtectionProvider);
        }

        public DataVariableState()
        {
        }

        public Guid Identity { get; set; }

        public Guid SessionId { get; set; }

        public string? Name { get; set; }

        public string? Namespace { get; set; }

        public string? Type { get; set; }

        public string? EncryptedValue { get; set; }

        public static new void CreateDataAnnotations(ModelBuilder modelBuilder)
        {
            ArgumentNullException.ThrowIfNull(modelBuilder);

            modelBuilder.Entity<DataVariableState>().ToTable(typeof(DataVariableState).Name.Pluralize());

            modelBuilder.Entity<DataVariableState>().HasKey(x => x.Id);

            modelBuilder.Entity<DataVariableState>().Property(x => x.CreatedAt).IsRequired();

            modelBuilder.Entity<DataVariableState>().Property(x => x.LastUpdatedAt).IsRequired();

            modelBuilder.Entity<DataVariableState>().HasIndex(x => x.Identity);

            modelBuilder.Entity<DataVariableState>().HasIndex(x => x.SessionId);

            modelBuilder.Entity<DataVariableState>().HasIndex(x => x.Namespace);

            modelBuilder.Entity<DataVariableState>().HasIndex(x => new { x.Name, x.Namespace, x.Type, x.SessionId, x.CreatedAt }).IsUnique();
        }

        public string GetValue(IDataProtectionProvider dataProtectionProvider)
        {
            ArgumentNullException.ThrowIfNull(dataProtectionProvider);

            if (this.EncryptedValue is null)
            {
                throw new InvalidOperationException(nameof(this.EncryptedValue));
            }

            var dataProtector = dataProtectionProvider.CreateProtector(ProtectedDataConstants.DataVariableStateValue);

            return DataProtectionCommonExtensions.Unprotect(dataProtector, this.EncryptedValue);
        }

        public void SetValue(string? value, IDataProtectionProvider dataProtectionProvider)
        {
            if (value is null)
            {
                return;
            }

            ArgumentNullException.ThrowIfNull(dataProtectionProvider);

            var dataProtector = dataProtectionProvider.CreateProtector(ProtectedDataConstants.DataVariableStateValue);

            this.EncryptedValue = DataProtectionCommonExtensions.Protect(dataProtector, value);
        }
    }
}