namespace House.ChatDesktop.Services;

/// <summary>
/// PROP-19.3 — Shared Her-screen pointer contract.
/// <para>
/// <b>HWND embed</b> — the guest is drawn 1:1 inside VirtualBox's picture child, not stretched
/// across the slot and not letterboxed in it. Menu, status bar, and the empty band under the
/// VM stay outside the map. <see cref="MapViewportToInject"/> is that map.
/// <see cref="ChooseGuestViewport"/> picks the picture child (height matches the framebuffer).
/// </para>
/// <para>
/// <b>JPEG / Playwright</b> — Uniform letterbox (<see cref="TryMapPointerToPage"/> /
/// <see cref="MapSlotToInject"/>). That map is not the embedded VM window.
/// </para>
/// </summary>
public static class VictoriaBrowserCoordMap
{
    /// <summary>Default guest size when embed is live but no screenshot has published frame size yet.</summary>
    public const int DefaultGuestWidth = 1280;
    public const int DefaultGuestHeight = 800;

    /// <summary>
    /// Guest framebuffer size for stretch-fill. Prefer live <c>IDisplay</c>, then hub,
    /// then HWND slot, then JPEG default. Delegates to <see cref="VictoriaVmPointerPlan.ResolveGuestFramebuffer"/>.
    /// </summary>
    public static (int Width, int Height) ResolveGuestFramebuffer(
        int hubWidth,
        int hubHeight,
        int? displayWidth,
        int? displayHeight,
        int surfaceWidth = 0,
        int surfaceHeight = 0) =>
        VictoriaVmPointerPlan.ResolveGuestFramebuffer(
            hubWidth, hubHeight, displayWidth, displayHeight, surfaceWidth, surfaceHeight);

    /// <summary>
    /// Guest 0-based framebuffer pixel → Absolute <c>[1,1]</c> origin (VirtualBox SDK).
    /// Soft-cursor and agent tools stay on 0-based Guest0; Absolute +1 is the last inject step only.
    /// </summary>
    public static (int X, int Y) ToAbsolute(int guestX, int guestY) =>
        VictoriaVmPointerPlan.ToAbsolute(guestX, guestY);

    /// <summary>
    /// JPEG / Playwright slot → Guest0 + Absolute (letterbox). Not the embedded VM window.
    /// </summary>
    public static (int GuestX, int GuestY, int AbsX, int AbsY)? MapSlotToInject(
        double pointerX,
        double pointerY,
        double surfaceWidth,
        double surfaceHeight,
        int framebufferWidth,
        int framebufferHeight)
    {
        var guest = TryMapPointerToPage(
            pointerX, pointerY, surfaceWidth, surfaceHeight,
            framebufferWidth, framebufferHeight);
        if (guest is null)
            return null;

        var abs = ToAbsolute(guest.Value.X, guest.Value.Y);
        return (guest.Value.X, guest.Value.Y, abs.X, abs.Y);
    }

    /// <summary>
    /// Uniform letterbox map (JPEG / Playwright frame). Null when pointer is in letterbox bands.
    /// </summary>
    public static (int X, int Y)? TryMapPointerToPage(
        double pointerX,
        double pointerY,
        double surfaceWidth,
        double surfaceHeight,
        int imagePixelWidth,
        int imagePixelHeight)
    {
        if (surfaceWidth <= 0 || surfaceHeight <= 0
            || imagePixelWidth <= 0 || imagePixelHeight <= 0)
            return null;

        var scale = Math.Min(surfaceWidth / imagePixelWidth, surfaceHeight / imagePixelHeight);
        if (scale <= 0)
            return null;

        var dispW = imagePixelWidth * scale;
        var dispH = imagePixelHeight * scale;
        var offsetX = (surfaceWidth - dispW) / 2.0;
        var offsetY = (surfaceHeight - dispH) / 2.0;

        if (pointerX < offsetX || pointerY < offsetY
            || pointerX > offsetX + dispW || pointerY > offsetY + dispH)
            return null;

        var x = (int)Math.Floor((pointerX - offsetX) / scale);
        var y = (int)Math.Floor((pointerY - offsetY) / scale);
        x = Math.Clamp(x, 0, imagePixelWidth - 1);
        y = Math.Clamp(y, 0, imagePixelHeight - 1);
        return (x, y);
    }

    /// <summary>
    /// Stretch-to-fill map (VM HWND embed fills the slot). Null when pointer is outside the surface.
    /// Guest0 for Absolute / soft-cursor tip — same transform both ways via
    /// <see cref="TryMapGuestTipToSurfaceStretchFill"/>.
    /// </summary>
    public static (int X, int Y)? TryMapPointerStretchFill(
        double pointerX,
        double pointerY,
        double surfaceWidth,
        double surfaceHeight,
        int imagePixelWidth,
        int imagePixelHeight)
    {
        if (surfaceWidth <= 0 || surfaceHeight <= 0
            || imagePixelWidth <= 0 || imagePixelHeight <= 0)
            return null;

        if (pointerX < 0 || pointerY < 0
            || pointerX > surfaceWidth || pointerY > surfaceHeight)
            return null;

        // Round to nearest guest pixel (Floor biased clicks half a pixel up-left).
        var x = (int)Math.Round(pointerX / surfaceWidth * imagePixelWidth, MidpointRounding.AwayFromZero);
        var y = (int)Math.Round(pointerY / surfaceHeight * imagePixelHeight, MidpointRounding.AwayFromZero);
        x = Math.Clamp(x, 0, imagePixelWidth - 1);
        y = Math.Clamp(y, 0, imagePixelHeight - 1);
        return (x, y);
    }

