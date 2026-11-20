using System.ComponentModel;

namespace UyapEditorMcp;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Parameter)]
internal sealed class ResourceDescriptionAttribute(string key) : DescriptionAttribute
{
    public override string Description => EditorResources.Get(key);
}
