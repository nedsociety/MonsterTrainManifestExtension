using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace MonsterTrainManifestExtension
{
    static class Utility
    {
        public static ScreenManager GetScreenManager(this UIScreen uiScreen)
        {
            return Traverse.Create(uiScreen).Field("screenManager").GetValue<ScreenManager>();
        }

        // Replace ShinyShoe.AppManager.PlatformServices.AreAnyModsNonCosmetic()
        // to <type>.Instance.AreAnyModsNonCosmetic()
        public static IEnumerable<CodeInstruction> Hook_AreAnyModsNonCosmetic(
            IEnumerable<CodeInstruction> instructions, Type type
        )
        {
            // Original IL fragment:
            // call class ShinyShoe.IPlatformServices ShinyShoe.AppManager::get_PlatformServices()
            // callvirt instance bool ShinyShoe.IPlatformServices::AreAnyModsNonCosmetic()
            var orig_get_PlatformServices = AccessTools.Method(typeof(ShinyShoe.AppManager), "get_PlatformServices");
            var orig_AreAnyModsNonCosmetic = AccessTools.Method(typeof(ShinyShoe.IPlatformServices), "AreAnyModsNonCosmetic");

            // To-be-replaced: <type>.Instance.AreAnyModsNonCosmetic()
            var repl_get_Instance = AccessTools.Method(type, "get_Instance");
            var repl_AreAnyModsNonCosmetic = AccessTools.Method(type, "AreAnyModsNonCosmetic");

            var instructionsAsList = new List<CodeInstruction>(instructions);
            for (var i = 0; i < instructionsAsList.Count - 1; ++i)
            {
                var curInst = instructionsAsList[i];
                var nextInst = instructionsAsList[i + 1];

                if (curInst.Calls(orig_get_PlatformServices) && nextInst.Calls(orig_AreAnyModsNonCosmetic))
                {
                    curInst.operand = repl_get_Instance;
                    nextInst.operand = repl_AreAnyModsNonCosmetic;
                }
            }
            return instructionsAsList.AsEnumerable();
        }

        private static bool Stub_AllowAdvancedProgression(RunType runType, int occurence, object processorInstance)
        {
            return (runType == RunType.Class)
                   && Traverse.Create(processorInstance).Method("AreAnyModsNonCosmetic", occurence).GetValue<bool>();
        }

        // Replace ShinyShoe.AppManager.PlatformServices.AreAnyModsNonCosmetic() calls within
        // RunTypeUtil.AllowAdvancedProgression(runType) with <type>.Instance.AreAnyModsNonCosmetic(occurence) instead.
        public static IEnumerable<CodeInstruction> Hook_AllowAdvancedProgression(
            IEnumerable<CodeInstruction> instructions, Type type
        )
        {
            // Original IL fragment:
            // ((push runType))
            // call bool RunTypeUtil::AllowAdvancedProgression(valuetype RunType)
            var orig_AllowAdvancedProgression = AccessTools.Method(typeof(RunTypeUtil), "AllowAdvancedProgression");

            // To-be-replaced: Utility.Stub_AllowAdvancedProgression(runType, occurence, <type>.Instance)
            var repl_get_Instance = AccessTools.Method(type, "get_Instance");
            var replStub = AccessTools.Method(typeof(Utility), nameof(Stub_AllowAdvancedProgression));

            int occurence = 0;

            var instructionsAsList = new List<CodeInstruction>(instructions);
            for (var i = 0; i < instructionsAsList.Count - 1; ++i)
            {
                var curInst = instructionsAsList[i];
                if (curInst.Calls(orig_AllowAdvancedProgression))
                {
                    curInst.operand = replStub;
                    instructionsAsList.Insert(i, new CodeInstruction(OpCodes.Ldc_I4, occurence));
                    instructionsAsList.Insert(i + 1, new CodeInstruction(OpCodes.Call, repl_get_Instance));

                    ++occurence;
                }
            }
            return instructionsAsList.AsEnumerable();
        }
    }
}