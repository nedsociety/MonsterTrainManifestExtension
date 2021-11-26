using HarmonyLib;
using ShinyShoe;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MonsterTrainManifestExtension
{
    class BootPhaseHandler
    {
        public static BootPhaseHandler Instance { get; private set; }

        public delegate void BootPhaseDoneHandler(bool isSteam, IEnumerable<ExtendedModManifest> extendedModManifests);
        public event BootPhaseDoneHandler OnBootPhaseDone;
        private bool done = false;

        private void ReportDone(bool isSteam, IEnumerable<ExtendedModManifest> extendedModManifests)
        {
            if (done)
                return;

            OnBootPhaseDone?.Invoke(isSteam, extendedModManifests);
            done = true;
        }

        public BootPhaseHandler(Harmony harmony)
        {
            Debug.Assert(Instance == null);
            Instance = this;

            try
            {
                harmony.ProcessorForAnnotatedClass(typeof(Harmony_SteamClientHades_Start)).Patch();
            }
            catch (TypeLoadException)
            {
                MonsterTrainManifestExtension.Instance.Warn(null, "Failed to find SteamClientHades class - might not be a Steam build.");
                // Ignore it for now, as OnAppManagerAwoken() would be enough to conclude that it's not a Steam build.
            }
            harmony.ProcessorForAnnotatedClass(typeof(Harmony_AppManager_Awake)).Patch();
        }

        public void OnAppManagerAwoken()
        {
            // At this point AppManager.PlatformServices is initialized and we can query its type.
            if (AppManager.PlatformServices.GetPlatformName() != "Steam")
            {
                ReportDone(false, null);
                return;
            }
            Debug.Assert(AppManager.PlatformServices is SteamClientHades);
        }

        internal void OnSteamClientHadesStarted(SteamClientHades steamClientHades)
        {
            // At this point the mod list is initialized.
            ReportDone(
                true,
                from mod in steamClientHades.GetModInformation()
                where mod.Enabled
                select mod.AsExtendedModManifest(steamClientHades)
            );
        }
    }

    [HarmonyPatch(typeof(AppManager))]
    [HarmonyPatch("Awake")]
    public static class Harmony_AppManager_Awake
    {
        static void Postfix()
        {
            BootPhaseHandler.Instance.OnAppManagerAwoken();
        }
    }

    [HarmonyPatch(typeof(SteamClientHades))]
    [HarmonyPatch("Start")]
    public static class Harmony_SteamClientHades_Start
    {
        static IEnumerator Postfix(IEnumerator __result, SteamClientHades __instance)
        {
            while (__result.MoveNext())
                yield return __result.Current;

            BootPhaseHandler.Instance.OnSteamClientHadesStarted(__instance);
        }
    }
}
