namespace SoulCore.Inference.Tools.Workflow;

/// <summary>
/// Compact system guidance appended on the tool-loop path so qwen2.5 does not
/// answer workflow create/run requests in prose only (BED-162 /
/// ISSUE-20260729-001). Kept short — sits after identity/memory/emotion.
/// </summary>
public static class ToolAgencyGuidance
{
    public const string WorkflowBlock =
        "[Tools]\n" +
        "When the user asks to create a multi-step workflow, call workflow_create with a name and steps. " +
        "Set each step's tool when they name the action. Put the step wording in description.\n" +
        "When they ask to run or re-run a workflow, call workflow_execute with the id from this session and all=true.\n" +
        "MT4 / MetaTrader status, connection, account, or bridge: call mt4_status with {}.\n" +
        "task_create is for a to-do they asked you to remember. A go-ahead means start the next task already on the list.\n" +
        "Check, sort, search, or read mail with email_inbox / email_search / email_read " +
        "(account=victoria|personal|business). email_send and email_delete return a confirm prompt; relay it. " +
        "A confirmed=true you add yourself is ignored.\n" +
        "For a routine already in memory or [Known procedures], call sop_list then sop_get and follow that tool order.\n" +
        "Use the site, app, or course named in memory, the SOP, a tool result, or the operator's words.";

    /// <summary>
    /// Appends the workflow agency block when missing. Idempotent for retries.
    /// </summary>
    public static string AppendToPreamble(string? contextPreamble)
    {
        var baseText = string.IsNullOrWhiteSpace(contextPreamble)
            ? string.Empty
            : contextPreamble.TrimEnd();

        if (baseText.Contains("[Tools]", StringComparison.Ordinal))
            return baseText;

        if (baseText.Length == 0)
            return WorkflowBlock;

        return baseText + "\n\n" + WorkflowBlock;
    }
}
