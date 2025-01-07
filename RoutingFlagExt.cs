using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;



#if BEPINEX

using BepInEx;
using BepInEx.Configuration;

[BepInPlugin("com.github.Kaden5480.poy-better-routing-flag", "BetterRoutingFlag", PluginInfo.PLUGIN_VERSION)]
    public class RoutingFlagExt : BaseUnityPlugin {

    private ConfigFile configFile;
    private ConfigEntry<string> createFlagKeyConfig;
    private ConfigEntry<string> switchFlagKeyConfig;
    private ConfigEntry<string> sceneFlagsEntry;

    private void Awake()
    {
        RoutingFlagLogger.Log("Initializing BetterRoutingFlag for BepInEx...");
        instance = this;

        configFile = new ConfigFile(Path.Combine(Paths.ConfigPath, "BetterRoutingFlag.cfg"), true);

        createFlagKeyConfig = configFile.Bind("Keybinds", "CreateKey", "H", "Key to create a routing flag.");
        switchFlagKeyConfig = configFile.Bind("Keybinds", "SwitchKey", "J", "Key to switch routing flags.");

        createFlagKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), createFlagKeyConfig.Value);
        switchFlagKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), switchFlagKeyConfig.Value);

        Harmony.CreateAndPatchAll(typeof(SetRoutingFlagPositionPatch));
        Harmony.CreateAndPatchAll(typeof(UpdateRoutingFlagPatch));

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    public void OnApplicationQuit()
    {
        SaveFlags();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void OnDestroy()
    {
        SaveFlags();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void OnDisable()
    {
        SaveFlags();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void Update()
    {
        CommonUpdate();
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CommonSceneLoad(scene.name);
    }

    public void OnSceneUnloaded(Scene scene)
    {
        CommonSceneUnload(scene.name);
    }

#elif MELONLOADER

using MelonLoader;
using MelonLoader.Utils;

[assembly: MelonInfo(typeof(RoutingFlagExt), "Better Routing Flag", PluginInfo.PLUGIN_VERSION, "Kaden + Kalico")]
[assembly: MelonGame("TraipseWare", "Peaks of Yore")]

public class RoutingFlagExt : MelonMod
{
    private MelonPreferences_Category prefCategory;
    private MelonPreferences_Entry<string> sceneFlagsEntry;

    public override void OnInitializeMelon()
    {
        RoutingFlagLogger.Log("Initializing...");

        instance = this;

        prefCategory = MelonPreferences.CreateCategory("BetterRoutingFlag");
        prefCategory.SetFilePath(Path.Combine(MelonEnvironment.UserDataDirectory, "BetterRoutingFlag.cfg"));

        prefCategory.CreateEntry<string>("CreateKey", createFlagKey.ToString());
        prefCategory.CreateEntry<string>("SwitchKey", switchFlagKey.ToString());

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
        CommonSceneLoad(sceneName);
    }


    public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
    {
        CommonSceneUnload(sceneName);
    }

    public override void OnUpdate()
    {
        CommonUpdate();
    }

#endif

    public static RoutingFlagExt instance { get; private set; }

    public Dictionary<string, RoutingFlagPosition> flags = new Dictionary<string, RoutingFlagPosition>();
    private Transform routingFlagTransform;

    public string currentFlagKey = string.Empty;
    private int flagCounter = 0;

    private KeyCode createFlagKey = KeyCode.H;
    private KeyCode switchFlagKey = KeyCode.J;

    public void CommonSceneLoad(string sceneName)
    {
        RoutingFlagLogger.Log($"Scene Loaded: {sceneName}");

        flags.Clear();
        currentFlagKey = string.Empty;
        flagCounter = 0;

        string configKey = $"{sceneName}_Flags";

#if MELONLOADER

        if (!prefCategory.HasEntry(configKey))
        {
            sceneFlagsEntry = prefCategory.CreateEntry<string>(configKey, string.Empty);
        }

#elif BEPINEX

        string configGroup = "Scene.Flags";

        if (!configFile.ContainsKey(new ConfigDefinition(configGroup, configKey)))
        {
            sceneFlagsEntry = configFile.Bind(configGroup, configKey, string.Empty);
        }

#endif

        InitializeReferences();
        LoadFlags();
        AdjustFlagCounter();

        if (!string.IsNullOrEmpty(currentFlagKey) && flags.TryGetValue(currentFlagKey, out RoutingFlagPosition flag))
        {
            ApplyFlag(flag);
        }
    }

    public void CommonSceneUnload(string sceneName)
    {
        SaveFlags();
        RoutingFlagLogger.Log($"Scene Unloaded: {sceneName}, flags saved.");
    }

    private void CommonUpdate()
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
            RoutingFlagLogger.Log("RoutingFlag transform successfully found!");
        }
        else
        {
            RoutingFlagLogger.Warning("RoutingFlag instance or transform not found!");
        }
    }

    public void AddOrUpdateFlag(string key, RoutingFlagPosition flag)
    {
        if (flags.ContainsKey(key))
        {
            flags[key] = flag;
            RoutingFlagLogger.Log($"Updated existing flag: {key}");
        }
        else
        {
            flags.Add(key, flag);
            RoutingFlagLogger.Log($"Added new flag: {key}");
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
        RoutingFlagLogger.Log($"Flag counter adjusted to: {flagCounter}");
    }

    private void ApplyFlag(RoutingFlagPosition flag)
    {
        CameraLook cameraLook = GameObject.Find("CamY").GetComponent<CameraLook>();
        Transform playerCameraHolder = GameObject.Find("PlayerCameraHolder").transform;

        if (routingFlagTransform != null && cameraLook != null && playerCameraHolder != null)
        {
            routingFlagTransform.position = flag.Position;
            routingFlagTransform.rotation = Quaternion.Euler(flag.Rotation);

            RoutingFlagLogger.Log($"Applied flag: {currentFlagKey}");
        }
        else
        {
            RoutingFlagLogger.Warning("RoutingFlag transform is null while trying to load flag.");
        }
    }

    private void CreateFlag()
    {
        if (routingFlagTransform == null)
        {
            RoutingFlagLogger.Warning("Cannot create a flag, RoutingFlag transform is null.");
            return;
        }

        string key = GenerateNextFlagKey();

        CameraLook cameraLook = GameObject.Find("CamY").GetComponent<CameraLook>();
        Transform playerCameraHolder = GameObject.Find("PlayerCameraHolder").transform;

        if (cameraLook != null && playerCameraHolder != null)
        {
            flags[key] = new RoutingFlagPosition(
                routingFlagTransform.position,
                routingFlagTransform.eulerAngles,
                playerCameraHolder.rotation.y,
                playerCameraHolder.rotation.w,
                cameraLook.rotationY
            );
        }

        currentFlagKey = key;
        RoutingFlagLogger.Log($"Created new flag: {key}");
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

#if MELONLOADER

                RoutingFlagLogger.Log($"Loaded {flags.Count} flags from sceneFlagsEntry: {sceneFlagsEntry.DisplayName}");

#elif BEPINEX

                RoutingFlagLogger.Log($"Loaded {flags.Count} flags from sceneFlagsEntry: {sceneFlagsEntry.Definition.Key}");

#endif

            }
        }
        catch (System.Exception ex)
        {
            RoutingFlagLogger.Error($"Failed to load flags: {ex.Message}");
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

#if MELONLOADER

            prefCategory.SaveToFile();

            RoutingFlagLogger.Log($"Flags saved to sceneFlagsEntry: {sceneFlagsEntry.DisplayName}");

#elif BEPINEX

            configFile.Save();

            RoutingFlagLogger.Log($"Flags saved to sceneFlagsEntry: {sceneFlagsEntry.Definition.Key}");

#endif

        }
        catch (System.Exception ex)
        {
            RoutingFlagLogger.Error($"Failed to save flags: {ex.Message}");
        }
    }

    public void SwitchRoutingFlag()
    {
        if (flags.Count == 0)
        {
            RoutingFlagLogger.Warning("No routing flags to switch.");
            return;
        }

        List<string> keys = new List<string>(flags.Keys);
        int currentIndex = keys.IndexOf(currentFlagKey);
        currentIndex = (currentIndex + 1) % keys.Count;

        currentFlagKey = keys[currentIndex];
        ApplyFlag(flags[currentFlagKey]);
        RoutingFlagLogger.Log($"Switched to flag: {currentFlagKey}");
    }
}