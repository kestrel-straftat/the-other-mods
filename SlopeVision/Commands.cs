using ChatCommands;
using ChatCommands.Attributes;

namespace SlopeVision;

[CommandCategory("SlopeVision")]
public static class Commands
{
    [Command("slv_toggle", "Toggles the slope vision overlay.", CommandFlags.IngameOnly | CommandFlags.ExplorationOnly)]
    public static void Toggle() {
        var plugin = Plugin.Instance;
        plugin.OverlayActive = !plugin.OverlayActive;
    }
    
    [Command("slv_enable", "Enables the slope vision overlay.", CommandFlags.IngameOnly | CommandFlags.ExplorationOnly)]
    public static void Enable() {
        Plugin.Instance.OverlayActive = true;
    }
    
    [Command("slv_disable", "Disables the slope vision overlay.", CommandFlags.IngameOnly | CommandFlags.ExplorationOnly)]
    public static void Disable() {
        Plugin.Instance.OverlayActive = false;
    }
}