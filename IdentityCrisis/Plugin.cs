using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using ComputerysModdingUtilities;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

[assembly: StraftatMod(isVanillaCompatible: true)]

namespace IdentityCrisis;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private static Dictionary<ItemBehaviour, (string original, string renamed)> m_renamedItems = [];
    private static ConfigEntry<float> m_renameChance;
    private static string[] m_names = [
        "Mid-life Crisis",
        "Identity Crisis",
        "Financial Crisis",
        "Housing Crisis",
        "Existential Crisis",
        "Energy Crisis",
        "Ecological Crisis",
        "Crysis (2007)",
        "Hostage Crisis",
        "Humanitarian Crisis",
    ];
    
    public static readonly string loadBearingColonThree = ":3";
    private void Awake() {
        if (loadBearingColonThree != ":3") Application.Quit();
        gameObject.hideFlags = HideFlags.HideAndDontSave;

        SceneManager.sceneLoaded += (_, _) => m_renamedItems.Clear();
        m_renameChance = Config.Bind("General", "Rename Chance", 0.2f, new ConfigDescription("The probability of a crisis being renamed. Requires a map restart to fully apply.", new AcceptableValueRange<float>(0.0f, 1.0f)));
        
        
        new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();
        Logger.LogInfo("Hiiiiiiiiiiii :3");
    }

    [HarmonyPatch(typeof(ItemBehaviour))]
    private static class ItemBehaviourPatch
    {
        [HarmonyPatch("Start"), HarmonyPrefix]
        private static void AddNames(ItemBehaviour __instance) {
            switch (__instance.weaponName) {
                case "crisis":
                    string newName = __instance.weaponName;
                    if (Random.value < m_renameChance.Value) {
                        newName = m_names[Random.Range(0, m_names.Length)];
                    }
                    
                    m_renamedItems.Add(__instance, (__instance.weaponName, newName));
                    
                    break;
                case "blank state":
                    if (Random.value < 0.1f) m_renamedItems.Add(__instance, (__instance.weaponName, "blank slate"));
                    break;
            }
        }
        
        [HarmonyPatch("OnFocus"), HarmonyPrefix]
        private static void GainFocus(ItemBehaviour __instance) {
            if (m_renamedItems.TryGetValue(__instance, out var rename)) {
                __instance.weaponName = rename.renamed;
            }
            
        }

        [HarmonyPatch("OnLoseFocus"), HarmonyPrefix]
        private static void LoseFocus(ItemBehaviour __instance) {
            if (m_renamedItems.TryGetValue(__instance, out var rename)) {
                __instance.weaponName = rename.original;
            }
        }
    }
}