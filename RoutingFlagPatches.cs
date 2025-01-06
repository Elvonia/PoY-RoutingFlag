using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Rewired;

[HarmonyPatch]
public static class SetRoutingFlagPositionPatch
{
    public static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(RoutingFlag), "SetRoutingFlagPosition", null, null);
    }

    [HarmonyPostfix]
    public static void SetRoutingFlagPosition(RoutingFlag __instance)
    {
        CameraLook cameraLook = GameObject.Find("CamY").GetComponent<CameraLook>();
        Transform playerCameraHolder = GameObject.Find("PlayerCameraHolder").transform;

        if ( __instance != null && __instance.routingFlagTransform != null &&
                cameraLook != null && playerCameraHolder != null)
        {
            RoutingFlagPosition flag = new RoutingFlagPosition(
                __instance.routingFlagTransform.position,
                __instance.routingFlagTransform.eulerAngles,
                playerCameraHolder.rotation.y,
                playerCameraHolder.rotation.w,
                cameraLook.rotationY
            );

            if (!string.IsNullOrEmpty(RoutingFlagExt.instance.currentFlagKey) &&
                RoutingFlagExt.instance.flags.ContainsKey(RoutingFlagExt.instance.currentFlagKey))
            {
                RoutingFlagExt.instance.flags[RoutingFlagExt.instance.currentFlagKey] = flag;
                Logger.Log($"Updated existing flag: {RoutingFlagExt.instance.currentFlagKey}");
            }
            else
            {
                string key = RoutingFlagExt.instance.GenerateNextFlagKey();
                RoutingFlagExt.instance.flags[key] = flag;
                RoutingFlagExt.instance.currentFlagKey = key;

                Logger.Log($"Created new flag: {key}");
            }
        }
        else
        {
            Logger.Warning("RoutingFlag instance or transform is null!");
        }
    }
}

[HarmonyPatch]
public static class UpdateRoutingFlagPatch
{
    public static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(RoutingFlag), "Update", null, null);
    }

    [HarmonyPrefix]
    public static void Prefix(RoutingFlag __instance)
    {
        CameraLook cameraLook = GameObject.Find("CamY").GetComponent<CameraLook>();
        Transform playerCameraHolder = GameObject.Find("PlayerCameraHolder").transform;

        if (
            string.IsNullOrEmpty(RoutingFlagExt.instance.currentFlagKey) 
            || !RoutingFlagExt.instance.flags.ContainsKey(RoutingFlagExt.instance.currentFlagKey) 
            || cameraLook == null 
            || playerCameraHolder == null
        )
        {
            Logger.Log($"Routing flag instance or camera components not found while attempt prefix patch.");
            return;
        }

        // Cases routing flag can't be used
        if (
            __instance.currentlyUsingFlag == false
            || InGameMenu.isCurrentlyNavigationMenu == true
            || EnterPeakScene.enteringPeakScene == true
            || ResetPosition.resettingPosition == true
        )
        {
            return;
        }

        // Checks specific to teleporting
        if (
            GetField<Player>(__instance, "player").GetButtonDown("Move To Routing Flag") == false
            || GetField<RopeAnchor>(__instance, "ropeanchor").attached == true
            || Crampons.cramponsActivated == true
            || Bivouac.currentlyUsingBivouac == true
            || __instance.usedFlagTeleport == true
            || __instance.flagPositionOnPeak_X[__instance.currentPeak] == 0
            || __instance.flagPositionOnPeak_Y[__instance.currentPeak] == 0
            || __instance.flagPositionOnPeak_Z[__instance.currentPeak] == 0
        )
        {
            return;
        }

        ResetCrampons();
        ResetStamina();

        playerCameraHolder.rotation = new Quaternion(
            0f, 
            RoutingFlagExt.instance.flags[RoutingFlagExt.instance.currentFlagKey].CameraHolderRotationY, 
            0f,
            RoutingFlagExt.instance.flags[RoutingFlagExt.instance.currentFlagKey].CameraHolderRotationW
        );
        cameraLook.rotationY = RoutingFlagExt.instance.flags[RoutingFlagExt.instance.currentFlagKey].CameraLookRotationY;
    }

    private static T GetField<T>(RoutingFlag flag, string fieldName)
    {
        return (T)typeof(RoutingFlag).GetField(fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance
        ).GetValue(flag);
    }

    private static void ResetCrampons()
    {
        StemFoot stemFoot = GameObject.Find("CramponsWallkick").GetComponent<StemFoot>();
        stemFoot.wallkickCooldown = 0f;
    }

    private static void ResetStamina()
    {
        ClimbingPitches pitches = GameObject.Find("ClimbingPitches").GetComponent<ClimbingPitches>();
        MicroHolds microHolds = GameObject.Find("Player").GetComponent<MicroHolds>();
        IceAxe iceAxes = GameObject.Find("IceAxes").GetComponent<IceAxe>();

        Vector3 originalStaminaCircleScale = (Vector3)typeof(ClimbingPitches).GetField(
            "originalStaminaCircleScale",
            BindingFlags.NonPublic | BindingFlags.Instance
        ).GetValue(pitches);

        // Crimps/pinches
        microHolds.leftHandGripStrength = 100f;
        microHolds.rightHandGripStrength = 100f;
        microHolds.leftHandGripStrength_Pinch = 100f;
        microHolds.rightHandGripStrength_Pinch = 100f;

        // Pitches
        pitches.staminaCircle.localScale = originalStaminaCircleScale;

        // Pickaxes
        iceAxes.iceAxeStaminaL = 100f;
        iceAxes.iceAxeStaminaR = 100f;
    }
}