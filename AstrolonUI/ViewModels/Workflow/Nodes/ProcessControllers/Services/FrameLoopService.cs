using AstrolonUI.ViewModels.Workflow.Nodes.ProcessControllers.Models;
using Avalonia.Threading;
using System;
using System.Threading;
using System.Threading.Tasks;
using VeloxDev.TimeLine;
using VeloxDev.WorkflowSystem;

namespace AstrolonUI.ViewModels.Workflow.Nodes.ProcessControllers.Services;

[MonoBehaviour(
    channel: nameof(FrameLoopService),
    fps: 60)]
public partial class FrameLoopService : NodeHelper
{
    public override void Install(IWorkflowNodeViewModel node)
    {
        base.Install(node);
        InitializeMonoBehaviour(); // 加入循环任务
    }

    public override void Uninstall(IWorkflowNodeViewModel node)
    {
        base.Uninstall(node);
        CloseMonoBehaviour(); // 移出循环任务
    }

    partial void Update(FrameEventArgs e)
    {
        if (source is not null && listener is not null)
        {
            if (listener.Invoke(source)) // 循环检测，直到满足条件才继续传播
            {
                Dispatcher.UIThread.Invoke(async () =>
                {
                    var cts = new CancellationTokenSource();
                    await BroadcastAsync(null, cts.Token);
                });
                source = null;
                listener = null;
            }
        }
    }

    private object? source;
    private Func<object, bool>? listener;

    public override Task WorkAsync(object? parameter, CancellationToken ct)
    {
        if (parameter is ListenerMedium<object> medium)
        {
            Interlocked.Exchange(ref source, medium.Source);
            Interlocked.Exchange(ref listener, medium.Listener);
        }
        return Task.CompletedTask;
    }
}
