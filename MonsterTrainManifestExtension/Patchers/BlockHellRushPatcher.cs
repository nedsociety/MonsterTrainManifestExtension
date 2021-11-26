using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace MonsterTrainManifestExtension
{
    class BlockHellRushPatcher
    {
        public static BlockHellRushPatcher Instance { get; private set; }

        private bool block;

        public BlockHellRushPatcher(Harmony harmony, ExtendedModManifest.ExtendedFields manifest)
        {
            Debug.Assert(Instance == null);
            Instance = this;

            block = manifest.BlockHellRush;

            harmony.ProcessorForAnnotatedClass(typeof(Harmony_BlockHellRushPatcher)).Patch();
        }

        public bool AreAnyModsNonCosmetic()
        {
            return block;
        }
    }

    [HarmonyPatch]
    public static class Harmony_BlockHellRushPatcher
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(
                // Compiler-generated (desugared) async primitive
                Type.GetType("GameStateManager+<StartHellRoyaleRun>d__44, Assembly-CSharp", true), "MoveNext"
            );
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return Utility.Hook_AreAnyModsNonCosmetic(instructions, typeof(BlockHellRushPatcher));
        }
    }
}
