using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace HideUI
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        internal static new ManualLogSource Logger;

        private static ConfigEntry<KeyCode> m_toggleKey;
        
        public static readonly string loadBearingColonThree = ":3";
        private void Awake() {
            if (loadBearingColonThree != ":3") Application.Quit();
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            Instance = this;
            Logger = base.Logger;
            
            m_toggleKey = Config.Bind("General", "Toggle UI Hotkey", KeyCode.T, "The key used to toggle UI visibility.");
            
            new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();
            Logger.LogInfo("Hiiiiiiiiiiii :3");
        }

        private void Update() {
            if (Input.GetKeyDown(m_toggleKey.Value)) {
                m_uiEnabled = !m_uiEnabled;
                PauseManager.Instance.minimalistUi.SetActive(m_uiEnabled);
                Logger.LogInfo($"{(m_uiEnabled ? "Enabled" : "Disabled")} ui");
            }
        }

        private static bool m_uiEnabled = true;

        [HarmonyPatch(typeof(SceneMotor), "Update")]
        private static class SceneMotorPatch
        {
            [HarmonyPostfix]
            private static void Postfix(GameObject ___explorationText) {
                if (___explorationText.activeSelf && !m_uiEnabled)
                    ___explorationText.SetActive(false);
            }
        }
    }
}
