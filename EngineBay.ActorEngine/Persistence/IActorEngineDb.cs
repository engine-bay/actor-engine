namespace EngineBay.ActorEngine
{
    using EngineBay.Persistence;
    using Microsoft.EntityFrameworkCore;

    public interface IActorEngineDb : IEngineDb
    {
        DbSet<SessionLog> SessionLogs { get; set; }

        DbSet<DataVariableState> DataVariableStates { get; set; }
    }
}