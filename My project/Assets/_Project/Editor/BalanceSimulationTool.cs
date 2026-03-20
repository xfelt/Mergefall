using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using MergeSurvivor.Data;
using MergeSurvivor.Gameplay;
using UnityEditor;
using UnityEngine;

namespace MergeSurvivor.Editor
{
    public static class BalanceSimulationTool
    {
        private enum SimulationTargetProfile
        {
            Standard = 0,
            Onboarding = 1,
            LateGame = 2
        }

        private const int DefaultRunsPerWave = 300;
        private const int DefaultMaxWave = 20;
        private const int DefaultRandomSeed = 1337;
        private const string ReportRelativePath = "Logs/balance-sim.csv";
        private const string SummaryRelativePath = "Logs/balance-sim-summary.md";
        private const string ReportPerProfileTemplate = "Logs/balance-sim-{0}.csv";
        private const string SummaryPerProfileTemplate = "Logs/balance-sim-summary-{0}.md";
        private const string DryRunBaselineTemplate = "Logs/balance-sim-{0}-baseline.csv";
        private const string DryRunAdjustedTemplate = "Logs/balance-sim-{0}-adjusted.csv";
        private const string DryRunCompareTemplate = "Logs/balance-sim-{0}-dryrun-compare.md";
        private const float SuggestionDampening = 0.25f;
        private const float MaxEnemyMultiplierDeltaPerPass = 0.03f;
        private const int MaxFlatDeltaPerPass = 6;
        private const int MaxWaveDeltaPerPass = 2;

        [MenuItem("Merge Survivor/Balance/Run Offline Balance Simulation")]
        public static void RunOfflineBalanceSimulationMenu()
        {
            RunOfflineBalanceSimulation(DefaultRunsPerWave, DefaultMaxWave, DefaultRandomSeed, SimulationTargetProfile.Standard);
        }

        [MenuItem("Merge Survivor/Balance/Run Simulation (Onboarding Profile)")]
        public static void RunOfflineBalanceSimulationOnboardingMenu()
        {
            RunOfflineBalanceSimulation(DefaultRunsPerWave, DefaultMaxWave, DefaultRandomSeed, SimulationTargetProfile.Onboarding);
        }

        [MenuItem("Merge Survivor/Balance/Run Simulation (Late-Game Profile)")]
        public static void RunOfflineBalanceSimulationLateGameMenu()
        {
            RunOfflineBalanceSimulation(DefaultRunsPerWave, DefaultMaxWave, DefaultRandomSeed, SimulationTargetProfile.LateGame);
        }

        [MenuItem("Merge Survivor/Balance/Run Dry-Run With Suggested Deltas")]
        public static void RunDryRunWithSuggestedDeltasMenu()
        {
            RunDryRunWithSuggestedDeltas(DefaultRunsPerWave, DefaultMaxWave, DefaultRandomSeed, SimulationTargetProfile.Standard);
        }

        // CLI:
        // Unity.exe -batchmode -nographics -projectPath "<path>" -executeMethod MergeSurvivor.Editor.BalanceSimulationTool.RunOfflineBalanceSimulationBatch -quit
        public static void RunOfflineBalanceSimulationBatch()
        {
            RunOfflineBalanceSimulation(DefaultRunsPerWave, DefaultMaxWave, DefaultRandomSeed, SimulationTargetProfile.Standard);
        }

        // Optional CLI helper for onboarding tuning:
        // Unity.exe ... -executeMethod MergeSurvivor.Editor.BalanceSimulationTool.RunOfflineBalanceSimulationBatchOnboarding -quit
        public static void RunOfflineBalanceSimulationBatchOnboarding()
        {
            RunOfflineBalanceSimulation(DefaultRunsPerWave, DefaultMaxWave, DefaultRandomSeed, SimulationTargetProfile.Onboarding);
        }