    /// <summary>
    /// Guest0 → soft-cursor tip in slot client pixels (inverse of <see cref="TryMapPointerToPage"/>).
    /// No glyph size — tip is the inject point. Matches the VirtualBox aspect-fit viewport.
    /// </summary>
    public static (double TipX, double TipY)? TryMapGuestTipToSurfaceUniform(
        int guestX,
        int guestY,
        double surfaceWidth,
        double surfaceHeight,
        int imagePixelWidth,
        int imagePixelHeight)
    {
        if (surfaceWidth <= 0 || surfaceHeight <= 0
            || imagePixelWidth <= 0 || imagePixelHeight <= 0)
            return null;

        var scale = Math.Min(surfaceWidth / imagePixelWidth, surfaceHeight / imagePixelHeight);
        if (scale <= 0)
            return null;

        var drawW = imagePixelWidth * scale;
        var drawH = imagePixelHeight * scale;
        var offsetX = (surfaceWidth - drawW) / 2.0;
        var offsetY = (surfaceHeight - drawH) / 2.0;
        return (offsetX + (guestX * scale), offsetY + (guestY * scale));
    }

    /// <summary>
    /// Guest0 → soft-cursor tip in slot client pixels (inverse of <see cref="TryMapPointerStretchFill"/>).
    /// No glyph size — tip is the inject point; draw the arrow with hotspot at this tip.
    /// </summary>
    public static (double TipX, double TipY)? TryMapGuestTipToSurfaceStretchFill(
        int guestX,
        int guestY,
        double surfaceWidth,
        double surfaceHeight,
        int imagePixelWidth,
        int imagePixelHeight)
    {
        if (surfaceWidth <= 0 || surfaceHeight <= 0
            || imagePixelWidth <= 0 || imagePixelHeight <= 0)
            return null;

        var tipX = (guestX / (double)imagePixelWidth) * surfaceWidth;
        var tipY = (guestY / (double)imagePixelHeight) * surfaceHeight;
        return (tipX, tipY);
    }

    /// <summary>
    /// Guest pixel → surface top-left for a centered soft-cursor glyph (stretch-to-fill).
    /// Prefer <see cref="TryMapGuestTipToSurfaceStretchFill"/> for tip = inject point.
    /// </summary>
    public static (double Left, double Top)? TryMapGuestToSurfaceStretchFill(
        int guestX,
        int guestY,
        double surfaceWidth,
        double surfaceHeight,
        int imagePixelWidth,
        int imagePixelHeight,
        double cursorWidth,
        double cursorHeight)
    {
        var tip = TryMapGuestTipToSurfaceStretchFill(
            guestX, guestY, surfaceWidth, surfaceHeight, imagePixelWidth, imagePixelHeight);
        if (tip is null)
            return null;

        return (tip.Value.TipX - (cursorWidth / 2), tip.Value.TipY - (cursorHeight / 2));
    }

    /// <summary>Guest pixel → surface point for soft-cursor draw (Uniform letterbox).</summary>
    public static (double Left, double Top)? TryMapGuestToSurfaceUniform(
        int guestX,
        int guestY,
        double surfaceWidth,
        double surfaceHeight,
        int imagePixelWidth,
        int imagePixelHeight,
        double cursorWidth,
        double cursorHeight)
    {
        if (surfaceWidth <= 0 || surfaceHeight <= 0
            || imagePixelWidth <= 0 || imagePixelHeight <= 0)
            return null;

        var scale = Math.Min(surfaceWidth / imagePixelWidth, surfaceHeight / imagePixelHeight);
        if (scale <= 0)
            return null;

        var drawW = imagePixelWidth * scale;
        var drawH = imagePixelHeight * scale;
        var offsetX = (surfaceWidth - drawW) / 2;
        var offsetY = (surfaceHeight - drawH) / 2;
        var left = offsetX + (guestX * scale) - (cursorWidth / 2);
        var top = offsetY + (guestY * scale) - (cursorHeight / 2);
        return (left, top);
    }

    /// <summary>
    /// Picture child of the embedded VirtualBox window, in the same client pixels as the click.
    /// Scroll is guest pixels already off the left/top of that child. 0 until a scrollbar is read.
    /// </summary>
    public readonly record struct EmbedViewport(
        int Left,
        int Top,
        int Width,
        int Height,
        int ScrollX = 0,
        int ScrollY = 0);

