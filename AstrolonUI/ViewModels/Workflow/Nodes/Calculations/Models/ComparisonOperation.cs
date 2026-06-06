using VeloxDev.AI;

namespace AstrolonUI.ViewModels.Workflow.Nodes.Calculations.Models;

[AgentContext(AgentLanguages.Chinese, "流程比较节点支持的比较运算")]
public enum ComparisonOperation
{
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Contains,
    StartsWith,
    EndsWith,
    IsNullOrEmpty
}