        // Optional CLI helper for late-game tuning:
        // Unity.exe ... -executeMethod MergeSurvivor.Editor.BalanceSimulationTool.RunOfflineBalanceSimulationBatchLateGame -quit
        public static void RunOfflineBalanceSimulationBatchLateGame()
        {
            RunOfflineBalanceSimulation(DefaultRunsPerWave, DefaultMaxWave, DefaultRandomSeed, SimulationTargetProfile.LateGame);
        }

        // CLI helper:
        // Unity.exe ... -executeMethod MergeSurvivor.Editor.BalanceSimulationTool.RunDryRunWithSuggestedDeltasBatch -quit
        public static void RunDryRunWithSuggestedDeltasBatch()
        {
            RunDryRunWithSuggestedDeltas(DefaultRunsPerWave, DefaultMaxWave, DefaultRandomSeed, SimulationTargetProfile.Standard);
        }

        /// <summary>Runs simulation with small params for regression tests. Writes to same Logs/ paths.</summary>
        public static void RunForRegressionTest(int runsPerWave = 5, int maxWave = 3, int randomSeed = 42)
        {
            RunOfflineBalanceSimulation(runsPerWave, maxWave, randomSeed, SimulationTargetProfile.Standard);
        }

        private static void RunOfflineBalanceSimulation(int runsPerWave, int maxWave, int randomSeed, SimulationTargetProfile targetProfile)
        {
            var boardCatalog = BuildBoardCatalog();
            var enemyCatalog = BuildEnemyCatalog();
            var combatConfig = BuildCombatConfig();
            var rows = RunSimulation(boardCatalog, enemyCatalog, combatConfig, runsPerWave, maxWave, randomSeed);
            var csv = BuildCsv(rows);

            var profileSlug = targetProfile.ToString().ToLowerInvariant();
            var reportPathProfile = Path.Combine(Directory.GetCurrentDirectory(), string.Format(CultureInfo.InvariantCulture, ReportPerProfileTemplate, profileSlug));
            var summaryPathProfile = Path.Combine(Directory.GetCurrentDirectory(), string.Format(CultureInfo.InvariantCulture, SummaryPerProfileTemplate, profileSlug));
            var summary = BuildSummary(rows, randomSeed, targetProfile, boardCatalog, enemyCatalog);
            File.WriteAllText(reportPathProfile, csv.ToString(), Encoding.UTF8);
            File.WriteAllText(summaryPathProfile, summary, Encoding.UTF8);

            // Keep "latest run" aliases for convenience.
            var reportPathLatest = Path.Combine(Directory.GetCurrentDirectory(), ReportRelativePath);
            var summaryPathLatest = Path.Combine(Directory.GetCurrentDirectory(), SummaryRelativePath);
            File.WriteAllText(reportPathLatest, csv.ToString(), Encoding.UTF8);
            File.WriteAllText(summaryPathLatest, summary, Encoding.UTF8);

            Debug.Log($"[BalanceSim] Report generated: {reportPathProfile}");
            Debug.Log($"[BalanceSim] Summary generated: {summaryPathProfile}");
            Debug.Log($"[BalanceSim] Latest aliases updated: {reportPathLatest} and {summaryPathLatest}");
        }

