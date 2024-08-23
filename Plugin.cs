using BepInEx;
using HarmonyLib;
using HarmonyLib.Tools;

namespace ProfitSliders;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class ProfitSliders : BaseUnityPlugin
{
    private void Awake()
    {
        // Plugin startup logic
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        new Harmony("DustTradeSliders").PatchAll(typeof(Patches));
        HarmonyFileLog.Enabled = false;
    }
}