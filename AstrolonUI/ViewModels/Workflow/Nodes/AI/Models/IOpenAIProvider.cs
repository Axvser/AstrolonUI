namespace AstrolonUI.ViewModels.Workflow.Nodes.AI.Models;

public interface IOpenAIProvider : IAIProvider
{
    public string EnvironmentVariableName { get; set; }
    public string Endpoint { get; set; }
    public string Model { get; set; }
}
