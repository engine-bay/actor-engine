namespace EngineApi.Tests
{
    using System.Threading.Tasks;
    using EngineBay.ActorEngine;
    using EngineBay.Core;
    using Microsoft.Extensions.Logging;
    using Proto.Cluster;
    using Xunit;

    public class ExpressionGrainTests : BaseActorSystemTest
    {
        public ExpressionGrainTests()
            : base()
        {
        }

        [Fact]
        public async Task CanEvaluateSomeMaths()
        {
            await this.ActorSystem
            .Cluster()
            .StartMemberAsync()
            .ConfigureAwait(false);

            var sessionId = Guid.NewGuid().ToString();
            var workbookId = Guid.NewGuid().ToString();

            await this.ActorSystem.Cluster().GetSessionLoggerGrain(sessionId).Start(
                new SessionStartRequest
                {
                    SessionId = sessionId,
                    LogLevel = (int)LogLevel.Trace,
                }, CancellationToken.None).ConfigureAwait(false);

            var dataVariableIdentity = Guid.NewGuid().ToString();

            var dataVariableGrain = this.ActorSystem
                    .Cluster()
                    .GetDataVariableGrain(dataVariableIdentity);

            await dataVariableGrain.UseSessionLogger(
                        new SessionLoggerRequest
                        {
                            SessionId = sessionId.ToString(),
                        },
                        CancellationToken.None).ConfigureAwait(false);

            await dataVariableGrain
                    .UpdateIdentity(
                        new DataVariableIdentity
                        {
                            Identity = dataVariableIdentity,
                            Name = "Result",
                            Namespace = "Global",
                            SessionId = sessionId,
                            Type = DataVariableTypes.FLOAT,
                        }, CancellationToken.None)
                    .ConfigureAwait(false);

            var expressionGrainIdentity = Guid.NewGuid().ToString();

            var expressionGrain = this.ActorSystem
                    .Cluster()
                    .GetExpressionGrain(expressionGrainIdentity);

            await expressionGrain.UseSessionLogger(
                        new SessionLoggerRequest
                        {
                            SessionId = sessionId.ToString(),
                        },
                        CancellationToken.None).ConfigureAwait(false);

            await expressionGrain
                    .UseExpression(
                        new UseExpressionRequest
                        {
                            Expression = "3 * 2",
                        }, CancellationToken.None)
                    .ConfigureAwait(false);

            await expressionGrain
                    .OutputTo(
                        new DataVariableDependantMsg
                        {
                            Identity = dataVariableIdentity,
                            Name = "Result",
                            Namespace = "Global",
                            Type = DataVariableTypes.FLOAT,
                        }, CancellationToken.None)
                    .ConfigureAwait(false);

            await expressionGrain
                    .Evaluate(CancellationToken.None)
                    .ConfigureAwait(false);

            var result = await dataVariableGrain
                    .GetValue(CancellationToken.None)
                    .ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal("6", result.Value);
        }
    }
}
