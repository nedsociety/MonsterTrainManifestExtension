using HarmonyLib;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace MonsterTrainManifestExtension
{
    class BlockCreatingCustomChallengePatcher
    {
        public static BlockCreatingCustomChallengePatcher Instance { get; private set; }

        private bool block;

        public BlockCreatingCustomChallengePatcher(Harmony harmony, ExtendedModManifest.ExtendedFields manifest)
        {
            Debug.Assert(Instance == null);
            Instance = this;

            block = manifest.BlockCreatingCustomChallenge;

            harmony.ProcessorForAnnotatedClass(typeof(Harmony_BlockCreatingCustomChallengePatcher)).Patch();
        }

        public bool AreAnyModsNonCosmetic()
        {
            // As noted in doc, it's bugged even if we do this.
            return block;
        }
    }

    [HarmonyPatch]
    public static class Harmony_BlockCreatingCustomChallengePatcher
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(ChallengeOverviewScreen), "ApplyScreenInput");
            yield return AccessTools.Method(typeof(RunSummaryScreen), "HandleCreateChallengeButton");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return Utility.Hook_AreAnyModsNonCosmetic(instructions, typeof(BlockCreatingCustomChallengePatcher));
        }
    }
}
