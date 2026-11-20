namespace UyapEditorMcp;

public sealed record ApprovalReview(string Action, string Status, ManualWorkflow? Workflow,
    ApprovalArtifact[] Artifacts, ApprovalItem[] Details, string[] Missing, string UserStep);
