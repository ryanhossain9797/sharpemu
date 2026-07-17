// Copyright (C) 2026 SharpEmu Emulator Project
// SPDX-License-Identifier: GPL-2.0-or-later

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace SharpEmu.GUI;

/// <summary>
/// One settings row: a label and description on the left and a value control
/// (the <see cref="ContentControl.Content"/>) on the right, styled to match the
/// Options page cards. Shared by both the Options page and the per-game dialog so
/// the two read as one app.
///
/// When <see cref="ShowOverride"/> is set (the per-game dialog), an extra toggle
/// appears; while it is off the value control is disabled and the setting
/// inherits the global value. The Options page leaves it off.
/// </summary>
public class SettingRow : ContentControl
{
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<SettingRow, string?>(nameof(Label));

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<SettingRow, string?>(nameof(Description));

    public static readonly StyledProperty<bool> ShowOverrideProperty =
        AvaloniaProperty.Register<SettingRow, bool>(nameof(ShowOverride));

    public static readonly StyledProperty<bool> IsOverriddenProperty =
        AvaloniaProperty.Register<SettingRow, bool>(
            nameof(IsOverridden), defaultBindingMode: BindingMode.TwoWay);

    private ContentPresenter? _slot;

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    /// <summary>Show the per-row "override" toggle (per-game editor); off for the global Options page.</summary>
    public bool ShowOverride
    {
        get => GetValue(ShowOverrideProperty);
        set => SetValue(ShowOverrideProperty, value);
    }

    /// <summary>Whether this row overrides the global value. Only meaningful when <see cref="ShowOverride"/> is true.</summary>
    public bool IsOverridden
    {
        get => GetValue(IsOverriddenProperty);
        set => SetValue(IsOverriddenProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _slot = e.NameScope.Find<ContentPresenter>("PART_Slot");
        UpdateSlotEnabled();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ShowOverrideProperty || change.Property == IsOverriddenProperty)
        {
            UpdateSlotEnabled();
        }
    }

    // The value control is live unless an override toggle is shown and unchecked.
    private void UpdateSlotEnabled()
    {
        if (_slot is not null)
        {
            _slot.IsEnabled = !ShowOverride || IsOverridden;
        }
    }
}
