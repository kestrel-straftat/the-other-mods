using System;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using ComputerysModdingUtilities;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

[assembly: StraftatMod(isVanillaCompatible: false)]

namespace AboubiUnbound;

[BepInDependency(MyceliumNetworking.MyPluginInfo.PLUGIN_GUID)]
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance { get; private set; }
    public const uint c_myceliumID = 26;
    internal static new ManualLogSource Logger;

    private static SyncedConfigEntry<bool> m_enabled;
    private static SyncedConfigEntry<bool> m_killzEnabled;
    
    public static readonly string loadBearingColonThree = ":3";
    private void Awake() {
        if (loadBearingColonThree != ":3") Application.Quit();
        gameObject.hideFlags = HideFlags.HideAndDontSave;
        Instance = this;
        Logger = base.Logger;
        SceneManager.sceneLoaded += RemoveInvisibleWalls;

        m_enabled = Config.BindSynced(
            "General",
            "Enabled",
            true,
            "[This option's value will be controlled by the lobby host]\n" +
            "Whether or not the plugin is enabled. " +
            "Requires a map restart to apply."
        );
        
        m_killzEnabled = Config.BindSynced(
            "General",
            "Remove killplanes",
            false,
            "[This option's value will be controlled by the lobby host]\n" +
            "Whether or not to also remove killplanes. Enabling this option will let you out of maps " +
            "that use killplanes in place of invisible walls, but will likely cause softlocks! " +
            "Requires a map restart to apply."
        );
        
        Logger.LogInfo("Hiiiiiiiiiiii :3");
    }

    // edge cases where things should not be considered invisible walls
    // because if theres one thing straftat is good at its fucking edge cases
    
    private static readonly string[] m_whitelistedTags = [
        "Killz",
        "Teleport",
        "MovingObject",
        "Footsteps/Water",
        "Footsteps/Metal/Grille",
        "Door/Indoor",
        "Door/Outdoor",
    ];
    
    private static readonly Type[] m_whitelistedComponents = [
        typeof(PostProcessVolume),
        typeof(GravityZone)
    ];

    private static readonly int[] m_whitelistedLayers = [
        LayerMask.NameToLayer("Ragdoll"),
        LayerMask.NameToLayer("Ladder"),
        LayerMask.NameToLayer("InteractEnvironment"),
    ];

    // shockingly, this method is not that slow™!
    private static bool IsInvisibleWall(Collider col) {
        return 
            (m_killzEnabled.Value && col.CompareTag("Killz")) ||
            (
                col.enabled &&
                col.GetComponent<MeshRenderer>() is null or { enabled: false } &&
                m_whitelistedTags.All(tag => col.gameObject.tag != tag) &&
                m_whitelistedLayers.All(layer => col.gameObject.layer != layer) &&
                m_whitelistedComponents.All(component => !col.GetComponent(component)) &&
                !col.gameObject.name.ToLower().StartsWith("stairs") // stair colliders for smooth stepping
            );
    }
    
    private static void RemoveInvisibleWalls(Scene scene, LoadSceneMode mode) {
        if (!m_enabled.Value || scene.name == "MainMenu") return;
        var timeBefore = Time.realtimeSinceStartup;
        var colliders = scene.GetRootGameObjects().SelectMany(obj => obj.GetComponentsInChildren<Collider>().Where(IsInvisibleWall));
        
        int c = 0;
        foreach (var col in colliders) {
            col.enabled = false;
            ++c;
        }

        Logger.LogInfo($"Removed {c} invisible walls in {((Time.realtimeSinceStartup - timeBefore) * 1000):F2}ms");
    }
}