using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

[BepInPlugin("denyscrasav4ik.basicallyukrainian.chaos", "Chaos", "1.1.0")]
public class BasePlugin : BaseUnityPlugin
{
    public static BasePlugin Instance;

    public static int MaxMonitorWidth;
    public static int MaxMonitorHeight;
    public static int MaxMonitorFPS;

    public static ConfigEntry<int> MinWidth;
    public static ConfigEntry<int> MinHeight;
    public static ConfigEntry<int> MaxWidthOverride;
    public static ConfigEntry<int> MaxHeightOverride;

    public static ConfigEntry<int> MinFPS;
    public static ConfigEntry<int> MaxFPSOverride;

    public static ConfigEntry<int> FullscreenModeConfig;

    void Awake()
    {
        Instance = this;

        MaxMonitorWidth = Screen.currentResolution.width;
        MaxMonitorHeight = Screen.currentResolution.height;
        MaxMonitorFPS = Screen.currentResolution.refreshRate;
        if (MaxMonitorFPS <= 0) MaxMonitorFPS = 60;

        MinWidth = Config.Bind("Resolution", "Minimum Width", 10, "Minimum Random Width");
        MinHeight = Config.Bind("Resolution", "Minimum Height", 10, "Minimum Random Height");
        MaxWidthOverride = Config.Bind("Resolution", "Maximum Width", 0, "Maximum Random Width (0 = Monitor Width)");
        MaxHeightOverride = Config.Bind("Resolution", "Maximum Height", 0, "Maximum Random Height (0 = Monitor Height)");

        MinFPS = Config.Bind("FPS", "Minimum FPS", 5, "Minimum Random FPS");
        MaxFPSOverride = Config.Bind("FPS", "Maximum FPS", 0, "Maximum Random FPS (0 = Monitor FPS)");

        FullscreenModeConfig = Config.Bind("Resolution", "FullscreenMode",
            0,
            "Whether the game is fullscreen or not. (0 = Random, 1 = Force Windowed, 2 = Force Fullscreen)");

        Harmony harmony = new Harmony("denyscrasav4ik.basicallyukrainian.chaos");
        harmony.PatchAll();
    }

    public static int GetMaxWidth() => MaxWidthOverride.Value > 0 ? MaxWidthOverride.Value : MaxMonitorWidth;
    public static int GetMaxHeight() => MaxHeightOverride.Value > 0 ? MaxHeightOverride.Value : MaxMonitorHeight;
    public static int GetMaxFPS() => MaxFPSOverride.Value > 0 ? MaxFPSOverride.Value : MaxMonitorFPS;

    public static bool GetFullscreenValue()
    {
        switch (FullscreenModeConfig.Value)
        {
            case 1: return false;
            case 2: return true;
            default: return Random.value > 0.5f;
        }
    }

    public IEnumerator ChangeSampleRate(int rate)
    {
        AudioSource[] allSources = Object.FindObjectsOfType<AudioSource>();
        List<AudioSource> playingSources = new List<AudioSource>();

        foreach (AudioSource source in allSources)
            if (source.isPlaying) playingSources.Add(source);

        yield return new WaitForEndOfFrame();

        AudioConfiguration configuration = AudioSettings.GetConfiguration();
        configuration.sampleRate = rate;
        configuration.dspBufferSize = 256;
        AudioSettings.Reset(configuration);

        foreach (AudioSource source in playingSources)
            if (source != null) try { source.Play(); } catch { }
    }
}

public static class ChaosController
{
    private static float lastTriggerTime;
    private const float Cooldown = 0.25f;

    public static void Trigger()
    {
        if (Time.unscaledTime - lastTriggerTime < Cooldown) return;
        lastTriggerTime = Time.unscaledTime;

        int width = Random.Range(BasePlugin.MinWidth.Value, BasePlugin.GetMaxWidth() + 1);
        int height = Random.Range(BasePlugin.MinHeight.Value, BasePlugin.GetMaxHeight() + 1);
        bool fullscreen = BasePlugin.GetFullscreenValue();

        Screen.SetResolution(width, height, fullscreen);

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = Random.Range(BasePlugin.MinFPS.Value, BasePlugin.GetMaxFPS() + 1);

        int[] rateOptions = { 8000, 11025, 16000, 22050, 44100, 48000, 96000 };
        int targetRate = rateOptions[Random.Range(0, rateOptions.Length)];
        BasePlugin.Instance.StartCoroutine(BasePlugin.Instance.ChangeSampleRate(targetRate));
    }
}

[HarmonyPatch(typeof(CoreGameManager))]
public class CoreManagerPatches
{
    [HarmonyPatch("AddPoints", new System.Type[] { typeof(int), typeof(int), typeof(bool) })]
    [HarmonyPostfix]
    static void PostAddPoints() => ChaosController.Trigger();
}

