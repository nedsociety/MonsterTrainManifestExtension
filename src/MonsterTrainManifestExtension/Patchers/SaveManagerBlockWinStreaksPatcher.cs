using HarmonyLib;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace MonsterTrainManifestExtension
{
    class SaveManagerBlockWinStreaksPatcher
    {
        public static SaveManagerBlockWinStreaksPatcher Instance { get; private set; }

        private bool block;

        public SaveManagerBlockWinStreaksPatcher(Harmony harmony, ExtendedModManifest.ExtendedFields manifest)
        {
            Debug.Assert(Instance == null);
            Instance = this;

            block = manifest.BlockWinStreaks;

            harmony.ProcessorForAnnotatedClass(typeof(Harmony_SaveManagerBlockWinStreaksPatcher)).Patch();
        }

        public bool AreAnyModsNonCosmetic(int occurence)
        {
            return block;
        }
    }

    [HarmonyPatch]
    public static class Harmony_SaveManagerBlockWinStreaksPatcher
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(SaveManager), "GetWinStreakDataForActiveRunType");
            yield return AccessTools.Method(typeof(SaveManager), "TrackClassDefeat");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return Utility.Hook_AllowAdvancedProgression(instructions, typeof(SaveManagerBlockWinStreaksPatcher));
        }
    }
}
