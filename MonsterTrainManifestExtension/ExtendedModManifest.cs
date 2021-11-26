using HarmonyLib;
using Newtonsoft.Json.Linq;
using ShinyShoe;
using System;
using System.IO;
using System.Linq;

namespace MonsterTrainManifestExtension
{
    public enum ReportRunSummaryAsModdedEnum
    {
        Never,
        OnlyWhenNewClansAreInvolved,
        Always
    }

    public class ExtendedModManifest
    {
        public ModDefinition baseFields; // Reduced modmanifest.json fields

        public class ExtendedFields
        {
            public bool BranchMetagameSave;
            public ReportRunSummaryAsModdedEnum ReportRunSummaryAsModded;
            public bool BlockCreatingCustomChallenge;
            public bool BlockHellRush;
            public bool BlockAchievements;
            public bool BlockWinStreaks;
            public bool BlockCovenantRank;
            public bool BlockCompendiumVictoryTracking;
            public bool BlockCompendiumCardTracking;
            public bool BlockLocalStats;

            static T JsonMapValueOrDefault<T>(JObject content, string key, T defaultIfMissing = default)
            {
                var match = content?[key];
                if (match == null)
                    return defaultIfMissing;
                else
                {
                    T x = match.ToObject<T>();
                    if (x == null)
                        return defaultIfMissing;
                    return x;
                }

            }

            static T JsonMapEnumOrDefault<T>(JObject content, string key, T defaultIfMissing = default) where T : struct
            {
                var rawstr = JsonMapValueOrDefault<string>(content, key, null);
                if (rawstr == null)
                    return defaultIfMissing;
                else if (Enum.TryParse<T>(rawstr, out var result))
                    return result;
                else
                {
                    MonsterTrainManifestExtension.Logger.LogError($"invalid enum value: {rawstr}");
                    return defaultIfMissing;
                }
            }

            public static ExtendedFields operator &(ExtendedFields lhs, ExtendedFields rhs)
            {
                return new ExtendedFields()
                {
                    BranchMetagameSave = lhs.BranchMetagameSave || rhs.BranchMetagameSave,
                    ReportRunSummaryAsModded = (ReportRunSummaryAsModdedEnum)Math.Max(
                        (int)lhs.ReportRunSummaryAsModded, (int)rhs.ReportRunSummaryAsModded
                    ),
                    BlockCreatingCustomChallenge = lhs.BlockCreatingCustomChallenge || rhs.BlockCreatingCustomChallenge,
                    BlockHellRush = lhs.BlockHellRush || rhs.BlockHellRush,
                    BlockAchievements = lhs.BlockAchievements || rhs.BlockAchievements,
                    BlockWinStreaks = lhs.BlockWinStreaks || rhs.BlockWinStreaks,
                    BlockCovenantRank = lhs.BlockCovenantRank || rhs.BlockCovenantRank,
                    BlockCompendiumVictoryTracking = lhs.BlockCompendiumVictoryTracking || rhs.BlockCompendiumVictoryTracking,
                    BlockCompendiumCardTracking = lhs.BlockCompendiumCardTracking || rhs.BlockCompendiumCardTracking,
                    BlockLocalStats = lhs.BlockLocalStats || rhs.BlockLocalStats,
                };
            }

            public static ExtendedFields DefaultForCosmeticOnly()
            {
                return new ExtendedFields()
                {
                    BranchMetagameSave = false,
                    ReportRunSummaryAsModded = ReportRunSummaryAsModdedEnum.Never,
                    BlockCreatingCustomChallenge = false,
                    BlockHellRush = false,
                    BlockAchievements = false,
                    BlockWinStreaks = false,
                    BlockCovenantRank = false,
                    BlockCompendiumVictoryTracking = false,
                    BlockCompendiumCardTracking = false,
                    BlockLocalStats = false
                };
            }

            public static ExtendedFields DefaultForNonCosmetic()
            {
                return new ExtendedFields()
                {
                    BranchMetagameSave = false,
                    ReportRunSummaryAsModded = ReportRunSummaryAsModdedEnum.Always,
                    BlockCreatingCustomChallenge = true,
                    BlockHellRush = true,
                    BlockAchievements = true,
                    BlockWinStreaks = true,
                    BlockCovenantRank = true,
                    BlockCompendiumVictoryTracking = true,
                    BlockCompendiumCardTracking = true,
                    BlockLocalStats = true
                };
            }