[HarmonyPatch(typeof(BaseGameManager))]
public class BaseManagerPatches
{
    [HarmonyPatch("CollectNotebooks")]
    [HarmonyPostfix]
    static void PostCollect() => ChaosController.Trigger();

    [HarmonyPatch("AngerBaldi", new System.Type[] { typeof(float) })]
    [HarmonyPostfix]
    static void PostAnger() => ChaosController.Trigger();

    [HarmonyPatch("ActivityCompleted")]
    [HarmonyPostfix]
    static void PostActivity() => ChaosController.Trigger();
}

[HarmonyPatch(typeof(MainGameManager))]
public class MainManagerPatches
{
    [HarmonyPatch("BeginSpoopMode")]
    [HarmonyPostfix]
    static void PostSpoop() => ChaosController.Trigger();

    [HarmonyPatch("LoadNextLevel")]
    [HarmonyPrefix]
    static void PreLoad() => ChaosController.Trigger();
}

[HarmonyPatch(typeof(ElevatorManager))]
public class ElevatorManagerPatches
{
    [HarmonyPatch("PlayerBrokeElevator")]
    [HarmonyPostfix]
    static void PostBroke() => ChaosController.Trigger();
}

[HarmonyPatch(typeof(ItemManager))]
public class ItemManagerPatches
{
    [HarmonyPatch("AddItem", new System.Type[] { typeof(ItemObject) })]
    [HarmonyPostfix]
    static void PostAddItem() => ChaosController.Trigger();

    [HarmonyPatch("UseItem", new System.Type[] { })]
    [HarmonyPostfix]
    static void PostUseItem() => ChaosController.Trigger();
}

[HarmonyPatch(typeof(SodaMachine))]
public class SodaMachinePatches
{
    [HarmonyPatch("InsertItem", new System.Type[] { typeof(PlayerManager), typeof(EnvironmentController) })]
    [HarmonyPostfix]
    static void PostInsertItem() => ChaosController.Trigger();
}

[HarmonyPatch(typeof(WaterFountain), "Clicked")]
public class FountainPatch
{
    [HarmonyPostfix]
    static void PostClicked() => ChaosController.Trigger();
}

[HarmonyPatch(typeof(HudManager))]
public class HudManagerPatch
{
    private static TMP_Text fpsDisplay;
    private static TMP_Text resDisplay;
    private static TMP_Text audioDisplay;
    private static CanvasScaler scaler;

    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    static void PostfixAwake(HudManager __instance, GameObject[] ___notebookDisplay, CanvasScaler ___canvasScaler)
    {
        scaler = ___canvasScaler;

        Transform parent = ___notebookDisplay[0].transform.parent;

        fpsDisplay = CreateText(parent, "Max FPS: ", new Vector2(10, -50));
        resDisplay = CreateText(parent, "Screen Resolution: ", new Vector2(10, -80));
        audioDisplay = CreateText(parent, "Audio Bitrate: ", new Vector2(10, -110));
    }

    [HarmonyPatch("Update")]
    [HarmonyPostfix]
    static void PostfixUpdate()
    {
        if (fpsDisplay != null)
        {
            int fps = Application.targetFrameRate;
            fpsDisplay.text = $"Max FPS: {(fps > 0 ? fps.ToString() : "Unlimited")}";
            resDisplay.text = $"Screen Resolution: {Screen.width}x{Screen.height} ({(Screen.fullScreen ? "Fullscreen" : "Windowed")})";
            audioDisplay.text = $"Audio Bitrate: {AudioSettings.outputSampleRate}Hz";
        }
    }

    [HarmonyPatch("UpdateScaleFactor")]
    [HarmonyPrefix]
    static bool PrefixScaleFactor(HudManager __instance, CanvasScaler ___canvasScaler)
    {
        float currentResY = Screen.height;
        float currentResX = Screen.width;

        if (currentResX / currentResY >= 1.3333f)
        {
            ___canvasScaler.scaleFactor = Mathf.Max(1, Mathf.RoundToInt(currentResY / 360f));
        }
        else
        {
            ___canvasScaler.scaleFactor = Mathf.Max(1, Mathf.FloorToInt(currentResY / 480f));
        }

        return false;
    }

    private static TMP_Text CreateText(Transform parent, string label, Vector2 position)
    {
        GameObject go = new GameObject("ChaosText_" + label);
        go.transform.SetParent(parent, false);

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = 16;
        text.color = Color.black;
        text.alignment = TextAlignmentOptions.Left;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = position;

        return text;
    }
}
