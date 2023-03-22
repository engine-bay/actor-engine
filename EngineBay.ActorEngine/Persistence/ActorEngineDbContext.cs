namespace EngineBay.ActorEngine
{
    using EngineBay.Persistence;
    using Microsoft.EntityFrameworkCore;

    public class ActorEngineDbContext : ModuleWriteDbContext
    {
        public ActorEngineDbContext(DbContextOptions<ModuleWriteDbContext> options)
            : base(options)
        {
        }

        public DbSet<SessionLog> SessionLogs { get; set; } = null!;

        public DbSet<DataVariableState> DataVariableStates { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            SessionLog.CreateDataAnnotations(modelBuilder);
            DataVariableState.CreateDataAnnotations(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }
    }
}