using HarmonyLib;
using System.Diagnostics;
using System.IO;

namespace MonsterTrainManifestExtension
{
    class BranchMetagameSavePatcher
    {
        public const string OLD_SAVE_NAME = "metagameSave";
        public const string NEW_SAVE_NAME = "metagameSave-modded";

        public static BranchMetagameSavePatcher Instance { get; private set; }

        public BranchMetagameSavePatcher(Harmony harmony, ExtendedModManifest.ExtendedFields manifest)
        {
            if (!manifest.BranchMetagameSave)
                return;

            Debug.Assert(Instance == null);
            Instance = this;

            harmony.ProcessorForAnnotatedClass(typeof(Harmony_SaveManager_InitUser)).Patch();
            harmony.ProcessorForAnnotatedClass(typeof(Harmony_SaveManager_StartLoadingMetagame)).Patch();
            harmony.ProcessorForAnnotatedClass(typeof(Harmony_SaveManager_StartSavingMetagame)).Patch();
        }

        public void BeforeSaveManagerInitUser(SaveManager saveManager)
        {
            // At this moment we clone an existing base game save.

            string oldSavePath = SaveManager.GetMetagameSavePath(OLD_SAVE_NAME);
            string newSavePath = SaveManager.GetMetagameSavePath(NEW_SAVE_NAME);

            if (File.Exists(newSavePath))
            {
                MonsterTrainManifestExtension.Instance.Warn(null, "Previously branched metagameSave found; using it.");
                return;
            }

            if (File.Exists(oldSavePath))
            {
                File.Copy(oldSavePath, newSavePath);

                MonsterTrainManifestExtension.Instance.Warn(
                    "Some of your mods have requested permission for modding the save file."
                    + " To ensure the integrity of your unmodded progression,"
                    + " we are going to maintain a separate save file only used for the modded game.<br><br>"
                    + "Do note that the save is effectively cloned at this point,"
                    + " so if you disable such mods in future your progression will be rolled back to now."
                    + " Also, the modded save will NOT be synced by Steam Cloud.",
                    "BranchMetagameSave successfully clone the MetagameSave file."
                );
            }
            else
            {
                // Huh, haven't played MT ever?
                MonsterTrainManifestExtension.Instance.Warn(
                    "Some of your mods have requested permission for modding the save file."
                    + " To ensure the integrity of your unmodded progression,"
                    + " we are going to maintain a separate save file only used for the modded game.<br><br>"
                    + "This should be fine, but do note that if you disable such mods you'll be starting from beginning."
                    + " Also, the modded save will NOT be synced by Steam Cloud.",
                    "BranchMetagameSave couldn't find the existing MetagameSave. We'll redirect it anyway."
                );
            }
        }
    }

    [HarmonyPatch(typeof(SaveManager))]
    [HarmonyPatch("InitUser")]
    public static class Harmony_SaveManager_InitUser
    {
        static void Prefix(SaveManager __instance)
        {
            BranchMetagameSavePatcher.Instance.BeforeSaveManagerInitUser(__instance);
        }
    }

    [HarmonyPatch(typeof(SaveManager))]
    [HarmonyPatch("StartLoadingMetagame")]
    public static class Harmony_SaveManager_StartLoadingMetagame
    {
        static void Prefix(ref string fileName)
        {
            if (fileName == BranchMetagameSavePatcher.OLD_SAVE_NAME)
                fileName = BranchMetagameSavePatcher.NEW_SAVE_NAME;
        }
    }

    [HarmonyPatch(typeof(SaveManager))]
    [HarmonyPatch("StartSavingMetagame")]
    public static class Harmony_SaveManager_StartSavingMetagame
    {
        static void Prefix(ref string fileName)
        {
            if (fileName == BranchMetagameSavePatcher.OLD_SAVE_NAME)
                fileName = BranchMetagameSavePatcher.NEW_SAVE_NAME;
        }
    }
}
