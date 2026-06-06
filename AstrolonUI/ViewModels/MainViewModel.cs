using AstrolonUI.ViewModels.Workflow.Trees.Services;
using Avalonia;
using Avalonia.Styling;
using System.Linq;
using System.Threading.Tasks;
using VeloxDev.DynamicTheme;
using VeloxDev.MVVM;
using VeloxDev.MVVM.Serialization;

namespace AstrolonUI.ViewModels;

public partial class MainViewModel
{
    [VeloxProperty] private TreeViewModel tree = new();

    internal async Task<bool> RestoreAgentCheckpointAsync(AgentCheckpointViewModel checkpoint)
    {
        if (!checkpoint.Snapshot.TryDeserialize<TreeViewModel>(out var restored) ||
            restored is null)
        {
            return false;
        }

        Tree.CancelAgentRequestCommand.Execute(null);
        restored.AgentTranscriptDocument.Text = checkpoint.Conversation;
        restored.PrepareAfterCheckpointRestore(Tree.AgentCheckpoints.ToArray());

        if (restored.GetHelper() is AgentHelper helper)
        {
            try
            {
                await helper.RestoreSessionAsync(checkpoint.AgentSession);
            }
            catch
            {
                return false;
            }
        }

        restored.ShowCheckpointRestoreNotice(checkpoint.CreatedAtText);
        Tree = restored;
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
}
