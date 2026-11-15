using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace HairShuffle;

public sealed class ConfigWindow : Window
{
    private readonly Plugin plugin;
    private Configuration Config => plugin.Configuration;

    public ConfigWindow(Plugin plugin) : base("Hair Shuffle###HairShuffleConfig")
    {
        this.plugin = plugin;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(380, 300),
            MaximumSize = new Vector2(800, 1000),
        };
    }

    public override void Draw()
    {
        var changed = false;

        // --- Status ---
        var version = plugin.Glamourer.GetApiVersion();
        if (version is { } v)
            ImGui.TextColored(new Vector4(0.5f, 0.9f, 0.5f, 1f), $"Glamourer connected (API {v.Major}.{v.Minor})");
        else
            ImGui.TextColored(new Vector4(1f, 0.5f, 0.4f, 1f), "Glamourer not detected. Install or enable Glamourer.");

        var enabled = Config.Enabled;
        if (ImGui.Checkbox("Change hair color automatically", ref enabled))
            plugin.SetEnabled(enabled);

        if (Config.Enabled)
        {
            var remaining = plugin.Randomizer.NextChange - DateTime.Now;
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
            ImGui.TextUnformatted($"Next change in {(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}");
            if (!plugin.Randomizer.IsWithinActiveHours(DateTime.Now))
                ImGui.TextDisabled("Outside active hours, waiting.");
        }

        ImGui.TextDisabled(plugin.Randomizer.LastResult);

        if (ImGui.Button("Change now"))
            plugin.Randomizer.RequestChangeNow();

        ImGui.Separator();

        // --- Interval ---
        ImGui.TextUnformatted("Interval (minutes)");
        ImGui.TextDisabled("Each wait is a random length between Min and Max. Make them equal for a fixed interval.");
        var min = Config.MinIntervalMinutes;
        var max = Config.MaxIntervalMinutes;
        ImGui.SetNextItemWidth(120);
        if (ImGui.InputFloat("Min##interval", ref min, 1f, 10f, "%.1f"))
        {
            Config.MinIntervalMinutes = Math.Max(0.1f, min);
            if (Config.MaxIntervalMinutes < Config.MinIntervalMinutes)
                Config.MaxIntervalMinutes = Config.MinIntervalMinutes;
            changed = true;
        }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(120);
        if (ImGui.InputFloat("Max##interval", ref max, 1f, 10f, "%.1f"))
        {
            Config.MaxIntervalMinutes = Math.Max(Config.MinIntervalMinutes, max);
            changed = true;
        }

        var useHours = Config.UseActiveHours;
        if (ImGui.Checkbox("Only between certain hours", ref useHours))
        {
            Config.UseActiveHours = useHours;
            changed = true;
        }
        if (Config.UseActiveHours)
        {
            var start = Config.ActiveStartHour;
            var end = Config.ActiveEndHour;
            ImGui.SetNextItemWidth(120);
            if (ImGui.SliderInt("From (hour)", ref start, 0, 23))
            {
                Config.ActiveStartHour = start;
                changed = true;
            }
            ImGui.SetNextItemWidth(120);
            if (ImGui.SliderInt("Until (hour)", ref end, 0, 23))
            {
                Config.ActiveEndHour = end;
                changed = true;
            }
            ImGui.TextDisabled("Uses your PC's local time. 22 to 6 wraps past midnight.");
        }

        ImGui.Separator();

        // --- Color mode ---
        ImGui.TextUnformatted("Color source");
        if (ImGui.RadioButton("Game palette", Config.Mode == ColorMode.Palette))
        {
            Config.Mode = ColorMode.Palette;
            changed = true;
        }
        ImGui.SameLine();
        if (ImGui.RadioButton("Any color (advanced dye)", Config.Mode == ColorMode.CustomRgb))
        {
            Config.Mode = ColorMode.CustomRgb;
            changed = true;
        }

        if (Config.Mode == ColorMode.Palette)
        {
            var pMin = Config.PaletteMin;
            var pMax = Config.PaletteMax;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderInt("Lowest palette index", ref pMin, 0, 191))
            {
                Config.PaletteMin = pMin;
                changed = true;
            }
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderInt("Highest palette index", ref pMax, 0, 191))
            {
                Config.PaletteMax = pMax;
                changed = true;
            }
            ImGui.TextDisabled("Index 0 is the top-left swatch in the character creator.");
        }
        else
        {
            var sMin = Config.MinSaturation;
            var sMax = Config.MaxSaturation;
            var bMin = Config.MinBrightness;
            var bMax = Config.MaxBrightness;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Min saturation", ref sMin, 0f, 1f))
            {
                Config.MinSaturation = Math.Min(sMin, Config.MaxSaturation);
                changed = true;
            }
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Max saturation", ref sMax, 0f, 1f))
            {
                Config.MaxSaturation = Math.Max(sMax, Config.MinSaturation);
                changed = true;
            }
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Min brightness", ref bMin, 0f, 1f))
            {
                Config.MinBrightness = Math.Min(bMin, Config.MaxBrightness);
                changed = true;
            }
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Max brightness", ref bMax, 0f, 1f))
            {
                Config.MaxBrightness = Math.Max(bMax, Config.MinBrightness);
                changed = true;
            }
        }

        var highlights = Config.RandomizeHighlights;
        if (ImGui.Checkbox("Also randomize highlights color", ref highlights))
        {
            Config.RandomizeHighlights = highlights;
            changed = true;
        }
        ImGui.TextDisabled("Only visible if highlights are turned on for your character.");

        ImGui.Separator();

        var onStartup = Config.ChangeOnStartup;
        if (ImGui.Checkbox("Change right away when the plugin loads", ref onStartup))
        {
            Config.ChangeOnStartup = onStartup;
            changed = true;
        }

        var notify = Config.ChatNotification;
        if (ImGui.Checkbox("Show a chat message on each change", ref notify))
        {
            Config.ChatNotification = notify;
            changed = true;
        }

        if (changed)
            Config.Save();
    }
}
