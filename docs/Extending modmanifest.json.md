# Extending `modmanifest.json`

## Summary

Create a mod that allows following features:

1. Instead of crude `CosmeticOnly` flag, support fine-grained control over what is affected by mods.
2. Support cloning and redirecting `metagameSave` file.

The major advantage of these changes would be:

1. Allow progressions on base game like XP, achievements, compendiums and Covenant ranks even if the game is modded.
2. Allow progressions on mod clans, if they adapt themselves into the `MetagameSaveData` class.

## New options

Along with the official `CosmeticOnly` field, we'll have some new fields. For the sake of compatibility, these values are NOT inspected when `"CosmeticOnly": true` is specified.

- `"BranchMetagameSave": true | false` (default: `false`)

  - Require the game to use a modded `metagameSave` file (where every progresses are saved). Clone it if it does not exist when the game boots.

  - This should allow `MetagameSaveData` class to be modded safely, where the mod clans may track their own progression in it.

  - LIMITATION: The cloned save will be used as long as at least one mod requires it. If all of such mods are disabled or removed, however, the game will revert to use the standard `metagameSave` so your base game progression would revert to whenever you've started using those mods. This is still better than no progressions at all.

    Related link (in MT discord): https://discord.com/channels/336546996779483136/715703725846429717/907422585212530698

- `"ReportRunSummaryAsModded": "Always" | "Never" | "OnlyWhenNewClansAreInvolved"` (default: `"Always"`)

  - Mark the run summaries to be modded or not. This should control whether the run should enter the leaderboards, or used to provide statistics to your friends.
  - Basically if your mod changes ANY outcomes under a run with same seeds, this should be enabled.
  - `"OnlyWhenNewClansAreInvolved"` reports the run as unmodded if the clan combo is available in unmodded context.
    - Note that even if your clan isn't included in the combo, consider things like Blank Pages should change the outcome based on its text. I'm not sure of its implementation though.
  - Also control the warning dialogs ("modded run won't be uploaded") related to it.

- `BlockCreatingCustomChallenge: true | false` (default: `true`)

  - Block creating a new custom challenge.
  - This feature is, as mentioned in *The Effects of Mods*, bugged in the base game. I'm uncertain if the fix should be included as an unofficial patch with this project.

- `BlockClanExp: true | false` (default: `true`)

  - Block clans from gaining exp and leveling.

- `BlockHellRush: true | false` (default: `true`)

  - Block Hell Rush.

- `BlockAchievements: true | false` (default: `true`)

  - Block achievements.

- `BlockWinStreaks: true | false` (default: `true`)

  - Block all win streaks related features.
  - If enabled, also show "progression blocked" warning label at the clan selection screen, over the win streak label.
  - If disabled, also highlight topped win streaks at the game over screen.

- `BlockCovenantRank: true | false` (default: `true`)

  - Block advancing to a new covenant rank, and prevent the highest covenant rank for each clan combo to be updated.
  - If enabled, also show "progression blocked" warning label at the clan selection screen, over the Covanent rank history label.

- `BlockCompendiumVictoryTracking: true | false` (default: `true`)

  - Block clan combo victories / divine victories to be recorded onto the compendium.

- `BlockCompendiumCardTracking: true | false` (default: `true`)

  - Block cards being encountered / mastered / divine mastered on the compendium.

- `BlockLocalStats: true | false` (default: `true`)

  - Block local run statistics to be updated.
  - If disabled, also highlight topped statistics at the game over screen.

------

The below part contains implementation details of what are actually affected by the extended `modmanifest.json`.


# Current effects of enabling Mod Loader (BepInEx)

** These are Steam-specific behaviors. If you've modded manually on Gog then you've been unintentionally circumventing these changes.

Just merely installing the Mod Loader affects `SteamClientHades.IsModSupportEnabled()` which already changes the behavior regardless of NonCosmetic mods being there or not.

I do not think it's useful for any of mods to have fine-grained control over the following behaviors.

## `bool SteamClientHades.IsModSupportEnabled()`

The method that the game detects whether BepInEx is installed or not is to check if the initial hooks (`winhttp.dll` and `doorstop_config.ini`) are present along the game binary. Actually the game itself manages `doorstop_config.ini` on its code.

- Disable analytics.
  - This is currently no-op. Regardless of mods, analytics in current builds are disabled because `SteamClientHades.ShouldSendAnalytics()` always returns false. I'd guess they stopped tracking them after deciding to stop balance patches.
  - `AnalyticsManager.IncludeAnalyticsTracking()`
- Display warning on feedback window (F8).
  - Affects UI only; no functional changes.
  - Full message: `Warning: MODS ENABLED. Please report bugs in mods to mod authors via Steam Workshop. The Monster Train development team cannot fix bugs in player-made mods.`
  - `FeedbackEntryUI.WindowFunction()`
