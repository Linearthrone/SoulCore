using House.ChatDesktop.Services;
using Xunit;

namespace House.ChatDesktop.Tests;

public class VictoriaBrowserCoordMapTests
{
    [Fact]
    public void TryMap_CenterOfLetterboxedImage_MapsToImageCenter()
    {
        // 1280x800 frame in a 640x500 surface → scale=0.5, letterbox vertical (offsetY=50)
        var mapped = VictoriaBrowserCoordMap.TryMapPointerToPage(
            pointerX: 320,
            pointerY: 50 + 200, // middle of displayed image
            surfaceWidth: 640,
            surfaceHeight: 500,
            imagePixelWidth: 1280,
            imagePixelHeight: 800);

        Assert.NotNull(mapped);
        Assert.Equal(640, mapped!.Value.X);
        Assert.Equal(400, mapped.Value.Y);
    }

    [Fact]
    public void TryMap_OutsideLetterbox_ReturnsNull()
    {
        var mapped = VictoriaBrowserCoordMap.TryMapPointerToPage(
            pointerX: 10,
            pointerY: 10, // in top letterbox band
            surfaceWidth: 640,
            surfaceHeight: 500,
            imagePixelWidth: 1280,
            imagePixelHeight: 800);

        Assert.Null(mapped);
    }

    [Fact]
    public void TryMap_TopLeftOfImage_IsZeroZero()
    {
        var mapped = VictoriaBrowserCoordMap.TryMapPointerToPage(
            pointerX: 0,
            pointerY: 50,
            surfaceWidth: 640,
            surfaceHeight: 500,
            imagePixelWidth: 1280,
            imagePixelHeight: 800);

        Assert.NotNull(mapped);
        Assert.Equal(0, mapped!.Value.X);
        Assert.Equal(0, mapped.Value.Y);
    }

    [Fact]
    public void FormatClickHint_MatchesWhatKayleighSays()
    {
        Assert.Equal("click (412, 277)", VictoriaBrowserCoordMap.FormatClickHint(412, 277));
    }

    [Fact]
    public void FormatHerHint_ShowsAgentCursor()
    {
        Assert.Equal("her (100, 200)", VictoriaBrowserCoordMap.FormatHerHint(100, 200));
    }

    [Fact]
    public void FormatCoordBadge_OptionC_BothWhenHovering()
    {
        Assert.Equal(
            "her (100, 200) · you click (412, 277)",
            VictoriaBrowserCoordMap.FormatCoordBadge(100, 200, 412, 277));
    }

    [Fact]
    public void FormatCoordBadge_OptionC_HerOnlyWhenNotHovering()
    {
        Assert.Equal("her (100, 200)", VictoriaBrowserCoordMap.FormatCoordBadge(100, 200, null, null));
    }

    [Fact]
    public void FormatCoordBadge_HoverOnly_WhenNoAgentCursor()
    {
        Assert.Equal("click (412, 277)", VictoriaBrowserCoordMap.FormatCoordBadge(null, null, 412, 277));
    }

    [Fact]
    public void FormatCoordBadge_Null_WhenNeither()
    {
        Assert.Null(VictoriaBrowserCoordMap.FormatCoordBadge(null, null, null, null));
    }

    [Fact]
    public void TryMapStretchFill_Center_MapsToGuestCenter()
    {
        // 1280x800 in a 640x400 surface — stretch (no letterbox)
        var mapped = VictoriaBrowserCoordMap.TryMapPointerStretchFill(
            pointerX: 320,
            pointerY: 200,
            surfaceWidth: 640,
            surfaceHeight: 400,
            imagePixelWidth: 1280,
            imagePixelHeight: 800);

        Assert.NotNull(mapped);
        Assert.Equal(640, mapped!.Value.X);
        Assert.Equal(400, mapped.Value.Y);
    }

    [Fact]
    public void TryMapStretchFill_TopLeft_IsZeroZero()
    {
        var mapped = VictoriaBrowserCoordMap.TryMapPointerStretchFill(
            pointerX: 0,
            pointerY: 0,
            surfaceWidth: 640,
            surfaceHeight: 400,
            imagePixelWidth: 1280,
            imagePixelHeight: 800);

        Assert.NotNull(mapped);
        Assert.Equal(0, mapped!.Value.X);
        Assert.Equal(0, mapped.Value.Y);
    }

    [Fact]
    public void TryMapStretchFill_HalfGuestPixel_RoundsNearestNotFloor()
    {
        // 2× stretch: each guest pixel is 0.5 surface px. Midway (0.25) → 0.5 guest
        // → Round AwayFromZero = 1 (Floor would keep the up-left bias at 0).
        var mapped = VictoriaBrowserCoordMap.TryMapPointerStretchFill(
            pointerX: 0.25,
            pointerY: 0.25,
            surfaceWidth: 640,
            surfaceHeight: 400,
            imagePixelWidth: 1280,
            imagePixelHeight: 800);

        Assert.NotNull(mapped);
        Assert.Equal(1, mapped!.Value.X);
        Assert.Equal(1, mapped.Value.Y);
    }

