namespace AstrolonUI.ViewModels.Workflow.Nodes.AI.Models;

public struct OpenAIConfig(string env, string url, string model) : IOpenAIProvider
{
    public string EnvironmentVariableName { get; set; } = env;
    public string Endpoint { get; set; } = url;
    public string Model { get; set; } = model;

    public static OpenAIConfig From(IOpenAIProvider provider)
        => new()
        {
            EnvironmentVariableName = provider.EnvironmentVariableName,
            Endpoint = provider.Endpoint,
            Model = provider.Model
        };
}