- Change "Mod Settings" button color on main menu.
  - Affects UI only; no functional changes.
  - `MainMenuScreen.Initialize()`
- Prevent C# exceptions getting reported to their dev machines.
  - `AppManager.SendErrorTracking()`

# Current effects when NonCosmetic mods are turned on

** These are Steam-specific behaviors. If you've modded manually on Gog then you've been unintentionally circumventing these changes.

The NonCosmetic flag is specified on `modmanifest.json` file. `AppManager.PlatformServices` (its Steam implementation to be precise) stores this value on its mod database, and primarily provides it via `AppManager.PlatformServices.AreAnyModsNonCosmetic()`.

## `bool IPlatformServices.AreAnyModsNonCosmetic()`

This is the primary function for querying NonCosmetic mods. All other queries for NonCosmetic mods indirectly calls this function.

- Show warning dialog when you try to start challenges that runs wouldn't be uploaded to the leaderboard.
  - Affects UI only; no functional changes.
  - The actual blocker for uploading leaderboard happens on `RunAggregateData.Reset()` where it marks the run to be modded before uploading to the server. The server itself seems to filter those runs out of the leaderboard.
  - `ChallengeDetailsScreen.CheckIfModsEnabled()`
    Called when you start a new run.
  - `ChallengeDetailsScreen.TrySubmit()`
    Called when you continue a run.
- Block creating a new custom challenge.
  - Supposed to affect UI (show error) and then block the actual process, if it worked as intended.
  - This feature is BUGGED in various ways:
    - Clone Challenge button at `ChallengeDetailsScreen` has no guard at all. You can clone, modify and submit challenges as you wish without any warnings, errors or blockers while modded.
    - New Challenge button at `ChallengeOverviewScreen` has a blocker code but invoked AFTER `ChallengeOverviewScreen.ShowChallengeCreationScreen()` which transits into creating a new challenge. You can see the error dialog appears briefly but it just gets ignored. Then you can create new challenges without problem while modded.
  - `ChallengeOverviewScreen.ApplyScreenInput()`
    The blocker code for New Challenge buttion. It's bugged as described above.
  - `RunSummaryScreen.HandleCreateChallengeButton()`
    The blocker code for Generate Challenge button. It actually works as intended.
- Show the "progression blocked" warning label at the clan selection screen.
  - Affects UI only; no functional changes. The warning label is placed over the win streak / Ascension history label so it occludes them when shown.
  - `ClassSelectionScreen.RefreshProgressionUI()`
- Block Hell Rush.
  - Affects UI (show error) and blocks the actual process.
  - `GameStateManager.StartHellRoyaleRun()`
- Block Achievements.
  - `AchievementManager.InnerTriggerAchievementById()`
- Mark the run as modded for run summaries.
  - NONE of the client code refers to `RunAggregateData.isModded` field as the result of this operation, so this affects nothing on client. It has pretty high chance for server to inspect this value and do stuffs accordingly, like preventing it from getting into the leaderboard.
  - `RunAggregateData.Reset()`

## `bool RunTypeUtil.AllowAdvancedProgression(this RunType runType)`

Returns true iff there aren't any NonCosmetic mods, and `runType == RunType.Class`.

In plain English, checks if the run in question is an unmodded, single player game.

- At the game over screen, highlight topped statistics and win streaks.
  - Affects UI only; no functional changes.
  - `GameOverScreen.Initialize()`
- Track progressions at the end of game: update compendium progressions (card mastery, divine victory), advance Ascension level, update win streaks.
  - `SaveManager.TrackRunResults()`
- Update statistics when finishing a run.
  - `SaveManager.FinishRun()`
- Reset win streak when defeated on a single player game that is not an expert challenge.
  - Includes any behavior that involves deleting currently saved runs. Of course just getting normally defeated on those runs also counts (called by `SaveManager.TrackRunResults()`).
  - `SaveManager.TrackClassDefeat()`

## `WinStreak SaveManager.GetWinStreakDataForActiveRunType()`

Returns the current win streak info iff there aren't any NonCosmetic mods, the currently active save data has run type of  `RunType.Class`, and it's not an expert challenge. Otherwise returns null.

In plain English, provides win streak information only if it's relevant, that is an unmodded, non-expert-challenge single player run. Otherwise it behaves like there are 0 win streaks at the moment.

- At Standard Run menu, show the win streak icon for the Saved Run section if any.
  - Affects UI only; no functional changes.
  - `ContinueGameOption.Initialize()`
- HUD: show the win streak icon if any.
  - Affects UI only; no functional changes.
  - `Hud.RefreshLocalizedUI()`
