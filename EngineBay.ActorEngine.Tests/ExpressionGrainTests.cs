namespace EngineApi.Tests
{
    using System.Threading.Tasks;
    using EngineBay.ActorEngine;
    using EngineBay.Blueprints;
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
            ;

            var sessionId = Guid.NewGuid().ToString();
            var workbookId = Guid.NewGuid().ToString();

            await this.ActorSystem.Cluster().GetSessionLoggerGrain(sessionId).Start(
                new SessionStartRequest
                {
                    SessionId = sessionId,
                    LogLevel = (int)LogLevel.Trace,
                }, CancellationToken.None);

            var dataVariableIdentity = Guid.NewGuid().ToString();

            var dataVariableGrain = this.ActorSystem
                    .Cluster()
                    .GetDataVariableGrain(dataVariableIdentity);

            await dataVariableGrain.UseSessionLogger(
                        new SessionLoggerRequest
                        {
                            SessionId = sessionId.ToString(),
                        },
                        CancellationToken.None);

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
                    ;

            var expressionGrainIdentity = Guid.NewGuid().ToString();

            var expressionGrain = this.ActorSystem
                    .Cluster()
                    .GetExpressionGrain(expressionGrainIdentity);

            await expressionGrain.UseSessionLogger(
                        new SessionLoggerRequest
                        {
                            SessionId = sessionId.ToString(),
                        },
                        CancellationToken.None);

            await expressionGrain
                    .UseExpression(
                        new UseExpressionRequest
                        {
                            Expression = "3 * 2",
                        }, CancellationToken.None)
                    ;

            await expressionGrain
                    .OutputTo(
                        new DataVariableDependantMsg
                        {
                            Identity = dataVariableIdentity,
                            Name = "Result",
                            Namespace = "Global",
                            Type = DataVariableTypes.FLOAT,
                        }, CancellationToken.None)
                    ;

            await expressionGrain
                    .Evaluate(CancellationToken.None)
                    ;

            var result = await dataVariableGrain
                    .GetValue(CancellationToken.None)
                    ;

            Assert.NotNull(result);
            Assert.Equal("6", result.Value);
        }
    }
}
