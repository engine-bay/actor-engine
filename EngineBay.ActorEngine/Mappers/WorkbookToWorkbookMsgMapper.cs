namespace EngineBay.ActorEngine
{
    using System;
    using EngineBay.Blueprints;

    public static class WorkbookToWorkbookMsgMapper
    {
        // This static mapper is temporarily placed here as we work through refactoring the blueprints module into the open-sourced EngineBay.ActorEngine module.
        // We probably need the Grains.proto and Messages.proto generation moved to EngineBay.Core if we want the mapping logic to live on the Models again?
        public static WorkbookMsg Map(Workbook workbook)
        {
            if (workbook is null)
            {
                throw new ArgumentNullException(nameof(workbook));
            }

            // Todo - yoh...this mapping horrid....clean it up!
            var workbookMsg = new WorkbookMsg
            {
                Id = workbook.Id.ToString(),
                Name = string.IsNullOrEmpty(workbook.Name) ? workbook.Name : string.Empty,
                Description = string.IsNullOrEmpty(workbook.Description) ? workbook.Description : string.Empty,
            };

            if (workbook.Blueprints is null)
            {
                throw new ArgumentException(nameof(workbook.Blueprints));
            }

            workbookMsg.Blueprints.AddRange(workbook.Blueprints.Select(blueprint =>
            {
                var blueprintMsg = new BlueprintMsg
                {
                    Id = blueprint.Id.ToString(),
                    Name = !string.IsNullOrEmpty(blueprint.Name) ? blueprint.Name : string.Empty,
                    Description = !string.IsNullOrEmpty(blueprint.Description) ? blueprint.Description : string.Empty,
                };

                // map the expressions
                if (blueprint.ExpressionBlueprints is null)
                {
                    throw new ArgumentException(nameof(blueprint.ExpressionBlueprints));
                }

                blueprintMsg.ExpressionBlueprints.AddRange(blueprint.ExpressionBlueprints.Select(expressionBlueprint =>
                {
                    if (expressionBlueprint.OutputDataVariableBlueprint is null)
                    {
                        throw new ArgumentException(nameof(expressionBlueprint.OutputDataVariableBlueprint));
                    }

                    var expressionBlueprintMsg = new ExpressionBlueprintMsg
                    {
                        Id = expressionBlueprint.Id.ToString(),
                        Expression = !string.IsNullOrEmpty(expressionBlueprint.Expression) ? expressionBlueprint.Expression : string.Empty,
                        Objective = !string.IsNullOrEmpty(expressionBlueprint.Objective) ? expressionBlueprint.Objective : string.Empty,
                        OutputDataVariableBlueprint = new OutputDataVariableBlueprintMsg
                        {
                            Id = expressionBlueprint.OutputDataVariableBlueprint.Id.ToString(),
                            Name = !string.IsNullOrEmpty(expressionBlueprint.OutputDataVariableBlueprint.Name) ? expressionBlueprint.OutputDataVariableBlueprint.Name : string.Empty,
                            Namespace = !string.IsNullOrEmpty(expressionBlueprint.OutputDataVariableBlueprint.Namespace) ? expressionBlueprint.OutputDataVariableBlueprint.Namespace : string.Empty,
                            Type = !string.IsNullOrEmpty(expressionBlueprint.OutputDataVariableBlueprint.Type) ? expressionBlueprint.OutputDataVariableBlueprint.Type : string.Empty,
                        },
                    };

                    if (expressionBlueprint.InputDataVariableBlueprints is null)
                    {
                        throw new ArgumentException(nameof(expressionBlueprint.InputDataVariableBlueprints));
                    }

                    expressionBlueprintMsg.InputDataVariableBlueprints.AddRange(expressionBlueprint.InputDataVariableBlueprints.Select(inputDataVariable =>
                    {
                        var inputDataVariableMsg = new InputDataVariableBlueprintMsg
                        {
                            Id = inputDataVariable.Id.ToString(),
                            Name = !string.IsNullOrEmpty(inputDataVariable.Name) ? inputDataVariable.Name : string.Empty,
                            Namespace = !string.IsNullOrEmpty(inputDataVariable.Namespace) ? inputDataVariable.Namespace : string.Empty,
                            Type = !string.IsNullOrEmpty(inputDataVariable.Type) ? inputDataVariable.Type : string.Empty,
                        };

                        return inputDataVariableMsg;
                    }));

                    if (expressionBlueprint.InputDataTableBlueprints is null)
                    {
                        throw new ArgumentException(nameof(expressionBlueprint.InputDataTableBlueprints));
                    }

                    expressionBlueprintMsg.InputDataTableBlueprints.AddRange(expressionBlueprint.InputDataTableBlueprints.Select(inputDataTable =>
                    {
                        var inputDataTableMsg = new InputDataTableBlueprintMsg
                        {
                            Id = inputDataTable.Id.ToString(),
                            Name = !string.IsNullOrEmpty(inputDataTable.Name) ? inputDataTable.Name : string.Empty,
                            Namespace = !string.IsNullOrEmpty(inputDataTable.Namespace) ? inputDataTable.Namespace : string.Empty,
                        };

                        return inputDataTableMsg;
                    }));

                    return expressionBlueprintMsg;
                }));

                // map the data variables
                if (blueprint.DataVariableBlueprints is null)
                {
                    throw new ArgumentException(nameof(blueprint.DataVariableBlueprints));
                }

                blueprintMsg.DataVariableBlueprints.AddRange(blueprint.DataVariableBlueprints.Select(dataVariableBlueprint =>
                {
                    var dataVariableBlueprintMsg = new DataVariableBlueprintMsg
                    {
                        Id = dataVariableBlueprint.Id.ToString(),
                        Name = !string.IsNullOrEmpty(dataVariableBlueprint.Name) ? dataVariableBlueprint.Name : string.Empty,
                        Namespace = !string.IsNullOrEmpty(dataVariableBlueprint.Namespace) ? dataVariableBlueprint.Namespace : string.Empty,
                        Description = !string.IsNullOrEmpty(dataVariableBlueprint.Description) ? dataVariableBlueprint.Description : string.Empty,
                        Type = !string.IsNullOrEmpty(dataVariableBlueprint.Type) ? dataVariableBlueprint.Type : string.Empty,
                        DefaultValue = !string.IsNullOrEmpty(dataVariableBlueprint.DefaultValue) ? dataVariableBlueprint.DefaultValue : string.Empty,
                    };

                    return dataVariableBlueprintMsg;
                }));

                // map the data tables
                if (blueprint.DataTableBlueprints is null)
                {
                    throw new ArgumentException(nameof(blueprint.DataTableBlueprints));
                }

                blueprintMsg.DataTableBlueprints.AddRange(blueprint.DataTableBlueprints.Select(dataTableBlueprint =>
                {
                    var dataTableBlueprintMsg = new DataTableBlueprintMsg
                    {
                        Id = dataTableBlueprint.Id.ToString(),
                        Name = !string.IsNullOrEmpty(dataTableBlueprint.Name) ? dataTableBlueprint.Name : string.Empty,
                        Namespace = !string.IsNullOrEmpty(dataTableBlueprint.Namespace) ? dataTableBlueprint.Namespace : string.Empty,
                        Description = !string.IsNullOrEmpty(dataTableBlueprint.Description) ? dataTableBlueprint.Description : string.Empty,
                    };

                    if (dataTableBlueprint.InputDataVariableBlueprints is null)
                    {
                        throw new ArgumentException(nameof(dataTableBlueprint.InputDataVariableBlueprints));
                    }

                    dataTableBlueprintMsg.InputDataVariableBlueprints.AddRange(dataTableBlueprint.InputDataVariableBlueprints.Select(inputDataVariableBlueprints =>
                    {
                        var inputDataVariableBlueprintMsg = new InputDataVariableBlueprintMsg
                        {
                            Id = inputDataVariableBlueprints.Id.ToString(),
                            Name = !string.IsNullOrEmpty(inputDataVariableBlueprints.Name) ? inputDataVariableBlueprints.Name : string.Empty,
                            Namespace = !string.IsNullOrEmpty(inputDataVariableBlueprints.Namespace) ? inputDataVariableBlueprints.Namespace : string.Empty,
                            Type = !string.IsNullOrEmpty(inputDataVariableBlueprints.Type) ? inputDataVariableBlueprints.Type : string.Empty,
                        };

                        return inputDataVariableBlueprintMsg;
                    }));

                    if (dataTableBlueprint.DataTableColumnBlueprints is null)
                    {
                        throw new ArgumentException(nameof(dataTableBlueprint.DataTableColumnBlueprints));
                    }

                    dataTableBlueprintMsg.DataTableColumnBlueprints.AddRange(dataTableBlueprint.DataTableColumnBlueprints.Select(dataTableColumnBlueprint =>
                    {
                        var dataTableColumnBlueprintMsg = new DataTableColumnBlueprintMsg
                        {
                            Id = dataTableColumnBlueprint.Id.ToString(),
                            Name = !string.IsNullOrEmpty(dataTableColumnBlueprint.Name) ? dataTableColumnBlueprint.Name : string.Empty,
                            Type = !string.IsNullOrEmpty(dataTableColumnBlueprint.Type) ? dataTableColumnBlueprint.Type : string.Empty,
                        };

                        return dataTableColumnBlueprintMsg;
                    }));

                    if (dataTableBlueprint.DataTableRowBlueprints is null)
                    {
                        throw new ArgumentException(nameof(dataTableBlueprint.DataTableRowBlueprints));
                    }

                    dataTableBlueprintMsg.DataTableRowBlueprints.AddRange(dataTableBlueprint.DataTableRowBlueprints.Select(dataTableRowBlueprint =>
                    {
                        var dataTableRowBlueprintMsg = new DataTableRowBlueprintMsg
                        {
                            Id = dataTableRowBlueprint.Id.ToString(),
                        };

                        if (dataTableRowBlueprint.DataTableCellBlueprints is null)
                        {
                            throw new ArgumentException(nameof(dataTableRowBlueprint.DataTableCellBlueprints));
                        }

                        dataTableRowBlueprintMsg.DataTableCellBlueprints.AddRange(dataTableRowBlueprint.DataTableCellBlueprints.Select(dataTableCellBlueprint =>
                        {
                            var dataTableCellBlueprintMsg = new DataTableCellBlueprintMsg
                            {
                                Id = dataTableCellBlueprint.Id.ToString(),
                                Key = !string.IsNullOrEmpty(dataTableCellBlueprint.Key) ? dataTableCellBlueprint.Key : string.Empty,
                                Value = !string.IsNullOrEmpty(dataTableCellBlueprint.Value) ? dataTableCellBlueprint.Value : string.Empty,
                                Name = !string.IsNullOrEmpty(dataTableCellBlueprint.Name) ? dataTableCellBlueprint.Name : string.Empty,
                                Namespace = !string.IsNullOrEmpty(dataTableCellBlueprint.Namespace) ? dataTableCellBlueprint.Namespace : string.Empty,
                            };

                            return dataTableCellBlueprintMsg;
                        }));

                        return dataTableRowBlueprintMsg;
                    }));

                    return dataTableBlueprintMsg;
                }));

                // map the triggers
                if (blueprint.TriggerBlueprints is null)
                {
                    throw new ArgumentException(nameof(blueprint.TriggerBlueprints));
                }

                blueprintMsg.TriggerBlueprints.AddRange(blueprint.TriggerBlueprints.Select(triggerBlueprint =>
                {
                    if (triggerBlueprint.OutputDataVariableBlueprint is null)
                    {
                        throw new ArgumentException(nameof(triggerBlueprint.OutputDataVariableBlueprint));
                    }

                    var triggerBlueprintMsg = new TriggerBlueprintMsg
                    {
                        Id = triggerBlueprint.Id.ToString(),
                        Name = !string.IsNullOrEmpty(triggerBlueprint.Name) ? triggerBlueprint.Name : string.Empty,
                        Description = !string.IsNullOrEmpty(triggerBlueprint.Description) ? triggerBlueprint.Description : string.Empty,
                        OutputDataVariableBlueprint = new OutputDataVariableBlueprintMsg
                        {
                            Id = triggerBlueprint.OutputDataVariableBlueprint.Id.ToString(),
                            Name = !string.IsNullOrEmpty(triggerBlueprint.OutputDataVariableBlueprint.Name) ? triggerBlueprint.OutputDataVariableBlueprint.Name : string.Empty,
                            Namespace = !string.IsNullOrEmpty(triggerBlueprint.OutputDataVariableBlueprint.Namespace) ? triggerBlueprint.OutputDataVariableBlueprint.Namespace : string.Empty,
                            Type = !string.IsNullOrEmpty(triggerBlueprint.OutputDataVariableBlueprint.Type) ? triggerBlueprint.OutputDataVariableBlueprint.Type : string.Empty,
                        },
                    };

                    if (triggerBlueprint.TriggerExpressionBlueprints is null)
                    {
                        throw new ArgumentException(nameof(triggerBlueprint.TriggerExpressionBlueprints));
                    }

                    triggerBlueprintMsg.TriggerExpressionBlueprints.AddRange(triggerBlueprint.TriggerExpressionBlueprints.Select(triggerExpressionBlueprint =>
                    {
                        if (triggerExpressionBlueprint.InputDataVariableBlueprint is null)
                        {
                            throw new ArgumentException(nameof(triggerExpressionBlueprint.InputDataVariableBlueprint));
                        }

                        var triggerExpressionBlueprintMsg = new TriggerExpressionBlueprintMsg
                        {
                            Id = triggerExpressionBlueprint.Id.ToString(),
                            Expression = !string.IsNullOrEmpty(triggerExpressionBlueprint.Expression) ? triggerExpressionBlueprint.Expression : string.Empty,
                            Objective = !string.IsNullOrEmpty(triggerExpressionBlueprint.Objective) ? triggerExpressionBlueprint.Objective : string.Empty,
                            InputDataVariableBlueprint = new InputDataVariableBlueprintMsg
                            {
                                Id = triggerExpressionBlueprint.InputDataVariableBlueprint.Id.ToString(),
                                Name = !string.IsNullOrEmpty(triggerExpressionBlueprint.InputDataVariableBlueprint.Name) ? triggerExpressionBlueprint.InputDataVariableBlueprint.Name : string.Empty,
                                Namespace = !string.IsNullOrEmpty(triggerExpressionBlueprint.InputDataVariableBlueprint.Namespace) ? triggerExpressionBlueprint.InputDataVariableBlueprint.Namespace : string.Empty,
                                Type = !string.IsNullOrEmpty(triggerExpressionBlueprint.InputDataVariableBlueprint.Type) ? triggerExpressionBlueprint.InputDataVariableBlueprint.Type : string.Empty,
                            },
                        };

                        return triggerExpressionBlueprintMsg;
                    }));

                    return triggerBlueprintMsg;
                }));

                return blueprintMsg;
            }));
            return workbookMsg;
        }
    }
}