namespace EngineBay.ActorEngine
{
    using System;
    using System.Data;
    using System.Globalization;
    using System.Threading.Tasks;
    using EngineBay.Blueprints;
    using EngineBay.SheetFunctions;
    using Flee.PublicTypes;
    using Newtonsoft.Json;
    using Proto;
    using Proto.Cluster;
    using UnitsNet;

    public class ExpressionGrain : ExpressionGrainBase
    {
        private readonly ClusterIdentity clusterIdentity;
        private string expression;
        private ExpressionContext? expressionContext;

        private IDynamicExpression? compiledExpression;

        private Dictionary<string, float> floatDataVariables;
        private Dictionary<string, string> stringDataVariables;

        private Dictionary<string, bool> boolDataVariables;
        private Dictionary<string, DateTime> dateTimeDataVariables;

        private Dictionary<string, DataTable> dataTablesDataVariables;

        private ICollection<DataVariableDependency> dataVariableDependencies;

        private DataVariableDependantMsg? outputDataVariable;

        private SessionLoggerGrainClient? logger;

        public ExpressionGrain(IContext context, ClusterIdentity clusterIdentity)
            : base(context)
        {
            this.clusterIdentity = clusterIdentity;
            this.expression = string.Empty;
            this.expressionContext = new ExpressionContext();
            this.floatDataVariables = new Dictionary<string, float>();
            this.stringDataVariables = new Dictionary<string, string>();
            this.boolDataVariables = new Dictionary<string, bool>();
            this.dateTimeDataVariables = new Dictionary<string, DateTime>();
            this.dataTablesDataVariables = new Dictionary<string, DataTable>();
            this.dataVariableDependencies = new List<DataVariableDependency>();

            this.expressionContext.Imports.AddType(typeof(Math));
            this.expressionContext.Imports.AddType(typeof(DateTime));
            this.expressionContext.Imports.AddType(typeof(UnitConverter));
            this.expressionContext.Imports.AddType(typeof(EngineFunctions));
        }

        public override async Task DependOn(DataVariableDependency request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            this.dataVariableDependencies.Add(request);

            await this.logger.Trace(
                      new SessionLogLineItem
                      {
                          Message = $"Expression '{this.expression}' now depends on input data variable '{request.Name}' in namespace '{request.Namespace}' of type '{request.Type}'.",
                      }, CancellationToken.None);
        }

        public override async Task OutputTo(DataVariableDependantMsg request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            this.outputDataVariable = request;

            await this.logger.Trace(
                      new SessionLogLineItem
                      {
                          Message = $"Expression '{this.expression}' updated to notify data variable '{request.Name}' in namespace '{request.Namespace}' of type '{request.Type}' with results.",
                      }, CancellationToken.None);
        }

        public override async Task UpdateDataVariable(DataVariableUpdate request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (this.expressionContext is null)
            {
                throw new ArgumentException(nameof(this.expressionContext));
            }

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
                    var parsedFloatValue = float.Parse(value, CultureInfo.InvariantCulture);
                    this.floatDataVariables[name] = parsedFloatValue;
                    this.expressionContext.Variables[name] = parsedFloatValue;
                    break;
                case DataVariableTypes.BOOL:
                    var parsedBoolValue = bool.Parse(value);
                    this.boolDataVariables[name] = parsedBoolValue;
                    this.expressionContext.Variables[name] = parsedBoolValue;
                    break;
                case DataVariableTypes.DATETIME:
                    var parsedDateTimeValue = DateTime.Parse(value, CultureInfo.InvariantCulture);
                    this.dateTimeDataVariables[name] = parsedDateTimeValue;
                    this.expressionContext.Variables[name] = parsedDateTimeValue;
                    break;
                case DataVariableTypes.STRING:
                    this.stringDataVariables[name] = value;
                    this.expressionContext.Variables[name] = value;
                    break;
                case DataVariableTypes.DATATABLE:
                    var dataTableMsg = JsonConvert.DeserializeObject<DataTableMsg>(value);
                    if (dataTableMsg is null)
                    {
                        throw new ArgumentException(nameof(dataTableMsg));
                    }

                    var dataTable = new DataTable();
                    foreach (var dataTableColumn in dataTableMsg.DataTableColumns)
                    {
                        var column = new DataColumn();
                        column.ColumnName = dataTableColumn.Name;
                        dataTable.Columns.Add(column);
                    }

                    foreach (var dataTableRow in dataTableMsg.DataTableRows)
                    {
                        var row = dataTable.NewRow();

                        foreach (var dataTableCell in dataTableRow.DataTableCells)
                        {
                            row[dataTableCell.Key] = dataTableCell.Value;
                        }

                        dataTable.Rows.Add(row);
                    }

                    this.dataTablesDataVariables[name] = dataTable;
                    this.expressionContext.Variables[name] = dataTable;
                    break;
                default:
                    var message = $"Unkown data variable type '{type}' for data variable '{name}' in namespace '{nameSpace}' when updating value in expression '{this.expression}'";
                    await this.logger.Error(
                        new SessionLogLineItem
                        {
                            Message = message,
                        }, CancellationToken.None);
                    throw new ArgumentException(message);
            }

