namespace EngineBay.ActorEngine
{
    using EngineBay.Persistence;
    using Microsoft.EntityFrameworkCore;

    public class ActorEngineDb : EngineWriteDb, IEngineWriteDb
    {
        public ActorEngineDb(DbContextOptions<EngineWriteDb> options)
            : base(options)
        {
        }

        public DbSet<SessionLog> SessionLogs { get; set; } = null!;

        public DbSet<DataVariableState> DataVariableStates { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            SessionLog.CreateDataAnnotations(modelBuilder);
            DataVariableState.CreateDataAnnotations(modelBuilder);
        }
    }
}