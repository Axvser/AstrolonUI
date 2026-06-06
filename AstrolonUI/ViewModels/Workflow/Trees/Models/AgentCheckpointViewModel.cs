using System;

namespace AstrolonUI.ViewModels;

public sealed class AgentCheckpointViewModel
{
    public required string Id { get; init; }

    public required string Summary { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required string Snapshot { get; init; }

    public required string AgentSession { get; init; }

    public required string Conversation { get; init; }

    public string CreatedAtText => CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");

    public string SnapshotSizeText =>
        $"{Snapshot.Length + AgentSession.Length + Conversation.Length:N0} chars";
}
