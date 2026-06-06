using System;
using System.Collections.Generic;
using VeloxDev.AI;

namespace AstrolonUI.ViewModels.Workflow.Nodes.Terminal.Models;

[AgentContext(AgentLanguages.Chinese, "Shell工作流介质，沿Shell链传递命令配置并交给ShellAggregate构建CliWrap任务")]
public class ShellMedium
{
    [AgentContext(AgentLanguages.Chinese, "本次Shell链构建的唯一标识")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [AgentContext(AgentLanguages.Chinese, "ShellAggregate执行完成后的结果路由")]
    public ShellResultPipelines Pipeline { get; set; } = ShellResultPipelines.Std_Output;

    [AgentContext(AgentLanguages.Chinese, "按工作流传播顺序收集到的Shell命令段")]
    public List<Shell> Shells { get; set; } = [];

    public ShellMedium Append(IShellProvider provider)
    {
        Shells.Add(Shell.From(provider));
        return this;
    }
}
