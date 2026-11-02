---
issue_id: ISSUE-20261031-001
discovered: 2026-10-31
severity: P2
status: Inconclusive
task: TASK-20261031-218
pm_note: 2026-10-31 — Not a missing-window defect. "Her screen overlay" is the Avalonia fallback title. The Win32 sibling overlay has no title and is created only after the VM embed is live. Pid 54360's log stops at MainWindow opened, so the embed was not up and no click was posted. The ValueTuple failure is not re-proven on this process.
---

## Problem Description

Release ChatDesktop pid 54360 (started 2026-10-31 03:39:36.027, title `House Victoria — Presence 0.1.51`) has no window titled `Her screen overlay`. One left click could not be posted to that overlay, and the guest picture was not compared. Tester note uses LinearThrone.

## Reproduction Steps

1. Leave SoulCore.Host running. Leave `victoria-sandbox` running. Do not restart ChatDesktop.
2. Confirm pid 54360 is `House.ChatDesktop` from `House\House.ChatDesktop\bin\Release\net8.0\House.ChatDesktop.exe`, started 2026-10-31 03:39:36.
3. Enumerate top-level and child windows for that pid, and scan the desktop for the exact title `Her screen overlay`.
4. Do not click the VirtualBox paint if that title is missing.

## Expected Result

A window titled `Her screen overlay` owned by pid 54360. One `WM_LBUTTONDOWN` (`MK_LBUTTON`) and one `WM_LBUTTONUP` at its client center. A log line `her-screen click abs=` with `buttons=` not `0`, ending in ` sent`. A before/after `screenshotpng` pair for `victoria-sandbox`.

## Actual Result

Exact title count was 0 for pid 54360 and 0 on any process (`OWNED_EXACT=0`, `ANY_PID_EXACT=0`) at 2026-10-31 03:42:14. Pid 54360 had 7 top-level windows. The main hwnd `0x950D06` had no children. No click was posted. `screenshotpng` was not run.

## Screenshots/Logs

Log `%LocalAppData%\HouseVictoria\presence-startup.log` lines at or after 03:39:36 (profile folder redacted as `<operator>`):

```text
2026-10-31 03:39:36.086 Main enter args=0 base=C:\Users\<operator>\Soul_Core\House\House.ChatDesktop\bin\Release\net8.0\
2026-10-31 03:39:36.122 Velopack Run completed
2026-10-31 03:39:36.162 Starting Avalonia lifetime
2026-10-31 03:39:36.551 Framework initialization completed
2026-10-31 03:39:36.552 Creating MainWindow
2026-10-31 03:39:36.624 MainWindow ctor begin
2026-10-31 03:39:36.856 InitializeComponent done
2026-10-31 03:39:36.885 MainWindow created
2026-10-31 03:39:37.059 MainWindow opened at 2939,7 size 1723.2x1108 state=Normal
```

No `her-screen click` line. No screenshot paths.

## Impact Scope

Operator clicks cannot be shown to reach Absolute inject on this Release desk. Her screen pointer acceptance for TASK-218 is blocked until the titled overlay exists.
