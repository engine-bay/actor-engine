namespace EngineBay.ActorEngine
{
    using System;
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;
    using EngineBay.Blueprints;
    using Newtonsoft.Json;
    using Proto;
    using Proto.Cluster;

    public class DataTableGrain : DataTableGrainBase
    {
        private readonly ClusterIdentity clusterIdentity;

        private string name;

        private string nameSpace;

        private DataTableMsg value;

        private Collection<DataVariableDependency> dataVariableDependencies;

        private DataVariableDependantMsg? outputDataVariable;

        private SessionLoggerGrainClient? logger;

        public DataTableGrain(IContext context, ClusterIdentity clusterIdentity)
            : base(context)
        {
            this.clusterIdentity = clusterIdentity;
            this.name = string.Empty;
            this.nameSpace = string.Empty;
            this.value = new DataTableMsg();
            this.dataVariableDependencies = new Collection<DataVariableDependency>();
        }

        public override async Task DependOn(DataVariableDependency request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            this.dataVariableDependencies.Add(request);

            await this.logger.Trace(
                      new SessionLogLineItem
                      {
                          Message = $"DataTableGrain '{this.name}' in namespace '{this.nameSpace}' now depends on input data variable '{request.Name}' in namespace '{request.Namespace}' of type '{request.Type}'.",
                      }, CancellationToken.None);
        }

        public override async Task OutputTo(DataVariableDependantMsg request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            this.outputDataVariable = request;

            await this.logger.Trace(
                      new SessionLogLineItem
                      {
                          Message = $"DataTableGrain '{this.name}' in namespace '{this.nameSpace}' updated to notify data variable '{request.Name}' in namespace '{request.Namespace}' of type '{request.Type}' with results.",
                      }, CancellationToken.None);
        }

        public override async Task UpdateDataVariable(DataVariableUpdate request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            var name = request.Name;
            if (name is null)
            {
                throw new ArgumentException(nameof(name));
            }

            var type = request.Type;
            if (type is null)
            {
                throw new ArgumentException(nameof(type));
            }

            var value = request.Value;
            if (value is null)
            {
                throw new ArgumentException(nameof(value));
            }

            var nameSpace = request.Namespace;
            if (nameSpace is null)
            {
                throw new ArgumentException(nameof(nameSpace));
            }

            switch (type)
            {
                case DataVariableTypes.FLOAT:
                case DataVariableTypes.BOOL:
                case DataVariableTypes.DATETIME:
                case DataVariableTypes.STRING:
                    // todo, this is super ineffecient, but just trying to get it working for now.
                    // find the cells that match our update and update it's value.
                    // Not dealing with any kind of new rows/columns/cells being added. This is an update only for now.
                    foreach (var row in this.value.DataTableRows)
                    {
                        foreach (var cell in row.DataTableCells)
                        {
                            if (cell.Name == name && cell.Namespace == nameSpace)
                            {
                                cell.Value = value;
                            }
                        }
                    }

                    break;
                case DataVariableTypes.DATATABLE:
                    var dataTable = JsonConvert.DeserializeObject<DataTableMsg>(value);
                    if (dataTable is null)
                    {
                        throw new ArgumentException(nameof(dataTable));
                    }

                    this.value = dataTable;
                    break;
                default:
                    var message = $"Unkown data variable type '{type}' for data variable '{name}' in namespace '{nameSpace}' when updating value in DataTableGrain '{this.name}'";
                    await this.logger.Error(
                        new SessionLogLineItem
                        {
                            Message = message,
                        }, CancellationToken.None);
                    throw new ArgumentException(message);
            }

            await this.logger.Debug(
                      new SessionLogLineItem
                      {
                          Message = $"DataTableGrain '{this.name}' in namespace '{this.nameSpace}' recieved an updated '{name}' data variable in namespace '{nameSpace}' with value '{value}' of type '{type}'",
                      }, CancellationToken.None);

            // tell the dependant data variable about the updated DataTable
            await this.UpdateDependant();
        }

        public override async Task UseSessionLogger(SessionLoggerRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            this.logger = this.Context.GetSessionLoggerGrain(request.SessionId.ToString());

            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            await this.logger.Trace(
                      new SessionLogLineItem
                      {
                          Message = $"Data table with identity {this.clusterIdentity}' was registered to start logging against sessionId '{request.SessionId}'",
                      }, CancellationToken.None);
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
                          Message = $"Data table with identity {this.clusterIdentity}' is being stopped.",
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

        private async Task UpdateDependant()
        {
            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            if (this.outputDataVariable is not null)
            {
                await this.Context.GetDataVariableGrain(this.outputDataVariable.Identity).UpdateValue(
                            new DataVariableValue
                            {
                                Value = JsonConvert.SerializeObject(this.value),
                            }, CancellationToken.None);

                await this.logger.Trace(
                       new SessionLogLineItem
                       {
                           Message = $"DataTableGrain '{this.name}' in namespace '{this.nameSpace}' has notified dependant of the update.",
                       }, CancellationToken.None);
            }
            else
            {
                await this.logger.Trace(
                       new SessionLogLineItem
                       {
                           Message = $"DataTableGrain '{this.name}' in namespace '{this.nameSpace}' has no dependants to update.",
                       }, CancellationToken.None);
            }
        }
    }
}
