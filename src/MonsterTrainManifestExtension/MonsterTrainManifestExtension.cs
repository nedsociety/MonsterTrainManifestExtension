using HarmonyLib;
using System.Collections.Generic;
using System.Diagnostics;

namespace MonsterTrainManifestExtension
{
    [BepInEx.BepInPlugin("com.nedsociety.monstertrainmanifestextension", "MonsterTrainManifestExtension", "1.0.0.0")]
    public class MonsterTrainManifestExtension : BepInEx.BaseUnityPlugin
    {
        const string MT_BUILD_NUMBER = "12923";

        public static MonsterTrainManifestExtension Instance { get; private set; }
        new public static BepInEx.Logging.ManualLogSource Logger { get; private set; }
        public bool Enabled { get; private set; }

        private Harmony harmony;
        private BootWarningMessageManager bootWarningMessageManager;

        public void Awake()
        {
            Debug.Assert(Instance == null);
            Instance = this;
            Logger = base.Logger;
            Enabled = false;

            harmony = new Harmony("com.nedsociety.monstertrainmanifestextension");
            bootWarningMessageManager = new BootWarningMessageManager(harmony);

            if (ShinyShoe.AppManager.BuildNumber != MT_BUILD_NUMBER)
            {
                Warn(
                    $"Some of your mods require a framework called MonsterTrainManifestExtension, which expects Monster Train version ##{MT_BUILD_NUMBER}. The framework should be updated to enable advanced features like progressions.",
                    $"Expected MT build {MT_BUILD_NUMBER} but encountered {ShinyShoe.AppManager.BuildNumber}; ManifestExtension is inactivated.");
                return;
            }

            new BootPhaseHandler(harmony).OnBootPhaseDone += OnBootPhaseDone;
        }

        private void OnBootPhaseDone(bool isSteam, IEnumerable<ExtendedModManifest> extendedModManifests)
        {
            if (!isSteam)
            {
                Warn(null, $"The game doesn't seem to be on Steam. The modmanifest.json file is invalid in this context, so ManifestExtension is effectively inactivated.");
                return;
            }

            ExtendedModManifest.ExtendedFields combinedResult = ExtendedModManifest.ExtendedFields.DefaultForCosmeticOnly();

            foreach (var extendedModManifest in extendedModManifests)
            {
                Logger.LogInfo(
                    $"Mod: {extendedModManifest.baseFields.ModName} (id = {extendedModManifest.baseFields.ModIdentifier}, CosmeticOnly = {extendedModManifest.baseFields.CosmeticOnly})\n"
                    + extendedModManifest.extendedFields.AsDetailedString()
                );
                combinedResult &= extendedModManifest.extendedFields;
            }
            Logger.LogInfo(
                "Combined result of modmanifest.json:\n"
                + combinedResult.AsDetailedString()
            );

            // Require the game to use a modded metagameSave file (where every progresses are saved).
            // Clone it if it does not exist when the game boots.
            new BranchMetagameSavePatcher(harmony, combinedResult);

            // Mark the run summaries to be modded or not before uploading to the server.
            new ReportRunSummaryAsModdedPatcher(harmony, combinedResult);

            // Control the warning dialog saying leaderboard is blocked.
            new LeaderboardUploadBlockedWarningUIPatcher(harmony, combinedResult);

            // Block creating a new custom challenge.
            new BlockCreatingCustomChallengePatcher(harmony, combinedResult);

            // Block achievements.
            new BlockAchievementsPatcher(harmony, combinedResult);

            // Block Hell Rush.
            new BlockHellRushPatcher(harmony, combinedResult);

            // In clan selection screen, "partially" hide the blocked contents.
            new ClassSelectionScreenPatcher(harmony, combinedResult);
            
            // Block WinStreaks and LocalStats on game over screen.
            new GameOverScreenPatcher(harmony, combinedResult);
            
            // Block updating stats.
            new SaveManagerBlockLocalStatsPatcher(harmony, combinedResult);

            // Block updating win streaks.
            new SaveManagerBlockWinStreaksPatcher(harmony, combinedResult);

            // Block most of progressions.
            new SaveManagerTrackRunResultsPatcher(harmony, combinedResult);

            Enabled = true;
        }

        public void Warn(string endUserMessage = null, string devMessage = null)
        {
            Debug.Assert((devMessage != null) || (endUserMessage != null));
            if (devMessage == null)
                devMessage = endUserMessage;

            Logger.LogWarning(devMessage);
            if (endUserMessage != null)
                bootWarningMessageManager.QueueWarning(endUserMessage);
        }
    }
}
