using System.Text.Json;
using SoulCore.Inference.Tooling;

namespace SoulCore.Inference.Tools.Desktop;

public sealed class VsCodeOpenTool : ITool
{
    private static readonly JsonElement Parameters = JsonDocument.Parse(
        """
        {"type":"object","properties":{"path":{"type":"string","description":"File or folder inside the Ubuntu guest home. Relative paths are under /home/victoria."},"line":{"type":"integer","description":"1-based line. Files only."},"column":{"type":"integer","description":"1-based column. Requires line."}},"required":["path"]}
        """).RootElement.Clone();

    private readonly IComputerControlGate _gate;
    private readonly IGuestWorkspace _workspace;

    public VsCodeOpenTool(IComputerControlGate gate, IGuestWorkspace workspace)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
    }

    public ToolDefinition Definition { get; } = new(
        Name: "vscode_open",
        Description:
            "Open a file or folder in the guest VS Code / VSCodium window on the Her screen. " +
            "Use this when a lesson says to open a folder, open a file, or go to a line. " +
            "Pass line (and optional column) to land on that line. Reuses the editor window. " +
            "Does not edit the file — guest_read_file and guest_write_file do that. " +
            "Lesson terminal commands stay on guest_shell. Paths stay under /home/victoria. " +
            "Requires AllowComputerControl.",
        Parameters: Parameters);

    public async Task<ToolResult> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        if (!_gate.AllowComputerControl)
            return DesktopToolGate.RefuseControl();
        var path = GuestListDirTool.ReadString(args, "path");
        if (string.IsNullOrWhiteSpace(path))
            return new ToolResult(false, "error: vscode_open requires 'path'.", null);

        int? line = null;
        int? column = null;
        if (!TryOptionalInt(args, "line", out line) || !TryOptionalInt(args, "column", out column))
            return new ToolResult(false, "error: vscode_open line and column must be integers.", null);

        var result = await _workspace.OpenInEditorAsync(path, line, column, ct).ConfigureAwait(false);
        return DesktopToolGate.FromBackend(result);
    }

    private static bool TryOptionalInt(JsonElement args, string name, out int? value)
    {
        value = null;
        if (args.ValueKind != JsonValueKind.Object || !args.TryGetProperty(name, out var el))
            return true;
        if (el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return true;
        if (el.ValueKind != JsonValueKind.Number || !el.TryGetInt32(out var n))
            return false;
        value = n;
        return true;
    }
}
