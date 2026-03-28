using ChatCommands;
using ChatCommands.Attributes;

namespace ChromaKey;

[CommandCategory("ChromaKey")]
public static class Commands
{
    [Command("cc_toggle", "Toggles the chroma key overlay.", CommandFlags.IngameOnly | CommandFlags.ExplorationOnly)]
    public static void Toggle() {
        var plugin = Plugin.Instance;
        plugin.OverlayActive = !plugin.OverlayActive;
    }
    
    [Command("cc_enable", "Enables the chroma key overlay.", CommandFlags.IngameOnly | CommandFlags.ExplorationOnly)]
    public static void Enable() {
        Plugin.Instance.OverlayActive = true;
    }
    
    [Command("cc_disable", "Disables the chroma key overlay.", CommandFlags.IngameOnly | CommandFlags.ExplorationOnly)]
    public static void Disable() {
        Plugin.Instance.OverlayActive = false;
    }
}