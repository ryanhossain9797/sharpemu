// Copyright (C) 2026 SharpEmu Emulator Project
// SPDX-License-Identifier: GPL-2.0-or-later

using Avalonia.Controls;
using Avalonia.Layout;

namespace SharpEmu.GUI;

/// <summary>
/// Editor for a single game's launch overrides. Each row has an "Override"
/// checkbox and a value control; while the checkbox is off the value inherits
/// the global setting (and is written as null). Persists to
/// user/custom_configs/&lt;titleId&gt;.json via <see cref="PerGameSettings"/>.
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

    private readonly CheckBox _overrideLogLevel = new() { Content = "Override log level" };
    private readonly ComboBox _logLevel = new() { ItemsSource = LogLevels, Width = 160 };

    private readonly CheckBox _overrideTrace = new() { Content = "Override import trace limit" };
    private readonly NumericUpDown _trace = new() { Minimum = 0, Maximum = 4096, Width = 160 };

    private readonly CheckBox _overrideStrict = new() { Content = "Override strict resolution" };
    private readonly CheckBox _strict = new() { Content = "Strict dynlib resolution" };

    private readonly CheckBox _overrideLogToFile = new() { Content = "Override log-to-file" };
    private readonly CheckBox _logToFile = new() { Content = "Write a log file" };

    private readonly CheckBox _overrideEnv = new() { Content = "Override environment toggles" };
    private readonly List<(string Name, CheckBox Box)> _envBoxes = new();

    public PerGameSettingsDialog(string titleId, string displayName, GuiSettings global)
    {
        _titleId = titleId;
        Title = $"Per-game settings — {displayName} ({titleId})";
        Width = 460;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;

        var existing = PerGameSettings.Load(titleId);

        var rows = new StackPanel { Orientation = Orientation.Vertical, Spacing = 12, Margin = new(16) };
        rows.Children.Add(new TextBlock
        {
            Text = "Unchecked settings inherit the global defaults.",
            Opacity = 0.7,
        });

        rows.Children.Add(Row(_overrideLogLevel, _logLevel));
        rows.Children.Add(Row(_overrideTrace, _trace));
        rows.Children.Add(Row(_overrideStrict, _strict));
        rows.Children.Add(Row(_overrideLogToFile, _logToFile));

        var envPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 4, Margin = new(20, 0, 0, 0) };
        foreach (var (name, label) in EnvToggles)
        {
            var box = new CheckBox { Content = label };
            _envBoxes.Add((name, box));
            envPanel.Children.Add(box);
        }

        rows.Children.Add(_overrideEnv);
        rows.Children.Add(envPanel);

        var save = new Button { Content = "Save" };
        var cancel = new Button { Content = "Cancel" };
        save.Click += (_, _) => { Persist(); Close(); };
        cancel.Click += (_, _) => Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancel, save },
        };
        rows.Children.Add(buttons);

        Content = new ScrollViewer { Content = rows };

        // Populate from defaults, then any existing overrides.
        _logLevel.SelectedItem = Array.IndexOf(LogLevels, global.LogLevel) >= 0 ? global.LogLevel : "Info";
        _trace.Value = global.ImportTraceLimit;
        _strict.IsChecked = global.StrictDynlibResolution;
        _logToFile.IsChecked = global.LogToFile;
        foreach (var (name, box) in _envBoxes)
        {
            box.IsChecked = global.EnvironmentToggles.Contains(name);
        }

        if (existing is not null)
        {
            if (existing.LogLevel is { } level) { _overrideLogLevel.IsChecked = true; _logLevel.SelectedItem = level; }
            if (existing.ImportTraceLimit is { } t) { _overrideTrace.IsChecked = true; _trace.Value = t; }
            if (existing.StrictDynlibResolution is { } s) { _overrideStrict.IsChecked = true; _strict.IsChecked = s; }
            if (existing.LogToFile is { } l) { _overrideLogToFile.IsChecked = true; _logToFile.IsChecked = l; }
            if (existing.EnvironmentToggles is { } env)
            {
                _overrideEnv.IsChecked = true;
                foreach (var (name, box) in _envBoxes)
                {
                    box.IsChecked = env.Contains(name);
                }
            }
        }

        // Enable value controls only while their override is checked.
        Bind(_overrideLogLevel, _logLevel);
        Bind(_overrideTrace, _trace);
        Bind(_overrideStrict, _strict);
        Bind(_overrideLogToFile, _logToFile);
        Bind(_overrideEnv, envPanel);
    }

    private static Control Row(CheckBox toggle, Control value)
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { toggle, value },
        };
    }

    private static void Bind(CheckBox toggle, Control value)
    {
        value.IsEnabled = toggle.IsChecked == true;
        toggle.IsCheckedChanged += (_, _) => value.IsEnabled = toggle.IsChecked == true;
    }

    private void Persist()
    {
        var settings = new PerGameSettings
        {
            LogLevel = _overrideLogLevel.IsChecked == true ? _logLevel.SelectedItem as string : null,
            ImportTraceLimit = _overrideTrace.IsChecked == true ? (int)(_trace.Value ?? 0) : null,
            StrictDynlibResolution = _overrideStrict.IsChecked == true ? _strict.IsChecked == true : null,
            LogToFile = _overrideLogToFile.IsChecked == true ? _logToFile.IsChecked == true : null,
            EnvironmentToggles = _overrideEnv.IsChecked == true
                ? _envBoxes.Where(e => e.Box.IsChecked == true).Select(e => e.Name).ToList()
                : null,
        };
        settings.Save(_titleId);
    }
}
