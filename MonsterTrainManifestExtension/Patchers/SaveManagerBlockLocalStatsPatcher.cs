using HarmonyLib;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace MonsterTrainManifestExtension
{
    class SaveManagerBlockLocalStatsPatcher
    {
        public static SaveManagerBlockLocalStatsPatcher Instance { get; private set; }

        private bool block;

        public SaveManagerBlockLocalStatsPatcher(Harmony harmony, ExtendedModManifest.ExtendedFields manifest)
        {
            Debug.Assert(Instance == null);
            Instance = this;

            block = manifest.BlockLocalStats;

            harmony.ProcessorForAnnotatedClass(typeof(Harmony_SaveManagerBlockLocalStatsPatcher)).Patch();
        }

        public bool AreAnyModsNonCosmetic(int occurence)
        {
            return block;
        }
    }

    [HarmonyPatch]
    public static class Harmony_SaveManagerBlockLocalStatsPatcher
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(SaveManager), "FinishRun");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return Utility.Hook_AllowAdvancedProgression(instructions, typeof(SaveManagerBlockLocalStatsPatcher));
        }
    }
}
