using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

[BepInPlugin("denyscrasav4ik.basicallyukrainian.chaos", "Chaos", "1.0.0")]
public class BasePlugin : BaseUnityPlugin
{
    public static BasePlugin Instance;

    public static int MaxMonitorWidth;
    public static int MaxMonitorHeight;
    public static int MaxMonitorFPS;

    void Awake()
    {
        Instance = this;

        MaxMonitorWidth = Screen.currentResolution.width;
        MaxMonitorHeight = Screen.currentResolution.height;
        MaxMonitorFPS = Screen.currentResolution.refreshRate;
        if (MaxMonitorFPS <= 0) MaxMonitorFPS = 60;

        Harmony harmony = new Harmony("denyscrasav4ik.basicallyukrainian.chaos");
        harmony.PatchAll();

    }

    public IEnumerator ChangeSampleRate(int rate)
    {
        AudioSource[] allSources = Object.FindObjectsOfType<AudioSource>();
        List<AudioSource> playingSources = new List<AudioSource>();
        foreach (AudioSource source in allSources)
        {
            if (source.isPlaying) { playingSources.Add(source); }
        }

        yield return new WaitForEndOfFrame();

        AudioConfiguration configuration = AudioSettings.GetConfiguration();
        configuration.sampleRate = rate;
        configuration.dspBufferSize = 256;
        AudioSettings.Reset(configuration);

        foreach (AudioSource source in playingSources)
        {
            if (source != null) { try { source.Play(); } catch { } }
        }
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

        int width = Random.Range(10, BasePlugin.MaxMonitorWidth + 1);
        int height = Random.Range(10, BasePlugin.MaxMonitorHeight + 1);
        Screen.SetResolution(width, height, false);

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = Random.Range(5, BasePlugin.MaxMonitorFPS + 1);

        int[] rateOptions = { 8000, 11025, 16000, 22050, 44100, 48000, 96000 };
        int targetRate = rateOptions[Random.Range(0, rateOptions.Length + 1)];
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

    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    static void PostfixAwake(HudManager __instance, GameObject[] ___notebookDisplay)
    {
        Transform parent = ___notebookDisplay[0].transform.parent;

        fpsDisplay = CreateText(parent, "Max FPS: ", new Vector2(10, -60));
        resDisplay = CreateText(parent, "Screen Resolution: ", new Vector2(10, -100));
        audioDisplay = CreateText(parent, "Audio Bitrate: ", new Vector2(10, -140));
    }

    [HarmonyPatch("Update")]
    [HarmonyPostfix]
    static void PostfixUpdate()
    {
        if (fpsDisplay != null)
        {
            int fps = Application.targetFrameRate;
            fpsDisplay.text = $"Max FPS: {(fps > 0 ? fps.ToString() : "Unlimited")}";
            resDisplay.text = $"Screen Resolution: {Screen.width}x{Screen.height}";
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
