using System;
using System.Collections.ObjectModel;
using System.Linq;
using VeloxDev.AI;

namespace AstrolonUI.ViewModels.Workflow.Nodes.Calculations.Models;

[AgentContext(AgentLanguages.Chinese, "计算工作流介质，携带唯一Id、期望参数数量和已收集的计算参数")]
public class CalculationMedium
{
    [AgentContext(AgentLanguages.Chinese, "本次计算链路唯一标识")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [AgentContext(AgentLanguages.Chinese, "计算节点需要等待的参数数量")]
    public int RequiredParameterCount { get; set; } = 2;

    [AgentContext(AgentLanguages.Chinese, "已收集的计算参数")]
    public ObservableCollection<CalculationParameter> Parameters { get; set; } = [];

    [AgentContext(AgentLanguages.Chinese, "计算结果；未完成时为空")]
    public double? Result { get; set; }

    [AgentContext(AgentLanguages.Chinese, "是否已完成计算")]
    public bool IsCompleted { get; set; }

    [AgentContext(AgentLanguages.Chinese, "最近执行的计算操作")]
    public string OperationName { get; set; } = string.Empty;

    public CalculationMedium Append(string source, double value)
    {
        Parameters.Add(new CalculationParameter
        {
            Source = source,
            Value = value,
            CreatedAt = DateTimeOffset.Now
        });
        return this;
    }

    public CalculationMedium CloneSnapshot()
        => new()
        {
            Id = Id,
            RequiredParameterCount = RequiredParameterCount,
            Parameters = [.. Parameters.Select(parameter => parameter.CloneSnapshot())],
            Result = Result,
            IsCompleted = IsCompleted,
            OperationName = OperationName
        };

    public override string ToString()
        => IsCompleted && Result is not null
            ? Result.Value.ToString("G")
            : $"{Id}: {Parameters.Count}/{RequiredParameterCount}";
}

[AgentContext(AgentLanguages.Chinese, "单个计算参数")]
public class CalculationParameter
{
    [AgentContext(AgentLanguages.Chinese, "参数来源节点")]
    public string Source { get; set; } = string.Empty;

    [AgentContext(AgentLanguages.Chinese, "参数数值")]
    public double Value { get; set; }

    [AgentContext(AgentLanguages.Chinese, "参数创建时间")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

    public CalculationParameter CloneSnapshot()
        => new()
        {
            Source = Source,
            Value = Value,
            CreatedAt = CreatedAt
        };
}
