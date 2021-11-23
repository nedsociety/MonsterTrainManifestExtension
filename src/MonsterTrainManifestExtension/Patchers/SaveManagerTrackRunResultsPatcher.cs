using HarmonyLib;
using System;
using System.Diagnostics;

namespace MonsterTrainManifestExtension
{
    class SaveManagerTrackRunResultsPatcher
    {
        public static SaveManagerTrackRunResultsPatcher Instance { get; private set; }

        private bool blockCompendiumVictoryTracking;
        private bool blockCompendiumCardTracking;
        private bool blockCovenantRank;
        private bool blockWinStreaks;

        public SaveManagerTrackRunResultsPatcher(Harmony harmony, ExtendedModManifest.ExtendedFields manifest)
        {
            Debug.Assert(Instance == null);
            Instance = this;

            blockCompendiumVictoryTracking = manifest.BlockCompendiumVictoryTracking;
            blockCompendiumCardTracking = manifest.BlockCompendiumCardTracking;
            blockCovenantRank = manifest.BlockCovenantRank;
            blockWinStreaks = manifest.BlockWinStreaks;

            harmony.ProcessorForAnnotatedClass(typeof(Harmony_SaveManager_TrackRunResults)).Patch();
        }

        [ThreadStatic]
        static object shortcutObject;
        static void SetField(string field, object value)
        {
            Traverse.Create(shortcutObject).Field(field).SetValue(value);
        }
        static T GetField<T>(string field)
        {
            return Traverse.Create(shortcutObject).Field(field).GetValue<T>();
        }
        static T Invoke<T>(string method, params object[] args)
        {
            return Traverse.Create(shortcutObject).Method(method, args).GetValue<T>();
        }
        static void Invoke(string method, params object[] args)
        {
            Traverse.Create(shortcutObject).Method(method, args).GetValue();
        }

        public void TrackRunResults(SaveManager saveManager)
        {
            shortcutObject = saveManager;

            var metagameSaveData = saveManager.GetMetagameSave();
            metagameSaveData.ClearChecklistChanges();

            if (Invoke<bool>("HasMainClass"))
            {
                SaveManager.VictoryType victoryType = Invoke<SaveManager.VictoryType>("GetVictoryType");
                if (victoryType > SaveManager.VictoryType.None)
                {
                    RunType runType = Invoke<RunType>("GetRunType");
                    if (runType == RunType.Class)
                    {
                        if (!blockCompendiumVictoryTracking && (victoryType >= SaveManager.VictoryType.Hellforged))
                        {
                            metagameSaveData.TrackHellforgedBossDefeat(new HellforgedMetagameSaveData.RunTrackingParameters
                            {
                                mainClassId = Invoke<ClassData>("GetMainClass").GetID(),
                                subClassId = Invoke<ClassData>("GetSubClass").GetID(),
                                covenantLevel = Invoke<int>("GetCovenantCount"),
                                spChallengeId = Invoke<string>("GetSpChallengeId"),
                                championIndex = Invoke<int>("GetMainChampionIndex")
                            });
                        }

                        // TrackClassVictory()
                        {
                            string mainClassId = Invoke<ClassData>("GetMainClass").GetID();
                            string subClassId = Invoke<ClassData>("GetSubClass").GetID();
                            int ascensionLevel = Invoke<int>("GetAscensionLevel");
                            int mainChampionIndex = Invoke<int>("GetMainChampionIndex");
                            string spChallengeId = Invoke<string>("GetSpChallengeId");

                            try
                            {
                                shortcutObject = metagameSaveData;
                                if (!blockCompendiumVictoryTracking)
                                {
                                    Invoke("TrackMainClassWin", mainClassId);
                                    Invoke(
                                        "TrackClassCombinationWin",
                                        mainClassId, subClassId, ascensionLevel, mainChampionIndex
                                    );
                                }

                                if (string.IsNullOrEmpty(spChallengeId))
                                {
                                    if (!blockWinStreaks)
                                        Invoke("IncrementWinStreak", ascensionLevel);
                                }
                                else
                                {
                                    if (!blockCompendiumVictoryTracking)
                                        Invoke("TrackSpChallengeWin", spChallengeId);
                                }
                            }
                            finally
                            {
                                shortcutObject = saveManager;
                            }
                        }

                        if (!blockCompendiumCardTracking)
                        {
                            Invoke("TrackCardWins", victoryType >= SaveManager.VictoryType.Hellforged);
                        }

                        if (!blockCovenantRank)
                        {
                            Invoke("IncreaseAscensionLevel");
                        }
                    }
                }
                else
                {
                    // SaveManagerBlockWinStreaksPatcher handles this
                    Invoke("TrackClassDefeat");
                }
            }

            Invoke("UpdateUnlockedMasteryCriteria");
            Invoke("StartSavingMetagame", GetField<string>("MetagameSaveFileName"));
        }
    }

    [HarmonyPatch(typeof(SaveManager))]
    [HarmonyPatch("TrackRunResults")]
    public static class Harmony_SaveManager_TrackRunResults
    {
        static bool Prefix(SaveManager __instance)
        {
            SaveManagerTrackRunResultsPatcher.Instance.TrackRunResults(__instance);
            return false;
        }
    }
}
