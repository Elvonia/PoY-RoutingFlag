using HarmonyLib;
using MelonLoader;
using MelonLoader.Utils;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[assembly: MelonInfo(typeof(RoutingFlagExt), "Routing Flag Extended", PluginInfo.PLUGIN_VERSION, "Kalico")]
[assembly: MelonGame("TraipseWare", "Peaks of Yore")]

public class RoutingFlagExt : MelonMod
{
    public static RoutingFlagExt instance { get; private set; }

    private MelonPreferences_Category prefCategory;
    private MelonPreferences_Entry<string> sceneFlagsEntry;

    public Dictionary<string, RoutingFlagPosition> flags = new Dictionary<string, RoutingFlagPosition>();
    private Transform routingFlagTransform;

    public string currentFlagKey = string.Empty;
    private int flagCounter = 0;

    private KeyCode createFlagKey = KeyCode.H;
    private KeyCode switchFlagKey = KeyCode.J;

    public override void OnInitializeMelon()
    {
        MelonLogger.Msg("Initializing...");

        instance = this;

        prefCategory = MelonPreferences.CreateCategory("RoutingFlagExtended");
        prefCategory.SetFilePath(Path.Combine(MelonEnvironment.UserDataDirectory, "RoutingFlagExt.cfg"));

        prefCategory.CreateEntry<string>("CreateKey", KeyCode.H.ToString());
        prefCategory.CreateEntry<string>("SwitchKey", KeyCode.J.ToString());

        if (prefCategory.HasEntry("CreateKey"))
        {
            createFlagKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), prefCategory.GetEntry<string>("CreateKey").Value);
        }

        if (prefCategory.HasEntry("SwitchKey"))
        {
            switchFlagKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), prefCategory.GetEntry<string>("SwitchKey").Value);
        }
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        MelonLogger.Msg($"Scene Loaded: {sceneName}");

        flags.Clear();
        currentFlagKey = string.Empty;
        flagCounter = 0;

        if (!prefCategory.HasEntry($"{sceneName}_Flags"))
        {
            sceneFlagsEntry = prefCategory.CreateEntry<string>($"{sceneName}_Flags", string.Empty);
        }

        InitializeReferences();
        LoadFlags();
        AdjustFlagCounter();

        if (!string.IsNullOrEmpty(currentFlagKey) && flags.TryGetValue(currentFlagKey, out RoutingFlagPosition flag))
        {
            ApplyFlag(flag);
        }
    }


    public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
    {
        SaveFlags();
        MelonLogger.Msg($"Scene Unloaded: {sceneName}, flags saved.");
    }

    public override void OnUpdate()
    {
        if (Input.GetKeyDown(createFlagKey))
        {
            CreateFlag();
        }

        if (Input.GetKeyDown(switchFlagKey))
        {
            SwitchRoutingFlag();
        }
    }

    private void InitializeReferences()
    {
        RoutingFlag routingFlagInstance = GameObject.FindObjectOfType<RoutingFlag>();
        if (routingFlagInstance != null && routingFlagInstance.routingFlagTransform != null)
        {
            routingFlagTransform = routingFlagInstance.routingFlagTransform;
            MelonLogger.Msg("RoutingFlag transform successfully found!");
        }
        else
        {
            MelonLogger.Warning("RoutingFlag instance or transform not found!");
        }
    }

    public void AddOrUpdateFlag(string key, RoutingFlagPosition flag)
    {
        if (flags.ContainsKey(key))
        {
            flags[key] = flag;
            MelonLogger.Msg($"Updated existing flag: {key}");
        }
        else
        {
            flags.Add(key, flag);
            MelonLogger.Msg($"Added new flag: {key}");
        }

        currentFlagKey = key;
    }

    private void AdjustFlagCounter()
    {
        int highestFlagNumber = 0;

        foreach (var key in flags.Keys)
        {
            if (key.StartsWith("Flag_"))
            {
                string numberPart = key.Substring(5);
                if (int.TryParse(numberPart, out int flagNumber))
                {
                    highestFlagNumber = Mathf.Max(highestFlagNumber, flagNumber);
                }
            }
        }

        flagCounter = highestFlagNumber;
        MelonLogger.Msg($"Flag counter adjusted to: {flagCounter}");
    }

    private void ApplyFlag(RoutingFlagPosition flag)
    {
        if (routingFlagTransform != null)
        {
            routingFlagTransform.position = flag.Position;
            routingFlagTransform.rotation = Quaternion.Euler(flag.Rotation);
            MelonLogger.Msg($"Applied flag: {currentFlagKey}");
        }
        else
        {
            MelonLogger.Warning("RoutingFlag transform is null while trying to load flag.");
        }
    }

    private void CreateFlag()
    {
        if (routingFlagTransform == null)
        {
            MelonLogger.Warning("Cannot create a flag, RoutingFlag transform is null.");
            return;
        }

        string key = GenerateNextFlagKey();

        flags[key] = new RoutingFlagPosition(
            routingFlagTransform.position,
            routingFlagTransform.eulerAngles
        );

        currentFlagKey = key;
        MelonLogger.Msg($"Created new flag: {key}");
    }


    public string GenerateNextFlagKey()
    {
        flagCounter++;
        return $"Flag_{flagCounter}";
    }

    private void LoadFlags()
    {
        try
        {
            if (!string.IsNullOrEmpty(sceneFlagsEntry.Value))
            {
                RoutingFlagData flagData = JsonUtility.FromJson<RoutingFlagData>(sceneFlagsEntry.Value);
                flags = flagData.ToDictionary();
                flagCounter = flags.Count;

                MelonLogger.Msg($"Loaded {flags.Count} flags from sceneFlagsEntry: {sceneFlagsEntry.DisplayName}");
            }
        }
        catch (System.Exception ex)
        {
            MelonLogger.Error($"Failed to load flags: {ex.Message}");
        }
    }

    private void SaveFlags()
    {
        try
        {
            RoutingFlagData flagData = new RoutingFlagData();
            flagData.FromDictionary(flags);

            string json = JsonUtility.ToJson(flagData);
            sceneFlagsEntry.Value = json;
            prefCategory.SaveToFile();

            MelonLogger.Msg($"Flags saved to sceneFlagsEntry: {sceneFlagsEntry.DisplayName}");
        }
        catch (System.Exception ex)
        {
            MelonLogger.Error($"Failed to save flags: {ex.Message}");
        }
    }

    public void SwitchRoutingFlag()
    {
        if (flags.Count == 0)
        {
            MelonLogger.Warning("No routing flags to switch.");
            return;
        }

        List<string> keys = new List<string>(flags.Keys);
        int currentIndex = keys.IndexOf(currentFlagKey);
        currentIndex = (currentIndex + 1) % keys.Count;

        currentFlagKey = keys[currentIndex];
        ApplyFlag(flags[currentFlagKey]);
        MelonLogger.Msg($"Switched to flag: {currentFlagKey}");
    }
}