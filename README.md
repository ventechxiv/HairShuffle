# Hair Shuffle

A Dalamud plugin that changes your hair color to a random one through Glamourer, on a timer you set.

## Build

1. Install the .NET 10 SDK (`winget install Microsoft.DotNet.SDK.10`).
2. Run XIVLauncher with Dalamud at least once so `%APPDATA%\XIVLauncher\addon\Hooks\dev` exists.
3. Run `dotnet build -c Release` in this folder.

## Install (dev plugin)

1. In game, open `/xlsettings` > Experimental > Dev Plugin Locations.
2. Add the full path to `bin\Release\HairShuffle.dll` and save.
3. Open `/xlplugins` > Dev Tools > Installed Dev Plugins and enable Hair Shuffle.

## Use

- `/hairshuffle`: open settings
- `/hairshuffle now`: change hair now
- `/hairshuffle on` / `off`: turn the timer on or off

Settings:
- **Interval**: each wait is a random length between Min and Max minutes. Make them equal for a fixed interval.
- **Active hours**: only change between certain hours of your PC's local time.
- **Color source**: *Game palette* picks one of the character creator swatches. *Any color* uses Glamourer's advanced hair dye and picks any color, with saturation and brightness limits.
- **Highlights**: also randomize the highlights color. This only shows if highlights are on for your character.

Only customization is applied, so your gear is never touched. To undo, use Glamourer's revert button.
If you switch from *Any color* back to *Game palette* and the color stops changing, revert your advanced dyes in Glamourer once.
