using HarmonyLib;
using System.Collections.Generic;
using System.Diagnostics;

namespace MonsterTrainManifestExtension
{
    class BootWarningMessageManager
    {
        public static BootWarningMessageManager Instance { get; private set; }
        private Queue<string> warningsToShow = new Queue<string>();

        public BootWarningMessageManager(Harmony harmony)
        {
            Debug.Assert(Instance == null);
            Instance = this;

            harmony.ProcessorForAnnotatedClass(typeof(Harmony_MainMenuScreen_Initialize)).Patch();
        }

        public void QueueWarning(string message)
        {
            warningsToShow.Enqueue(message);
        }

        private void ShowNextWarningIfAny(ScreenManager screenManager)
        {
            if (warningsToShow.TryDequeue(out var message))
                screenManager.ShowNotificationDialog(message, delegate { ShowNextWarningIfAny(screenManager); });
        }

        public void OnMainMenuOpened(MainMenuScreen mainMenuScreen)
        {
            ShowNextWarningIfAny(mainMenuScreen.GetScreenManager());
        }
    }

    [HarmonyPatch(typeof(MainMenuScreen))]
    [HarmonyPatch("Initialize")]
    public static class Harmony_MainMenuScreen_Initialize
    {
        static void Postfix(MainMenuScreen __instance)
        {
            BootWarningMessageManager.Instance.OnMainMenuOpened(__instance);
        }
    }
}