        private static void RunDryRunWithSuggestedDeltas(int runsPerWave, int maxWave, int randomSeed, SimulationTargetProfile profile)
        {
            var baseBoardCatalog = BuildBoardCatalog();
            var baseEnemyCatalog = BuildEnemyCatalog();
            var combatConfig = BuildCombatConfig();
            var baselineRows = RunSimulation(baseBoardCatalog, baseEnemyCatalog, combatConfig, runsPerWave, maxWave, randomSeed);
            var suggestions = BuildDeltaSuggestions(baselineRows, profile, baseBoardCatalog);

            var adjustedBoardCatalog = CloneBoardCatalog(baseBoardCatalog);
            var adjustedEnemyCatalog = CloneEnemyCatalog(baseEnemyCatalog);
            ApplyDeltaSuggestions(adjustedBoardCatalog, adjustedEnemyCatalog, suggestions);
            var adjustedRows = RunSimulation(adjustedBoardCatalog, adjustedEnemyCatalog, combatConfig, runsPerWave, maxWave, randomSeed);

            var profileSlug = profile.ToString().ToLowerInvariant();
            var baselineCsvPath = Path.Combine(Directory.GetCurrentDirectory(), string.Format(CultureInfo.InvariantCulture, DryRunBaselineTemplate, profileSlug));
            var adjustedCsvPath = Path.Combine(Directory.GetCurrentDirectory(), string.Format(CultureInfo.InvariantCulture, DryRunAdjustedTemplate, profileSlug));
            var compareMdPath = Path.Combine(Directory.GetCurrentDirectory(), string.Format(CultureInfo.InvariantCulture, DryRunCompareTemplate, profileSlug));

            File.WriteAllText(baselineCsvPath, BuildCsv(baselineRows).ToString(), Encoding.UTF8);
            File.WriteAllText(adjustedCsvPath, BuildCsv(adjustedRows).ToString(), Encoding.UTF8);
            File.WriteAllText(compareMdPath, BuildDryRunCompare(baselineRows, adjustedRows, suggestions, profile), Encoding.UTF8);

            Debug.Log($"[BalanceSimDryRun] Baseline CSV: {baselineCsvPath}");
            Debug.Log($"[BalanceSimDryRun] Adjusted CSV: {adjustedCsvPath}");
            Debug.Log($"[BalanceSimDryRun] Compare report: {compareMdPath}");
        }

        private static List<SimulationRow> RunSimulation(
            BoardCatalog boardCatalog,
            EnemyCatalog enemyCatalog,
            CombatConfig combatConfig,
            int runsPerWave,
            int maxWave,
            int randomSeed)
        {
            var random = new System.Random(randomSeed);
            var rows = new List<SimulationRow>();
            for (var boardIndex = 0; boardIndex < boardCatalog.Count; boardIndex++)
            {
                var board = boardCatalog.Get(boardIndex);
                var archetype = enemyCatalog.GetById(board.EnemyArchetypeId);
                var archetypeName = archetype?.DisplayName ?? "Unknown";

                for (var wave = 1; wave <= maxWave; wave++)
                {
                    var wins = 0;
                    var totalPlayer = 0f;
                    var totalEnemy = 0f;
                    var totalMargin = 0f;

                    for (var i = 0; i < runsPerWave; i++)
                    {
                        var sampledPlayer = SamplePlayerPower(boardIndex, wave, random);
                        var baseEnemy = combatConfig.BaseEnemyStrength + (wave - 1) * combatConfig.PerWaveIncrease;
                        var archetypeBonus = (archetype?.FlatPowerBonus ?? 0) + (wave - 1) * (archetype?.WavePowerBonusPerWave ?? 0);
                        var enemy = Mathf.RoundToInt(baseEnemy * board.EnemyMultiplier) + archetypeBonus;
                        var (effectivePlayer, effectiveEnemy) = CombatCalculator.ApplyEnemyModifiers(sampledPlayer, enemy, archetype?.Modifiers, wave);
                        var result = CombatCalculator.Resolve(effectivePlayer, effectiveEnemy);

                        if (result.Won) wins++;
                        totalPlayer += result.PlayerPower;
                        totalEnemy += result.EnemyPower;
                        totalMargin += (result.PlayerPower - result.EnemyPower);
                    }

                    rows.Add(new SimulationRow
                    {
                        BoardIndex = boardIndex,
                        BoardId = board.Id,
                        BoardName = board.DisplayName,
                        ArchetypeName = archetypeName,
                        Wave = wave,
                        Runs = runsPerWave,
                        WinRate = (float)wins / runsPerWave,
                        AvgPlayerPower = totalPlayer / runsPerWave,
                        AvgEnemyPower = totalEnemy / runsPerWave,
                        AvgMargin = totalMargin / runsPerWave
                    });
                }
            }

            return rows;
        }

