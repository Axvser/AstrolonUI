using AstrolonUI.ViewModels;
using AstrolonUI.ViewModels.Workflow.Nodes.Terminal.Models;
using Avalonia.Controls;
using Avalonia.Input;
using System.IO;

namespace AstrolonUI.Views
{
    public partial class MainView : UserControl
    {
        public MainView()
        {
            InitializeComponent();

            Loaded += async (s, e) =>
            {
                MainViewModel context = new();
                TreeViewModel tree = new();
                tree.Layout.OriginSize = new(Bounds.Width, Bounds.Height);
                ShellControllorViewModel controller = new()
                {
                    Name = "Shell Controller",
                    Size = new(220, 120),
                    Anchor = new() { Horizontal = 60, Vertical = 80 }
                };
                ShellConfigViewModel shell = new()
                {
                    Name = ".NET CLI",
                    Size = new(340, 260),
                    Anchor = new() { Horizontal = 320, Vertical = 40 },
                    ExecutionPath = Directory.GetCurrentDirectory(),
                    Wrap = new ShellElement() { Value = "dotnet" },
                    Arguments = [new ShellElement() { Value = "--version" }]
                };
                ShellAggregateViewModel aggregate = new()
                {
                    Name = "Shell Aggregate",
                    Size = new(260, 140),
                    Anchor = new() { Horizontal = 720, Vertical = 90 }
                };
                tree.CreateNodeCommand.Execute(controller);
                tree.CreateNodeCommand.Execute(shell);
                tree.CreateNodeCommand.Execute(aggregate);
                context.Tree = tree;
                DataContext = context;
            };
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

        private void Ask_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.Tree is TreeViewModel tree)
            {
                var message = AgentInput.Text;
                tree.AskCommand.Execute(message);
                AgentInput.Text = string.Empty;
            }
        }

        private async void RestoreCheckpoint_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Control { DataContext: AgentCheckpointViewModel checkpoint } &&
                DataContext is MainViewModel vm)
            {
                await vm.RestoreAgentCheckpointAsync(checkpoint);
            }
        }
    }
}



