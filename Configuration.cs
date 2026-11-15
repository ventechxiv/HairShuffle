using System;
using Dalamud.Configuration;

namespace HairShuffle;

public enum ColorMode
{
    /// <summary>Pick a random entry from the game's hair color palette (what the character creator offers).</summary>
    Palette = 0,

    /// <summary>Pick any RGB color using Glamourer's advanced hair dye parameters.</summary>
    CustomRgb = 1,
}

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool Enabled { get; set; } = false;

    // Interval: a random value between Min and Max minutes. Set them equal for a fixed interval.
    public float MinIntervalMinutes { get; set; } = 30f;
    public float MaxIntervalMinutes { get; set; } = 30f;

    public ColorMode Mode { get; set; } = ColorMode.Palette;

    // Palette mode: range of palette indices to pick from.
    public int PaletteMin { get; set; } = 0;
    public int PaletteMax { get; set; } = 191;

    // Custom RGB mode: limits so colors don't come out muddy or black.
    public float MinSaturation { get; set; } = 0.35f;
    public float MaxSaturation { get; set; } = 1.0f;
    public float MinBrightness { get; set; } = 0.35f;
    public float MaxBrightness { get; set; } = 1.0f;

    public bool RandomizeHighlights { get; set; } = false;

    // Only change hair between these local hours (24h). If Start > End the window wraps past midnight.
    public bool UseActiveHours { get; set; } = false;
    public int ActiveStartHour { get; set; } = 8;
    public int ActiveEndHour { get; set; } = 23;

    public bool ChangeOnStartup { get; set; } = false;
    public bool ChatNotification { get; set; } = true;

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}
