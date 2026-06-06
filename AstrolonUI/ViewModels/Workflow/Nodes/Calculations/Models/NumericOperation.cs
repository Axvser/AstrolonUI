using VeloxDev.AI;

namespace AstrolonUI.ViewModels.Workflow.Nodes.Calculations.Models;

[AgentContext(AgentLanguages.Chinese, "数值计算节点支持的二元运算")]
public enum NumericOperation
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulo,
    Power
}
