namespace AstrolonUI.ViewModels.Workflow.Nodes.Terminal.Models;

public interface IShellExecutionState
{
    public bool IsRunning { get; }

    public void SetRunningState(bool isRunning);
}