        private static StringBuilder BuildCsv(List<SimulationRow> rows)
        {
            var csv = new StringBuilder();
            csv.AppendLine("board_index,board_id,board_name,archetype,wave,runs,win_rate,avg_player_power,avg_enemy_power,avg_margin");
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                csv.AppendLine(
                    $"{row.BoardIndex}," +
                    $"{Escape(row.BoardId)}," +
                    $"{Escape(row.BoardName)}," +
                    $"{Escape(row.ArchetypeName)}," +
                    $"{row.Wave}," +
                    $"{row.Runs}," +
                    $"{row.WinRate.ToString("0.000", CultureInfo.InvariantCulture)}," +
                    $"{row.AvgPlayerPower.ToString("0.0", CultureInfo.InvariantCulture)}," +
                    $"{row.AvgEnemyPower.ToString("0.0", CultureInfo.InvariantCulture)}," +
                    $"{row.AvgMargin.ToString("0.0", CultureInfo.InvariantCulture)}");
            }

            return csv;
        }

        private static int SamplePlayerPower(int boardIndex, int wave, System.Random random)
        {
            // Synthetic player-power model for fast design iteration:
            // - scales with wave
            // - slightly reduced on later boards
            // - includes run-to-run variance to estimate win-rate curves
            var baseline = 38 + wave * 9 - boardIndex * 2;
            var noise = random.Next(-18, 19);
            return Mathf.Max(1, baseline + noise);
        }

