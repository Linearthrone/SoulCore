using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using House.ChatDesktop.Services;

namespace House.ChatDesktop;

/// <summary>
/// PROP-20.3 — Identity tab persona switcher + create/edit wizard.
/// Lean port of CreatorEdition: no radial, viewport policy, glass chrome, or thread archive.
/// </summary>
public partial class MainWindow
{
    private readonly SoulCorePersonaClient _personas = new();
    private bool _personaSwitcherHydrating;
    private string? _activePersonaId;
    private bool? _personaApiOk;
    private List<PersonaPackInfo> _knownPersonas = new();

    private async Task RefreshPersonasAsync()
    {
        var snap = await _personas.ListAsync().ConfigureAwait(true);
        if (!snap.Reachable)
        {
            ClearPersonaSwitcherItems();
            _personaApiOk = null;
            SetPersonaStatus(snap.Detail ?? "Host unreachable", warn: true);
            return;
        }

        if (snap.PersonaApiMissing)
        {
            ClearPersonaSwitcherItems();
            _personaApiOk = false;
            SetPersonaStatus(snap.Detail ?? PersonaHostMessages.NoPersonaApi, warn: true);
            return;
        }

        _personaApiOk = true;
        ApplyPersonaList(snap);
    }

    private void ClearPersonaSwitcherItems()
    {
        if (PersonaSwitcher is null)
            return;

        _personaSwitcherHydrating = true;
        try
        {
            PersonaSwitcher.ItemsSource = null;
            PersonaSwitcher.SelectedItem = null;
        }
        finally
        {
            _personaSwitcherHydrating = false;
        }
    }

    private void SetPersonaStatus(string? text, bool warn)
    {
        var brush = warn ? _badBrush : _mutedBrush;
        foreach (var tb in new[] { PersonaChromeStatusText, PersonaSwitcherStatusText })
        {
            if (tb is null)
                continue;
            tb.Text = text ?? "";
            tb.IsVisible = !string.IsNullOrWhiteSpace(text);
            tb.Foreground = brush;
        }
    }

    private void ApplyPersonaList(PersonaListSnapshot snap)
    {
        _knownPersonas = snap.Personas.ToList();
        if (PersonaSwitcher is null)
            return;

        _personaSwitcherHydrating = true;
        try
        {
            var items = snap.Personas.ToList();
            PersonaSwitcher.ItemsSource = items;

            var activeId = snap.ActivePersonaId;
            PersonaPackInfo? active = null;
            if (!string.IsNullOrWhiteSpace(activeId))
            {
                active = items.FirstOrDefault(p =>
                    string.Equals(p.PersonaId, activeId, StringComparison.OrdinalIgnoreCase));
            }

            active ??= items.FirstOrDefault(p => p.IsActive) ?? items.FirstOrDefault();
            PersonaSwitcher.SelectedItem = active;

            if (active is not null)
                ApplyActivePersonaToShell(active.PersonaId, active.DisplayName, syncLocalName: true);
            else
                ApplyActivePersonaToShell(activeId, LocalUiSettings.DefaultDisplayName, syncLocalName: false);

            SetPersonaStatus(
                items.Count == 0
                    ? "No persona packs on Host"
                    : $"{items.Count} pack(s) · active: {active?.DisplayName ?? "none"}",
                warn: false);
        }
        finally
        {
            _personaSwitcherHydrating = false;
        }
    }

