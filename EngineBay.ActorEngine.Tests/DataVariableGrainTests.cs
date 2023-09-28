namespace EngineApi.Tests
{
    using System.Threading.Tasks;
    using EngineBay.ActorEngine;
    using Microsoft.Extensions.Logging;
    using Proto.Cluster;
    using Xunit;

    public class DataVariableGrainTests : BaseActorSystemTest
    {
        public DataVariableGrainTests()
            : base()
        {
        }

        [Fact]
        public async Task DefaultsToValueOfAnEmptyString()
        {
            await this.ActorSystem
            .Cluster()
            .StartMemberAsync()
            ;

            var sessionId = Guid.NewGuid().ToString();
            var workbookId = Guid.NewGuid().ToString();

            await this.ActorSystem.Cluster().GetSessionLoggerGrain(sessionId).Start(
                new SessionStartRequest
                {
                    SessionId = sessionId,
                    LogLevel = (int)LogLevel.Trace,
                }, CancellationToken.None);

            var dataVariableGrain = this.ActorSystem
                    .Cluster()
                    .GetDataVariableGrain("test");

            await dataVariableGrain.UseSessionLogger(
                        new SessionLoggerRequest
                        {
                            SessionId = sessionId.ToString(),
                        },
                        CancellationToken.None);

            var result = await dataVariableGrain.GetValue(CancellationToken.None)
                    ;

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.Value);
        }
    }
}
