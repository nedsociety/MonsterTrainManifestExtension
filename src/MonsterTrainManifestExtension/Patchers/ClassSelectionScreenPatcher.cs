using HarmonyLib;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace MonsterTrainManifestExtension
{
    class ClassSelectionScreenPatcher
    {
        public static ClassSelectionScreenPatcher Instance { get; private set; }

        private bool blockCovenantRank;
        private bool blockWinStreaks;

        public ClassSelectionScreenPatcher(Harmony harmony, ExtendedModManifest.ExtendedFields manifest)
        {
            Debug.Assert(Instance == null);
            Instance = this;

            blockCovenantRank = manifest.BlockCovenantRank;
            blockWinStreaks = manifest.BlockWinStreaks;

            harmony.ProcessorForAnnotatedClass(typeof(Harmony_ClassSelectionScreenPatcher)).Patch();
        }

        public bool AreAnyModsNonCosmetic()
        {
            return blockCovenantRank || blockWinStreaks;
        }

        public void OnRefreshProgressionUI(ClassSelectionScreen classSelectionScreen)
        {
            bool hasCustomizedRun = Traverse.Create(classSelectionScreen).Field("mutatorOptionsUI")
                .GetValue<SpMutatorOptionsUI>().HasCustomizedRun();
            Traverse.Create(classSelectionScreen).Field("clanCovenantRankUI").GetValue<ClanCovenantRankUI>()
                .gameObject.SetActive(!hasCustomizedRun && !blockCovenantRank);
            Traverse.Create(classSelectionScreen).Field("winStreakUI").GetValue<WinStreakUI>()
                .gameObject.SetActive(!hasCustomizedRun && !blockWinStreaks);
        }
    }

    [HarmonyPatch]
    public static class Harmony_ClassSelectionScreenPatcher
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(ClassSelectionScreen), "RefreshProgressionUI");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return Utility.Hook_AreAnyModsNonCosmetic(instructions, typeof(ClassSelectionScreenPatcher));
        }

        static void Postfix(ClassSelectionScreen __instance)
        {
            ClassSelectionScreenPatcher.Instance.OnRefreshProgressionUI(__instance);
        }
    }
}
