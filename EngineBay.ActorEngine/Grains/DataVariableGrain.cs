namespace EngineBay.ActorEngine
{
    using System;
    using System.Globalization;
    using System.Threading.Tasks;
    using Proto;
    using Proto.Cluster;

    public class DataVariableGrain : DataVariableGrainBase
    {
        private readonly ClusterIdentity clusterIdentity;

        private Guid? identity;

        private Guid? sessionId;

        private string name;

        private string nameSpace;

        private string type;

        private string value;

        private ICollection<string> dependentExpressionGrains;
        private ICollection<string> dependentDataTableGrains;

        private SessionLoggerGrainClient? logger;

        public DataVariableGrain(IContext context, ClusterIdentity clusterIdentity)
            : base(context)
        {
            this.clusterIdentity = clusterIdentity;
            this.name = string.Empty;
            this.nameSpace = string.Empty;
            this.value = string.Empty;
            this.type = string.Empty;
            this.dependentExpressionGrains = new List<string>();
            this.dependentDataTableGrains = new List<string>();
        }

        public override Task<DataVariableValue> GetValue()
        {
            return Task.FromResult(new DataVariableValue
            {
                Value = this.value,
            });
        }

        public override async Task RegisterExpressionGrainDependant(GrainIdentity request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            this.dependentExpressionGrains.Add(request.Identity);

            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            await this.logger.Trace(
                      new SessionLogLineItem
                      {
                          Message = $"Data variable '{this.name}' in namespace '{this.nameSpace}' had an expression registered as a dependant",
                      }, CancellationToken.None).ConfigureAwait(false);
        }

        public override async Task RegisterDataTableGrainDependant(GrainIdentity request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            this.dependentDataTableGrains.Add(request.Identity);

            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            await this.logger.Trace(
                      new SessionLogLineItem
                      {
                          Message = $"Data variable '{this.name}' in namespace '{this.nameSpace}' had a data table registered as a dependant",
                      }, CancellationToken.None).ConfigureAwait(false);
        }

        public override async Task UpdateValue(DataVariableValue request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            if (this.value.Equals(request.Value, StringComparison.Ordinal))
            {
                // stop propagating changes that will have no impact on end results.
                await this.logger.Trace(
                      new SessionLogLineItem
                      {
                          Message = $"Data variable '{this.name}' in namespace '{this.nameSpace}' value was updated but did not change it's actual value of '{this.value}'. The propagation of updates was halted.",
                      }, CancellationToken.None).ConfigureAwait(false);
                return;
            }

            this.value = request.Value;

            await this.logger.Debug(
                      new SessionLogLineItem
                      {
                          Message = $"Data variable '{this.name}' in namespace '{this.nameSpace}' value was updated to '{this.value}'.",
                      }, CancellationToken.None).ConfigureAwait(false);

            await this.NotifyDependents().ConfigureAwait(false);
            await this.SaveState().ConfigureAwait(false);
        }

        public override async Task UpdateIdentity(DataVariableIdentity request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            this.identity = new Guid(request.Identity);
            this.sessionId = new Guid(request.SessionId);
            this.name = request.Name;
            this.nameSpace = request.Namespace;
            this.type = request.Type;
            await this.SaveState().ConfigureAwait(false);
        }

        public override async Task UseSessionLogger(SessionLoggerRequest request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            this.logger = this.Context.GetSessionLoggerGrain(request.SessionId.ToString());

            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            await this.logger.Trace(
                      new SessionLogLineItem
                      {
                          Message = $"Data variable with identity {this.clusterIdentity}' was registered to start logging against sessionId '{request.SessionId}'",
                      }, CancellationToken.None).ConfigureAwait(false);
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
                          Message = $"Data variable with identity {this.clusterIdentity}' is being stopped.",
                      }, CancellationToken.None).ConfigureAwait(false);

            this.logger = null;

#pragma warning disable CA1849 // PoisonAsync causes a lockup, uncertain as to why for the moment
            foreach (var child in this.Context.Children)
            {
                this.Context.Poison(child);
            }

            this.Context.Poison(this.Context.Self);
#pragma warning restore CA1849
        }

        private async Task NotifyDependents()
        {
            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            var dataVariableUpdate = new DataVariableUpdate
            {
                Identity = this.identity.ToString(),
                Name = this.name,
                Namespace = this.nameSpace,
                SessionId = this.sessionId.ToString(),
                Value = this.value,
                Type = this.type,
            };

            foreach (var dependent in this.dependentDataTableGrains)
            {
                await this.Context.GetDataTableGrain(dependent).UpdateDataVariable(dataVariableUpdate, CancellationToken.None).ConfigureAwait(false);
            }

            foreach (var dependent in this.dependentExpressionGrains)
            {
                await this.Context.GetExpressionGrain(dependent).UpdateDataVariable(dataVariableUpdate, CancellationToken.None).ConfigureAwait(false);
            }

            await this.logger.Trace(
                      new SessionLogLineItem
                      {
                          Message = $"Notified {this.dependentExpressionGrains.Count + this.dependentDataTableGrains.Count} dependents of updated value of '{this.value}' for data variable '{this.name}' in namespace '{this.nameSpace}'",
                      }, CancellationToken.None).ConfigureAwait(false);
        }

        private async Task SaveState()
        {
            if (this.identity is null)
            {
                throw new ArgumentNullException(nameof(this.identity));
            }

            if (this.sessionId is null)
            {
                throw new ArgumentNullException(nameof(this.sessionId));
            }

            var sessionIdentity = this.sessionId.ToString();

            if (sessionIdentity is null)
            {
                throw new ArgumentException(nameof(sessionIdentity));
            }

            var dataVariableUpdate = new DataVariableUpdate
            {
                Identity = this.identity.Value.ToString(),
                Name = this.name,
                Namespace = this.nameSpace,
                Type = this.type,
                SessionId = sessionIdentity,
                Value = this.value.ToString(CultureInfo.InvariantCulture),
            };

            await this.Context.Cluster().GetSessionStateGrain(sessionIdentity)
                            .UpdateDataVariable(dataVariableUpdate, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
