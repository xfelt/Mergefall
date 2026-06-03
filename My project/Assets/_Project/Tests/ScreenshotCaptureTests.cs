using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MergeSurvivor.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MergeSurvivor.Tests
{
    // Drives the runtime UI into each gameplay state and renders REAL frames to PNGs.
    // Output: <projectRoot>/Screenshots/*.png . Canvas is ScreenSpaceOverlay at runtime;
    // we flip it to ScreenSpaceCamera at capture time so a dedicated camera can render it
    // to a RenderTexture (ScreenCapture/WaitForEndOfFrame are unavailable in batch).
    public sealed class ScreenshotCaptureTests
    {
        private const int W = 1080;
        private const int H = 1920;

        private PrototypeBootstrap _bootstrap;
        private Camera _captureCam;
        private RenderTexture _rt;
        private string _outDir;

        [UnityTest]
        public IEnumerator Capture_GameplayScreenshots()
        {
            _outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Screenshots"));
            Directory.CreateDirectory(_outDir);

            // Start from a first-run state so the onboarding/title overlay is available,
            // but keep the tutorial coroutine from auto-driving the UI during later shots.
            PlayerPrefs.DeleteKey("merge_survivor_onboarding_done");
            PlayerPrefs.SetInt("merge_survivor_tutorial_done", 1);
            PlayerPrefs.Save();

            // Prefer the authored Launch scene: its PrototypeBootstrap has the art
            // catalogs (gem sprites, backgrounds, icons, enemy portraits) wired. Fall
            // back to a bare instance if the scene can't be loaded.
            TryLoadLaunchScene();
            yield return null;
            yield return null;
            _bootstrap = Object.FindFirstObjectByType<PrototypeBootstrap>();
            if (_bootstrap == null)
            {
                var go = new GameObject("CaptureBootstrap");
                _bootstrap = go.AddComponent<PrototypeBootstrap>();
                yield return null;
                yield return null;
            }

            SetupCaptureCamera();

            // 1) Title / how-to-play overlay (first-run onboarding).
            var onboarding = Field<GameObject>("_onboardingOverlay");
            if (onboarding != null) onboarding.SetActive(true);
            yield return Shoot("01_title");
            if (onboarding != null) onboarding.SetActive(false);

            // 2) Hub: pre-run home with the Start Run call to action.
            Invoke("RefreshRunStateUI");
            Invoke("RefreshAll");
            yield return Shoot("02_hub");

            // 3) Active run: board seeded with a real spawn.
            Invoke("StartRunInternal");
            yield return null;
            yield return Shoot("03_board_run");

            // 4) Merge progression: stage all six tiers so the art ladder is visible.
            StageTierLadder();
            Invoke("RefreshAll");
            yield return null;
            yield return Shoot("04_merge_tiers");

            // 5) Fight: capture the animated power-race mid-resolution.
            Invoke("OnFight");
            yield return new WaitForSecondsRealtime(1.0f);
            yield return Shoot("05_fight_race");

            // 6) Fight verdict + rewards panel.
            yield return new WaitForSecondsRealtime(2.2f);
            yield return Shoot("06_fight_result");

            // Dismiss the result panel before opening the hub.
            var fightPanel = Field<GameObject>("_fightResultPanel");
            if (fightPanel != null) fightPanel.SetActive(false);

            // 7) Meta Hub / Merchant Tent upgrades.
            Invoke("OpenMeta");
            yield return null;
            yield return Shoot("07_meta_hub");
            var meta = Field<GameObject>("_metaPanel");
            if (meta != null) meta.SetActive(false);

            // 8) Coached tutorial overlay (staged manually, not the live coroutine).
            var tut = Field<GameObject>("_tutorialOverlay");
            var tutText = Field<TMP_Text>("_tutorialText");
            var tutStep = Field<TMP_Text>("_tutorialStepLabel");
            if (tut != null)
            {
                tut.SetActive(true);
                if (tutStep != null) tutStep.text = "Step 1 / 3";
                if (tutText != null) tutText.text = "Drag a crystal onto a matching one. Line up three of a kind to merge!";
                yield return null;
                yield return Shoot("08_tutorial");
                tut.SetActive(false);
            }

            // List what we produced so the result message is useful.
            var files = Directory.GetFiles(_outDir, "*.png");
            Debug.Log($"[Screenshots] Wrote {files.Length} files to {_outDir}");
            foreach (var f in files) Debug.Log($"[Screenshots]  {Path.GetFileName(f)}");

            Assert.GreaterOrEqual(files.Length, 6, "Expected at least 6 screenshots.");
        }

        // Load Launch.unity in play mode via reflection so this test assembly doesn't
        // need a UnityEditor reference. No-op (caught) outside the editor.
        private static void TryLoadLaunchScene()
        {
            try
            {
                var esmType = System.Type.GetType(
                    "UnityEditor.SceneManagement.EditorSceneManager, UnityEditor");
                if (esmType == null) return;
                var lsp = new UnityEngine.SceneManagement.LoadSceneParameters(
                    UnityEngine.SceneManagement.LoadSceneMode.Single);
                var m = esmType.GetMethod("LoadSceneInPlayMode", new[]
                {
                    typeof(string), typeof(UnityEngine.SceneManagement.LoadSceneParameters)
                });
                m?.Invoke(null, new object[]
                {
                    "Assets/_Project/Gameplay/Scenes/Launch.unity", lsp
                });
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Screenshots] Launch scene load failed, using bare bootstrap: {e.Message}");
            }
        }

        private void SetupCaptureCamera()
        {
            var camGo = new GameObject("ScreenshotCaptureCamera");
            _captureCam = camGo.AddComponent<Camera>();
            _captureCam.clearFlags = CameraClearFlags.SolidColor;
            _captureCam.backgroundColor = new Color(0.06f, 0.05f, 0.04f, 1f);
            _captureCam.orthographic = true;
            _captureCam.nearClipPlane = 0.1f;
            _captureCam.farClipPlane = 5000f;
            _captureCam.cullingMask = ~0;
            _captureCam.transform.position = new Vector3(0, 0, -100f);

            _rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
            _rt.Create();
            _captureCam.targetTexture = _rt;
        }

        private void RouteCanvasesToCaptureCamera()
        {
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = _captureCam;
                canvas.planeDistance = 50f;
            }
        }

        private IEnumerator Shoot(string name)
        {
            RouteCanvasesToCaptureCamera();
            Canvas.ForceUpdateCanvases();
            yield return null; // let layout + canvas batches rebuild against the RT size

            _captureCam.Render();

            var prev = RenderTexture.active;
            RenderTexture.active = _rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;

            var path = Path.Combine(_outDir, name + ".png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.Destroy(tex);
            Debug.Log($"[Screenshots] Captured {name} -> {path}");
        }

        private void StageTierLadder()
        {
            var session = Field<object>("_session");
            var board = session.GetType().GetProperty("Board").GetValue(session);
            var boardType = board.GetType();
            var set = boardType.GetMethod("Set");
            var widthProp = boardType.GetProperty("Width");
            var heightProp = boardType.GetProperty("Height");
            int width = (int)widthProp.GetValue(board);
            int height = (int)heightProp.GetValue(board);

            // Clear, then place a tidy ladder of the six tiers plus a few duplicates
            // to read as a real mid-merge board.
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                set.Invoke(board, new object[] { x, y, null });

            string[] layout =
            {
                "pawn_t6", "pawn_t5", "pawn_t4", "pawn_t3",
                "pawn_t2", "pawn_t2", "pawn_t1", "pawn_t1",
                "pawn_t1", "pawn_t3", "pawn_t3", "pawn_t2",
                "pawn_t1", "pawn_t1", "pawn_t2", "pawn_t1",
            };
            for (var i = 0; i < layout.Length; i++)
            {
                int x = i % width;
                int y = i / width;
                if (y >= height) break;
                set.Invoke(board, new object[] { x, y, layout[i] });
            }
        }

        // --- reflection helpers ---

        private T Field<T>(string name) where T : class
        {
            var f = typeof(PrototypeBootstrap).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            return f?.GetValue(_bootstrap) as T;
        }

        private void Invoke(string name)
        {
            var m = typeof(PrototypeBootstrap).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
            m?.Invoke(_bootstrap, null);
        }

        [TearDown]
        public void Cleanup()
        {
            if (_captureCam != null) _captureCam.targetTexture = null;
            if (_rt != null) { _rt.Release(); Object.Destroy(_rt); }
            if (_captureCam != null) Object.Destroy(_captureCam.gameObject);
            if (_bootstrap != null) Object.Destroy(_bootstrap.gameObject);
        }
    }
}
