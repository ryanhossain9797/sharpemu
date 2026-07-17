// Copyright (C) 2026 SharpEmu Emulator Project
// SPDX-License-Identifier: GPL-2.0-or-later

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace SharpEmu.GUI;

/// <summary>
/// Editor for a single game's launch overrides. Its own popup layout, but woven
/// from the shared <see cref="SettingRow"/> pieces and card styling the Options
/// page uses, so the two read as one app. Each row's Override switch gates its
/// value control; off means inherit the global setting (written as null).
/// Persists to user/custom_configs/&lt;titleId&gt;.json.
/// </summary>
public sealed class PerGameSettingsDialog : Window
{
    private static readonly string[] LogLevels =
        { "Trace", "Debug", "Info", "Warning", "Error", "Critical" };

    // Matches the SHARPEMU_* switches the global Environment tab exposes.
    private static readonly (string Name, string Label)[] EnvToggles =
    {
        ("SHARPEMU_BTHID_UNAVAILABLE", "Disable Bluetooth HID"),
        ("SHARPEMU_DISABLE_IMPORT_LOOP_GUARD", "Disable import loop guard"),
        ("SHARPEMU_WRITABLE_APP0", "Writable /app0"),
        ("SHARPEMU_VK_VALIDATION", "Vulkan validation layers"),
        ("SHARPEMU_DUMP_SPIRV", "Dump SPIR-V"),
        ("SHARPEMU_LOG_DIRECT_MEMORY", "Log direct memory"),
        ("SHARPEMU_LOG_IO", "Log I/O"),
        ("SHARPEMU_LOG_NP", "Log NP"),
    };

    private readonly string _titleId;

    private readonly SettingRow _logLevelRow;
    private readonly ComboBox _logLevel = new() { ItemsSource = LogLevels, Width = 160 };

    private readonly SettingRow _traceRow;
    private readonly NumericUpDown _trace = new()
    {
        Minimum = 0, Maximum = 4096, Increment = 16, Width = 160, FormatString = "0",
    };

    private readonly SettingRow _strictRow;
    private readonly ToggleSwitch _strict = new() { OnContent = "On", OffContent = "Off" };

    private readonly SettingRow _logToFileRow;
    private readonly ToggleSwitch _logToFile = new() { OnContent = "On", OffContent = "Off" };

    private readonly SettingRow _envRow;
    private readonly StackPanel _envList = new() { Orientation = Orientation.Vertical, Spacing = 8, Margin = new(0, 4, 0, 0) };
    private readonly List<(string Name, ToggleSwitch Box)> _envBoxes = new();

    public PerGameSettingsDialog(string titleId, string displayName, GuiSettings global)
    {
        _titleId = titleId;
        Title = $"Per-game settings — {displayName} ({titleId})";
        Width = 520;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;

        _logLevelRow = Row("Log level", "Verbosity of the emulator console output.", _logLevel);
        _traceRow = Row("Import trace limit", "Trace the first N imports per module (0 = off).", _trace);
        _strictRow = Row("Strict dynlib resolution", "Fail the launch when an imported symbol cannot be resolved.", _strict);
        _logToFileRow = Row("Log to file", "Mirror emulator output to a log file.", _logToFile);
        _envRow = new SettingRow
        {
            Label = "Environment toggles",
            Description = "Override the global set of SHARPEMU_* switches for this game.",
            ShowOverride = true,
        };

        foreach (var (name, label) in EnvToggles)
        {
            var box = new ToggleSwitch { OnContent = label, OffContent = label };
            _envBoxes.Add((name, box));
            _envList.Children.Add(box);
        }

        var content = new StackPanel { Orientation = Orientation.Vertical, Spacing = 12, Margin = new(16) };
        content.Children.Add(new TextBlock
        {
            Text = "Unchecked rows inherit the global defaults.",
            Foreground = new SolidColorBrush(Color.Parse("#8B94A7")),
            FontSize = 12,
        });
        content.Children.Add(Card("EMULATION", _strictRow));
        content.Children.Add(Card("LOGGING", _logLevelRow, _traceRow, _logToFileRow));
        content.Children.Add(Card("ENVIRONMENT", _envRow, _envList));

        var save = new Button { Content = "Save", Classes = { "accent" } };
        var cancel = new Button { Content = "Cancel", Classes = { "ghost" } };
        save.Click += (_, _) => { Persist(); Close(); };
        cancel.Click += (_, _) => Close();
        content.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancel, save },
        });

        Content = new ScrollViewer { Content = content };

        LoadValues(global);
        _envRow.PropertyChanged += (_, e) =>
        {
            if (e.Property == SettingRow.IsOverriddenProperty)
            {
                _envList.IsEnabled = _envRow.IsOverridden;
            }
        };
        _envList.IsEnabled = _envRow.IsOverridden;
    }

    private static SettingRow Row(string label, string description, Control value) => new()
    {
        Label = label,
        Description = description,
        ShowOverride = true,
        Content = value,
    };

    private static Border Card(string title, params Control[] rows)
    {
        var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 14 };
        stack.Children.Add(new TextBlock { Text = title, Classes = { "sectionTitle" } });
        foreach (var row in rows)
        {
            stack.Children.Add(row);
        }

        var card = new Border { Child = stack };
        card.Classes.Add("card");
        return card;
    }

    private void LoadValues(GuiSettings global)
    {
        // Base every control on the global value, then flag any existing overrides.
        _logLevel.SelectedItem = Array.IndexOf(LogLevels, global.LogLevel) >= 0 ? global.LogLevel : "Info";
        _trace.Value = global.ImportTraceLimit;
        _strict.IsChecked = global.StrictDynlibResolution;
        _logToFile.IsChecked = global.LogToFile;
        foreach (var (name, box) in _envBoxes)
        {
            box.IsChecked = global.EnvironmentToggles.Contains(name);
        }

        var existing = PerGameSettings.Load(_titleId);
        if (existing is null)
        {
            return;
        }

        if (existing.LogLevel is { } level) { _logLevelRow.IsOverridden = true; _logLevel.SelectedItem = level; }
        if (existing.ImportTraceLimit is { } t) { _traceRow.IsOverridden = true; _trace.Value = t; }
        if (existing.StrictDynlibResolution is { } s) { _strictRow.IsOverridden = true; _strict.IsChecked = s; }
        if (existing.LogToFile is { } l) { _logToFileRow.IsOverridden = true; _logToFile.IsChecked = l; }
        if (existing.EnvironmentToggles is { } env)
        {
            _envRow.IsOverridden = true;
            foreach (var (name, box) in _envBoxes)
            {
                box.IsChecked = env.Contains(name);
            }
        }
    }

    private void Persist()
    {
        var settings = new PerGameSettings
        {
            LogLevel = _logLevelRow.IsOverridden ? _logLevel.SelectedItem as string : null,
            ImportTraceLimit = _traceRow.IsOverridden ? (int)(_trace.Value ?? 0) : null,
            StrictDynlibResolution = _strictRow.IsOverridden ? _strict.IsChecked == true : null,
            LogToFile = _logToFileRow.IsOverridden ? _logToFile.IsChecked == true : null,
            EnvironmentToggles = _envRow.IsOverridden
                ? _envBoxes.Where(e => e.Box.IsChecked == true).Select(e => e.Name).ToList()
                : null,
        };
        settings.Save(_titleId);
    }
}
