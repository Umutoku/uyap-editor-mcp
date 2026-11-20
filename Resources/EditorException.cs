namespace UyapEditorMcp;

public sealed class EditorException : Exception
{
    internal EditorException(EditorError error, params object?[] arguments)
        : this(error.Code, EditorResources.Get(error.MessageKey, arguments))
    {
    }

    private EditorException(string code, string message) : base(message)
    {
        Code = code;
    }

    public string Code
    {
        get;
    }

    internal static EditorException FromNative(string? code) =>
        new(code ?? EditorErrors.EditorUiError.Code, EditorResources.Get(EditorErrors.EditorUiError.MessageKey));
}
