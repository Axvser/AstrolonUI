using System.Collections.Generic;
using VeloxDev.AI;
using VeloxDev.MVVM;

namespace AstrolonUI.ViewModels.Workflow.Nodes.Terminal.Models;

[AgentContext(AgentLanguages.Chinese, "包装CLI需要的字符串元素")]
public partial class ShellElement
{
    [AgentContext(AgentLanguages.Chinese, "内部机制决定")]
    [VeloxProperty] private object? _parent = null;

    [AgentContext(AgentLanguages.Chinese, "字符串实际值，视位置不同，可以作为Wrap头，也可作为Argument段")]
    [VeloxProperty] private string _value = string.Empty;

    [VeloxCommand]
    private void Delete()
    {
        if (Parent is ICollection<ShellElement> collection)
        {
            collection.Remove(this);
        }
    }

    public override bool Equals(object? obj) =>
        (obj is string str && str == Value) ||
        (obj is ShellElement element && element.Value == Value) ||
        (obj is not null && obj.ToString() == Value);

    public override int GetHashCode() => Value.GetHashCode();
}
