namespace SoulCore.Inference.Tools.Desktop;

/// <summary>
/// Argv for the guest VS Code / VSCodium CLI. Paths are already resolved under the
/// guest home. The binary is one of three fixed paths, never a caller string.
/// </summary>
public static class VsCodeLaunch
{
    public static readonly string[] EditorCandidates =
    {
        "/usr/bin/code",
        "/usr/bin/codium",
        "/snap/bin/code",
    };

    public static bool IsEditor(string? exe)
    {
        if (string.IsNullOrWhiteSpace(exe))
            return false;
        foreach (var candidate in EditorCandidates)
        {
            if (string.Equals(candidate, exe.Trim(), StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    public static bool TryPlan(
        string editorExe,
        string absolutePath,
        bool directory,
        int? line,
        int? column,
        out string[] args,
        out string error)
    {
        args = Array.Empty<string>();
        error = "";
        if (!IsEditor(editorExe))
        {
            error = "editor binary is not allowlisted";
            return false;
        }

        if (string.IsNullOrWhiteSpace(absolutePath) || absolutePath.Contains(':'))
        {
            error = "path must be an absolute guest path without ':'";
            return false;
        }

        if (line is < 1 or > 1_000_000)
        {
            error = "line must be from 1 to 1000000";
            return false;
        }

        if (column is < 1 or > 10_000)
        {
            error = "column must be from 1 to 10000";
            return false;
        }

        if (column is not null && line is null)
        {
            error = "column needs a line";
            return false;
        }

        if (directory && line is not null)
        {
            error = "line applies to a file, and this path is a directory";
            return false;
        }

        if (directory || line is null)
        {
            args = new[] { "-r", absolutePath };
            return true;
        }

        var target = column is int col
            ? absolutePath + ":" + line.Value + ":" + col
            : absolutePath + ":" + line.Value;
        args = new[] { "-r", "-g", target };
        return true;
    }
}