    /// <summary>One descendant of the embed parent. Overlay windows are not the picture.</summary>
    public readonly record struct ViewportChild(int X, int Y, int Width, int Height, bool IsOverlay);

    /// <summary>
    /// Pick the 1:1 picture. Prefer a child whose height matches the guest framebuffer
    /// (the menu and the status bar do not). When the Her screen pane is shorter than
    /// the guest, accept the width-matched top crop so clicks on the visible pixels
    /// stay 1:1 instead of being dropped. Null when neither match exists.
    /// </summary>
    public static EmbedViewport? ChooseGuestViewport(
        IReadOnlyList<ViewportChild> children,
        int guestWidth,
        int guestHeight)
    {
        if (guestWidth <= 0 || guestHeight <= 0 || children is null || children.Count == 0)
            return null;

        ViewportChild? best = null;
        var bestScore = int.MaxValue;
        foreach (var child in children)
        {
            if (child.IsOverlay || child.Width < 200 || child.Height < 200)
                continue;

            var score = Math.Abs(child.Height - guestHeight);
            if (score > 8)
                continue;

            var better = best is null
                || score < bestScore
                || (score == bestScore && child.Width > best.Value.Width);
            if (!better)
                continue;

            best = child;
            bestScore = score;
        }

        if (best is { } chosen)
            return new EmbedViewport(chosen.X, chosen.Y, chosen.Width, chosen.Height);

        ViewportChild? crop = null;
        foreach (var child in children)
        {
            if (child.IsOverlay || child.Width < 200 || child.Height < 200)
                continue;
            if (Math.Abs(child.Width - guestWidth) > 8)
                continue;
            if (child.Height > guestHeight + 8)
                continue;
            if (crop is null || child.Height > crop.Value.Height)
                crop = child;
        }

        if (crop is not { } cropped)
            return null;

        return new EmbedViewport(cropped.X, cropped.Y, cropped.Width, cropped.Height);
    }

    /// <summary>
    /// Embed client pixel → guest framebuffer. 1:1 inside <paramref name="view"/>.
    /// Null on the menu, the status bar, the scrollbar gap, and the empty band.
    /// </summary>
    public static (int GuestX, int GuestY, int AbsX, int AbsY)? MapViewportToInject(
        double pointerX,
        double pointerY,
        EmbedViewport view,
        int framebufferWidth,
        int framebufferHeight)
    {
        if (view.Width <= 1 || view.Height <= 1
            || framebufferWidth <= 0 || framebufferHeight <= 0)
            return null;

        var localX = pointerX - view.Left;
        var localY = pointerY - view.Top;
        if (localX < 0 || localY < 0 || localX >= view.Width || localY >= view.Height)
            return null;

        var x = view.ScrollX + (int)Math.Round(localX, MidpointRounding.AwayFromZero);
        var y = view.ScrollY + (int)Math.Round(localY, MidpointRounding.AwayFromZero);
        if (x < view.ScrollX || y < view.ScrollY
            || x >= view.ScrollX + view.Width || y >= view.ScrollY + view.Height
            || x >= framebufferWidth || y >= framebufferHeight)
            return null;

        var abs = ToAbsolute(x, y);
        return (x, y, abs.X, abs.Y);
    }

    /// <summary>
    /// Guest pixel → soft-cursor tip in embed client pixels. Inverse of
    /// <see cref="MapViewportToInject"/>. Null when that pixel is scrolled off the picture.
    /// </summary>
    public static (double TipX, double TipY)? TryMapGuestTipToViewport(
        int guestX,
        int guestY,
        EmbedViewport view,
        int framebufferWidth,
        int framebufferHeight)
    {
        if (view.Width <= 1 || view.Height <= 1
            || framebufferWidth <= 0 || framebufferHeight <= 0
            || guestX < 0 || guestY < 0
            || guestX >= framebufferWidth || guestY >= framebufferHeight)
            return null;

        var localX = guestX - view.ScrollX;
        var localY = guestY - view.ScrollY;
        if (localX < 0 || localY < 0 || localX >= view.Width || localY >= view.Height)
            return null;

        return (view.Left + localX, view.Top + localY);
    }

    public static string FormatClickHint(int x, int y) => $"click ({x}, {y})";

    /// <summary>Her agent soft-cursor guest coords (option C / always).</summary>
    public static string FormatHerHint(int x, int y) => $"her ({x}, {y})";

    /// <summary>
    /// Option C badge: her agent coords always; operator hover while present.
    /// Null when neither source has coordinates.
    /// </summary>
    public static string? FormatCoordBadge(int? herX, int? herY, int? hoverX, int? hoverY)
    {
        if (herX is int hx && herY is int hy && hoverX is int ox && hoverY is int oy)
            return $"{FormatHerHint(hx, hy)} · you {FormatClickHint(ox, oy)}";
        if (herX is int hxOnly && herY is int hyOnly)
            return FormatHerHint(hxOnly, hyOnly);
        if (hoverX is int oxOnly && hoverY is int oyOnly)
            return FormatClickHint(oxOnly, oyOnly);
        return null;
    }
}
