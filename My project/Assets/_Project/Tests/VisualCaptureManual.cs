using System.Collections;
using System.IO;
using System.Reflection;
using MergeSurvivor.Gameplay;
using MergeSurvivor.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MergeSurvivor.Tests
{
    // Not part of the regression suite: run explicitly via
    //   -testFilter "MergeSurvivor.Tests.VisualCaptureManual.*"
    // to render the real Launch UI and dump PNGs to Logs/screenshots for visual review.
    public sealed class VisualCaptureManual
    {
        // Overridable via env vars CAPTURE_W / CAPTURE_H (default portrait phone 1080x1920).
        private static readonly int W = EnvInt("CAPTURE_W", 1080);
        private static readonly int H = EnvInt("CAPTURE_H", 1920);

        private static int EnvInt(string key, int fallback)
        {
            var v = System.Environment.GetEnvironmentVariable(key);
            return int.TryParse(v, out var n) && n > 0 ? n : fallback;
        }

        private static string ShotDir =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "screenshots"));

        private Camera _cam;
        private RenderTexture _rt;

        [UnityTest]
        [Explicit("Manual visual-capture harness; excluded from the default suite. " +
                  "Run via -testFilter \"MergeSurvivor.Tests.VisualCaptureManual.CaptureKeyScreens\".")]
        public IEnumerator CaptureKeyScreens()
        {
            Directory.CreateDirectory(ShotDir);
            PlayerPrefs.DeleteKey("merge_survivor_onboarding_done");
            PlayerPrefs.DeleteKey("merge_survivor_tutorial_done");
            PlayerPrefs.Save();

            // Load the REAL Launch scene so the scene's configured PrototypeBootstrap runs
            // with its Inspector-assigned sprites/catalogs (a bare AddComponent would have none).
            UnityEngine.SceneManagement.SceneManager.LoadScene("Launch");
            yield return null;
            yield return null;
            yield return null;
            var bootstrap = Object.FindFirstObjectByType<PrototypeBootstrap>();
            Assert.IsNotNull(bootstrap, "Launch scene bootstrap not found.");

            // Audio evidence: confirm the real scene wired music + SFX clips and music is playing.
            var music = GetField<AudioSource>(bootstrap, "_musicAudio");
            var sfxMerge = GetField<AudioClip>(bootstrap, "sfxMerge");
            var sfxWin = GetField<AudioClip>(bootstrap, "sfxFightWin");
            var sfxLoss = GetField<AudioClip>(bootstrap, "sfxFightLoss");
            var sfxStart = GetField<AudioClip>(bootstrap, "sfxFightStart");
            Debug.Log($"[Capture][Audio] music={(music != null && music.clip != null ? music.clip.name : "null")} " +
                      $"isPlaying={(music != null && music.isPlaying)} merge={(sfxMerge != null)} win={(sfxWin != null)} " +
                      $"loss={(sfxLoss != null)} start={(sfxStart != null)}");

            // ScreenCapture.CaptureScreenshot does not work in batchmode (no display swap),
            // so render the Overlay canvas through an explicit camera into a RenderTexture.
            SetupOffscreenCamera();
            yield return null;

            // 01 - first-run onboarding overlay
            yield return Shot("01_onboarding");

            // Begin Journey -> interactive tutorial (step 1 highlights two matching crystals)
            Find<Button>("Btn_Begin Journey")?.onClick.Invoke();
            yield return null;
            yield return Shot("02_tutorial_step1");

            // step 2 - merge result
            Find<Button>("Btn_Next")?.onClick.Invoke();
            yield return null;
            yield return Shot("03_tutorial_merge");

            // step 3 - FIGHT highlighted
            Find<Button>("Btn_Next")?.onClick.Invoke();
            yield return null;
            yield return Shot("04_tutorial_fight");

            // finish tutorial -> in a run
            Find<Button>("Btn_Next")?.onClick.Invoke();
            yield return null;
            yield return Shot("05_board");

            // fresh merge to show the on-cell sparkle burst
            var session = GetField<GameSession>(bootstrap, "_session");
            if (session != null)
            {
                for (var y = 0; y < session.Board.Height; y++)
                    for (var x = 0; x < session.Board.Width; x++)
                        session.Board.Set(x, y, null);
                session.Board.Set(0, 0, "pawn_t1");
                session.Board.Set(1, 0, "pawn_t1");
                session.Board.Set(2, 0, "pawn_t1");
                Invoke(bootstrap, "RefreshAll");
                yield return null;
                var c00 = GameObject.Find("Cell_0_0")?.GetComponent<MergeSurvivor.UI.CellView>();
                var c10 = GameObject.Find("Cell_1_0")?.GetComponent<MergeSurvivor.UI.CellView>();
                if (c00 != null && c10 != null && EventSystem.current != null)
                {
                    c00.OnPointerClick(new PointerEventData(EventSystem.current));
                    c10.OnPointerClick(new PointerEventData(EventSystem.current));
                }
                yield return null;
                yield return Shot("06_merge_burst");

                // empty the board so the fight is a guaranteed loss -> Defeat panel + revive
                for (var y = 0; y < session.Board.Height; y++)
                    for (var x = 0; x < session.Board.Width; x++)
                        session.Board.Set(x, y, null);
                Invoke(bootstrap, "RefreshAll");
                yield return null;
            }

            // fight -> Defeat result panel (transparent enemy portrait)
            Invoke(bootstrap, "OnFight");
            yield return null;
            yield return Shot("07_fight_result");

            // Continue -> revive prompt on loss
            Find<Button>("Btn_Continue")?.onClick.Invoke();
            yield return null;
            yield return Shot("08_revive");

            // Give Up -> hub, then open Meta Hub
            Find<Button>("Btn_Give Up")?.onClick.Invoke();
            yield return null;
            Invoke(bootstrap, "OpenMeta");
            yield return null;
            yield return Shot("09_meta");

            if (_rt != null) { _rt.Release(); Object.DestroyImmediate(_rt); }
            if (_cam != null) Object.DestroyImmediate(_cam.gameObject);
            Assert.Pass($"Screenshots written to {ShotDir}");
        }

        private void SetupOffscreenCamera()
        {
            var canvas = Find<Canvas>("Canvas");
            Assert.IsNotNull(canvas, "Bootstrap canvas not found for capture.");

            var camGo = new GameObject("CaptureCamera");
            _cam = camGo.AddComponent<Camera>();
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0.05f, 0.04f, 0.03f, 1f);
            _cam.orthographic = true;
            _cam.cullingMask = ~0;
            _cam.transform.position = new Vector3(0, 0, -100);

            _rt = new RenderTexture(W, H, 24) { antiAliasing = 2 };
            _cam.targetTexture = _rt;

            // Drive the Overlay canvas through our camera so Camera.Render() captures it.
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _cam;
            canvas.planeDistance = 100f;
        }

        private IEnumerator Shot(string name)
        {
            // NB: WaitForEndOfFrame hangs in batchmode; Canvas.ForceUpdateCanvases + a normal
            // frame yield is enough to flush layout before the explicit Camera.Render().
            Canvas.ForceUpdateCanvases();
            yield return null;
            _cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = _rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(ShotDir, name + ".png"), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static T Find<T>(string objectName) where T : Object
        {
            foreach (var c in Resources.FindObjectsOfTypeAll<T>())
            {
                if (c is Component comp && comp.gameObject.name == objectName && comp.gameObject.scene.IsValid())
                    return c;
            }
            return null;
        }

        private static void Invoke(object instance, string method)
        {
            var m = instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
            m?.Invoke(instance, null);
        }

        private static T GetField<T>(object instance, string field) where T : class
        {
            var f = instance.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            return f?.GetValue(instance) as T;
        }
    }
}