    private async void PersonaSwitcher_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_personaSwitcherHydrating || PersonaSwitcher is null)
            return;
        if (PersonaSwitcher.SelectedItem is not PersonaPackInfo selected)
            return;
        await SwitchToPersonaAsync(selected).ConfigureAwait(true);
    }

    private void RestorePersonaSwitcherSelection(PersonaPackInfo? previousItem)
    {
        _personaSwitcherHydrating = true;
        try
        {
            if (PersonaSwitcher is not null)
                PersonaSwitcher.SelectedItem = previousItem;
        }
        finally
        {
            _personaSwitcherHydrating = false;
        }
    }

    private async Task SwitchToPersonaAsync(PersonaPackInfo selected)
    {
        if (string.Equals(selected.PersonaId, _activePersonaId, StringComparison.OrdinalIgnoreCase))
            return;

        var previousItem = FindPersonaItem(_activePersonaId);

        var confirmed = await ConfirmPersonaQuarantineAsync(selected).ConfigureAwait(true);
        if (!confirmed)
        {
            RestorePersonaSwitcherSelection(previousItem);
            return;
        }

        var result = await _personas.ActivateAsync(selected.PersonaId).ConfigureAwait(true);
        if (!result.Ok || result.Persona is null)
        {
            var detail = result.Detail ?? "Host rejected activate";
            if (detail.Contains("404", StringComparison.Ordinal)
                || detail.Contains("Not Found", StringComparison.OrdinalIgnoreCase))
            {
                detail = PersonaHostMessages.NoPersonaApi;
                _personaApiOk = false;
            }

            AppendSystemNotice($"Persona switch failed: {detail}");
            SetPersonaStatus(detail, warn: true);
            RestorePersonaSwitcherSelection(previousItem);
            return;
        }

        ApplyActivePersonaToShell(
            result.Persona.PersonaId,
            result.Persona.DisplayName,
            syncLocalName: true);

        AppendSystemNotice(
            $"Active persona → {result.Persona.DisplayName} ({result.Persona.PersonaId}). " +
            "Memories stay quarantined; identity applies on the next chat turn.");

        SetPersonaStatus(result.Note ?? $"Active: {result.Persona.DisplayName}", warn: false);
    }

    private PersonaPackInfo? FindPersonaItem(string? personaId)
    {
        if (PersonaSwitcher?.ItemsSource is not IEnumerable<PersonaPackInfo> items
            || string.IsNullOrWhiteSpace(personaId))
        {
            return null;
        }

        return items.FirstOrDefault(p =>
            string.Equals(p.PersonaId, personaId, StringComparison.OrdinalIgnoreCase));
    }

    private void ApplyActivePersonaToShell(string? personaId, string? displayName, bool syncLocalName)
    {
        var name = string.IsNullOrWhiteSpace(displayName) ? LocalUiSettings.DefaultDisplayName : displayName.Trim();
        _activePersonaId = string.IsNullOrWhiteSpace(personaId) ? null : personaId.Trim();

        if (IdentityNameText is not null)
            IdentityNameText.Text = name;

        if (ChatInput is not null)
            ChatInput.Watermark = $"Message {name}…";

        if (syncLocalName
            && !string.Equals(_uiSettings.DisplayName, name, StringComparison.Ordinal))
        {
            _uiSettings.DisplayName = name;
            _uiSettings.Save();
            if (DisplayNameBox is not null)
                DisplayNameBox.Text = name;
        }

        UpdateIdentityDetail();
    }

    private async Task<bool> ConfirmPersonaQuarantineAsync(PersonaPackInfo selected)
    {
        var dialog = new Window
        {
            Title = "Confirm persona switch",
            Width = 440,
            Height = 260,
            MinWidth = 360,
            MinHeight = 220,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false,
            Background = Res("BgBrush"),
            Foreground = Res("TextBrush"),
            SystemDecorations = SystemDecorations.BorderOnly
        };

        var message = new TextBlock
        {
            Text = PersonaQuarantineConfirm.BuildMessage(selected.DisplayName, selected.PersonaId),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 16)
        };

        var cancel = new Button
        {
            Content = "Cancel",
            MinWidth = 88,
            Margin = new Thickness(0, 0, 8, 0)
        };
        cancel.Classes.Add("ghost");
        var confirm = new Button { Content = "Switch", MinWidth = 88 };
        confirm.Classes.Add("primary");

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancel, confirm }
        };

        dialog.Content = new Border
        {
            Padding = new Thickness(20),
            Child = new DockPanel { Children = { buttons, message } }
        };
        DockPanel.SetDock(buttons, Dock.Bottom);

        var tcs = new TaskCompletionSource<bool>();
        cancel.Click += (_, _) =>
        {
            tcs.TrySetResult(false);
            dialog.Close();
        };
        confirm.Click += (_, _) =>
        {
            tcs.TrySetResult(true);
            dialog.Close();
        };
        dialog.Closed += (_, _) => tcs.TrySetResult(false);

        await dialog.ShowDialog(this).ConfigureAwait(true);
        return await tcs.Task.ConfigureAwait(true);
    }

    private async void PersonaRefresh_Click(object? sender, RoutedEventArgs e) =>
        await RefreshPersonasAsync().ConfigureAwait(true);

    private async void PersonaCreate_Click(object? sender, RoutedEventArgs e) =>
        await OpenPersonaWizardAsync(editTarget: null).ConfigureAwait(true);

    private async void PersonaEdit_Click(object? sender, RoutedEventArgs e)
    {
        var selected = PersonaSwitcher?.SelectedItem as PersonaPackInfo
            ?? FindPersonaItem(_activePersonaId);
        if (selected is null)
        {
            AppendSystemNotice("Select a persona in the Identity switcher before editing.");
            return;
        }

        // Reload full pack (traits/charter) before edit.
        var get = await _personas.GetAsync(selected.PersonaId).ConfigureAwait(true);
        if (!get.Ok || get.Persona is null)
        {
            AppendSystemNotice($"Could not load persona for edit: {get.Detail ?? "Host error"}");
            return;
        }

        await OpenPersonaWizardAsync(get.Persona).ConfigureAwait(true);
    }

    private async Task OpenPersonaWizardAsync(PersonaPackInfo? editTarget)
    {
        var list = await _personas.ListAsync().ConfigureAwait(true);
        if (!list.Reachable || list.PersonaApiMissing)
        {
            var notice = !list.Reachable
                ? $"Persona Host unreachable: {list.Detail ?? "no detail"}"
                : (list.Detail ?? PersonaHostMessages.NoPersonaApi);
            AppendSystemNotice(notice);
            if (list.PersonaApiMissing)
            {
                _personaApiOk = false;
                SetPersonaStatus(notice, warn: true);
            }

            return;
        }

        var wizard = new PersonaWizardWindow(_personas, list.Personas, editTarget);
        var saved = await wizard.ShowDialog<bool>(this).ConfigureAwait(true);
        if (!saved || wizard.SavedPersona is null)
            return;

        AppendSystemNotice(wizard.ResultNotice
            ?? PersonaWizardLogic.BuildSaveNextTurnNotice(wizard.SavedPersona.DisplayName, editTarget is null));

        await RefreshPersonasAsync().ConfigureAwait(true);

        // Keep switcher selection on the saved pack without forcing activate.
        var item = FindPersonaItem(wizard.SavedPersona.PersonaId);
        if (item is not null)
            RestorePersonaSwitcherSelection(item);

        if (editTarget is not null
            && string.Equals(wizard.SavedPersona.PersonaId, _activePersonaId, StringComparison.OrdinalIgnoreCase))
        {
            ApplyActivePersonaToShell(
                wizard.SavedPersona.PersonaId,
                wizard.SavedPersona.DisplayName,
                syncLocalName: true);
        }
    }
}
