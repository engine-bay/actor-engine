namespace EngineBay.ActorEngine
{
    using System;
    using System.Threading.Tasks;
    using Proto;
    using Proto.Cluster;

    public class SessionStateGrain : SessionStateGrainBase
    {
        private readonly ClusterIdentity clusterIdentity;

        private Guid? sessionId;

        private SessionLoggerGrainClient? logger;

        private Dictionary<string, DataVariableStateMsg> dataVariableStates;

        public SessionStateGrain(IContext context, ClusterIdentity clusterIdentity)
            : base(context)
        {
            this.clusterIdentity = clusterIdentity;
            this.dataVariableStates = new Dictionary<string, DataVariableStateMsg>();
        }

        public override async Task Start(SessionStartRequest request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            this.sessionId = new Guid(request.SessionId);

            this.logger = this.Context.GetSessionLoggerGrain(request.SessionId.ToString());

            await this.logger.Start(request, CancellationToken.None);

            await this.logger.Trace(
                new SessionLogLineItem
                {
                    Message = $"Starting session state for sessionId '{this.sessionId}'",
                }, CancellationToken.None);
        }

        public override async Task UpdateDataVariable(DataVariableUpdate request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var key = $"{request.Namespace}_{request.Name}";

            this.dataVariableStates[key] = new DataVariableStateMsg
            {
                Identity = request.Identity,
                Name = request.Name,
                Namespace = request.Namespace,
                SessionId = request.SessionId,
                Type = request.Type,
                Value = request.Value,
            };

            if (this.logger is not null)
            {
                await this.logger.Trace(
                      new SessionLogLineItem
                      {
                          Message = $"Session state tracked the updated the value of data variable '{request.Name}' in namespace '{request.Namespace}'",
                      }, CancellationToken.None);
            }
        }

        public override async Task Stop()
        {
            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            await this.logger.Trace(
                      new SessionLogLineItem
                      {
                          Message = $"Session state with identity {this.clusterIdentity}' is being stopped.",
                      }, CancellationToken.None);

            this.logger = null;
#pragma warning disable CA1849 // PoisonAsync causes a lockup, uncertain as to why for the moment
            foreach (var child in this.Context.Children)
            {
                this.Context.Poison(child);
            }

            this.Context.Poison(this.Context.Self);
#pragma warning restore CA1849
        }

        public override async Task<SessionStateResponse> GetState()
        {
            var sessionStateResponse = new SessionStateResponse { };

            sessionStateResponse.DataVariableStates.AddRange(this.dataVariableStates.Values);

            if (this.sessionId is null)
            {
                throw new ArgumentException(nameof(this.sessionId));
            }

            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            await this.logger.Trace(
                      new SessionLogLineItem
                      {
                          Message = $"Session state returned the values of {this.dataVariableStates.Values.Count} data variables.",
                      }, CancellationToken.None);

            return sessionStateResponse;
        }
    }
}
