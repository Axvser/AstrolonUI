using System;

namespace AstrolonUI.ViewModels.Workflow.Nodes.AI.Models;

public interface IChatCallBackProvider
{
    public EventHandler<string>? OnSyncResponse { get; }
    public EventHandler<string>? OnStreamChunk { get; }
}
