using AstrolonUI.ViewModels.Workflow.Nodes.Terminal.Models;
using AstrolonUI.ViewModels.Workflow.Trees.Services;
using Avalonia;
using Avalonia.Styling;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VeloxDev.DynamicTheme;
using VeloxDev.MVVM;
using VeloxDev.MVVM.Serialization;
using WorkflowSize = VeloxDev.WorkflowSystem.Size;

namespace AstrolonUI.ViewModels;

public partial class MainViewModel
{
    private int untitledWorkspaceIndex = 1;

    public MainViewModel()
    {
        Workspaces = [];
        Tree = CreateStarterWorkspace();
        Workspaces.Add(Tree);
        StatusMessage = "Ready";
    }

    public ObservableCollection<TreeViewModel> Workspaces { get; }

    [VeloxProperty] public partial TreeViewModel Tree { get; set; }

    [VeloxProperty] public partial string StatusMessage { get; set; }

    public TreeViewModel CreateWorkspace(WorkflowSize? originSize = null)
    {
        var tree = CreateEmptyWorkspace(originSize);
        Workspaces.Add(tree);
        Tree = tree;
        StatusMessage = $"Created {tree.WorkspaceName}";
        return tree;
    }

    public async Task<TreeViewModel> OpenWorkspaceAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(path);
        var opened = Workspaces.FirstOrDefault(workspace =>
            string.Equals(workspace.LocalPath, fullPath, StringComparison.OrdinalIgnoreCase));
        if (opened is not null)
        {
            Tree = opened;
            StatusMessage = $"Selected {opened.WorkspaceName}";
            return opened;
        }

        await using var stream = File.OpenRead(fullPath);
        var restored = await stream.DeserializeFromStreamAsync<TreeViewModel>(
            SerializationOptions.Create().WithIndented(),
            cancellationToken);

        restored.PrepareAfterCheckpointRestore(restored.AgentCheckpoints.ToArray());
        restored.LocalPath = fullPath;
        restored.WorkspaceName = Path.GetFileNameWithoutExtension(fullPath);
        Workspaces.Add(restored);
        Tree = restored;
        StatusMessage = $"Opened {restored.WorkspaceName}";
        return restored;
    }

    public async Task SaveWorkspaceAsync(
        TreeViewModel workspace,
        string path,
        CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = new FileStream(
            fullPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None);
        await workspace.SerializeToStreamAsync(
            stream,
            SerializationOptions.Create().WithIndented(),
            cancellationToken);

        workspace.LocalPath = fullPath;
        workspace.WorkspaceName = Path.GetFileNameWithoutExtension(fullPath);
        StatusMessage = $"Saved {workspace.WorkspaceName}";
    }

    internal async Task<bool> RestoreAgentCheckpointAsync(
        AgentCheckpointViewModel checkpoint)
    {
        if (!checkpoint.Snapshot.TryDeserialize<TreeViewModel>(out var restored) ||
            restored is null)
        {
            StatusMessage = "Checkpoint restore failed";
            return false;
        }

        var previous = Tree;
        previous.CancelAgentRequestCommand.Execute(null);
        restored.AgentTranscriptDocument.Text = checkpoint.Conversation;
        restored.PrepareAfterCheckpointRestore(previous.AgentCheckpoints.ToArray());
        restored.LocalPath = previous.LocalPath;
        restored.WorkspaceName = previous.WorkspaceName;

        if (restored.GetHelper() is AgentHelper helper)
        {
            try
            {
                await helper.RestoreSessionAsync(checkpoint.AgentSession);
            }
            catch
            {
                StatusMessage = "Agent session restore failed";
                return false;
            }
        }

        restored.ShowCheckpointRestoreNotice(checkpoint.CreatedAtText);
        var index = Workspaces.IndexOf(previous);
        if (index >= 0)
        {
            Workspaces[index] = restored;
        }
        else
        {
            Workspaces.Add(restored);
        }

        Tree = restored;
        StatusMessage = $"Restored {checkpoint.CreatedAtText}";
        return true;
    }

    [VeloxCommand]
    private void ReverseTheme()
    {
        if (ThemeManager.Current == typeof(Dark))
        {
            ThemeManager.Jump<Light>();
            if (Application.Current is { } app)
            {
                app.RequestedThemeVariant = ThemeVariant.Light;
            }
        }
        else
        {
            ThemeManager.Jump<Dark>();
            if (Application.Current is { } app)
            {
                app.RequestedThemeVariant = ThemeVariant.Dark;
            }
        }
    }

    private TreeViewModel CreateEmptyWorkspace(WorkflowSize? originSize = null)
    {
        var tree = new TreeViewModel
        {
            WorkspaceName = $"Untitled {untitledWorkspaceIndex++}"
        };

        if (originSize is { } size)
        {
            tree.Layout.OriginSize = size;
        }

        return tree;
    }

    private TreeViewModel CreateStarterWorkspace()
    {
        var tree = CreateEmptyWorkspace();
        var controller = new ShellControllorViewModel
        {
            Name = "Shell Controller",
            Size = new WorkflowSize(220, 120),
            Anchor = new() { Horizontal = 60, Vertical = 80 }
        };
        var shell = new ShellConfigViewModel
        {
            Name = ".NET CLI",
            Size = new WorkflowSize(340, 260),
            Anchor = new() { Horizontal = 320, Vertical = 40 },
            ExecutionPath = Directory.GetCurrentDirectory(),
            Wrap = new ShellElement { Value = "dotnet" },
            Arguments = [new ShellElement { Value = "--version" }]
        };
        var aggregate = new ShellAggregateViewModel
        {
            Name = "Shell Aggregate",
            Size = new WorkflowSize(260, 140),
            Anchor = new() { Horizontal = 720, Vertical = 90 }
        };

        tree.CreateNodeCommand.Execute(controller);
        tree.CreateNodeCommand.Execute(shell);
        tree.CreateNodeCommand.Execute(aggregate);
        return tree;
    }
}
