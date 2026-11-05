using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;

namespace House.ChatDesktop.Services;

/// <summary>
/// Polls the system cursor against Her screen surface bounds without stealing Win32 input
/// from an embedded VirtualBox HWND (Avalonia PointerMoved never fires over the child).
/// </summary>
public static class HerScreenCursorProbe
{
    /// <summary>
    /// Returns pointer position in surface-local coordinates when the system cursor is over
    /// <paramref name="surface"/>; otherwise null.
    /// </summary>
    public static Point? TryGetPointerInSurface(Control surface)
    {
        if (!OperatingSystem.IsWindows() || surface is null)
            return null;

        var bounds = surface.Bounds;
        if (bounds.Width <= 1 || bounds.Height <= 1)
            return null;

        if (!TryScreenSpan(surface, out var left, out var top, out var right, out var bottom))
            return null;
        if (!GetCursorPos(out var pt))
            return null;
        if (pt.X < left || pt.X > right || pt.Y < top || pt.Y > bottom)
            return null;

        var localX = (pt.X - left) / (double)Math.Max(1, right - left) * bounds.Width;
        var localY = (pt.Y - top) / (double)Math.Max(1, bottom - top) * bounds.Height;
        return new Point(localX, localY);
    }

    /// <summary>Pointer in the surface's screen pixels (the embed HWND's client space).</summary>
    public static (int X, int Y)? TryGetPointerInSurfacePixels(Control surface)
    {
        if (!OperatingSystem.IsWindows() || surface is null)
            return null;
        if (surface.Bounds.Width <= 1 || surface.Bounds.Height <= 1)
            return null;
        if (!TryScreenSpan(surface, out var left, out var top, out var right, out var bottom))
            return null;
        if (!GetCursorPos(out var pt))
            return null;
        if (pt.X < left || pt.X > right || pt.Y < top || pt.Y > bottom)
            return null;

        return (pt.X - left, pt.Y - top);
    }

    private static bool TryScreenSpan(Control surface, out int left, out int top, out int right, out int bottom)
    {
        left = top = right = bottom = 0;
        try
        {
            var topLeft = surface.PointToScreen(new Point(0, 0));
            var bottomRight = surface.PointToScreen(new Point(surface.Bounds.Width, surface.Bounds.Height));
            left = Math.Min(topLeft.X, bottomRight.X);
            right = Math.Max(topLeft.X, bottomRight.X);
            top = Math.Min(topLeft.Y, bottomRight.Y);
            bottom = Math.Max(topLeft.Y, bottomRight.Y);
            return true;
        }
        catch
        {
            return false;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);
}
