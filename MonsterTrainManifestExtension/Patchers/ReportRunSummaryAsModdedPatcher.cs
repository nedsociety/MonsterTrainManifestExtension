using HarmonyLib;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace MonsterTrainManifestExtension
{
    class ReportRunSummaryAsModdedPatcher
    {
        public static ReportRunSummaryAsModdedPatcher Instance { get; private set; }

        static readonly List<string> DEFAULT_CLANS_NAME = new List<string>()
        {
            "ClassAwoken",
            "ClassHellhorned",
            "ClassRemnant",
            "ClassStygian",
            "ClassUmbra",
            "ClassWurm"
        };

        private ReportRunSummaryAsModdedEnum reportRunSummaryAsModded;

        public ReportRunSummaryAsModdedPatcher(Harmony harmony, ExtendedModManifest.ExtendedFields manifest)
        {
            Debug.Assert(Instance == null);
            Instance = this;

            reportRunSummaryAsModded = manifest.ReportRunSummaryAsModded;

            harmony.ProcessorForAnnotatedClass(typeof(Harmony_ReportRunSummaryAsModdedPatcher)).Patch();
        }

        public bool AreAnyModsNonCosmetic()
        {
            // Should be handled by postfix. For safety just set to true atm.
            return true;
        }

        static bool IsClassFromBaseGame(ClassData data)
        {
            if (data == null)
                return false;
            return DEFAULT_CLANS_NAME.Contains(data.name);
        }

        public void OnReset(RunAggregateData runAggregateData, SaveManager saveManager)
        {
            var isModdedField = Traverse.Create(runAggregateData).Field("isModded");
            if (reportRunSummaryAsModded == ReportRunSummaryAsModdedEnum.Never)
            {
                isModdedField.SetValue(false);
            }
            else if (reportRunSummaryAsModded == ReportRunSummaryAsModdedEnum.OnlyWhenNewClansAreInvolved)
            {
                var allGameData = saveManager.GetAllGameData();
                if (
                    IsClassFromBaseGame(allGameData.FindClassData(runAggregateData.GetMainClassID()))
                    && IsClassFromBaseGame(allGameData.FindClassData(runAggregateData.GetSubClassID()))
                )
                    isModdedField.SetValue(false);
                else
                    isModdedField.SetValue(true);

            }
            else if (reportRunSummaryAsModded == ReportRunSummaryAsModdedEnum.Always)
            {
                isModdedField.SetValue(true);
            }
            else
                Debug.Assert(false);
        }
    }

    [HarmonyPatch]
    public static class Harmony_ReportRunSummaryAsModdedPatcher
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(RunAggregateData), "Reset");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return Utility.Hook_AreAnyModsNonCosmetic(instructions, typeof(ReportRunSummaryAsModdedPatcher));
        }

        static void Postfix(RunAggregateData __instance, SaveManager saveManager)
        {
            ReportRunSummaryAsModdedPatcher.Instance.OnReset(__instance, saveManager);
        }
    }
}
