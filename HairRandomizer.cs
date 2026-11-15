using System;
using Dalamud.Plugin.Services;
using Newtonsoft.Json.Linq;

namespace HairShuffle;

/// <summary>Runs on the framework thread, keeps the timer, and applies random hair colors through Glamourer.</summary>
public sealed class HairRandomizer : IDisposable
{
    private const int LocalPlayerIndex = 0;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(30);

    private readonly Configuration config;
    private readonly GlamourerIpc glamourer;
    private readonly Random rng = new();

    private bool applyRequested;
    private bool manualRequest;

    public DateTime NextChange { get; private set; }
    public string LastResult { get; private set; } = "No change yet.";

    public HairRandomizer(Configuration config, GlamourerIpc glamourer)
    {
        this.config = config;
        this.glamourer = glamourer;

        if (config.Enabled && config.ChangeOnStartup)
            NextChange = DateTime.Now;
        else
            ScheduleNext();

        Plugin.Framework.Update += OnUpdate;
    }

    public void Dispose() => Plugin.Framework.Update -= OnUpdate;

    /// <summary>Queue a change for the next framework tick (safe to call from UI or commands).</summary>
    public void RequestChangeNow()
    {
        applyRequested = true;
        manualRequest = true;
    }

    public void ScheduleNext()
    {
        var min = Math.Max(0.1f, Math.Min(config.MinIntervalMinutes, config.MaxIntervalMinutes));
        var max = Math.Max(0.1f, Math.Max(config.MinIntervalMinutes, config.MaxIntervalMinutes));
        var minutes = min + rng.NextDouble() * (max - min);
        NextChange = DateTime.Now.AddMinutes(minutes);
    }

    public bool IsWithinActiveHours(DateTime time)
    {
        if (!config.UseActiveHours)
            return true;

        var start = config.ActiveStartHour;
        var end = config.ActiveEndHour;
        var hour = time.Hour;
        if (start == end)
            return true;
        return start < end
            ? hour >= start && hour < end
            : hour >= start || hour < end; // wraps past midnight
    }

    private void OnUpdate(IFramework framework)
    {
        if (!applyRequested)
        {
            if (!config.Enabled || DateTime.Now < NextChange)
                return;

            if (!IsWithinActiveHours(DateTime.Now))
            {
                // Check again in a minute; the change happens as soon as the window opens.
                NextChange = DateTime.Now.AddMinutes(1);
                return;
            }

            applyRequested = true;
        }

        if (!Plugin.ClientState.IsLoggedIn || Plugin.ObjectTable.LocalPlayer == null)
        {
            // Not in game yet (loading screen, character select): try again shortly.
            if (!manualRequest)
            {
                applyRequested = false;
                NextChange = DateTime.Now + RetryDelay;
            }
            return;
        }

        var wasManual = manualRequest;
        applyRequested = false;
        manualRequest = false;

        var success = TryApplyRandomHair(out var message);
        LastResult = $"[{DateTime.Now:HH:mm}] {message}";

        if (success)
        {
            ScheduleNext();
            if (config.ChatNotification)
                Plugin.ChatGui.Print(message, "Hair Shuffle");
        }
        else
        {
            Plugin.Log.Warning(message);
            if (wasManual || config.ChatNotification)
                Plugin.ChatGui.PrintError(message, "Hair Shuffle");
            if (!wasManual)
                NextChange = DateTime.Now + RetryDelay;
        }
    }

    private bool TryApplyRandomHair(out string message)
    {
        if (!glamourer.IsAvailable)
        {
            message = "Glamourer isn't loaded. Install or enable Glamourer.";
            return false;
        }

        try
        {
            var (ec, state) = glamourer.GetState(LocalPlayerIndex);
            if (ec != GlamourerIpc.ApiEc.Success || state == null)
            {
                message = $"Couldn't read your Glamourer state ({ec}).";
                return false;
            }

            var customize = GetOrCreate(state, "Customize");
            var parameters = GetOrCreate(state, "Parameters");

            if (config.Mode == ColorMode.Palette)
            {
                var lo = Math.Clamp(Math.Min(config.PaletteMin, config.PaletteMax), 0, 255);
                var hi = Math.Clamp(Math.Max(config.PaletteMin, config.PaletteMax), 0, 255);
                var hair = rng.Next(lo, hi + 1);
                SetCustomize(customize, "HairColor", hair);
                // The current state's advanced dye values would override the palette color; don't apply them.
                DisableParameter(parameters, "HairDiffuse");

                message = $"Hair color changed to palette #{hair}";
                if (config.RandomizeHighlights)
                {
                    var highlight = rng.Next(lo, hi + 1);
                    SetCustomize(customize, "HighlightsColor", highlight);
                    DisableParameter(parameters, "HairHighlight");
                    message += $", highlights #{highlight}";
                }
                message += ".";
            }
            else
            {
                var (r, g, b) = RandomRgb();
                SetRgbParameter(parameters, "HairDiffuse", r, g, b);
                message = $"Hair color changed to {ToHex(r, g, b)}";
                if (config.RandomizeHighlights)
                {
                    var (hr, hg, hb) = RandomRgb();
                    SetRgbParameter(parameters, "HairHighlight", hr, hg, hb);
                    message += $", highlights {ToHex(hr, hg, hb)}";
                }
                message += ".";
            }

            // Only apply customization so gear is never touched.
            var result = glamourer.ApplyState(state, LocalPlayerIndex, GlamourerIpc.ApplyFlag.Customization);
            if (result != GlamourerIpc.ApiEc.Success)
            {
                message = $"Glamourer refused the change ({result}).";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to apply hair color.");
            message = $"Error talking to Glamourer: {ex.Message}";
            return false;
        }
    }

    private (float R, float G, float B) RandomRgb()
    {
        var h = (float)rng.NextDouble();
        var s = Lerp(config.MinSaturation, config.MaxSaturation, (float)rng.NextDouble());
        var v = Lerp(config.MinBrightness, config.MaxBrightness, (float)rng.NextDouble());
        return HsvToRgb(h, s, v);
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    private static (float, float, float) HsvToRgb(float h, float s, float v)
    {
        var i = (int)(h * 6f) % 6;
        var f = h * 6f - (int)(h * 6f);
        var p = v * (1 - s);
        var q = v * (1 - f * s);
        var t = v * (1 - (1 - f) * s);
        return i switch
        {
            0 => (v, t, p),
            1 => (q, v, p),
            2 => (p, v, t),
            3 => (p, q, v),
            4 => (t, p, v),
            _ => (v, p, q),
        };
    }

    private static string ToHex(float r, float g, float b)
        => $"#{(int)(r * 255):X2}{(int)(g * 255):X2}{(int)(b * 255):X2}";

    private static JObject GetOrCreate(JObject parent, string name)
    {
        if (parent[name] is JObject obj)
            return obj;
        obj = new JObject();
        parent[name] = obj;
        return obj;
    }

    private static void SetCustomize(JObject customize, string key, int value)
        => customize[key] = new JObject { ["Value"] = value, ["Apply"] = true };

    private static void DisableParameter(JObject parameters, string key)
    {
        if (parameters[key] is JObject p)
            p["Apply"] = false;
    }

    private static void SetRgbParameter(JObject parameters, string key, float r, float g, float b)
        => parameters[key] = new JObject { ["Red"] = r, ["Green"] = g, ["Blue"] = b, ["Apply"] = true };
}
