// Copyright (C) 2026 SharpEmu Emulator Project
// SPDX-License-Identifier: GPL-2.0-or-later

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Media;

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
public sealed class SettingRow : ContentControl
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

    public static readonly StyledProperty<FontFamily?> LabelFontFamilyProperty =
        AvaloniaProperty.Register<SettingRow, FontFamily?>(nameof(LabelFontFamily));

    private ContentPresenter? _slot;
    private TextBlock? _label;

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

    /// <summary>
    /// Optional font for the label only. Left unset the label inherits the theme font;
    /// set it (e.g. a monospace family) for rows whose label is a literal identifier
    /// such as the SHARPEMU_* environment-variable names.
    /// </summary>
    public FontFamily? LabelFontFamily
    {
        get => GetValue(LabelFontFamilyProperty);
        set => SetValue(LabelFontFamilyProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _slot = e.NameScope.Find<ContentPresenter>("PART_Slot");
        _label = e.NameScope.Find<TextBlock>("PART_Label");
        UpdateSlotEnabled();
        UpdateLabelFont();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ShowOverrideProperty || change.Property == IsOverriddenProperty)
        {
            UpdateSlotEnabled();
        }
        else if (change.Property == LabelFontFamilyProperty)
        {
            UpdateLabelFont();
        }
    }

    // Apply the custom label font only when one is set; otherwise leave the
    // label inheriting the theme font so every other row stays consistent.
    private void UpdateLabelFont()
    {
        if (_label is not null && LabelFontFamily is { } family)
        {
            _label.FontFamily = family;
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
