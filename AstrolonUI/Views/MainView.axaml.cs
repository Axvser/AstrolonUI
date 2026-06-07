using AstrolonUI.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AstrolonUI.Views;

public partial class MainView : UserControl
{
    private static readonly FilePickerFileType WorkflowFileType = new("Astrolon Workflow")
    {
        Patterns = ["*.json"],
        MimeTypes = ["application/json"]
    };

    public MainView()
    {
        InitializeComponent();
        Loaded += MainView_Loaded;
    }

    private void MainView_Loaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            UpdateWorkspaceOrigin(vm.Tree);
        }
    }

    private void NewWorkflow_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.CreateWorkspace(GetCanvasSize());
        }
    }

    private async void OpenWorkflow_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm ||
            TopLevel.GetTopLevel(this)?.StorageProvider is not { } storageProvider)
        {
            return;
        }

        try
        {
            var files = await storageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "打开 Astrolon 工作流",
                    AllowMultiple = false,
                    FileTypeFilter = [WorkflowFileType, FilePickerFileTypes.Json]
                });
            var path = files.Count == 1 ? files[0].TryGetLocalPath() : null;
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var workspace = await vm.OpenWorkspaceAsync(path);
            UpdateWorkspaceOrigin(workspace);
        }
        catch (Exception exception)
        {
            vm.StatusMessage = $"Open failed: {exception.Message}";
        }
    }

    private async void SaveWorkflow_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(vm.Tree.LocalPath))
        {
            await SaveWorkspaceAsAsync(vm);
            return;
        }

        await SaveWorkspaceAsync(vm, vm.Tree.LocalPath);
    }

    private async void SaveWorkflowAs_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            await SaveWorkspaceAsAsync(vm);
        }
    }

    private async Task SaveWorkspaceAsAsync(MainViewModel vm)
    {
        if (TopLevel.GetTopLevel(this)?.StorageProvider is not { } storageProvider)
        {
            return;
        }

        try
        {
            var file = await storageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = "保存 Astrolon 工作流",
                    SuggestedFileName = $"{vm.Tree.WorkspaceName}.json",
                    DefaultExtension = "json",
                    FileTypeChoices = [WorkflowFileType, FilePickerFileTypes.Json],
                    ShowOverwritePrompt = true
                });
            var path = file?.TryGetLocalPath();
            if (!string.IsNullOrWhiteSpace(path))
            {
                await SaveWorkspaceAsync(vm, path);
            }
        }
        catch (Exception exception)
        {
            vm.StatusMessage = $"Save failed: {exception.Message}";
        }
    }

    private static async Task SaveWorkspaceAsync(
        MainViewModel vm,
        string path)
    {
        try
        {
            await vm.SaveWorkspaceAsync(vm.Tree, path);
        }
        catch (Exception exception)
        {
            vm.StatusMessage = $"Save failed: {exception.Message}";
        }
    }

    private void WorkspaceSelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            UpdateWorkspaceOrigin(vm.Tree);
            vm.StatusMessage = $"Selected {vm.Tree.WorkspaceName}";
        }
    }

    private void NodeTool_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: NodeToolViewModel tool })
        {
            WorkflowGridDecorator.BeginToolDrag(tool);
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    private void Ask_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            var message = AgentInput.Text;
            vm.Tree.AskCommand.Execute(message);
            AgentInput.Text = string.Empty;
        }
    }

    private async void RestoreCheckpoint_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: AgentCheckpointViewModel checkpoint } &&
            DataContext is MainViewModel vm)
        {
            await vm.RestoreAgentCheckpointAsync(checkpoint);
        }
    }

    private VeloxDev.WorkflowSystem.Size GetCanvasSize()
        => new()
        {
            Width = Math.Max(1, Bounds.Width - 365),
            Height = Math.Max(1, Bounds.Height - 42)
        };

    private void UpdateWorkspaceOrigin(TreeViewModel workspace)
    {
        workspace.Layout.OriginSize = GetCanvasSize();
        workspace.Layout.UpdateCommand.Execute(null);
    }
}
