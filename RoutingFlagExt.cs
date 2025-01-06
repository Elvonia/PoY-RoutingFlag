using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if BEPINEX

using BepInEx;

[BepInPlugin("com.github.Kaden5480.poy-better-routing-flag", "BetterRoutingFlag", PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin {
        public void Awake() {
            foreach (string sceneName in validScenes) {
                Rotation rotation = new Rotation(
                    Config.Bind(sceneName, "rotY", 0f),
                    Config.Bind(sceneName, "rotW", 0f),
                    Config.Bind(sceneName, "rotationY", 0f)
                );

                rotations.Add(sceneName, rotation);
            }

            Harmony.CreateAndPatchAll(typeof(Plugin.PatchRoutingFlagRestore));
            Harmony.CreateAndPatchAll(typeof(Plugin.PatchRoutingFlagSave));

            // Remove falling rocks
            Harmony.CreateAndPatchAll(typeof(Patches.PatchFallingRock));
            Harmony.CreateAndPatchAll(typeof(Patches.PatchIceFall));
        }

#elif MELONLOADER

using MelonLoader;
using MelonLoader.Utils;

[assembly: MelonInfo(typeof(RoutingFlagExt), "Better Routing Flag", PluginInfo.PLUGIN_VERSION, "Kaden + Kalico")]
[assembly: MelonGame("TraipseWare", "Peaks of Yore")]

public class RoutingFlagExt : MelonMod
{
    public override void OnInitializeMelon()
    {
        Logger.Log("Initializing...");

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
        Logger.Log($"Scene Loaded: {sceneName}");

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
        Logger.Log($"Scene Unloaded: {sceneName}, flags saved.");
    }

    public override void OnUpdate()
    {
        //CommonUpdate();
        if (Input.GetKeyDown(createFlagKey))
        {
            CreateFlag();
        }

        if (Input.GetKeyDown(switchFlagKey))
        {
            SwitchRoutingFlag();
        }
    }

#endif

    public static RoutingFlagExt instance { get; private set; }

    private MelonPreferences_Category prefCategory;
    private MelonPreferences_Entry<string> sceneFlagsEntry;

    public Dictionary<string, RoutingFlagPosition> flags = new Dictionary<string, RoutingFlagPosition>();
    private Transform routingFlagTransform;

    public string currentFlagKey = string.Empty;
    private int flagCounter = 0;

    private KeyCode createFlagKey = KeyCode.H;
    private KeyCode switchFlagKey = KeyCode.J;

    private string[] validScenes = new string[] {
            "Peak_1_GreenhornNEW", "Peak_2_PaltryNEW", "Peak_3_OldMill", "Peak_3_GrayGullyNEW",
            "Peak_LighthouseNew", "Peak_4_OldManOfSjorNEW", "Peak_5_GiantsShelfNEW", "Peak_8_EvergreensEndNEW",
            "Peak_9_TheTwinsNEW", "Peak_6_OldGroveSkelf", "Peak_7_HangmansLeapNEW", "Peak_13_LandsEndNEW",
            "Peak_19_OldLangr", "Peak_14_Cavern", "Peak_16_ThreeSeaStacks", "Peak_10_WaltersCragNEW",
            "Peak_15_TheGreatCrevice", "Peak_17_RainyPeak", "Peak_18_FallingBoulders", "Peak_11_WutheringCrestNEW",

            "Boulder_1_OldWalkersBoulder", "Boulder_2_JotunnsThumb", "Boulder_3_OldSkerry", "Boulder_4_TheHamarrStone",
            "Boulder_5_GiantsNose", "Boulder_6_WaltersBoulder", "Boulder_7_SunderedSons", "Boulder_8_OldWealdsBoulder",
            "Boulder_9_LeaningSpire", "Boulder_10_Cromlech",

            "Tind_1_WalkersPillar", "Tind_3_GreatGaol", "Tind_2_Eldenhorn",
            "Tind_4_StHaelga", "Tind_5_YmirsShadow",

            "Category4_1_FrozenWaterfall", "Category4_2_SolemnTempest",

            "Alps_1_TrainingTower", "Alps_2_BalancingBoulder", "Alps_3_SeaArch",
            "Alps_4_SunfullSpire", "Alps_5_Tree", "Alps_6_Treppenwald",
            "Alps_7_Castle", "Alps_8_SeaSideTraining", "Alps_9_IvoryGranites",
            "Alps_10_Rekkja", "Alps_11_Quietude", "Alps_12_Overlook",

            "Alps2_1_Waterfall", "Alps2_2_Dam",
            "Alps2_3_Dunderhorn", "Alps2_4_ElfenbenSpires",
            "Alps2_5_WelkinPass",

            "Alps3_1_SeigrCraeg", "Alps3_2_UllrsGate",
            "Alps3_3_GreatSilf", "Alps3_4_ToweringVisir",
            "Alps3_5_EldrisWall", "Alps3_6_MountMhorgorm",
        };

    private void InitializeReferences()
    {
        RoutingFlag routingFlagInstance = GameObject.FindObjectOfType<RoutingFlag>();
        if (routingFlagInstance != null && routingFlagInstance.routingFlagTransform != null)
        {
            routingFlagTransform = routingFlagInstance.routingFlagTransform;
            Logger.Log("RoutingFlag transform successfully found!");
        }
        else
        {
            Logger.Warning("RoutingFlag instance or transform not found!");
        }
    }

    public void AddOrUpdateFlag(string key, RoutingFlagPosition flag)
    {
        if (flags.ContainsKey(key))
        {
            flags[key] = flag;
            Logger.Log($"Updated existing flag: {key}");
        }
        else
        {
            flags.Add(key, flag);
            Logger.Log($"Added new flag: {key}");
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
        Logger.Log($"Flag counter adjusted to: {flagCounter}");
    }

    private void ApplyFlag(RoutingFlagPosition flag)
    {
        CameraLook cameraLook = GameObject.Find("CamY").GetComponent<CameraLook>();
        Transform playerCameraHolder = GameObject.Find("PlayerCameraHolder").transform;

        if (routingFlagTransform != null && cameraLook != null && playerCameraHolder != null)
        {
            routingFlagTransform.position = flag.Position;
            routingFlagTransform.rotation = Quaternion.Euler(flag.Rotation);

            //playerCameraHolder.rotation = new Quaternion(0f, flag.CameraHolderRotationY, 0f, flag.CameraHolderRotationW);
            //cameraLook.rotationY = flag.CameraLookRotationY;

            Logger.Log($"Applied flag: {currentFlagKey}");
        }
        else
        {
            Logger.Warning("RoutingFlag transform is null while trying to load flag.");
        }
    }

    private void CreateFlag()
    {
        if (routingFlagTransform == null)
        {
            Logger.Warning("Cannot create a flag, RoutingFlag transform is null.");
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
        Logger.Log($"Created new flag: {key}");
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

                Logger.Log($"Loaded {flags.Count} flags from sceneFlagsEntry: {sceneFlagsEntry.DisplayName}");
            }
        }
        catch (System.Exception ex)
        {
            Logger.Error($"Failed to load flags: {ex.Message}");
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

            Logger.Log($"Flags saved to sceneFlagsEntry: {sceneFlagsEntry.DisplayName}");
        }
        catch (System.Exception ex)
        {
            Logger.Error($"Failed to save flags: {ex.Message}");
        }
    }

    public void SwitchRoutingFlag()
    {
        if (flags.Count == 0)
        {
            Logger.Warning("No routing flags to switch.");
            return;
        }

        List<string> keys = new List<string>(flags.Keys);
        int currentIndex = keys.IndexOf(currentFlagKey);
        currentIndex = (currentIndex + 1) % keys.Count;

        currentFlagKey = keys[currentIndex];
        ApplyFlag(flags[currentFlagKey]);
        Logger.Log($"Switched to flag: {currentFlagKey}");
    }
}