    [Fact]
    public void TryMapStretchFill_Outside_ReturnsNull()
    {
        var mapped = VictoriaBrowserCoordMap.TryMapPointerStretchFill(
            pointerX: -1,
            pointerY: 10,
            surfaceWidth: 640,
            surfaceHeight: 400,
            imagePixelWidth: 1280,
            imagePixelHeight: 800);

        Assert.Null(mapped);
    }

    [Fact]
    public void TryMapGuestToSurfaceStretchFill_RoundTripsCenter()
    {
        var guest = VictoriaBrowserCoordMap.TryMapPointerStretchFill(
            320, 200, 640, 400, 1280, 800);
        Assert.NotNull(guest);

        var surface = VictoriaBrowserCoordMap.TryMapGuestToSurfaceStretchFill(
            guest!.Value.X, guest.Value.Y,
            640, 400, 1280, 800,
            cursorWidth: 0, cursorHeight: 0);
        Assert.NotNull(surface);
        Assert.InRange(surface!.Value.Left, 319.5, 320.5);
        Assert.InRange(surface.Value.Top, 199.5, 200.5);
    }

    /// <summary>
    /// PROP-19.3: Kurt inject + soft-cursor tip share one Guest0; Absolute is +1 only at COM.
    /// Slot 900×600 ≠ FB 1280×800 — center must not land at Absolute(450,300).
    /// </summary>
    [Fact]
    public void MapSlotToInject_SlotDiffersFromFramebuffer_CenterMapsToGuestCenterAndAbsolute()
    {
        var inject = VictoriaBrowserCoordMap.MapSlotToInject(
            pointerX: 450,
            pointerY: 300,
            surfaceWidth: 900,
            surfaceHeight: 600,
            framebufferWidth: 1280,
            framebufferHeight: 800);
        Assert.NotNull(inject);
        Assert.Equal(640, inject!.Value.GuestX);
        Assert.Equal(400, inject.Value.GuestY);
        Assert.Equal(641, inject.Value.AbsX);
        Assert.Equal(401, inject.Value.AbsY);

        var tip = VictoriaBrowserCoordMap.TryMapGuestTipToSurfaceUniform(
            inject.Value.GuestX, inject.Value.GuestY,
            900, 600, 1280, 800);
        Assert.NotNull(tip);
        Assert.InRange(tip!.Value.TipX, 449.5, 450.5);
        Assert.InRange(tip.Value.TipY, 299.5, 300.5);
    }

    [Fact]
    public void MapSlotToInject_ContentCorners_MatchUniformTipInverse()
    {
        // 900×600 slot, 1280×800 guest: aspect-fit leaves a vertical bar.
        // scale = 900/1280, offsetY = (600 - 562.5) / 2 = 18.75.
        const double offsetY = 18.75;
        var corners = new[]
        {
            (x: 0.0, y: offsetY, gx: 0, gy: 0),
            (x: 899.0, y: offsetY, gx: 1278, gy: 0),
            (x: 0.0, y: offsetY + 562.5 - 0.01, gx: 0, gy: 799),
        };
        foreach (var (x, y, gx, gy) in corners)
        {
            var inject = VictoriaBrowserCoordMap.MapSlotToInject(x, y, 900, 600, 1280, 800);
            Assert.NotNull(inject);
            Assert.Equal(gx, inject!.Value.GuestX);
            Assert.Equal(gy, inject.Value.GuestY);

            var tip = VictoriaBrowserCoordMap.TryMapGuestTipToSurfaceUniform(
                inject.Value.GuestX, inject.Value.GuestY, 900, 600, 1280, 800);
            Assert.NotNull(tip);
            var back = VictoriaBrowserCoordMap.TryMapPointerToPage(
                tip!.Value.TipX, tip.Value.TipY, 900, 600, 1280, 800);
            Assert.NotNull(back);
            Assert.Equal(inject.Value.GuestX, back!.Value.X);
            Assert.Equal(inject.Value.GuestY, back.Value.Y);
        }
    }

