// Copyright (C) 2026 SharpEmu Emulator Project
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharpEmu.GUI;

/// <summary>
/// Per-title launch overrides. Every field is nullable: null means "inherit the
/// global <see cref="GuiSettings"/> value". Only settings that affect a single
/// game's run are overridable; library-wide settings stay global.
///
/// Stored one file per game at user/custom_configs/&lt;titleId&gt;.json, matching the
/// convention other emulators use (shadPS4 user/custom_configs, RPCS3
/// custom_configs, PCSX2 GameSettings): the third tier on top of the built-in
/// defaults and the global preferences that already exist.
/// </summary>
public sealed class PerGameSettings
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    public string? LogLevel { get; set; }

    public int? ImportTraceLimit { get; set; }

    public bool? StrictDynlibResolution { get; set; }

    public bool? LogToFile { get; set; }

    /// <summary>
    /// Null inherits the global toggle set; a non-null list replaces it wholesale
    /// (so a game can opt out of a globally-enabled toggle, not just add to it).
    /// </summary>
    public List<string>? EnvironmentToggles { get; set; }

    /// <summary>True when nothing is overridden, i.e. the file may be deleted.</summary>
    [JsonIgnore]
    public bool IsEmpty =>
        LogLevel is null &&
        ImportTraceLimit is null &&
        StrictDynlibResolution is null &&
        LogToFile is null &&
        EnvironmentToggles is null;

    // Per-game configs live under the portable user/ directory (like logs and
    // savedata), not next to gui-settings.json, to mirror shadPS4's layout.
    public static string DirectoryPath =>
        Path.Combine(AppContext.BaseDirectory, "user", "custom_configs");

    public static string PathFor(string titleId) =>
        Path.Combine(DirectoryPath, SanitizeTitleId(titleId) + ".json");

    /// <summary>Loads a game's overrides, or null when the title id is blank or no file exists.</summary>
    public static PerGameSettings? Load(string? titleId)
    {
        if (string.IsNullOrWhiteSpace(titleId))
        {
            return null;
        }

        try
        {
            var path = PathFor(titleId);
            if (File.Exists(path))
            {
                return JsonSerializer.Deserialize<PerGameSettings>(File.ReadAllText(path), SerializerOptions);
            }
        }
        catch (Exception)
        {
            // A corrupt per-game file falls back to inheriting global settings.
        }

        return null;
    }

    /// <summary>Persists the overrides, or deletes the file when nothing is overridden.</summary>
    public void Save(string titleId)
    {
        if (string.IsNullOrWhiteSpace(titleId))
        {
            return;
        }

        try
        {
            var path = PathFor(titleId);
            if (IsEmpty)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                return;
            }

            Directory.CreateDirectory(DirectoryPath);
            File.WriteAllText(path, JsonSerializer.Serialize(this, SerializerOptions));
        }
        catch (Exception)
        {
            // Persistence is best-effort, as with the global settings.
        }
    }

    private static string SanitizeTitleId(string titleId)
    {
        var trimmed = titleId.Trim();
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            trimmed = trimmed.Replace(invalid, '_');
        }

        return trimmed.Length == 0 ? "UNKNOWN" : trimmed;
    }
}

/// <summary>
/// The launch-affecting settings after resolving the three tiers. Every value is
/// concrete: a per-game override if present, otherwise the global preference
/// (which already carries the built-in default).
/// </summary>
public sealed record EffectiveLaunchSettings(
    string LogLevel,
    int ImportTraceLimit,
    bool StrictDynlibResolution,
    bool LogToFile,
    IReadOnlyList<string> EnvironmentToggles)
{
    public static EffectiveLaunchSettings Resolve(GuiSettings global, PerGameSettings? perGame) => new(
        perGame?.LogLevel ?? global.LogLevel,
        perGame?.ImportTraceLimit ?? global.ImportTraceLimit,
        perGame?.StrictDynlibResolution ?? global.StrictDynlibResolution,
        perGame?.LogToFile ?? global.LogToFile,
        perGame?.EnvironmentToggles ?? global.EnvironmentToggles);
}
