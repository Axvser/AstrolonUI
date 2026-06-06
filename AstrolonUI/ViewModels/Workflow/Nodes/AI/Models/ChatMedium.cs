using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Collections.Generic;

namespace AstrolonUI.ViewModels.Workflow.Nodes.AI.Models;

public struct ChatMedium
{
    public string Prompt { get; set; }
    public string Message { get; set; }
    public AgentSession? Session { get; set; }
    public IList<AITool> Tools { get; set; }
    public bool AllowStreaming { get; set; }
}
