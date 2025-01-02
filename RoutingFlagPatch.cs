using HarmonyLib;
using MelonLoader;
using System.Reflection;

[HarmonyPatch]
public static class RoutingFlagPatch
{
    public static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(RoutingFlag), "SetRoutingFlagPosition", null, null);
    }

    [HarmonyPostfix]
    public static void SaveRoutingFlagPosition(RoutingFlag __instance)
    {
        if (__instance != null && __instance.routingFlagTransform != null)
        {
            var flag = new RoutingFlagPosition(
                __instance.routingFlagTransform.position,
                __instance.routingFlagTransform.eulerAngles
            );

            if (!string.IsNullOrEmpty(RoutingFlagExt.instance.currentFlagKey) &&
                RoutingFlagExt.instance.flags.ContainsKey(RoutingFlagExt.instance.currentFlagKey))
            {
                RoutingFlagExt.instance.flags[RoutingFlagExt.instance.currentFlagKey] = flag;
                MelonLogger.Msg($"[HarmonyX] Updated existing flag: {RoutingFlagExt.instance.currentFlagKey}");
            }
            else
            {
                string key = RoutingFlagExt.instance.GenerateNextFlagKey();
                RoutingFlagExt.instance.flags[key] = flag;
                RoutingFlagExt.instance.currentFlagKey = key;

                MelonLogger.Msg($"[HarmonyX] Created new flag: {key}");
            }
        }
        else
        {
            MelonLogger.Warning("[HarmonyX] RoutingFlag instance or transform is null!");
        }
    }
}