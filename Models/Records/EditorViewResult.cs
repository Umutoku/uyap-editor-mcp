namespace UyapEditorMcp;

public sealed record EditorViewResult(int ZoomPercent, bool ShowWhitespace, int Caret, int SelectionStart,
    int SelectionEnd, int TotalCharacters, double RenderScale, int[] ZoomOptions, bool ShowComments);