            await this.logger.Trace(
                      new SessionLogLineItem
                      {
                          Message = $"Expression '{this.expression}' recieved an updated '{name}' data variable in namespace '{nameSpace}' with value '{value}' of type '{type}'",
                      }, CancellationToken.None);

            await this.Evaluate();
        }

        public override async Task UseExpression(UseExpressionRequest request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            this.expression = request.Expression;

            await this.logger.Trace(
                       new SessionLogLineItem
                       {
                           Message = $"Updated expression to '{request.Expression}'",
                       }, CancellationToken.None);
        }

        public override async Task Evaluate()
        {
            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            if (this.compiledExpression is null && await this.InputsAreSatisfied())
            {
                if (this.expressionContext is null)
                {
                    throw new ArgumentException(nameof(this.expressionContext));
                }

                this.compiledExpression = this.expressionContext.CompileDynamic(this.expression);
            }

            if (this.compiledExpression is null)
            {
                await this.logger.Warning(
                       new SessionLogLineItem
                       {
                           Message = $"Expression '{this.expression}' cannot evaluate at this time because the compiledExpression was null.",
                       }, CancellationToken.None);
                return;
            }

            // Evaluate the expressions
            var result = this.compiledExpression.Evaluate().ToString();

            await this.logger.Debug(
                       new SessionLogLineItem
                       {
                           Message = $"Expression '{this.expression}' evaluated to '{result}'",
                       }, CancellationToken.None);

            if (result is not null)
            {
                await this.UpdateDependant(result);
            }
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
                          Message = $"Expression with identity {this.clusterIdentity}' was registered to start logging against sessionId '{request.SessionId}'",
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
                          Message = $"Expression with identity {this.clusterIdentity}' is being stopped.",
                      }, CancellationToken.None);

            this.expressionContext = null;
            this.compiledExpression = null;
            this.logger = null;
#pragma warning disable CA1849 // PoisonAsync causes a lockup, uncertain as to why for the moment
            foreach (var child in this.Context.Children)
            {
                this.Context.Poison(child);
            }

            this.Context.Poison(this.Context.Self);
#pragma warning restore CA1849
        }

        private async Task UpdateDependant(string result)
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
                                Value = result.ToString(CultureInfo.InvariantCulture),
                            }, CancellationToken.None);

                await this.logger.Trace(
                       new SessionLogLineItem
                       {
                           Message = $"Expression '{this.expression}' has notified it's dependant of updated result.",
                       }, CancellationToken.None);
            }
            else
            {
                await this.logger.Trace(
                       new SessionLogLineItem
                       {
                           Message = $"Expression '{this.expression}' has no dependant to update.",
                       }, CancellationToken.None);
            }
        }

        private async Task<bool> InputsAreSatisfied()
        {
            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            var satisfied = true;

            foreach (var dependency in this.dataVariableDependencies)
            {
                switch (dependency.Type)
                {
                    case DataVariableTypes.FLOAT:
                        satisfied = satisfied && this.floatDataVariables.ContainsKey(dependency.Name);
                        break;
                    case DataVariableTypes.BOOL:
                        satisfied = satisfied && this.boolDataVariables.ContainsKey(dependency.Name);
                        break;
                    case DataVariableTypes.DATETIME:
                        satisfied = satisfied && this.dateTimeDataVariables.ContainsKey(dependency.Name);
                        break;
                    case DataVariableTypes.STRING:
                        satisfied = satisfied && this.stringDataVariables.ContainsKey(dependency.Name);
                        break;
                    case DataVariableTypes.DATATABLE:
                        satisfied = satisfied && this.dataTablesDataVariables.ContainsKey(dependency.Name);
                        break;
                    default:
                        var message = $"Unkown data variable type '{dependency.Type}' for data variable '{dependency.Name}' in namespace '{dependency.Namespace}' when checking if expression dependencies are satisfied.";
                        await this.logger.Error(
                        new SessionLogLineItem
                        {
                            Message = message,
                        }, CancellationToken.None);
                        throw new ArgumentException(message);
                }

                if (!satisfied)
                {
                    await this.logger.Trace(
                        new SessionLogLineItem
                        {
                            Message = $"Dependency '{dependency.Name}' in namespace '{dependency.Namespace}' for expression '{this.expression}' is NOT satisfied.",
                        }, CancellationToken.None);
                    return false;
                }
            }

            await this.logger.Trace(
               new SessionLogLineItem
               {
                   Message = $"{this.dataVariableDependencies.Count} dependencies are satisfied for expression '{this.expression}'.",
               }, CancellationToken.None);

            return true;
        }
    }
}
