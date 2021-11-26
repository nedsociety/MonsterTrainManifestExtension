using HarmonyLib;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace MonsterTrainManifestExtension
{
    class LeaderboardUploadBlockedWarningUIPatcher
    {
        public static LeaderboardUploadBlockedWarningUIPatcher Instance { get; private set; }

        private bool warn;

        public LeaderboardUploadBlockedWarningUIPatcher(Harmony harmony, ExtendedModManifest.ExtendedFields manifest)
        {
            Debug.Assert(Instance == null);
            Instance = this;

            // Assume that only base clan combos appear at ChallengeDetailsScreen
            warn = manifest.ReportRunSummaryAsModded == ReportRunSummaryAsModdedEnum.Always;

            harmony.ProcessorForAnnotatedClass(typeof(Harmony_LeaderboardUploadBlockedWarningUIPatcher)).Patch();
        }

        public bool AreAnyModsNonCosmetic()
        {
            return warn;
        }
    }

    [HarmonyPatch]
    public static class Harmony_LeaderboardUploadBlockedWarningUIPatcher
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(ChallengeDetailsScreen), "CheckIfModsEnabled");
            yield return AccessTools.Method(typeof(ChallengeDetailsScreen), "TrySubmit");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return Utility.Hook_AreAnyModsNonCosmetic(instructions, typeof(LeaderboardUploadBlockedWarningUIPatcher));
        }
    }
}
