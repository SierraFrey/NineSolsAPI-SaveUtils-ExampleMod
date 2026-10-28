using System;
using System.IO;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine.SceneManagement;
using NineSolsAPI;
using NineSolsAPI.Utils;

namespace save_example;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(NineSolsAPICore.PluginGUID)]
[BepInProcess("NineSols.exe")]
public class Plugin : BaseUnityPlugin
{
    internal static Plugin Instance = null!;
    internal static new ManualLogSource Logger;
    private Harmony harmony = null!;

    internal static Dictionary<string, FlagsInSpecificScene> allScenes = null!;
    internal static GameFlagInt jumpCount = null!;
    internal static GameFlagInt killCount = null!;
    private void OnDestroy()
    {
        harmony.UnpatchSelf();
    }
    private void Awake()
    {
        Instance = this;
        Logger = base.Logger;
        harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

        allScenes = new();
        try
        {
            SaveUtils.SetURL(Instance, "https://github.com/SierraFrey/NineSolsAPI-SaveUtils-ExampleMod/tree/main");
            /*
            Calling these methods allows the API to do most of the work for you by:
            Creating its field
            Setting a default value
            Setting an ID
            Adding it to the SaveManager's flag dictionary

            Make sure that every name is unique!
            */

            jumpCount = SaveUtils.CreateGameFlagInt(Instance, "jumpCount", 0);
            killCount = SaveUtils.CreateGameFlagInt(Instance, "killCount", 0);

            /*
            The following 4 flag types can be created with SaveUtils:
            ScriptableDataBool - Bool
            GameFlagInt - Int
            ScriptableDataFloat - Float
            GameFlagString - String

            If you want to save something more complicated, you can create classes that use the SaveUtils methods in its constructor.
            */
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string name = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));
                allScenes.Add(name, new FlagsInSpecificScene(name));
            }
            harmony.PatchAll();
        }
        catch (Exception e)
        {
            Logger.LogError($"Plugin {MyPluginInfo.PLUGIN_GUID} failed to load! {e}");
        }
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }
    
    public class FlagsInSpecificScene
    {
        public FlagsInSpecificScene(string name)
        {
            breakablesDestroyed = SaveUtils.CreateGameFlagInt(Instance, $"{name}.breakablesDestroyed", 0);
            timesEntered = SaveUtils.CreateGameFlagInt(Instance, $"{name}.timesEntered", 0);
        }
        public GameFlagInt breakablesDestroyed;
        public GameFlagInt timesEntered;
    }
}

[HarmonyPatch]
public class Patches
{
    [HarmonyPatch(typeof(Player), nameof(Player.MechanicalJump))]
    [HarmonyPostfix]
    private static void MechanicalJump()
    {
        Plugin.jumpCount.CurrentValue++;
        ToastManager.Toast($"Jumps: {Plugin.jumpCount.CurrentValue}");
    }

    [HarmonyPatch(typeof(MonsterBase), nameof(MonsterBase.Die))]
    [HarmonyPostfix]
    private static void Die()
    {
        Plugin.killCount.CurrentValue++;
        ToastManager.Toast($"Kills: {Plugin.killCount.CurrentValue}");
    }

    [HarmonyPatch(typeof(DebrisBreakable), nameof(DebrisBreakable.Break))]
    [HarmonyPostfix]
    private static void Break()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        GameFlagInt flag = Plugin.allScenes[sceneName].breakablesDestroyed;
        flag.CurrentValue++;
        ToastManager.Toast($"Breakables destroyed in this scene: {flag.CurrentValue}");
    }

    [HarmonyPatch(typeof(GameLevel), nameof(GameLevel.Awake))]
    [HarmonyPostfix]
    private static void Awake(GameLevel __instance)
    {
        if (!Plugin.allScenes.ContainsKey(__instance.SceneName))
            return;
        GameFlagInt flag = Plugin.allScenes[__instance.SceneName].timesEntered;
        flag.CurrentValue++;
        ToastManager.Toast($"Times you entered this scene ({__instance.SceneName}): {flag.CurrentValue}");
    }
}


