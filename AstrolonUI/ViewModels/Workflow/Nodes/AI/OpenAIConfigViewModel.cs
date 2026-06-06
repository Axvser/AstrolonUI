using AstrolonUI.ViewModels.Workflow.Nodes.AI.Models;
using AstrolonUI.ViewModels.Workflow.Nodes.AI.Services;
using VeloxDev.AI;
using VeloxDev.MVVM;
using VeloxDev.WorkflowSystem;

namespace AstrolonUI.ViewModels;

[AgentContext(AgentLanguages.Chinese, "OpenAI专用配置节点，输出兼容OpenAI接口的服务的配置")]
[AgentContext(AgentLanguages.Chinese, "默认大小：320x180")]
[WorkflowBuilder.Node<OpenAIConfigService>]
public partial class OpenAIConfigViewModel : IOpenAIProvider
{
    [AgentContext(AgentLanguages.Chinese, "节点名称")]
    [VeloxProperty] public partial string Name { get; set; }

    [AgentContext(AgentLanguages.Chinese, "本机环境变量，对应服务所需的 API_KEY")]
    [VeloxProperty] public partial string EnvironmentVariableName { get; set; }

    [AgentContext(AgentLanguages.Chinese, "服务请求地址，例如：https://api.deepseek.com")]
    [VeloxProperty] public partial string Endpoint { get; set; }

    [AgentContext(AgentLanguages.Chinese, "模型名称，例如：deepseek-v4-flash")]
    [VeloxProperty] public partial string Model { get; set; }

    [AgentContext(AgentLanguages.Chinese, "输出口")]
    [VeloxProperty] public partial SlotViewModel OutputSlot { get; set; }

    public OpenAIConfigViewModel()
    {
        InitializeWorkflow();
        Name = "OpenAI Config";
        EnvironmentVariableName = string.Empty;
        Endpoint = string.Empty;
        Model = string.Empty;
        OutputSlot = new SlotViewModel()
        {
            Channel = SlotChannel.MultipleTargets
        };
    }
}