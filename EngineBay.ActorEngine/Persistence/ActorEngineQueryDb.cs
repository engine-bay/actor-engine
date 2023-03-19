namespace EngineBay.ActorEngine
{
    using EngineBay.Persistence;
    using Microsoft.EntityFrameworkCore;

    public class ActorEngineQueryDb : ActorEngineDb
    {
        public ActorEngineQueryDb(DbContextOptions<EngineWriteDb> options)
            : base(options)
        {
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException($"Tried to save changes on a read only db context {nameof(ActorEngineQueryDb)}");
        }
    }
}