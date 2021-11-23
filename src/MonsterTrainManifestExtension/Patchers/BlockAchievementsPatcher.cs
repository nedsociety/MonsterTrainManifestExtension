using HarmonyLib;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace MonsterTrainManifestExtension
{
    class BlockAchievementsPatcher
    {
        public static BlockAchievementsPatcher Instance { get; private set; }

        private bool block;

        public BlockAchievementsPatcher(Harmony harmony, ExtendedModManifest.ExtendedFields manifest)
        {
            Debug.Assert(Instance == null);
            Instance = this;

            block = manifest.BlockAchievements;

            harmony.ProcessorForAnnotatedClass(typeof(Harmony_BlockAchievementsPatcher)).Patch();
        }

        public bool AreAnyModsNonCosmetic()
        {
            return block;
        }
    }

    [HarmonyPatch]
    public static class Harmony_BlockAchievementsPatcher
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(AchievementManager), "InnerTriggerAchievementById");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return Utility.Hook_AreAnyModsNonCosmetic(instructions, typeof(BlockAchievementsPatcher));
        }
    }
}