            public static ExtendedFields FromJObject(JObject content)
            {
                return new ExtendedFields()
                {
                    BranchMetagameSave = JsonMapValueOrDefault(content, "BranchMetagameSave", false),
                    ReportRunSummaryAsModded = JsonMapEnumOrDefault(content, "ReportRunSummaryAsModded", ReportRunSummaryAsModdedEnum.Always),
                    BlockCreatingCustomChallenge = JsonMapValueOrDefault(content, "BlockCreatingCustomChallenge", true),
                    BlockHellRush = JsonMapValueOrDefault(content, "BlockHellRush", true),
                    BlockAchievements = JsonMapValueOrDefault(content, "BlockAchievements", true),
                    BlockWinStreaks = JsonMapValueOrDefault(content, "BlockWinStreaks", true),
                    BlockCovenantRank = JsonMapValueOrDefault(content, "BlockCovenantRank", true),
                    BlockCompendiumVictoryTracking = JsonMapValueOrDefault(content, "BlockCompendiumVictoryTracking", true),
                    BlockCompendiumCardTracking = JsonMapValueOrDefault(content, "BlockCompendiumCardTracking", true),
                    BlockLocalStats = JsonMapValueOrDefault(content, "BlockLocalStats", true)
                };
            }

            public string AsDetailedString()
            {
                return $"- BranchMetagameSave: {BranchMetagameSave}\n" +
                       $"- ReportRunSummaryAsModded: {ReportRunSummaryAsModded}\n" +
                       $"- BlockCreatingCustomChallenge: {BlockCreatingCustomChallenge}\n" +
                       $"- BlockHellRush: {BlockHellRush}\n" +
                       $"- BlockAchievements: {BlockAchievements}\n" +
                       $"- BlockWinStreaks: {BlockWinStreaks}\n" +
                       $"- BlockCovenantRank: {BlockCovenantRank}\n" +
                       $"- BlockCompendiumVictoryTracking: {BlockCompendiumVictoryTracking}\n" +
                       $"- BlockCompendiumCardTracking: {BlockCompendiumCardTracking}\n" +
                       $"- BlockLocalStats: {BlockLocalStats}";
            }
        }

        public ExtendedFields extendedFields;
    }

    static class ExtendedModManifestUtility
    {
        const ulong BEPINEX_STEAM_WORKSHOP_ID = 2187468759u;
        static string steamWorkshopRootPath = null;

        static string GetSteamWorkshopRootPath(SteamClientHades steamClientHades)
        {
            if (steamWorkshopRootPath == null)
            {
                var subscribedWorkshopItems = Traverse.Create(steamClientHades)
                                              .Method("GetSubscribedWorkshopItems")
                                              .GetValue<Steamworks.PublishedFileId_t[]>();

                var bepinexItem = (from subscribedWorkshopItem in subscribedWorkshopItems
                                   where subscribedWorkshopItem.m_PublishedFileId == BEPINEX_STEAM_WORKSHOP_ID
                                   select subscribedWorkshopItem).First();

                var bepinexRootPath = Traverse.Create(steamClientHades)
                                      .Method("GetRootPathForWorkshopItem", bepinexItem)
                                      .GetValue<string>();

                steamWorkshopRootPath = Directory.GetParent(bepinexRootPath).FullName;
            }
            return steamWorkshopRootPath;
        }

        public static ExtendedModManifest AsExtendedModManifest(this ModDefinition modDefinition, SteamClientHades steamClientHades)
        {
            // In case of CosmeticOnly mods we don't inspect them deeply
            if (modDefinition.CosmeticOnly)
            {
                return new ExtendedModManifest
                {
                    baseFields = modDefinition,
                    extendedFields = ExtendedModManifest.ExtendedFields.DefaultForCosmeticOnly()
                };
            }

            var manifestPath = Path.Combine(
                GetSteamWorkshopRootPath(steamClientHades), modDefinition.ModIdentifier.ToString(), "modmanifest.json"
            );

            try
            {
                using (var streamreader = new StreamReader(manifestPath))
                using (var jsonreader = new Newtonsoft.Json.JsonTextReader(streamreader))
                {
                    var manifestContent = new Newtonsoft.Json.JsonSerializer().Deserialize(jsonreader) as JObject;
                    return new ExtendedModManifest
                    {
                        baseFields = modDefinition,
                        extendedFields = ExtendedModManifest.ExtendedFields.FromJObject(manifestContent)
                    };
                }
            }
            catch (Exception e) when (e is FileNotFoundException || e is DirectoryNotFoundException)
            {
                // A mod without a proper modmanifest.json. Apply default for extendedFields.
                return new ExtendedModManifest
                {
                    baseFields = modDefinition,
                    extendedFields = ExtendedModManifest.ExtendedFields.DefaultForNonCosmetic()
                };
            }
            catch (Exception e)
            {
                // Other parsing errors?
                MonsterTrainManifestExtension.Logger.LogError(
                    $"Error while parsing modmanifest.json for mod {modDefinition.ModIdentifier}:\n{e}"
                );
                return new ExtendedModManifest
                {
                    baseFields = modDefinition,
                    extendedFields = ExtendedModManifest.ExtendedFields.DefaultForNonCosmetic()
                };
            }
        }
    }
}
