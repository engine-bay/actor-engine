namespace EngineBay.ActorEngine
{
    using System;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Text;
    using System.Threading.Tasks;
    using EngineBay.Blueprints;
    using Google.Protobuf.Collections;
    using Newtonsoft.Json;
    using Proto;
    using Proto.Cluster;

    public class SessionGrain : SessionGrainBase
    {
        private readonly ClusterIdentity clusterIdentity;

        private Guid? sessionId;

        private Dictionary<string, string> dataVariableIdentities;

        private ICollection<string> rootExpressionIdentities;

        private ICollection<string> expressionIdentities;

        private ICollection<string> dataTableIdentities;

        private SessionLoggerGrainClient? logger;

        public SessionGrain(IContext context, ClusterIdentity clusterIdentity)
            : base(context)
        {
            this.clusterIdentity = clusterIdentity;
            this.dataVariableIdentities = new Dictionary<string, string>();
            this.expressionIdentities = new Collection<string>();
            this.dataTableIdentities = new Collection<string>();
            this.rootExpressionIdentities = new Collection<string>();
        }

        public override async Task Start(SessionStartRequest request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            this.sessionId = new Guid(request.SessionId);

            var sessionIdentity = request.SessionId.ToString();

            var workbook = request.Workbook;

            if (workbook is null)
            {
                throw new ArgumentException(nameof(workbook));
            }

            this.logger = this.Context.GetSessionLoggerGrain(sessionIdentity);

            await this.logger.Start(request, CancellationToken.None);

            await this.logger.Info(
                new SessionLogLineItem
                {
                    Message = $"Starting session based on workbookId '{workbook.Id}'",
                }, CancellationToken.None);

            var sessionStateGrain = this.Context.GetSessionStateGrain(sessionIdentity);
            await sessionStateGrain.Start(request, CancellationToken.None);

            var blueprints = workbook.Blueprints;

            if (blueprints is null)
            {
                throw new ArgumentException(nameof(blueprints));
            }

            foreach (var blueprint in workbook.Blueprints)
            {
                await this.CreateDataVariables(blueprint);
                await this.CreateExpressions(blueprint);
                await this.CreateDataTables(blueprint);
                await this.CreateTriggers(blueprint);
            }

            // Apply default values to the graph, This has to be done at the end of the setup so that initial values can propagate through expressions that depend on them
            await this.SetDefaultValues(workbook);
            await this.TriggerRootExpressionEvaluations();

            await this.logger.Info(
                new SessionLogLineItem
                {
                    Message = $"Session setup complete.",
                }, CancellationToken.None);
        }

        public override async Task UpdateDataVariable(DataVariable request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            var dataVariableIdentity = this.TryGetDataVariableGrainIdentity(request.Namespace, request.Name);

            if (string.IsNullOrEmpty(dataVariableIdentity))
            {
                await this.logger.Warning(
                               new SessionLogLineItem
                               {
                                   Message = $"Tried to update a data variable value for a key '{request.Name}' in namespace '{request.Namespace}' that the session had not created.",
                               }, CancellationToken.None);
                return;
            }

            var dataVariableGrain = this.Context.GetDataVariableGrain(dataVariableIdentity);

            await dataVariableGrain.UpdateValue(
                   new DataVariableValue
                   {
                       Value = request.Value,
                   },
                   CancellationToken.None);

            await this.logger.Trace(
                               new SessionLogLineItem
                               {
                                   Message = $"Update a data variable value for a key '{request.Name}' in namespace '{request.Namespace}' with a value of '{request.Value}'.",
                               }, CancellationToken.None);
        }

        public override async Task UpdateDataTable(DataTableMsg request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            var dataVariableIdentity = this.TryGetDataVariableGrainIdentity(request.Namespace, request.Name);

            if (string.IsNullOrEmpty(dataVariableIdentity))
            {
                await this.logger.Warning(
                               new SessionLogLineItem
                               {
                                   Message = $"Tried to update a data table value for a key '{request.Name}' in namespace '{request.Namespace}' that the session had not created.",
                               }, CancellationToken.None);
                return;
            }

            var dataVariableGrain = this.Context.GetDataVariableGrain(dataVariableIdentity);

            await dataVariableGrain.UpdateValue(
                new DataVariableValue
                {
                    Value = JsonConvert.SerializeObject(request),
                }, CancellationToken.None);

            await this.logger.Trace(
                               new SessionLogLineItem
                               {
                                   Message = $"Update a data table value for a key '{request.Name}' in namespace '{request.Namespace}' with a new value.",
                               }, CancellationToken.None);
        }

        public override async Task Stop()
        {
            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            var sessionIdentity = this.sessionId.ToString();

            if (sessionIdentity is null)
            {
                throw new ArgumentException(nameof(sessionIdentity));
            }

            await this.logger.Trace(
                new SessionLogLineItem
                {
                    Message = $"Stopping session'",
                }, CancellationToken.None);

            foreach (var dataVariableIdentity in this.dataVariableIdentities)
            {
                await this.Context.GetDataVariableGrain(dataVariableIdentity.Value).Stop(CancellationToken.None);
            }

            foreach (var expressionIdentity in this.expressionIdentities)
            {
                await this.Context.GetExpressionGrain(expressionIdentity).Stop(CancellationToken.None);
            }

            foreach (var dataTableIdentity in this.dataTableIdentities)
            {
                await this.Context.GetDataTableGrain(dataTableIdentity).Stop(CancellationToken.None);
            }

            await this.Context.GetSessionStateGrain(sessionIdentity).Stop(CancellationToken.None);
            await this.Context.GetSessionLoggerGrain(sessionIdentity).Stop(CancellationToken.None);

            this.logger = null;
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
            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            var sessionLogsResponse = await this.logger.GetLogs(CancellationToken.None);

            if (sessionLogsResponse is null)
            {
                throw new ArgumentException(nameof(sessionLogsResponse));
            }

            return sessionLogsResponse;
        }

        public override async Task<SessionStateResponse> GetState()
        {
            var sessionIdentity = this.sessionId.ToString();

            if (sessionIdentity is null)
            {
                throw new ArgumentException(nameof(sessionIdentity));
            }

            var sessionStateGrain = this.Context.GetSessionStateGrain(sessionIdentity);

            var sessionStateResponse = await sessionStateGrain.GetState(CancellationToken.None);

            if (sessionStateResponse is null)
            {
                throw new ArgumentException(nameof(sessionStateResponse));
            }

            return sessionStateResponse;
        }

        private static string BuildTriggerExpression(TriggerBlueprintMsg? triggerBlueprint)
        {
            if (triggerBlueprint is null)
            {
                throw new ArgumentNullException(nameof(triggerBlueprint));
            }

            if (triggerBlueprint.TriggerExpressionBlueprints.Count == 1)
            {
                return triggerBlueprint.TriggerExpressionBlueprints[0].Expression;
            }

            var expressionBuilder = new StringBuilder();

            for (int index = 0; index < triggerBlueprint.TriggerExpressionBlueprints.Count; index++)
            {
                var triggerExpressionBlueprint = triggerBlueprint.TriggerExpressionBlueprints[index];

                // If it's not the first or last one
                if (index != 0 && index != triggerBlueprint.TriggerExpressionBlueprints.Count)
                {
                    expressionBuilder.Append(" AND ");
                }

                expressionBuilder.Append(CultureInfo.CurrentCulture, $"({triggerExpressionBlueprint.Expression})");
            }

            var expression = expressionBuilder.ToString();
            return expression;
        }

        private async Task CreateTriggers(BlueprintMsg blueprint)
        {
            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            if (blueprint is null)
            {
                throw new ArgumentNullException(nameof(blueprint));
            }

            var triggerBlueprints = blueprint.TriggerBlueprints;

            if (triggerBlueprints is null)
            {
                throw new ArgumentException(nameof(triggerBlueprints));
            }

            foreach (var triggerBlueprint in triggerBlueprints)
            {
                var expressionGrainIdentity = Guid.NewGuid().ToString();

                var expressionGrain = this.Context.GetExpressionGrain(expressionGrainIdentity);

                this.expressionIdentities.Add(expressionGrainIdentity);

                await expressionGrain.UseSessionLogger(
                    new SessionLoggerRequest
                    {
                        SessionId = this.sessionId.ToString(),
                    },
                    CancellationToken.None);

                var expression = BuildTriggerExpression(triggerBlueprint);

                if (expression is null)
                {
                    throw new ArgumentException(nameof(expression));
                }

                await expressionGrain.UseExpression(
                    new UseExpressionRequest
                    {
                        Expression = expression,
                    },
                    CancellationToken.None);

                var outputDataVariableBlueprint = triggerBlueprint.OutputDataVariableBlueprint;

                if (outputDataVariableBlueprint is null)
                {
                    throw new ArgumentException(nameof(outputDataVariableBlueprint));
                }

                await this.LinkOutputDataVariable(blueprint, outputDataVariableBlueprint, expressionGrain);

                var inputDataVariableBlueprints = triggerBlueprint.TriggerExpressionBlueprints.Select(x => x.InputDataVariableBlueprint).ToList();

                if (inputDataVariableBlueprints is null)
                {
                    throw new ArgumentException(nameof(inputDataVariableBlueprints));
                }

                await this.LinkInputDataVariables(blueprint, inputDataVariableBlueprints, expressionGrainIdentity, expressionGrain);
            }

            await this.logger.Trace(
                new SessionLogLineItem
                {
                    Message = $"Created {triggerBlueprints.Count} triggers from blueprint '{blueprint.Name}'.",
                }, CancellationToken.None);
        }

        private async Task CreateDataTables(BlueprintMsg blueprint)
        {
            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            if (blueprint is null)
            {
                throw new ArgumentNullException(nameof(blueprint));
            }

            foreach (var dataTableBlueprint in blueprint.DataTableBlueprints)
            {
                // create a DataTableGrain
                var dataTableGrainIdentity = Guid.NewGuid().ToString();

                var dataTableGrain = this.Context.GetDataTableGrain(dataTableGrainIdentity);

                this.dataTableIdentities.Add(dataTableGrainIdentity);

                await dataTableGrain.UseSessionLogger(
                    new SessionLoggerRequest
                    {
                        SessionId = this.sessionId.ToString(),
                    },
                    CancellationToken.None);

                // get the data variable that the datatable will output a dataTableMsg value to
                var outputDatavariableGrainIdentity = this.TryGetDataVariableGrainIdentity(dataTableBlueprint.Namespace, dataTableBlueprint.Name);

                if (outputDatavariableGrainIdentity is null)
                {
                    throw new ArgumentException(nameof(outputDatavariableGrainIdentity));
                }

                var dataVariableGrain = this.Context.GetDataVariableGrain(outputDatavariableGrainIdentity);

                await dataTableGrain.OutputTo(
                    new DataVariableDependantMsg
                    {
                        Identity = outputDatavariableGrainIdentity,
                        Name = dataTableBlueprint.Name,
                        Namespace = dataTableBlueprint.Namespace,
                        Type = DataVariableTypes.DATATABLE,
                    },
                    CancellationToken.None);

                // maybe set an initial dataTableMsg value?
                var dataTableMsg = new DataTableMsg
                {
                    Name = dataTableBlueprint.Name,
                    Namespace = dataTableBlueprint.Namespace,
                    Description = dataTableBlueprint.Description,
                };

                dataTableMsg.DataTableColumns.AddRange(dataTableBlueprint.DataTableColumnBlueprints.Select(dataTableColumnBlueprint =>
                    {
                        var dataTableColumnMsg = new DataTableColumnMsg
                        {
                            Name = !string.IsNullOrEmpty(dataTableColumnBlueprint.Name) ? dataTableColumnBlueprint.Name : string.Empty,
                            Type = !string.IsNullOrEmpty(dataTableColumnBlueprint.Type) ? dataTableColumnBlueprint.Type : string.Empty,
                        };

                        return dataTableColumnMsg;
                    }));

                dataTableMsg.DataTableRows.AddRange(dataTableBlueprint.DataTableRowBlueprints.Select(dataTableRowBlueprint =>
                    {
                        var dataTableRowMsg = new DataTableRowMsg
                        {
                        };

                        if (dataTableRowBlueprint.DataTableCellBlueprints is null)
                        {
                            throw new ArgumentException(nameof(dataTableRowBlueprint.DataTableCellBlueprints));
                        }

                        dataTableRowMsg.DataTableCells.AddRange(dataTableRowBlueprint.DataTableCellBlueprints.Select(dataTableCellBlueprint =>
                        {
                            var dataTableCellMsg = new DataTableCellMsg
                            {
                                Key = !string.IsNullOrEmpty(dataTableCellBlueprint.Key) ? dataTableCellBlueprint.Key : string.Empty,
                                Value = !string.IsNullOrEmpty(dataTableCellBlueprint.Value) ? dataTableCellBlueprint.Value : string.Empty,
                                Name = !string.IsNullOrEmpty(dataTableCellBlueprint.Name) ? dataTableCellBlueprint.Name : string.Empty,
                                Namespace = !string.IsNullOrEmpty(dataTableCellBlueprint.Namespace) ? dataTableCellBlueprint.Namespace : string.Empty,
                            };

                            return dataTableCellMsg;
                        }));

                        return dataTableRowMsg;
                    }));

                var dataVariableUpdate = new DataVariableUpdate
                {
                    Identity = dataTableGrainIdentity,
                    Name = dataTableBlueprint.Name,
                    Namespace = dataTableBlueprint.Namespace,
                    Type = DataVariableTypes.DATATABLE,
                    SessionId = this.sessionId.ToString(),
                    Value = JsonConvert.SerializeObject(dataTableMsg),
                };

                await dataTableGrain.UpdateDataVariable(dataVariableUpdate, CancellationToken.None);

                // for each data variable that our datatable depends on, get it and make it output to our DataTableGrain
                foreach (var inputDataVariableBlueprint in dataTableBlueprint.InputDataVariableBlueprints)
                {
                    // get each input data variable, make the dataTable depend on it
                    var inputDatavariableGrainIdentity = this.TryGetDataVariableGrainIdentity(inputDataVariableBlueprint.Namespace, inputDataVariableBlueprint.Name);

                    if (inputDatavariableGrainIdentity is null)
                    {
                        throw new ArgumentException(nameof(inputDatavariableGrainIdentity));
                    }

                    var inputDataVariableGrain = this.Context.GetDataVariableGrain(inputDatavariableGrainIdentity);

                    await inputDataVariableGrain.UseSessionLogger(
                   new SessionLoggerRequest
                   {
                       SessionId = this.sessionId.ToString(),
                   },
                   CancellationToken.None);

                    await inputDataVariableGrain.RegisterDataTableGrainDependant(
                        new GrainIdentity
                        {
                            Identity = dataTableGrainIdentity,
                        },
                        CancellationToken.None);
                }

                await this.logger.Trace(
                    new SessionLogLineItem
                    {
                        Message = $"Created DataTableGrain '{dataTableBlueprint.Name}' with {dataTableBlueprint.InputDataVariableBlueprints.Count} dependencies from blueprint '{blueprint.Name}'.",
                    }, CancellationToken.None);
            }
        }

        private async Task CreateDataVariables(BlueprintMsg blueprint)
        {
            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            if (blueprint is null)
            {
                throw new ArgumentNullException(nameof(blueprint));
            }

            var dataVariableBlueprints = blueprint.DataVariableBlueprints;

            if (dataVariableBlueprints is null)
            {
                throw new ArgumentException(nameof(dataVariableBlueprints));
            }

            foreach (var dataVariableBlueprint in dataVariableBlueprints)
            {
                await this.CreateDataVariable(dataVariableBlueprint);
            }

            await this.logger.Trace(
                new SessionLogLineItem
                {
                    Message = $"Created {dataVariableBlueprints.Count} data variables from blueprint '{blueprint.Name}'.",
                }, CancellationToken.None);
        }

        private async Task CreateDataVariable(DataVariableBlueprintMsg dataVariableBlueprint)
        {
            // this is the only place that creating a data variable should occur, anywhere else trying to access a data varaiable grain with an error shows that the setup was not correct.
            var dataVariableGrainIdentity = this.GetDataVariableGrainIdentity(dataVariableBlueprint.Namespace, dataVariableBlueprint.Name);

            var dataVariableGrain = this.Context.GetDataVariableGrain(dataVariableGrainIdentity);

            await dataVariableGrain.UseSessionLogger(
                new SessionLoggerRequest
                {
                    SessionId = this.sessionId.ToString(),
                },
                CancellationToken.None);

            await dataVariableGrain.UpdateIdentity(
                new DataVariableIdentity
                {
                    Identity = dataVariableGrainIdentity,
                    Name = dataVariableBlueprint.Name,
                    Namespace = dataVariableBlueprint.Namespace,
                    SessionId = this.sessionId?.ToString(),
                    Type = dataVariableBlueprint.Type,
                },
                CancellationToken.None);
        }

        private async Task CreateExpressions(BlueprintMsg blueprint)
        {
            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            if (blueprint is null)
            {
                throw new ArgumentNullException(nameof(blueprint));
            }

            var expressionBlueprints = blueprint.ExpressionBlueprints;

            if (expressionBlueprints is null)
            {
                throw new ArgumentException(nameof(expressionBlueprints));
            }

            // setup the graph
            foreach (var expressionBlueprint in expressionBlueprints)
            {
                await this.CreateExpression(blueprint, expressionBlueprint);
            }

            await this.logger.Trace(
                new SessionLogLineItem
                {
                    Message = $"Created {expressionBlueprints.Count} expressions from blueprint '{blueprint.Name}'.",
                }, CancellationToken.None);
        }

        private async Task CreateExpression(BlueprintMsg blueprint, ExpressionBlueprintMsg expressionBlueprint)
        {
            var expressionGrainIdentity = Guid.NewGuid().ToString();

            var expressionGrain = this.Context.GetExpressionGrain(expressionGrainIdentity);

            this.expressionIdentities.Add(expressionGrainIdentity);

            // if the expression has no input data variables, we need to track it as a root expression to "trigger" it's evaluation for the first time after we setup all the data variable values. Otherwise, it'll never propagate.
            if (expressionBlueprint.InputDataVariableBlueprints.Count == 0)
            {
                this.rootExpressionIdentities.Add(expressionGrainIdentity);
            }

            await expressionGrain.UseSessionLogger(
                new SessionLoggerRequest
                {
                    SessionId = this.sessionId.ToString(),
                },
                CancellationToken.None);

            await expressionGrain.UseExpression(
                new UseExpressionRequest
                {
                    Expression = expressionBlueprint.Expression,
                },
                CancellationToken.None);

            var outputDataVariableBlueprint = expressionBlueprint.OutputDataVariableBlueprint;

            if (outputDataVariableBlueprint is null)
            {
                throw new ArgumentException(nameof(outputDataVariableBlueprint));
            }

            await this.LinkOutputDataVariable(blueprint, outputDataVariableBlueprint, expressionGrain);

            var inputDataTableBlueprints = expressionBlueprint.InputDataTableBlueprints;

            if (inputDataTableBlueprints is null)
            {
                throw new ArgumentException(nameof(inputDataTableBlueprints));
            }

            await this.LinkInputDataTables(blueprint, inputDataTableBlueprints, expressionGrainIdentity, expressionGrain);

            var inputDataVariableBlueprints = expressionBlueprint.InputDataVariableBlueprints;

            if (inputDataVariableBlueprints is null)
            {
                throw new ArgumentException(nameof(inputDataVariableBlueprints));
            }

            await this.LinkInputDataVariables(blueprint, inputDataVariableBlueprints, expressionGrainIdentity, expressionGrain);
        }

        private async Task SetDefaultValues(WorkbookMsg? workbook)
        {
            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            if (workbook is null)
            {
                throw new ArgumentNullException(nameof(workbook));
            }

            if (workbook.Blueprints is null)
            {
                throw new ArgumentException(nameof(workbook.Blueprints));
            }

            foreach (var blueprint in workbook.Blueprints)
            {
                var dataTableBlueprints = blueprint.DataTableBlueprints;
                await this.SetDefaultDataTableValues(dataTableBlueprints);

                var dataVariableBlueprints = blueprint.DataVariableBlueprints;
                await this.SetDefaultDataVariableValues(dataVariableBlueprints);
            }

            await this.logger.Debug(
                new SessionLogLineItem
                {
                    Message = $"Set default values for data variables for session.",
                }, CancellationToken.None);
        }

        private async Task TriggerRootExpressionEvaluations()
        {
            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            await this.logger.Debug(
                new SessionLogLineItem
                {
                    Message = $"Triggering {this.rootExpressionIdentities.Count} root expressions to kick start propagation.",
                }, CancellationToken.None);

            foreach (var rootExpressionIdentity in this.rootExpressionIdentities)
            {
                var expressionGrain = this.Context.GetExpressionGrain(rootExpressionIdentity);
                await expressionGrain.Evaluate(CancellationToken.None); // some technical debt here with the CancellationToken.None, this stuff doesn't seem to propagate between proto.actor message calls?
            }
        }

        private async Task SetDefaultDataVariableValues(RepeatedField<DataVariableBlueprintMsg>? dataVariableBlueprints)
        {
            if (dataVariableBlueprints is null)
            {
                throw new ArgumentNullException(nameof(dataVariableBlueprints));
            }

            foreach (var dataVariableBlueprint in dataVariableBlueprints)
            {
                if (!string.IsNullOrEmpty(dataVariableBlueprint.DefaultValue))
                {
                    await this.UpdateDataVariable(new DataVariable
                    {
                        Name = dataVariableBlueprint.Name,
                        Value = dataVariableBlueprint.DefaultValue,
                        Namespace = dataVariableBlueprint.Namespace,
                    });
                }
            }
        }

        private async Task SetDefaultDataTableValues(RepeatedField<DataTableBlueprintMsg>? dataTableBlueprints)
        {
            if (dataTableBlueprints is null)
            {
                throw new ArgumentNullException(nameof(dataTableBlueprints));
            }

            foreach (var dataTableBlueprint in dataTableBlueprints)
            {
                var dataTableMsg = new DataTableMsg
                {
                    Name = dataTableBlueprint.Name,
                    Namespace = dataTableBlueprint.Namespace,
                    Description = dataTableBlueprint.Description,
                };

                dataTableMsg.DataTableColumns.AddRange(dataTableBlueprint.DataTableColumnBlueprints.Select(dataTableColumnBlueprint =>
                    {
                        var dataTableColumnMsg = new DataTableColumnMsg
                        {
                            Name = !string.IsNullOrEmpty(dataTableColumnBlueprint.Name) ? dataTableColumnBlueprint.Name : string.Empty,
                            Type = !string.IsNullOrEmpty(dataTableColumnBlueprint.Type) ? dataTableColumnBlueprint.Type : string.Empty,
                        };

                        return dataTableColumnMsg;
                    }));

                dataTableMsg.DataTableRows.AddRange(dataTableBlueprint.DataTableRowBlueprints.Select(dataTableRowBlueprint =>
                    {
                        var dataTableRowMsg = new DataTableRowMsg
                        {
                        };

                        if (dataTableRowBlueprint.DataTableCellBlueprints is null)
                        {
                            throw new ArgumentException(nameof(dataTableRowBlueprint.DataTableCellBlueprints));
                        }

                        dataTableRowMsg.DataTableCells.AddRange(dataTableRowBlueprint.DataTableCellBlueprints.Select(dataTableCellBlueprint =>
                        {
                            var dataTableCellMsg = new DataTableCellMsg
                            {
                                Key = !string.IsNullOrEmpty(dataTableCellBlueprint.Key) ? dataTableCellBlueprint.Key : string.Empty,
                                Value = !string.IsNullOrEmpty(dataTableCellBlueprint.Value) ? dataTableCellBlueprint.Value : string.Empty,
                                Name = !string.IsNullOrEmpty(dataTableCellBlueprint.Name) ? dataTableCellBlueprint.Name : string.Empty,
                                Namespace = !string.IsNullOrEmpty(dataTableCellBlueprint.Namespace) ? dataTableCellBlueprint.Namespace : string.Empty,
                            };

                            return dataTableCellMsg;
                        }));

                        return dataTableRowMsg;
                    }));

                await this.UpdateDataTable(dataTableMsg);
            }
        }

        private async Task LinkOutputDataVariable(BlueprintMsg blueprint, OutputDataVariableBlueprintMsg outputDataVariableBlueprint, ExpressionGrainClient expressionGrain)
        {
            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            var name = outputDataVariableBlueprint.Name;
            var nameSpace = outputDataVariableBlueprint.Namespace;
            var outputDatavariableGrainIdentity = this.TryGetDataVariableGrainIdentity(nameSpace, name);

            if (outputDatavariableGrainIdentity is null)
            {
                throw new ArgumentException(nameof(outputDatavariableGrainIdentity));
            }

            var outputDataVariableGrain = this.Context.GetDataVariableGrain(outputDatavariableGrainIdentity);

            await expressionGrain.OutputTo(
                    new DataVariableDependantMsg
                    {
                        Identity = outputDatavariableGrainIdentity,
                        Name = name,
                        Namespace = nameSpace,
                        Type = outputDataVariableBlueprint.Type,
                    },
                    CancellationToken.None);

            await this.logger.Trace(
               new SessionLogLineItem
               {
                   Message = $"Created output data {outputDataVariableBlueprint.Type} variable '{name}' in namespace '{nameSpace}'.",
               }, CancellationToken.None);
        }

        private string GetDataVariableGrainIdentity(string dataVariableNamespace, string? dataVariableName)
        {
            if (dataVariableNamespace is null)
            {
                throw new ArgumentNullException(nameof(dataVariableNamespace));
            }

            if (dataVariableName is null)
            {
                throw new ArgumentNullException(nameof(dataVariableName));
            }

            // Try check if we have and identity for a data variable of this name already
            var dataVariableGrainIdentity = string.Empty;
            var fullyQualifiedDataVariableName = dataVariableNamespace + "_" + dataVariableName;
            if (!this.dataVariableIdentities.TryGetValue(fullyQualifiedDataVariableName, out dataVariableGrainIdentity))
            {
                dataVariableGrainIdentity = Guid.NewGuid().ToString(); // Generate a new identity
                this.dataVariableIdentities.Add(fullyQualifiedDataVariableName, dataVariableGrainIdentity);
            }

            return dataVariableGrainIdentity;
        }

        private string? TryGetDataVariableGrainIdentity(string dataVariableNamespace, string? dataVariableName)
        {
            if (dataVariableNamespace is null)
            {
                throw new ArgumentNullException(nameof(dataVariableNamespace));
            }

            if (dataVariableName is null)
            {
                throw new ArgumentNullException(nameof(dataVariableName));
            }

            var fullyQualifiedDataVariableName = dataVariableNamespace + "_" + dataVariableName;
            var dataVariableIdentity = string.Empty;

            this.dataVariableIdentities.TryGetValue(fullyQualifiedDataVariableName, out dataVariableIdentity);
            if (string.IsNullOrEmpty(dataVariableIdentity))
            {
                // todo, maybe throw an error here?
                Console.WriteLine($"Tried to get data variable '{dataVariableName}' in namespace '{dataVariableNamespace}' that did not exist in the dictionary of known data variables. Probably something wrong with the setup.");
            }

            return dataVariableIdentity;
        }

        private async Task LinkInputDataVariables(BlueprintMsg blueprint, ICollection<InputDataVariableBlueprintMsg> inputDataVariableBlueprints, string expressionGrainIdentity, ExpressionGrainClient expressionGrain)
        {
            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            foreach (var inputDataVariableBlueprint in inputDataVariableBlueprints)
            {
                await this.LinkInputDataVariable(blueprint, inputDataVariableBlueprint, expressionGrainIdentity, expressionGrain);
            }

            await this.logger.Trace(
                new SessionLogLineItem
                {
                    Message = $"Created {inputDataVariableBlueprints.Count} input data variables.",
                }, CancellationToken.None);
        }

        private async Task LinkInputDataTables(BlueprintMsg blueprint, ICollection<InputDataTableBlueprintMsg> inputDataTableBlueprints, string expressionGrainIdentity, ExpressionGrainClient expressionGrain)
        {
            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            foreach (var inputDataTableBlueprint in inputDataTableBlueprints)
            {
                await this.LinkInputDataTable(blueprint, inputDataTableBlueprint, expressionGrainIdentity, expressionGrain);
            }

            await this.logger.Trace(
                new SessionLogLineItem
                {
                    Message = $"Created {inputDataTableBlueprints.Count} input data tables.",
                }, CancellationToken.None);
        }

        private async Task LinkInputDataVariable(BlueprintMsg blueprint, InputDataVariableBlueprintMsg inputDataVariableBlueprint, string expressionGrainIdentity, ExpressionGrainClient expressionGrain)
        {
            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            var name = inputDataVariableBlueprint.Name;

            if (name is null)
            {
                throw new ArgumentException(nameof(name));
            }

            var nameSpace = inputDataVariableBlueprint.Namespace;

            if (nameSpace is null)
            {
                throw new ArgumentException(nameof(nameSpace));
            }

            await expressionGrain.DependOn(
                new DataVariableDependency
                {
                    Name = name,
                    Namespace = nameSpace,
                    Type = inputDataVariableBlueprint.Type,
                },
                CancellationToken.None);

            var dataVariableGrainIdentity = this.TryGetDataVariableGrainIdentity(nameSpace, name);

            if (dataVariableGrainIdentity is null)
            {
                throw new ArgumentException(nameof(dataVariableGrainIdentity));
            }

            var dataVariableGrain = this.Context.GetDataVariableGrain(dataVariableGrainIdentity);

            await dataVariableGrain.RegisterExpressionGrainDependant(
                new GrainIdentity
                {
                    Identity = expressionGrainIdentity,
                },
                CancellationToken.None);

            await this.logger.Trace(
            new SessionLogLineItem
            {
                Message = $"Created input data {inputDataVariableBlueprint.Type} variable '{name}' in namespace '{nameSpace}'.",
            }, CancellationToken.None);
        }

        private async Task LinkInputDataTable(BlueprintMsg blueprint, InputDataTableBlueprintMsg inputDataTableBlueprint, string expressionGrainIdentity, ExpressionGrainClient expressionGrain)
        {
            if (this.logger is null)
            {
                throw new ArgumentException(nameof(this.logger));
            }

            var name = inputDataTableBlueprint.Name;

            if (name is null)
            {
                throw new ArgumentException(nameof(name));
            }

            var dataTableVariablesNamespace = blueprint.DataTableBlueprints.Where(dataTableBlueprints => dataTableBlueprints.Name == name).First().Namespace;

            await expressionGrain.DependOn(
                new DataVariableDependency
                {
                    Name = name,
                    Type = DataVariableTypes.DATATABLE,
                    Namespace = dataTableVariablesNamespace,
                },
                CancellationToken.None);

            var dataVariableGrainIdentity = this.TryGetDataVariableGrainIdentity(dataTableVariablesNamespace, name);

            if (dataVariableGrainIdentity is null)
            {
                throw new ArgumentException(nameof(dataVariableGrainIdentity));
            }

            var dataVariableGrain = this.Context.GetDataVariableGrain(dataVariableGrainIdentity);

            await dataVariableGrain.RegisterExpressionGrainDependant(
                new GrainIdentity
                {
                    Identity = expressionGrainIdentity,
                },
                CancellationToken.None);

            await this.logger.Trace(
            new SessionLogLineItem
            {
                Message = $"Created input data table '{inputDataTableBlueprint.Name}' in namespace '{dataTableVariablesNamespace}'.",
            }, CancellationToken.None);
        }
    }
}