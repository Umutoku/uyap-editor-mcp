namespace UyapEditorMcp;

internal sealed class EditorFailure : Exception
{
    internal EditorFailure(EditorError error, params object?[] arguments)
        : this(error.Code, EditorResources.Get(error.MessageKey, arguments))
    {
    }

    private EditorFailure(string code, string message) : base(message)
    {
        Code = code;
    }

    internal string Code
    {
        get;
    }

    internal static EditorFailure FromNative(string? code) =>
        new(code ?? EditorErrors.EditorUiError.Code, EditorResources.Get(EditorErrors.EditorUiError.MessageKey));
}