    /// <summary>
    /// Measured 2026-11-04 on the live embed. Guest screenshot matched this child at
    /// 1:1 (mean channel error 0.66). Stretch of the 1131×903 slot and letterbox of
    /// the slot both miss. Menu and the empty band are not guest pixels.
    /// </summary>
    [Fact]
    public void LiveEmbed_OneToOnePicture_NotStretchOrLetterbox()
    {
        var children = new VictoriaBrowserCoordMap.ViewportChild[]
        {
            new(0, 830, 1113, 26, false),
            new(0, 0, 1113, 41, false),
            new(0, 41, 1113, 789, false),
            new(0, 69, 1113, 735, false),
            new(0, 789, 1113, 15, false),
            new(1098, 69, 15, 720, false),
            new(0, 69, 1113, 720, false),
            new(0, 0, 1131, 903, true),
        };
        var view = VictoriaBrowserCoordMap.ChooseGuestViewport(children, 1280, 720);
        Assert.Equal(new VictoriaBrowserCoordMap.EmbedViewport(0, 69, 1113, 720), view);

        var click = VictoriaBrowserCoordMap.MapViewportToInject(484, 141, view!.Value, 1280, 720);
        Assert.Equal((484, 72, 485, 73), (click!.Value.GuestX, click.Value.GuestY, click.Value.AbsX, click.Value.AbsY));

        var stretch = VictoriaBrowserCoordMap.TryMapPointerStretchFill(484, 141, 1131, 903, 1280, 720);
        Assert.NotEqual(click.Value.GuestX, stretch!.Value.X);
        Assert.NotEqual(click.Value.GuestY, stretch.Value.Y);

        var letterbox = VictoriaBrowserCoordMap.TryMapPointerToPage(484, 141, 1131, 903, 1280, 720);
        Assert.NotEqual(click.Value.GuestX, letterbox!.Value.X);
        Assert.NotEqual(click.Value.GuestY, letterbox.Value.Y);

        Assert.Null(VictoriaBrowserCoordMap.MapViewportToInject(40, 20, view.Value, 1280, 720));
        Assert.Null(VictoriaBrowserCoordMap.MapViewportToInject(100, 68, view.Value, 1280, 720));
        Assert.Null(VictoriaBrowserCoordMap.MapViewportToInject(100, 789, view.Value, 1280, 720));
        Assert.Null(VictoriaBrowserCoordMap.MapViewportToInject(100, 880, view.Value, 1280, 720));
        Assert.Null(VictoriaBrowserCoordMap.MapViewportToInject(1113, 200, view.Value, 1280, 720));

        var top = VictoriaBrowserCoordMap.MapViewportToInject(100, 69, view.Value, 1280, 720);
        Assert.Equal((100, 0), (top!.Value.GuestX, top.Value.GuestY));
        var stretchTop = VictoriaBrowserCoordMap.TryMapPointerStretchFill(100, 69, 1131, 903, 1280, 720);
        Assert.True(stretchTop!.Value.Y - top.Value.GuestY >= 50);
    }

    [Fact]
    public void LiveEmbed_PictureRoundTripsOnAGrid_AndScrollShiftsX()
    {
        var view = new VictoriaBrowserCoordMap.EmbedViewport(0, 69, 1113, 720);
        for (var x = 0; x < 1113; x += 37)
        {
            for (var y = 69; y < 69 + 720; y += 31)
            {
                var inject = VictoriaBrowserCoordMap.MapViewportToInject(x, y, view, 1280, 720);
                Assert.NotNull(inject);
                var tip = VictoriaBrowserCoordMap.TryMapGuestTipToViewport(
                    inject!.Value.GuestX, inject.Value.GuestY, view, 1280, 720);
                Assert.NotNull(tip);
                Assert.Equal(x, tip!.Value.TipX);
                Assert.Equal(y, tip.Value.TipY);
                var back = VictoriaBrowserCoordMap.MapViewportToInject(
                    tip.Value.TipX, tip.Value.TipY, view, 1280, 720);
                Assert.Equal(inject.Value.GuestX, back!.Value.GuestX);
                Assert.Equal(inject.Value.GuestY, back.Value.GuestY);
            }
        }

        var scrolled = view with { ScrollX = 40 };
        var shifted = VictoriaBrowserCoordMap.MapViewportToInject(100, 69, scrolled, 1280, 720);
        Assert.Equal((140, 0), (shifted!.Value.GuestX, shifted.Value.GuestY));
        Assert.Null(VictoriaBrowserCoordMap.TryMapGuestTipToViewport(10, 0, scrolled, 1280, 720));
    }

    [Fact]
    public void ChooseGuestViewport_WithoutThePictureChild_ReturnsNull()
    {
        var frameOnly = new VictoriaBrowserCoordMap.ViewportChild[]
        {
            new(0, 0, 1113, 41, false),
            new(0, 41, 1113, 789, false),
            new(0, 69, 1113, 735, false),
        };
        Assert.Null(VictoriaBrowserCoordMap.ChooseGuestViewport(frameOnly, 1280, 720));
    }

    [Fact]
    public void ToAbsolute_MatchesVmPointerPlan()
    {
        Assert.Equal(
            VictoriaVmPointerPlan.ToAbsolute(0, 0),
            VictoriaBrowserCoordMap.ToAbsolute(0, 0));
        Assert.Equal(
            VictoriaVmPointerPlan.ToAbsolute(639, 399),
            VictoriaBrowserCoordMap.ToAbsolute(639, 399));
    }

    [Fact]
    public void ResolveGuestFramebuffer_NeverPrefersSlotWhenHubKnown()
    {
        var fb = VictoriaBrowserCoordMap.ResolveGuestFramebuffer(
            hubWidth: 1280,
            hubHeight: 800,
            displayWidth: null,
            displayHeight: null,
            surfaceWidth: 900,
            surfaceHeight: 600);
        Assert.Equal((1280, 800), fb);
    }
}
