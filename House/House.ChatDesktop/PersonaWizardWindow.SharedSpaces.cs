using Avalonia.Controls;
using Avalonia.Interactivity;
using House.ChatDesktop.Services;

namespace House.ChatDesktop;

/// <summary>PROP-20.3 — shared-spaces step (after blurb, before quarantine).</summary>
public partial class PersonaWizardWindow
{
    private readonly HashSet<string> _desiredSpaceIds = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<SharedSpaceInfo> _sharedSpaces = Array.Empty<SharedSpaceInfo>();
    private bool _sharedSpacesLoaded;
    private bool _bindingSpaces;
    private bool _creatingSpace;

    private void InitSharedSpaces()
    {
        SharedSpacesHintText.Text = PersonaWizardLogic.SharedSpacesHint;
        if (_editTarget is not null)
        {
            foreach (var id in _editTarget.SharedSpaceIds)
                _desiredSpaceIds.Add(id.Trim());
        }

        _ = LoadSharedSpacesAsync();
    }

    private async Task LoadSharedSpacesAsync()
    {
        var snap = await _client.ListSharedSpacesAsync().ConfigureAwait(true);
        _sharedSpacesLoaded = snap.Reachable && snap.Ok;
        _sharedSpaces = snap.Spaces;
        BindSharedSpaces();

        SharedSpacesEmptyText.IsVisible = !_sharedSpacesLoaded || _sharedSpaces.Count == 0;
        SharedSpacesEmptyText.Text = !_sharedSpacesLoaded
            ? (snap.Detail ?? "Shared spaces unavailable. Attachments are left unchanged on save.")
            : "No shared spaces yet. Create one below.";
        SharedSpaceCreateButton.IsEnabled = _sharedSpacesLoaded;
    }

    private void BindSharedSpaces()
    {
        _bindingSpaces = true;
        try
        {
            SharedSpaceList.ItemsSource = _sharedSpaces;
            SharedSpaceList.SelectedItems?.Clear();
            foreach (var space in _sharedSpaces)
            {
                if (_desiredSpaceIds.Contains(space.SpaceId))
                    SharedSpaceList.SelectedItems?.Add(space);
            }
        }
        finally
        {
            _bindingSpaces = false;
        }
    }

    private void SharedSpaceList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_bindingSpaces)
            return;

        foreach (var removed in e.RemovedItems.OfType<SharedSpaceInfo>())
            _desiredSpaceIds.Remove(removed.SpaceId);
        foreach (var added in e.AddedItems.OfType<SharedSpaceInfo>())
            _desiredSpaceIds.Add(added.SpaceId);
    }

    private async void SharedSpaceCreate_Click(object? sender, RoutedEventArgs e)
    {
        if (_creatingSpace || _saving)
            return;

        var name = (SharedSpaceNewNameBox.Text ?? "").Trim();
        var spaceId = PersonaWizardLogic.SuggestSpaceId(name);
        if (string.IsNullOrWhiteSpace(name) || !PersonaWizardLogic.IsValidPersonaId(spaceId))
        {
            StatusText.Text = "Enter a space name (letters, digits, '-' or '_' produce the id).";
            return;
        }

        _creatingSpace = true;
        SharedSpaceCreateButton.IsEnabled = false;
        try
        {
            StatusText.Text = "Creating shared space…";
            var result = await _client.CreateSharedSpaceAsync(spaceId, name).ConfigureAwait(true);
            if (!result.Ok || result.Space is null)
            {
                StatusText.Text = result.Detail ?? "Host rejected the shared space.";
                return;
            }

            _desiredSpaceIds.Add(result.Space.SpaceId);
            SharedSpaceNewNameBox.Text = "";
            await LoadSharedSpacesAsync().ConfigureAwait(true);
            StatusText.Text = $"Created '{result.Space.DisplayName}'. It attaches when you save.";
        }
        finally
        {
            _creatingSpace = false;
            SharedSpaceCreateButton.IsEnabled = _sharedSpacesLoaded;
        }
    }

    /// <summary>Apply attach/detach diff after the pack is saved. Returns a notice suffix, or null.</summary>
    private async Task<string?> ApplySharedSpacesAsync(string personaId)
    {
        if (!_sharedSpacesLoaded)
            return null;

        var current = _editTarget?.SharedSpaceIds ?? Array.Empty<string>();
        var (attach, detach) = PersonaWizardLogic.DiffSharedSpaces(current, _desiredSpaceIds);
        var failures = new List<string>();

        foreach (var id in attach)
        {
            var r = await _client.AttachSharedSpaceAsync(personaId, id).ConfigureAwait(true);
            if (!r.Ok)
                failures.Add($"attach {id}: {r.Detail ?? "failed"}");
        }

        foreach (var id in detach)
        {
            var r = await _client.DetachSharedSpaceAsync(personaId, id).ConfigureAwait(true);
            if (!r.Ok)
                failures.Add($"detach {id}: {r.Detail ?? "failed"}");
        }

        return failures.Count == 0
            ? null
            : "Shared spaces not fully updated — " + string.Join("; ", failures);
    }
}
