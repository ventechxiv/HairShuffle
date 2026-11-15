using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace HairShuffle;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/hairshuffle";

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    public Configuration Configuration { get; }
    public GlamourerIpc Glamourer { get; }
    public HairRandomizer Randomizer { get; }

    private readonly WindowSystem windowSystem = new("HairShuffle");
    private readonly ConfigWindow configWindow;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Glamourer = new GlamourerIpc(PluginInterface);
        Randomizer = new HairRandomizer(Configuration, Glamourer);

        configWindow = new ConfigWindow(this);
        windowSystem.AddWindow(configWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open Hair Shuffle settings. Use \"now\" to change hair immediately, \"on\"/\"off\" to toggle the timer.",
        });

        PluginInterface.UiBuilder.Draw += windowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += configWindow.Toggle;
        PluginInterface.UiBuilder.OpenMainUi += configWindow.Toggle;
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= windowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= configWindow.Toggle;
        PluginInterface.UiBuilder.OpenMainUi -= configWindow.Toggle;
        windowSystem.RemoveAllWindows();
        CommandManager.RemoveHandler(CommandName);
        Randomizer.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        switch (args.Trim().ToLowerInvariant())
        {
            case "now":
                Randomizer.RequestChangeNow();
                break;
            case "on":
                SetEnabled(true);
                ChatGui.Print("Hair Shuffle timer on.", "Hair Shuffle");
                break;
            case "off":
                SetEnabled(false);
                ChatGui.Print("Hair Shuffle timer off.", "Hair Shuffle");
                break;
            default:
                configWindow.Toggle();
                break;
        }
    }

    public void SetEnabled(bool enabled)
    {
        Configuration.Enabled = enabled;
        Configuration.Save();
        if (enabled)
            Randomizer.ScheduleNext();
    }
}