- Show different warning messages on abandoning/erasing a run, based on there are win streaks or not.
  - Affects UI only; no functional changes.
  - `GameStateManager.LocalizeConfirmationMessageWithWinstreak()`

# Networking

Networking interfaces are especially important on modding because the game server isn't really supposed to interop seriously with modded contents.

There are three noticeable networking interfaces that are active on end-user clients:

- `PubSubManager`: Provides communication in multiplayer rooms.
- `AnalyticsProviderPostHog`: Tracks user's selections through the game -- card picks, upgrades -- and report them. This data is primarily used to balance the game.
- `HadesNetworkCaller`: Any other things are implemented here.

## PubSubManager

This manager handles all the user-to-user communications on multiplayer sessions. Unlike other interfaces which are built upon raw UnityWebRequest it uses [PubNub](https://www.pubnub.com/docs/quickstarts/unity) internally.

I'm going to skip this part because of two reasons:

- There are no serious use case for this. Yes, it DOES get affected by NonCosmetic mods, in a way that you're allowed to enter the daily/custom challenges and broadcast your "modded" score to that multiplayer session. This might hurt someone's feeling (*damn how are they getting higher score while I'm perfectly playing on >300 shards*) but as long as it doesn't get into the leaderboard it really doesn't seem to matter.
- These are a little bit tricky to disable code-wise, though not impossible.

If anyone has a use case for disabling this, i.e. prevent entering multiplayer rooms when modded, please let me know.

## AnalyticsProviderPostHog

As noted on `AnalyticsManager.IncludeAnalyticsTracking()`, this is non-issue since the current build of the game stopped sending analytics to the server for Steam at least, modded or not. I'm unaware if Gog build still has it enabled.

This is the exhaustive list of things tracked by the analytics:

```
  256: 		Analytics.Track("CardPick", analyticsProps);
  394: 		Analytics.Track("ChampionUpgrade", analyticsProps);
   767: 			Analytics.Track("PurgeCard", analyticsProps);
  1797: 		Analytics.Track("PurgeCard", analyticsProps);
  1805: 		Analytics.Track("CardUpgrade", analyticsProps);
  1812: 		Analytics.Track("CardDuplication", analyticsProps);
  1402: 		Analytics.Track("RunStart", analyticsProps);
  488: 		Analytics.Track("MapChoice", analyticsProps);
  493: 		Analytics.Track("MerchantPurchase", analyticsProps);
  126: 		Analytics.Track("BlessingPick", analyticsProps);
  476: 				Analytics.Track("BlessingPick", analyticsProps);
  483: 			Analytics.Track("HealthPick", analyticsProps2);
  492: 				Analytics.Track("GoldPactPick", analyticsProps3);
  499: 			Analytics.Track("BlessingPactPick", analyticsProps4);
  4828: 		Analytics.Track("BattleStart", analyticsProps);
  4850: 		Analytics.Track("BattleEnd", analyticsProps);
  4883: 		Analytics.Track("RunEnd", analyticsProps);
  4890: 		Analytics.Track("CovenantUnlock", analyticsProps);
  4897: 		Analytics.Track("ClassUnlock", analyticsProps);
  4907: 			Analytics.Track("ClassLevelUp", analyticsProps);
  534: 		Analytics.Track("StoryEventStart", analyticsProps);
  550: 			Analytics.Track("StoryChoice", analyticsProps);
  78: 			Analytics.Track("SynthesisPurgeCard", analyticsProps);
  274: 			ShinyShoe.Analytics.Analytics.Track("GameBoot", new AnalyticsProps { 
```

## HadesNetworkCaller

This class handles all the rest of network calls.

I'll exclude calls related to following things because they're pretty much unrelated to most of the mods:

- API key authorization.
- Time syncing.
- Fetching the patch notes.
- Fetching the last update time of challenges.

### Challenges and leaderboard

- `HadesNetworkCaller.GetCurrentChallengeChannel()`: Get PubNub channel name for multiplayer room of the current daily challenge.
  - `GameStateManager.StartGame()`
- `HadesNetworkCaller.JoinBattleChallenge()`: Send request to join regular Hell Rush, and fetch the details.
  - `GameStateManager.JoinBattleChallenge()`
- `HadesNetworkCaller.JoinSharecodeBattleChallenge()`: Send request to join custom Hell Rush, and fetch the details.
  - `GameStateManager.StartSharecodeBattleRun()`
- `HadesNetworkCaller.SaveShareCodeData()`: Submit a new custom challenge.
  - `GameStateManager.SubmitShareChallenge()`
- `HadesNetworkCaller.GetSharedRun()`: Fetch a custom challenge that is not a hell rush.
  - `ChallengeDetailsScreen.FetchChallenge()`
  - `ChallengeOverviewScreen.LookUpChallengeBySharecode()`
  - `GameStateManager.ShowSharedRunDetails()`
- `HadesNetworkCaller.GetCurrentChallenge()`: Fetch a daily challenge.
  - `ChallengeDetailsScreen.FetchChallenge()`
  - `GameStateManager.ShowDailyChallengeDetails()`
- `HadesNetworkCaller.GetChallenge()`: Fetch a custom hell rush.
  - `ChallengeDetailsScreen.FetchChallenge()`
- `HadesNetworkCaller.GetFeaturedCustomChallenges()`: Fetch challenges for custom challenge listing.
  - `FeaturedChallengesUI.LoadFeaturedChallenges()`
- `HadesNetworkCaller.GetChallengeLeaderboard()`: Fetch leaderboard, one page at a time.
  - `LeaderboardUI.FetchLeaderboardEntriesCoroutine()`
  - `RunSummaryScreen.FetchLeaderboardEntriesCoroutine()`
- `HadesNetworkCaller.GetChallengeResults()`: Given a multiplayer game ID, retrieve the list of players who fought for each node on the map.
  - This function is used in multiplayer sessions to show crashed trains on each map node.
  - `SaveManager.CheckChallengeResults()`

### Uploading, fetching and migrating run summaries

You'll probably know what uploading and fetching run summaries mean. There is a third feature related to run summaries: the run history migration. Basically the server holds all of your run history as a backup and restores them on request. Interestingly, the Run history database is already stored and synced by Steam Cloud (you can see it [here](https://store.steampowered.com/account/remotestorageapp/?appid=1102190)), so this feature is unnecessary on Steam.

I'm only guessing if it's mostly used on other platforms (or for cross-platform if it makes sense) where it doesn't support data sync at all OR have a harsh capacity limit so that it cannot contain all the `runHistory*.db` files. Of course it also might have worked for some edge cases where player's Steam Cloud content have been compromised for whatever reason.

All run summaries have a flag that indicate if it is from a modded game. See `RunAggregateData.Reset()` in *The Effects of Mods*.

- `HadesNetworkCaller.GetGameRun()`: Download a run summary from the server.
  - `GameStateManager.ShowRunSummary()`
  - `LeaderboardUI.ShowRunSummary()`
  - `RunHistoryManager.GetGameRun()`
    - Called only when the history migration is not done for that specific record.
  - `RunSummaryScreen.FetchCurrentRunDetails()`
- `HadesNetworkCaller.UploadPlayerSaveFile()`: Upload run summaries to the server.
  - `RunSummaryScreen.HandleRunSummaryUpload()`
  - `SaveManager.SendToNetwork()`
    - Called by various location where you abandon/erase a run save, *or whenever you end a game*. That's right, all game summaries are uploaded to the server regardless of whether you've explicitly requested or not. it's just being private until you decided to "upload it" when it actually becomes public and is assigned a sharecode.
    - This is, as my guess, the primary source of all the server-backed run data like friend stats, run history migration and leaderboard entries.
- `HadesNetworkCaller.GetGameRunIdsToSync()`: Get the list of run IDs stored on the server for migrating run history.
  - `RunHistoryMigrationHelper.DownloadRunList()`
- `HadesNetworkCaller.SetGameRunSyncComplete()`: Report to server that the migration job is finished.
  - `RunHistoryMigrationHelper.TellServerDone()`
- `HadesNetworkCaller.GetGameRuns()`: Download batch of run summaries for a specific page view in run history.
  - The summary data may not be complete. The code mentions the returned data to be "minimal Run summaries". Looks like this function is used to build a list of partial data enough to show the Run history list, where the actual full-fledged summaries are individually downloaded by `HadesNetworkCaller.GetGameRun()`.
  - `RunHistoryManager.GetGameRuns()`

### Player/friend statistics

- `HadesNetworkCaller.GetPlayersWithRuns()`: Given a list of player IDs (which is obtained as your friend list from the platform API), filter out players who have sent runs to the server at least once, basically who played the game.
  - `PlayerStatsManager.DownloadFriendStats()`
- `HadesNetworkCaller.GetPlayerStats`: Given a list of player IDs (filtered by `HadesNetworkCaller.GetPlayersWithRuns()`), obtain their  statistics.
  - `PlayerStatsManager.GetPlayerStats()`

### Debugging

- `HadesNetworkCaller.SaveErrorTracking()`: Report C# exceptions.
  - As mentioned in *The Effects of Mods*, this is already disabled by merely enabling BepInEx.
  - `AppManager.SendErrorTracking()`
- `HadesNetworkCaller.SaveFeedback()`: Submit a feedback (F8).
  - As mentioned in *The Effects of Mods*, there's a proper warning that any modded games are not supported for feedback reports.
  - `SaveManager.SendFeedback()`