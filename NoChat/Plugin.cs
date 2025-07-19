using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using DG.Tweening;
using Goodgulf.Graphics;
using HarmonyLib;
using HeathenEngineering.DEMO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ComputerysModdingUtilities;

[assembly: StraftatMod(isVanillaCompatible: true)]

namespace NoChat;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance { get; private set; }
    internal static new ManualLogSource Logger;

    private static ConfigEntry<bool> m_showChatWhenPaused;
    private static bool m_shouldDisableChat = true;
    private static GameObject m_chatPanel;
    private static TMP_InputField m_inputField;
    
    public static readonly string loadBearingColonThree = ":3";
    private void Awake() {
        if (loadBearingColonThree != ":3") Application.Quit();
        gameObject.hideFlags = HideFlags.HideAndDontSave;
        Instance = this;
        Logger = base.Logger;
        m_showChatWhenPaused = Config.Bind("General", "Show chat when paused", true, "Should chat be shown again when the game is paused?");
        
        new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();
        Logger.LogInfo("Hiiiiiiiiiiii :3");
    } 

    [HarmonyPatch(typeof(LobbyChatUILogic))]
    internal static class LobbyChatUILogicPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        public static void DisableChat(GameObject ___chatPanel, TMP_InputField ___inputField) {
            m_chatPanel = ___chatPanel;
            m_inputField = ___inputField;
            if (m_shouldDisableChat) ___chatPanel.transform.localScale = Vector3.zero;
        }
        
        // still want to handle messages if we're showing chat when the game is paused
        [HarmonyPatch("HandleChatMessage")]
        [HarmonyPrefix]
        public static bool DisableMessageHandling() => m_showChatWhenPaused.Value;
    }

    [HarmonyPatch(typeof(MatchChat))]
    internal static class MatchChatPatch
    {
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static bool DisableOpeningChat() => !m_shouldDisableChat;
    }

    [HarmonyPatch(typeof(MatchChatLine))]
    internal static class MatchChatLinePatch
    {
        // this could very easily be done with a transpiler but    you know
        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        public static bool DisableChatMessageSound(MatchChatLine __instance, ref float ___ialpha, ref bool ___startCounting, RawImage ___matchImg) {
            ___ialpha = ___matchImg.color.a;
            if (__instance.deleteMe) ___startCounting = true;
            if (__instance.chatMsg && !m_shouldDisableChat) SoundManager.Instance.PlaySound(PauseManager.Instance.matchChatClip);
            
            __instance.transform.DOPunchScale(new Vector3(__instance.tweenSize,__instance.tweenSize,__instance.tweenSize), 0.3f).SetEase(__instance.easeType);
            
            return false;
        }
    }
    
    [HarmonyPatch(typeof(PauseManager))]
    internal static class PauseManagerPatch
    {
        private static bool m_pausedLast;
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static void CheckPaused(GameObject ___pauseMenu) {
            var pausedCurrent = ___pauseMenu.activeSelf;
            
            if (pausedCurrent != m_pausedLast && m_showChatWhenPaused.Value) {
                m_shouldDisableChat = !pausedCurrent;
                m_chatPanel.transform.localScale = pausedCurrent ? Vector3.one : Vector3.zero;
                // close chat box manually since at this point we're already ignoring any inputs in MatchChat
                m_inputField.gameObject.SetActive(false);
            }
            m_pausedLast = pausedCurrent;
        }
    }
}
