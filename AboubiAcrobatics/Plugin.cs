using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using ComputerysModdingUtilities;
using HarmonyLib;
using UnityEngine;

[assembly: StraftatMod(isVanillaCompatible: false)]

namespace AboubiAcrobatics;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private static ConfigEntry<bool> m_enabled;
    private static ConfigEntry<bool> m_enableAutocorrects;
    private static ConfigEntry<float> m_autocorrectDuration;
    private static ConfigEntry<float> m_fullAutocorrectThreshold;
    private static ConfigEntry<bool> m_performFullAutocorrects;
    
    private static float m_autocorrectTimer;
    private static float m_initialRotation;
    private static float m_targetRotation;
    private static float m_inAirTime;
    
    public static readonly string loadBearingColonThree = ":3";
    private void Awake() {
        if (loadBearingColonThree != ":3") Application.Quit();
        gameObject.hideFlags = HideFlags.HideAndDontSave;

        m_enabled = Config.Bind(
            "General",
            "Enabled",
            true,
            "Whether or not the plugin is enabled."
        );
        m_enableAutocorrects = Config.Bind(
            "General",
            "Enable Autocorrects",
            true,
            "Whether or not to automatically correct your rotation when landing upside down."
        );
        m_autocorrectDuration = Config.Bind(
            "General",
            "Autocorrect duration",
            0.15f,
            "The duration of the automatic flip back to the normal rotation limit upon hitting the ground, in seconds."
        );
        m_performFullAutocorrects = Config.Bind(
            "General",
            "Perform full autocorrects",
            true,
            "Whether you should be corrected back to looking straight forwards instead of up or down after a given amount of time in the air."
        );
        m_fullAutocorrectThreshold = Config.Bind(
            "General",
            "Full autocorrect threshold",
            0.5f,
            "The amount of time spent in the air after which full autocorrects will be performed, in seconds."
        );

        m_autocorrectDuration.SettingChanged += (_, _) => InitAutocorrectTimer();

        new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();
        Logger.LogInfo("Hiiiiiiiiiiii :3");
    }

    private static void InitAutocorrectTimer() {
        m_autocorrectTimer = m_autocorrectDuration.Value;
    }

    private static float ClampInjected(float value, float min, float max) {
        if (!m_enabled.Value) {
            return Mathf.Clamp(value, min, max);
        }
        
        // make value (-180, 180]
        value %= 360;
        value = (value + 360) % 360;
        if (value > 180) value -= 360;

        if (!m_enableAutocorrects.Value) {
            return value;
        }
        
        // yeah firstpersoncontroller has an isGrounded field but. i do not trust 
        // sirius enough to assume that it's set consistently
        var grounded = Settings.Instance.localPlayer?.characterController.isGrounded ?? true;

        if (m_autocorrectTimer < m_autocorrectDuration.Value) {
            m_autocorrectTimer += Time.deltaTime;
            value = Mathf.SmoothStep(m_initialRotation, m_targetRotation, m_autocorrectTimer / m_autocorrectDuration.Value);
        }

        if (grounded && m_autocorrectTimer >= m_autocorrectDuration.Value && (value < min || value > max)) {
            if (m_inAirTime > 0f) {
                // hack~ account for autocorrectDuration possibly being 0
                m_autocorrectTimer = -float.Epsilon;
                m_initialRotation = value;
                // if we were in the air for longer than the threshold autocorrect to 0
                // otherwise autocorrect to the normal limits
                m_targetRotation = m_inAirTime >= m_fullAutocorrectThreshold.Value && m_performFullAutocorrects.Value ? 0f : Mathf.Clamp(value, min, max);
            }
            else {
                value = Mathf.Clamp(value, min, max);
            }
        }

        if (!grounded) m_inAirTime += Time.deltaTime;
        else m_inAirTime = 0f;

        return value;
    }

    [HarmonyPatch(typeof(FirstPersonController))]
    private static class FirstPersonControllerPatch
    {
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        private static void Awake() => InitAutocorrectTimer();

        [HarmonyPatch("HandleMouseLook")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> ReplaceClamp(IEnumerable<CodeInstruction> instructions) {
            return new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Mathf), "Clamp", [typeof(float), typeof(float), typeof(float)])))
                .Set(OpCodes.Call, AccessTools.Method(typeof(Plugin), "ClampInjected"))
                .InstructionEnumeration();
        }
    }
}