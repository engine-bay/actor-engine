namespace EngineBay.ActorEngine
{
    using EngineBay.Persistence;
    using Microsoft.EntityFrameworkCore;

    public class ActorEngineWriteDb : ActorEngineDb
    {
        public ActorEngineWriteDb(DbContextOptions<EngineWriteDb> options)
            : base(options)
        {
        }
    }
}