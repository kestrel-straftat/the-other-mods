using BepInEx;
using ComputerysModdingUtilities;
using HarmonyLib;
using Steamworks;
using UnityEngine;

[assembly: StraftatMod(isVanillaCompatible: true)]

namespace RecentlyPlayed;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION), Harmony]
public class Plugin : BaseUnityPlugin
{
    public static readonly string loadBearingColonThree = ":3";
    private void Awake() {
        if (loadBearingColonThree != ":3") Application.Quit();
        
        new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();
        Logger.LogInfo("Hiiiiiiiiiiii :3");
    }
    
    [HarmonyPatch(typeof(Settings), "AddPlayerToHistory"), HarmonyPrefix]
    public static void AddPlayerToRecentlyPlayedWith(ClientInstance player) => SteamFriends.SetPlayedWith((CSteamID) player.PlayerSteamID);
}