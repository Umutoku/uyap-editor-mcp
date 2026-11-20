using System.Globalization;
using System.Resources;

namespace UyapEditorMcp;

internal static class EditorResources
{
    private static readonly ResourceManager Messages =
        new("UyapEditorMcp.Resources.EditorMessages", typeof(EditorResources).Assembly);

    internal static string Get(string key, params object?[] arguments)
    {
        var template = Messages.GetString(key, CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException(key);
        return arguments.Length == 0 ? template : string.Format(CultureInfo.CurrentCulture, template, arguments);
    }
}
