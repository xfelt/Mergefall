# Merge Survivor — Minimal CI Checklist

Use this for pre-merge or scheduled CI to ensure the project compiles, tests pass, and (optionally) balance simulation runs.

## 1. Unity batch compile

- **Goal**: Confirm no compile errors and no broken assembly references.
- **Command** (replace `<UNITY_EXE>` and `<PROJECT_PATH>` with your Unity path and `My project` path):
  ```text
  "<UNITY_EXE>" -batchmode -nographics -projectPath "<PROJECT_PATH>" -quit
  ```
- **Success**: Process exits with code 0 and no "Compilation failed" in the log.

## 2. PlayMode tests

- **Goal**: All PlayMode tests pass (bootstrap, merge, fight, board unlock, modifiers, save/load).
- **Command**:
  ```text
  "<UNITY_EXE>" -batchmode -nographics -projectPath "<PROJECT_PATH>" -runTests -testPlatform PlayMode -testResults "TestResults/PlayMode.xml" -quit
  ```
- **Success**: Test result XML shows all tests passed (or parse exit code / result file in your pipeline).

## 3. Editor tests (balance regression)

- **Goal**: Balance simulation produces valid CSV and summary (regression test).
- **Command**:
  ```text
  "<UNITY_EXE>" -batchmode -nographics -projectPath "<PROJECT_PATH>" -runTests -testPlatform EditMode -testFilter "MergeSurvivor.Editor.BalanceSimulationEditorTests" -testResults "TestResults/EditMode.xml" -quit
  ```
- **Success**: `BalanceSimulationEditorTests.BalanceSimulator_ProducesValidCsvAndSummary` passes; `Logs/balance-sim.csv` and `Logs/balance-sim-summary.md` exist.

## 4. Optional: balance dry-run

- **Goal**: Full balance dry-run with suggested deltas (slower; use on schedule or before release).
- **Command**:
  ```text
  "<UNITY_EXE>" -batchmode -nographics -projectPath "<PROJECT_PATH>" -executeMethod MergeSurvivor.Editor.BalanceSimulationTool.RunDryRunWithSuggestedDeltasBatch -quit
  ```
- **Success**: Process exits 0; `Logs/` contains dry-run outputs.

## 5. Optional: runtime smoke

- **Goal**: Bootstrap setup + BuildUi + RefreshAll run without exception.
- **Command**:
  ```text
  "<UNITY_EXE>" -batchmode -nographics -projectPath "<PROJECT_PATH>" -executeMethod MergeSurvivor.Editor.RuntimeSmokeCheck.Run -quit
  ```
- **Success**: No exception; log contains `[SmokeCheck] Bootstrap runtime path executed without exception.`

---

## Where to document this

- **This file** (`CI_CHECKLIST.md`) is the single place for the minimal CI steps.
- Reference it from:
  - **README_MergeSurvivor.md** — "How to run and test" and "CI" mention `CI_CHECKLIST.md`.
  - Your CI config (GitHub Actions, Jenkins, etc.): add a job that runs the commands above with your Unity installation path and project path.

## Suggested CI order

1. Batch compile (fast; fail fast).
2. EditMode tests (balance regression).
3. PlayMode tests.
4. Optionally: smoke, then balance dry-run (if desired for nightly/release).
