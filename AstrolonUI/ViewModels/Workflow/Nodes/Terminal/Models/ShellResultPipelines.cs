using System;
using VeloxDev.AI;

namespace AstrolonUI.ViewModels.Workflow.Nodes.Terminal.Models;

[Flags]
[AgentContext(AgentLanguages.Chinese, "命令行执行结果可凭此路由到不同处理方式")]
public enum ShellResultPipelines : int
{
    [AgentContext(AgentLanguages.Chinese, "标准输出")] Std_Output = 1,
    [AgentContext(AgentLanguages.Chinese, "标准错误")] Std_Error = 2,
    [AgentContext(AgentLanguages.Chinese, "混合")] Mixed = Std_Output | Std_Error
}
