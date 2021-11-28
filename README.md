

[toc]

# MonsterTrainManifestExtension

Enable fine-grained control of blocked contents and features for Monster Train mods via `modmanifest.json`.

## Basic usage

1. Add this NuGet package to your project: https://www.nuget.org/packages/MonsterTrainManifestExtension

2. Modify your `modmanifest.json` according to [this documentation](docs/Extending%20modmanifest.json.md). This is an example of modified `modmanifest.json` file for [Arcadian Clan](https://github.com/Tempus/Disciple-Monster-Train) mod:

   ```json
   {
     "ModId": 2205081572,
     "Title": "Arcadian Clan",
     "Tags": [
       "Clan",
       "Monsters",
       "Spells",
       "Artifacts"
     ],
     "CosmeticOnly": false,
   
     "BranchMetagameSave": true,
     "ReportRunSummaryAsModded": "OnlyWhenNewClansAreInvolved",
     "BlockCreatingCustomChallenge": true,
     "BlockHellRush": false,
     "BlockAchievements": false,
     "BlockWinStreaks": false,
     "BlockCovenantRank": false,
     "BlockCompendiumVictoryTracking": false,
     "BlockCompendiumCardTracking": false,
     "BlockLocalStats": false
   }
   ```

3. Make sure to publish your mod with `MonsterTrainManifestExtension.dll` and `Newtonsoft.Json.dll` in `plugins` directory.

## Advanced usage

The `BranchMetagameSave` option isolates the modded progress file. That alone already protects the players from having a save with modded data which might result in an invalid save on the base game synced over the Steam Cloud.

But if you'd like to step further and extensively modify the `MetagameSaveData` class in MT, you can follow these steps to ensure it's safe to modify them:

1. Enable `BranchMetagameSave` option (`"BranchMetagameSave": true`)

2. Add `[BepInEx.BepInDependency("com.nedsociety.monstertrainmanifestextension")]` attribute to your class:

   ```csharp
       [BepInEx.BepInDependency("com.nedsociety.monstertrainmanifestextension")]
       [BepInEx.BepInPlugin("com.my.mtmod", "MyMtMod", "1.0.0.0")]
       public class MyMtMod : BepInEx.BaseUnityPlugin
       {
              // ...
   ```

3. From your `Awake()`, or `Initialize()` if you're using Trainworks, check if `MonsterTrainManifestExtension.MonsterTrainManifestExtension.Instance.Enabled` is true. This property indicates whether you're safe to modify the save file or not.

   ```csharp
   if (MonsterTrainManifestExtension.MonsterTrainManifestExtension.Instance.Enabled)
   {
       // Modify the MetagameSaveData class or instance. 
       // You can access the instance via Trainworks.Managers.ProviderManager.SaveManager.GetMetagameSave()
   }
   ```

4. Done! Your modifications will be safely serialized into an isolated save.

## DISCLAIMER

*Monster Train* is a trademark of *Shiny Shoe LLC*. Unless otherwise stated, the authors and the contributors of this repository is not affiliated with nor endorsed by Shiny Shoe LLC.

## 
