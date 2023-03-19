namespace EngineBay.ActorEngine
{
    using System;
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;
    using Proto;
    using Proto.Cluster;

    public class SessionLoggerGrain : SessionLoggerGrainBase
    {
        private readonly ClusterIdentity clusterIdentity;

        private Guid? sessionId;

        private LogLevel logLevel;

        private ICollection<SessionLogMsg> sessionLogs;

        public SessionLoggerGrain(IContext context, ClusterIdentity clusterIdentity)
            : base(context)
        {
            this.clusterIdentity = clusterIdentity;
            this.logLevel = LogLevel.Information;
            this.sessionLogs = new Collection<SessionLogMsg>();
        }

        public override async Task Critical(SessionLogLineItem request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            await this.Log(request, LogLevel.Critical).ConfigureAwait(false);
        }

        public override async Task Debug(SessionLogLineItem request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            await this.Log(request, LogLevel.Debug).ConfigureAwait(false);
        }

        public override async Task Error(SessionLogLineItem request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            await this.Log(request, LogLevel.Error).ConfigureAwait(false);
        }

        public override async Task Info(SessionLogLineItem request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            await this.Log(request, LogLevel.Information).ConfigureAwait(false);
        }

        public override async Task Trace(SessionLogLineItem request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            await this.Log(request, LogLevel.Trace).ConfigureAwait(false);
        }

        public override async Task Warning(SessionLogLineItem request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            await this.Log(request, LogLevel.Warning).ConfigureAwait(false);
        }

        public override async Task Start(SessionStartRequest request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            this.sessionId = new Guid(request.SessionId);
            this.logLevel = (LogLevel)request.LogLevel;

            await this.Info(new SessionLogLineItem
            {
                Message = $"Starting logger for sessionId '{this.sessionId}'",
            }).ConfigureAwait(false);
        }

#pragma warning disable CS1998 // These methods are async on the generated classes from the protobuf definitions, but are not acually async internally.
        public override async Task Stop()
        {
#pragma warning disable CA1849 // PoisonAsync causes a lockup, uncertain as to why for the moment
            foreach (var child in this.Context.Children)
            {
                this.Context.Poison(child);
            }

            this.Context.Poison(this.Context.Self);
#pragma warning restore CA1849
        }

        public override async Task<SessionLogsResponse> GetLogs()
        {
            if (this.sessionId is null)
            {
                throw new ArgumentException(nameof(this.sessionId));
            }

            var sessionLogsResponse = new SessionLogsResponse { };

            sessionLogsResponse.Sessionlogs.AddRange(this.sessionLogs);

            return sessionLogsResponse;
        }

        private async Task Log(SessionLogLineItem request, LogLevel requestLogLevel)
        {
            if (this.sessionId is null)
            {
                throw new ArgumentException(nameof(this.sessionId));
            }

            if (requestLogLevel >= this.logLevel)
            {
                this.sessionLogs.Add(new SessionLogMsg
                {
                    SessionId = this.sessionId.Value.ToString(),
                    Message = request.Message,
                    LogLevel = (int)requestLogLevel,
                });
            }
        }
#pragma warning restore CS1998
    }
}
