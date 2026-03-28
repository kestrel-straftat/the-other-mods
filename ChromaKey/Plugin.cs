using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ChatCommands;
using ComputerysModdingUtilities;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

[assembly: StraftatMod(isVanillaCompatible: true)]

namespace ChromaKey;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency("kestrel.straftat.chatcommands")]
public class Plugin : BaseUnityPlugin
{
    private struct MaterialSwapData
    {
        public Shader originalShader;
        public Color? originalColor;
    }
    
    public static Plugin Instance { get; private set; }
    
    internal static new ManualLogSource Logger;
    
    private static readonly int m_colorPropertyID = Shader.PropertyToID("_Color");
    
    public Shader keyingShader = Shader.Find("Unlit/Color");
    
    private Dictionary<Material, MaterialSwapData> m_swapDatas = [];
    private HashSet<Scene> m_seenScenes = [];

    private ConfigEntry<bool> m_enabled;
    private ConfigEntry<Color> m_keyColor;

    private ColorfulFog babbdiFog;
    private bool inBabbdi = false;
    
    public bool OverlayActive {
        get;
        set {
            field = value;
            foreach (var kv in m_swapDatas) {
                if (value) {
                    ApplyShaderSwapFor(kv.Key);
                }
                else {
                    RevertShaderSwapFor(kv.Key);
                }
            }

            UpdateBabbdiFog();
        }
    }
    
    public static readonly string loadBearingColonThree = ":3";
    private void Awake() {
        if (loadBearingColonThree != ":3") Application.Quit();
        gameObject.hideFlags = HideFlags.HideAndDontSave;
        Instance = this;
        Logger = base.Logger;
        CommandRegistry.RegisterCommandsFromAssembly();
        SceneManager.sceneLoaded += ShaderSwap;
        Config.SettingChanged += (_, _) => ApplyConfigs();
        m_enabled = Config.Bind("General", "Enabled", false, "Whether the key effect is enabled.");
        m_keyColor = Config.Bind("General", "Chroma Key Color", Color.green, "The color used for the key effect.");
        ApplyConfigs();
        
        new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();
        Logger.LogInfo("Hiiiiiiiiiiii :3");
    }

    public void ApplyShaderSwapFor(Material mat) {
        
        mat.shader = keyingShader;
        mat.color = m_keyColor.Value;
    }

    public void RevertShaderSwapFor(Material mat) {
        if (m_swapDatas.TryGetValue(mat, out var data)) {
            mat.shader = data.originalShader;
            if (data.originalColor is not null) {
                mat.color = data.originalColor.Value;
            }
        }
    }
    
    public void ApplyConfigs() {
        OverlayActive = m_enabled.Value;
        if (!OverlayActive) return;
        foreach (var mat in m_swapDatas.Keys) {
            mat.color = m_keyColor.Value;
        }
    }
    
    public void ShaderSwap(Scene scene, LoadSceneMode mode) {
        inBabbdi = scene.name == "Babbdi";
        
        if (!m_seenScenes.Add(scene)) {
            return;
        }
        
        var materials = scene.GetRootGameObjects()
            .SelectMany(obj => obj.GetComponentsInChildren<MeshRenderer>())
            .SelectMany(mr => mr.sharedMaterials)
            .Where(m => (bool)m && !m_swapDatas.ContainsKey(m)).Distinct();

        foreach (var mat in materials) {
            var data = new MaterialSwapData {
                originalShader = mat.shader,
                originalColor = mat.HasProperty(m_colorPropertyID) ? mat.color : null
            };
        
            m_swapDatas.Add(mat, data);
            ApplyShaderSwapFor(mat);
            if (!OverlayActive) {
                RevertShaderSwapFor(mat);
            }
        }
    }

    private void UpdateBabbdiFog() {
        if (babbdiFog is null) return;
        if (inBabbdi) {
            babbdiFog.enabled = !Instance.OverlayActive;
        }
        else {
            babbdiFog.enabled = false;
        }
    }

    [HarmonyPatch(typeof(FirstPersonController))]
    public static class FirstPersonControllerPatch
    {
        // i don't *want* to do this in update but IsOwner seems to misbehave in Start & Awake
        [HarmonyPatch("Update"), HarmonyPostfix]
        public static void GetFog(FirstPersonController __instance) {
            if ((bool)Instance.babbdiFog
                || !__instance.IsOwner
                || __instance.playerCamera.GetComponent<ColorfulFog>() is not { } fog) {
                return;
            }
            
            Instance.babbdiFog = fog;
            Instance.UpdateBabbdiFog();
        }
    }
}
