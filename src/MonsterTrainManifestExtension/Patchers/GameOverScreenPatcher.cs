using HarmonyLib;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace MonsterTrainManifestExtension
{
    class GameOverScreenPatcher
    {
        public static GameOverScreenPatcher Instance { get; private set; }

        private bool blockWinStreaks;
        private bool blockLocalStats;

        public GameOverScreenPatcher(Harmony harmony, ExtendedModManifest.ExtendedFields manifest)
        {
            Debug.Assert(Instance == null);
            Instance = this;

            blockWinStreaks = manifest.BlockWinStreaks;
            blockLocalStats = manifest.BlockLocalStats;

            harmony.ProcessorForAnnotatedClass(typeof(Harmony_GameOverScreenPatcher)).Patch();
        }

        public bool AreAnyModsNonCosmetic(int occurence)
        {
            switch (occurence)
            {
                case 0:
                    // Determines whether WinStreakUI should be shown
                    return blockWinStreaks;

                case 1:
                case 2:
                    // Determines whether stat comparisons should be done
                    return blockLocalStats;

                default:
                    Debug.Assert(false);
                    return true;
            }
        }
    }

    [HarmonyPatch]
    public static class Harmony_GameOverScreenPatcher
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(GameOverScreen), "Initialize");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return Utility.Hook_AllowAdvancedProgression(instructions, typeof(GameOverScreenPatcher));
        }
    }
}
