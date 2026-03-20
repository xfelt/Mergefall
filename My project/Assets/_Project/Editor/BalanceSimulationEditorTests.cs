using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace MergeSurvivor.Editor
{
    public static class BalanceSimulationEditorTests
    {
        private static string LogsDir => Path.Combine(Application.dataPath, "..", "Logs");
        private static string CsvPath => Path.Combine(LogsDir, "balance-sim.csv");
        private static string SummaryPath => Path.Combine(LogsDir, "balance-sim-summary.md");

        [Test]
        public static void BalanceSimulator_ProducesValidCsvAndSummary()
        {
            BalanceSimulationTool.RunForRegressionTest(runsPerWave: 5, maxWave: 3, randomSeed: 42);

            var csvPath = Path.GetFullPath(CsvPath);
            var summaryPath = Path.GetFullPath(SummaryPath);
            Assert.IsTrue(File.Exists(csvPath), $"CSV should exist at {csvPath}");
            Assert.IsTrue(File.Exists(summaryPath), $"Summary should exist at {summaryPath}");

            var csv = File.ReadAllText(csvPath);
            Assert.IsFalse(string.IsNullOrWhiteSpace(csv), "CSV should not be empty.");
            StringAssert.Contains("board_index,board_id,board_name,archetype,wave,runs,win_rate,avg_player_power,avg_enemy_power,avg_margin", csv, "CSV should have expected header.");
            var csvLines = csv.Trim().Split('\n');
            Assert.Greater(csvLines.Length, 1, "CSV should have header + at least one data row.");
            var firstData = csvLines[1].Split(',');
            Assert.GreaterOrEqual(firstData.Length, 6, "Data row should have enough columns.");
            Assert.IsTrue(int.TryParse(firstData[0], out _), "board_index should be numeric.");
            Assert.IsTrue(int.TryParse(firstData[4], out _), "wave should be numeric.");
            Assert.IsTrue(float.TryParse(firstData[6], out _), "win_rate should be numeric.");

            var summary = File.ReadAllText(summaryPath);
            Assert.IsFalse(string.IsNullOrWhiteSpace(summary), "Summary should not be empty.");
            StringAssert.Contains("Balance Simulation Summary", summary, "Summary should contain title.");
            StringAssert.Contains("42", summary, "Summary should contain seed.");
        }
    }
}
