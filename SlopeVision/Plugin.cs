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

namespace SlopeVision;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency("kestrel.straftat.chatcommands")]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance { get; private set; }
    
    internal static new ManualLogSource Logger;
    
    private static string pluginPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    private static readonly int m_showWalkableID = Shader.PropertyToID("_ShowWalkable");
    private static readonly int m_showStandableID = Shader.PropertyToID("_ShowStandable");
    private static readonly int m_showVaultableID = Shader.PropertyToID("_ShowVaultable");
    private static readonly int m_showCustomID = Shader.PropertyToID("_ShowCustom");
    private static readonly int m_standableThresholdID = Shader.PropertyToID("_StandableThreshold");
    private static readonly int m_customMinID = Shader.PropertyToID("_CustomMin");
    private static readonly int m_customMaxID = Shader.PropertyToID("_CustomMax");
    private static readonly int m_walkableColorID = Shader.PropertyToID("_WalkableColor");
    private static readonly int m_standableColorID = Shader.PropertyToID("_StandableColor");
    private static readonly int m_vaultableColorID = Shader.PropertyToID("_VaultableColor");
    private static readonly int m_customColorID = Shader.PropertyToID("_CustomColor");
    private static readonly int m_mainTexID = Shader.PropertyToID("_MainTex");
    private static readonly int m_bcID = Shader.PropertyToID("_BC");
    private static readonly int m_textureSample0ID = Shader.PropertyToID("_TextureSample0");
    private static readonly int m_topTexID = Shader.PropertyToID("_TopTex");
    private static readonly int m_sideTexID = Shader.PropertyToID("_SideTex");
    private static readonly int m_bottomTexID = Shader.PropertyToID("_BottomTex");
    
    public Shader gradientShader;
    
    private Dictionary<Material, Shader> m_originalShaders = [];
    private HashSet<Scene> m_seenScenes = [];
    
    private ConfigEntry<bool> m_showWalkable;
    private ConfigEntry<bool> m_showStandable;
    private ConfigEntry<bool> m_showVaultable;
    private ConfigEntry<bool> m_showCustom;
    
    private ConfigEntry<float> m_standableThreshold;
    private ConfigEntry<float> m_customMin;
    private ConfigEntry<float> m_customMax;

    private ConfigEntry<Color> m_walkableColor;
    private ConfigEntry<Color> m_standableColor;
    private ConfigEntry<Color> m_vaultableColor;
    private ConfigEntry<Color> m_customColor;

    private ColorfulFog babbdiFog;
    private bool inBabbdi = false;
    
    public bool OverlayActive {
        get;
        set {
            field = value;
            foreach (var kv in m_originalShaders) {
                kv.Key.shader = value ? gradientShader : kv.Value;
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
        InitConfigs();
        gradientShader = AssetBundle.LoadFromFile(Path.Combine(pluginPath, "gradient")).LoadAsset<Shader>("Gradient");
        
        new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();
        Logger.LogInfo("Hiiiiiiiiiiii :3");
    }
    
    public void InitConfigs() {
        var gradientRange = new AcceptableValueRange<float>(0f, 180f);
        
        m_showWalkable = Config.Bind("Overlays", "Show Walkable Overlay", true, "Whether to show the walkable surfaces overlay.");
        m_walkableColor = Config.Bind("Overlays", "Walkable Overlay Color", Color.green, "The color of the walkable surfaces overlay.");
        
        m_showStandable = Config.Bind("Overlays", "Show Standable Overlay", true, "Whether to show the standable surfaces overlay.");
        m_standableColor = Config.Bind("Overlays", "Standable Overlay Color", new Color(1.0f, 0.5f, 0.0f), "The color of the standable surfaces overlay.");
        m_standableThreshold = Config.Bind("Overlays", "Standable Overlay Max", 89f, 
            new ConfigDescription("The maximum slope angle for the standable surfaces overlay (exclusive, you may want to change this if you find yourself slipping from surfaces considered standable).", gradientRange)
        );
        
        m_showVaultable = Config.Bind("Overlays", "Show Vaultable Overlay", false, "Whether to show the vaultable surfaces overlay.");
        m_vaultableColor = Config.Bind("Overlays", "Vaultable Overlay Color", Color.magenta, "The color of the vaultable surfaces overlay.");

        m_showCustom = Config.Bind("Overlays", "Show Custom Overlay", false, "Whether to show the custom overlay.");
        m_customColor = Config.Bind("Overlays", "Custom Overlay Color", Color.red, "The color of the custom overlay.");
        m_customMin = Config.Bind("Overlays", "Custom Overlay Min Gradient", 45f, new ConfigDescription("The minimum slope angle for the custom overlay (exclusive).", gradientRange));
        m_customMax = Config.Bind("Overlays", "Custom Overlay Max Gradient", 75f, new ConfigDescription("The maximum slope angle for the custom overlay (exclusive).", gradientRange));
        
        ApplyConfigs();
    }
    
    public void ApplyConfigsTo(Material mat) {
        mat.SetInteger(m_showWalkableID, m_showWalkable.Value ? 1 : 0);
        mat.SetInteger(m_showStandableID, m_showStandable.Value ? 1 : 0);
        mat.SetInteger(m_showVaultableID, m_showVaultable.Value ? 1 : 0);
        mat.SetInteger(m_showCustomID, m_showCustom.Value ? 1 : 0);
        mat.SetFloat(m_standableThresholdID, m_standableThreshold.Value);
        mat.SetFloat(m_customMinID, m_customMin.Value);
        mat.SetFloat(m_customMaxID, m_customMax.Value);
        mat.SetColor(m_walkableColorID, m_walkableColor.Value);
        mat.SetColor(m_standableColorID, m_standableColor.Value);
        mat.SetColor(m_vaultableColorID, m_vaultableColor.Value);
        mat.SetColor(m_customColorID, m_customColor.Value);
    }
    
    public void ApplyConfigs() {
        foreach (var material in m_originalShaders.Keys) {
            ApplyConfigsTo(material);
        }
    }
    
    public void ShaderSwap(Scene scene, LoadSceneMode mode) {
        inBabbdi = scene.name == "Babbdi";
        
        if (!m_seenScenes.Add(scene)) {
            return;
        }

        var validRenderers = scene.GetRootGameObjects()
            .SelectMany(obj => obj.GetComponentsInChildren<MeshRenderer>())
            .Where(mr => mr?.gameObject.GetComponent<MeshCollider>() is { enabled: true, isTrigger: false });

        foreach (var mat in validRenderers.SelectMany(mr => mr.sharedMaterials).Where(m => (bool)m && !m_originalShaders.ContainsKey(m)).Distinct()) {
            if (!TryGetMainTexture(mat, out var mainTexture)) {
                continue;
            }

            m_originalShaders.Add(mat, mat.shader);
            var originalShader = mat.shader;
            mat.shader = gradientShader;
            mat.mainTexture = mainTexture;
            ApplyConfigsTo(mat);

            if (!OverlayActive) {
                mat.shader = originalShader;
            }
        }
    }

    private void UpdateBabbdiFog() {
        if (inBabbdi) {
            babbdiFog.enabled = !Instance.OverlayActive;
        }
        else {
            babbdiFog.enabled = false;
        }
    }
    
    private static bool TryGetMainTexture(Material mat, out Texture texture) {
        if (mat.HasProperty(m_mainTexID)) {
            texture = mat.GetTexture(m_mainTexID);
        }
        else if (mat.HasProperty(m_bcID)) {
            texture = mat.GetTexture(m_bcID);
        }
        else if (mat.HasProperty(m_textureSample0ID)) {
            texture = mat.GetTexture(m_textureSample0ID);
        }
        else if (mat.HasProperty(m_topTexID)) {
            texture = mat.GetTexture(m_topTexID);
        }
        else if (mat.HasProperty(m_sideTexID)) {
            texture = mat.GetTexture(m_sideTexID);
        }
        else if (mat.HasProperty(m_bottomTexID)) {
            texture = mat.GetTexture(m_bottomTexID);
        }
        else {
            texture = null;
            return false;
        }

        return true;
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
