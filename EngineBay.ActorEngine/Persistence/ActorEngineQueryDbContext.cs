namespace EngineBay.ActorEngine
{
    using EngineBay.Persistence;
    using Microsoft.EntityFrameworkCore;

    public class ActorEngineQueryDbContext : ActorEngineDbContext
    {
        public ActorEngineQueryDbContext(DbContextOptions<ModuleWriteDbContext> options)
            : base(options)
        {
        }
    }
}