        private static BoardCatalog BuildBoardCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<BoardCatalog>();
            catalog.ConfigureRuntime(new List<BoardDefinition>
            {
                new() { Id = "board_garden", DisplayName = "Garden Path", EnemyMultiplier = 1f, EnemyArchetypeId = "grunt" },
                new() { Id = "board_city", DisplayName = "City Crossing", EnemyMultiplier = 1.2f, EnemyArchetypeId = "shield" },
                new() { Id = "board_castle", DisplayName = "Castle Siege", EnemyMultiplier = 1.4f, EnemyArchetypeId = "berserk" }
            });
            return catalog;
        }

        private static EnemyCatalog BuildEnemyCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<EnemyCatalog>();
            catalog.ConfigureRuntime(new List<EnemyArchetypeDefinition>
            {
                new()
                {
                    Id = "grunt",
                    DisplayName = "Grunt Patrol",
                    FlatPowerBonus = 0,
                    WavePowerBonusPerWave = 0,
                    Modifiers = new List<EnemyModifierDefinition>()
                },
                new()
                {
                    Id = "shield",
                    DisplayName = "Shield Squad",
                    FlatPowerBonus = 8,
                    WavePowerBonusPerWave = 2,
                    Modifiers = new List<EnemyModifierDefinition>
                    {
                        new() { Order = 10, ModifierType = EnemyModifierType.ArmorPercent, ModifierValue = 0.2f, AllowStacking = false, StackGroupId = "defense" },
                        new() { Order = 20, ModifierType = EnemyModifierType.HealFlat, ModifierValue = 4f, AllowStacking = true, StackGroupId = "sustain" }
                    }
                },
                new()
                {
                    Id = "berserk",
                    DisplayName = "Berserker Mob",
                    FlatPowerBonus = 14,
                    WavePowerBonusPerWave = 4,
                    Modifiers = new List<EnemyModifierDefinition>
                    {
                        new() { Order = 10, ModifierType = EnemyModifierType.RagePercentPerWave, ModifierValue = 0.08f, AllowStacking = true, StackGroupId = "rage" }
                    }
                }
            });
            return catalog;
        }

        private static CombatConfig BuildCombatConfig()
        {
            return ScriptableObject.CreateInstance<CombatConfig>();
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Contains(",") ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
        }

        private static string BuildSummary(
            List<SimulationRow> rows,
            int seed,
            SimulationTargetProfile profile,
            BoardCatalog boardCatalog,
            EnemyCatalog enemyCatalog)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Balance Simulation Summary");
            sb.AppendLine();
            sb.AppendLine($"- Seed: `{seed}`");
            sb.AppendLine($"- Target profile: `{profile}`");
            sb.AppendLine($"- Wave band examples: wave1 `{FormatBand(GetTargetBand(profile, 1))}` / wave10 `{FormatBand(GetTargetBand(profile, 10))}` / wave20 `{FormatBand(GetTargetBand(profile, 20))}`");
            sb.AppendLine();

            var byBoard = new Dictionary<int, List<SimulationRow>>();
            foreach (var row in rows)
            {
                if (!byBoard.TryGetValue(row.BoardIndex, out var list))
                {
                    list = new List<SimulationRow>();
                    byBoard[row.BoardIndex] = list;
                }

                list.Add(row);
            }

            foreach (var kv in byBoard)
            {
                var boardRows = kv.Value;
                boardRows.Sort((a, b) => a.Wave.CompareTo(b.Wave));
                var first = boardRows[0];
                var avg = 0f;
                var tooEasyCount = 0;
                var tooHardCount = 0;
                for (var i = 0; i < boardRows.Count; i++)
                {
                    var row = boardRows[i];
                    avg += row.WinRate;
                    var (low, high) = GetTargetBand(profile, row.Wave);
                    if (row.WinRate > high) tooEasyCount++;
                    if (row.WinRate < low) tooHardCount++;
                }

                avg /= boardRows.Count;
                var firstTooHardWave = FindFirstWaveBelow(boardRows, profile);
                var firstTooEasyWave = FindFirstWaveAbove(boardRows, profile);
                var recommendation = BuildRecommendation(avg, firstTooHardWave, firstTooEasyWave, profile);

                sb.AppendLine($"## {first.BoardName} ({first.ArchetypeName})");
                sb.AppendLine($"- Average win-rate: `{avg.ToString("0.000", CultureInfo.InvariantCulture)}`");
                sb.AppendLine($"- Waves above target: `{tooEasyCount}` | below target: `{tooHardCount}`");
                sb.AppendLine($"- First wave below band: `{FormatWave(firstTooHardWave)}`");
                sb.AppendLine($"- First wave above band: `{FormatWave(firstTooEasyWave)}`");
                sb.AppendLine($"- Recommendation: {recommendation}");
                sb.AppendLine();
            }

            sb.AppendLine("## Suggested Parameter Deltas");
            sb.AppendLine();
            sb.AppendLine("| Board | Avg WinRate | Target Mid (W10) | Enemy Mult Delta | Archetype Flat Delta | Archetype Wave Delta |");
            sb.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: |");
            var suggestions = BuildDeltaSuggestions(rows, profile, boardCatalog);
            for (var i = 0; i < suggestions.Count; i++)
            {
                var suggestion = suggestions[i];
                sb.AppendLine(
                    $"| {suggestion.BoardName} | " +
                    $"{suggestion.AverageWinRate.ToString("0.000", CultureInfo.InvariantCulture)} | " +
                    $"{suggestion.TargetMid.ToString("0.000", CultureInfo.InvariantCulture)} | " +
                    $"{Signed(suggestion.EnemyMultiplierDelta, "0.000")} | " +
                    $"{Signed(suggestion.ArchetypeFlatDelta)} | " +
                    $"{Signed(suggestion.ArchetypeWaveDelta)} |");
            }

            sb.AppendLine();
            sb.AppendLine("`Enemy Mult Delta`: positive means make harder, negative means make easier.");
            sb.AppendLine($"Dampening `{SuggestionDampening:0.00}` with per-pass caps: enemyMult `{MaxEnemyMultiplierDeltaPerPass:0.000}`, flat `{MaxFlatDeltaPerPass}`, wave `{MaxWaveDeltaPerPass}`.");
            sb.AppendLine("Apply deltas gradually and rerun simulation after each balancing pass.");

            return sb.ToString();
        }

        private static List<DeltaSuggestion> BuildDeltaSuggestions(
            List<SimulationRow> rows,
            SimulationTargetProfile profile,
            BoardCatalog boardCatalog)
        {
            var result = new List<DeltaSuggestion>();
            var byBoard = new Dictionary<int, List<SimulationRow>>();
            foreach (var row in rows)
            {
                if (!byBoard.TryGetValue(row.BoardIndex, out var list))
                {
                    list = new List<SimulationRow>();
                    byBoard[row.BoardIndex] = list;
                }
                list.Add(row);
            }

            var (targetLowW10, targetHighW10) = GetTargetBand(profile, 10);
            var targetMidW10 = (targetLowW10 + targetHighW10) * 0.5f;
            foreach (var kv in byBoard)
            {
                var boardRows = kv.Value;
                var avg = 0f;
                for (var i = 0; i < boardRows.Count; i++) avg += boardRows[i].WinRate;
                avg /= boardRows.Count;
                var gap = avg - targetMidW10;
                var scaledGap = gap * SuggestionDampening;

                result.Add(new DeltaSuggestion
                {
                    BoardIndex = kv.Key,
                    BoardName = boardCatalog.Get(kv.Key)?.DisplayName ?? $"Board {kv.Key}",
                    AverageWinRate = avg,
                    TargetMid = targetMidW10,
                    EnemyMultiplierDelta = Mathf.Clamp(scaledGap * 0.25f, -MaxEnemyMultiplierDeltaPerPass, MaxEnemyMultiplierDeltaPerPass),
                    ArchetypeFlatDelta = Mathf.Clamp(Mathf.RoundToInt(scaledGap * 45f), -MaxFlatDeltaPerPass, MaxFlatDeltaPerPass),
                    ArchetypeWaveDelta = Mathf.Clamp(Mathf.RoundToInt(scaledGap * 12f), -MaxWaveDeltaPerPass, MaxWaveDeltaPerPass)
                });
            }

            return result;
        }

        private static BoardCatalog CloneBoardCatalog(BoardCatalog source)
        {
            var clone = ScriptableObject.CreateInstance<BoardCatalog>();
            var list = new List<BoardDefinition>();
            for (var i = 0; i < source.Count; i++)
            {
                var b = source.Get(i);
                list.Add(new BoardDefinition
                {
                    Id = b.Id,
                    DisplayName = b.DisplayName,
                    EnemyMultiplier = b.EnemyMultiplier,
                    UnlockCostResource = b.UnlockCostResource,
                    SpawnCapacityBonus = b.SpawnCapacityBonus,
                    MergeRewardMultiplier = b.MergeRewardMultiplier,
                    EnemyArchetypeId = b.EnemyArchetypeId
                });
            }
            clone.ConfigureRuntime(list);
            return clone;
        }

        private static EnemyCatalog CloneEnemyCatalog(EnemyCatalog source)
        {
            var clone = ScriptableObject.CreateInstance<EnemyCatalog>();
            var list = new List<EnemyArchetypeDefinition>();
            var ids = new[] { "grunt", "shield", "berserk" };
            for (var i = 0; i < ids.Length; i++)
            {
                var src = source.GetById(ids[i]);
                if (src == null) continue;
                var copiedModifiers = new List<EnemyModifierDefinition>();
                if (src.Modifiers != null)
                {
                    for (var m = 0; m < src.Modifiers.Count; m++)
                    {
                        var mod = src.Modifiers[m];
                        if (mod == null) continue;
                        copiedModifiers.Add(new EnemyModifierDefinition
                        {
                            Order = mod.Order,
                            ModifierType = mod.ModifierType,
                            ModifierValue = mod.ModifierValue,
                            AllowStacking = mod.AllowStacking,
                            StackGroupId = mod.StackGroupId
                        });
                    }
                }
                list.Add(new EnemyArchetypeDefinition
                {
                    Id = src.Id,
                    DisplayName = src.DisplayName,
                    FlatPowerBonus = src.FlatPowerBonus,
                    WavePowerBonusPerWave = src.WavePowerBonusPerWave,
                    Modifiers = copiedModifiers
                });
            }
            clone.ConfigureRuntime(list);
            return clone;
        }

        private static void ApplyDeltaSuggestions(
            BoardCatalog boardCatalog,
            EnemyCatalog enemyCatalog,
            List<DeltaSuggestion> suggestions)
        {
            for (var i = 0; i < suggestions.Count; i++)
            {
                var suggestion = suggestions[i];
                var board = boardCatalog.Get(suggestion.BoardIndex);
                if (board == null)
                {
                    continue;
                }

                board.EnemyMultiplier = Mathf.Max(0.5f, board.EnemyMultiplier + suggestion.EnemyMultiplierDelta);
                var archetype = enemyCatalog.GetById(board.EnemyArchetypeId);
                if (archetype == null)
                {
                    continue;
                }

                archetype.FlatPowerBonus = Mathf.Max(0, archetype.FlatPowerBonus + suggestion.ArchetypeFlatDelta);
                archetype.WavePowerBonusPerWave = Mathf.Max(0, archetype.WavePowerBonusPerWave + suggestion.ArchetypeWaveDelta);
            }
        }

        private static string BuildDryRunCompare(
            List<SimulationRow> baselineRows,
            List<SimulationRow> adjustedRows,
            List<DeltaSuggestion> suggestions,
            SimulationTargetProfile profile)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Balance Dry-Run Compare");
            sb.AppendLine();
            sb.AppendLine($"- Target profile: `{profile}`");
            sb.AppendLine("- Shows baseline vs adjusted after applying suggested deltas.");
            sb.AppendLine($"- Dampening: `{SuggestionDampening.ToString("0.00", CultureInfo.InvariantCulture)}` | Caps: enemyMult `{MaxEnemyMultiplierDeltaPerPass.ToString("0.000", CultureInfo.InvariantCulture)}`, flat `{MaxFlatDeltaPerPass}`, wave `{MaxWaveDeltaPerPass}`");
            sb.AppendLine();
            sb.AppendLine("| Board | Baseline Avg WR | Adjusted Avg WR | Delta WR | Applied EnemyMult Delta | Applied Flat Delta | Applied Wave Delta |");
            sb.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: |");

            var baselineByBoard = GroupAvgWinRate(baselineRows);
            var adjustedByBoard = GroupAvgWinRate(adjustedRows);
            for (var i = 0; i < suggestions.Count; i++)
            {
                var s = suggestions[i];
                var baseAvg = baselineByBoard.TryGetValue(s.BoardIndex, out var b) ? b : 0f;
                var adjAvg = adjustedByBoard.TryGetValue(s.BoardIndex, out var a) ? a : 0f;
                var delta = adjAvg - baseAvg;
                sb.AppendLine(
                    $"| {s.BoardName} | " +
                    $"{baseAvg.ToString("0.000", CultureInfo.InvariantCulture)} | " +
                    $"{adjAvg.ToString("0.000", CultureInfo.InvariantCulture)} | " +
                    $"{Signed(delta, "0.000")} | " +
                    $"{Signed(s.EnemyMultiplierDelta, "0.000")} | " +
                    $"{Signed(s.ArchetypeFlatDelta)} | " +
                    $"{Signed(s.ArchetypeWaveDelta)} |");
            }

            return sb.ToString();
        }

        private static Dictionary<int, float> GroupAvgWinRate(List<SimulationRow> rows)
        {
            var sums = new Dictionary<int, float>();
            var counts = new Dictionary<int, int>();
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                sums[row.BoardIndex] = sums.TryGetValue(row.BoardIndex, out var current) ? current + row.WinRate : row.WinRate;
                counts[row.BoardIndex] = counts.TryGetValue(row.BoardIndex, out var c) ? c + 1 : 1;
            }

            var averages = new Dictionary<int, float>();
            foreach (var kv in sums)
            {
                averages[kv.Key] = kv.Value / counts[kv.Key];
            }

            return averages;
        }

        private static int FindFirstWaveBelow(List<SimulationRow> rows, SimulationTargetProfile profile)
        {
            for (var i = 0; i < rows.Count; i++)
            {
                var (low, _) = GetTargetBand(profile, rows[i].Wave);
                if (rows[i].WinRate < low)
                {
                    return rows[i].Wave;
                }
            }

            return -1;
        }

        private static int FindFirstWaveAbove(List<SimulationRow> rows, SimulationTargetProfile profile)
        {
            for (var i = 0; i < rows.Count; i++)
            {
                var (_, high) = GetTargetBand(profile, rows[i].Wave);
                if (rows[i].WinRate > high)
                {
                    return rows[i].Wave;
                }
            }

            return -1;
        }

        private static string FormatWave(int wave) => wave < 0 ? "none" : wave.ToString(CultureInfo.InvariantCulture);

        private static string BuildRecommendation(float avgWinRate, int firstTooHardWave, int firstTooEasyWave, SimulationTargetProfile profile)
        {
            var (midLow, midHigh) = GetTargetBand(profile, 10);
            if (avgWinRate > midHigh)
            {
                return "Too easy overall. Increase enemy multiplier by ~0.05 or add +5 flat enemy power.";
            }

            if (avgWinRate < midLow)
            {
                return "Too hard overall. Decrease enemy multiplier by ~0.05 or reduce archetype flat bonus.";
            }

            if (firstTooHardWave > 0)
            {
                return $"Band is OK early but drops at wave {firstTooHardWave}. Reduce per-wave enemy growth slightly.";
            }

            if (firstTooEasyWave > 0)
            {
                return $"Band is OK early but too easy at wave {firstTooEasyWave}. Raise per-wave growth or archetype rage.";
            }

            return "Within target band. Keep parameters and test with real player-power telemetry next.";
        }

        private static (float low, float high) GetTargetBand(SimulationTargetProfile profile, int wave)
        {
            // Curves intentionally simple and readable for designers.
            return profile switch
            {
                SimulationTargetProfile.Onboarding => wave switch
                {
                    <= 5 => (0.65f, 0.80f),
                    <= 10 => (0.55f, 0.70f),
                    _ => (0.45f, 0.60f)
                },
                SimulationTargetProfile.LateGame => wave switch
                {
                    <= 5 => (0.45f, 0.60f),
                    <= 10 => (0.40f, 0.55f),
                    _ => (0.30f, 0.45f)
                },
                _ => (0.45f, 0.60f)
            };
        }

        private static string FormatBand((float low, float high) band)
        {
            return $"{band.low.ToString("0.00", CultureInfo.InvariantCulture)}-{band.high.ToString("0.00", CultureInfo.InvariantCulture)}";
        }

        private static string Signed(int value)
        {
            return value >= 0 ? $"+{value}" : value.ToString(CultureInfo.InvariantCulture);
        }

        private static string Signed(float value, string format)
        {
            var text = value.ToString(format, CultureInfo.InvariantCulture);
            return value >= 0f ? $"+{text}" : text;
        }

        private struct SimulationRow
        {
            public int BoardIndex;
            public string BoardId;
            public string BoardName;
            public string ArchetypeName;
            public int Wave;
            public int Runs;
            public float WinRate;
            public float AvgPlayerPower;
            public float AvgEnemyPower;
            public float AvgMargin;
        }

        private struct DeltaSuggestion
        {
            public int BoardIndex;
            public string BoardName;
            public float AverageWinRate;
            public float TargetMid;
            public float EnemyMultiplierDelta;
            public int ArchetypeFlatDelta;
            public int ArchetypeWaveDelta;
        }
    }
}
