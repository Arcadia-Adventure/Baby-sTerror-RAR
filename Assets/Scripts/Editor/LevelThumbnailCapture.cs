using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ommy.Prefs;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Plays each night and photographs it for the level select buttons and the win screen's next-night
/// teaser. The shot is the player's view a moment after the night starts, or the level's
/// LevelSceneSetup.thumbnailView when set (lit by the flashlight when thumbnailFlashlight is on),
/// without the HUD. Each picture lands in Assets/GameData/Thumbnails and is assigned to its
/// LevelDefinition.thumbnail.
/// </summary>
[InitializeOnLoad]
public static class LevelThumbnailCapture
{
    /// <summary>Twice the level select button's picture (279x353), the same shape.</summary>
    public const int Width = 558;
    public const int Height = 706;
    public const float Aspect = Width / (float)Height;
    public const string Folder = "Assets/GameData/Thumbnails";

    // Lets the night's start events run (fire, power cut, lighting) and any fade finish.
    // The thumbnail views were framed at this moment, so the Nanny and the fires are where they expect.
    const float SettleSeconds = 3f;
    // A night that never starts is skipped instead of hanging the run.
    const float TimeoutSeconds = 45f;
    // Rendered this many times larger, then halved, for smooth edges.
    const int Supersample = 2;

    // SessionState survives the domain reload on entering play mode; statics don't.
    const string QueueKey = "BabysTerror.ThumbnailQueue";
    const string DoneKey = "BabysTerror.ThumbnailDone";

    static double _settledAt = -1;
    static double _requestedAt = -1;

    static LevelThumbnailCapture()
    {
        if (!string.IsNullOrEmpty(SessionState.GetString(QueueKey, "")))
            Hook();
    }

    [MenuItem("Baby's Terror/20 Nights/Capture Level Thumbnails")]
    static void CaptureAll() =>
        Capture(LevelConfigLoader.Database.Levels.Where(d => d != null).Select(d => d.level).ToArray());

    /// <summary>Plays the given nights one after another and captures each one's thumbnail.</summary>
    public static void Capture(params int[] levels)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[LevelThumbnailCapture] Stop play mode first.");
            return;
        }

        int[] valid = levels.Where(l => LevelConfigLoader.Database.Get(l) != null).Distinct().ToArray();
        if (valid.Length == 0)
        {
            Debug.LogWarning("[LevelThumbnailCapture] No matching levels in the LevelDatabase.");
            return;
        }

        Save(QueueKey, valid);
        Save(DoneKey, System.Array.Empty<int>());
        _settledAt = _requestedAt = -1;
        Hook();

        NightPlaytest.Play(valid[0]);
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
            Clear(); // The save-scene prompt was cancelled.
        else
            Debug.Log($"[LevelThumbnailCapture] Capturing nights {string.Join(", ", valid)}.");
    }

    static void Hook()
    {
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isPaused)
            return;

        List<int> queue = Load(QueueKey);
        if (queue.Count == 0)
            return;

        int level = queue[0];
        double now = EditorApplication.timeSinceStartup;
        if (_requestedAt < 0)
            _requestedAt = now;

        var manager = Object.FindFirstObjectByType<GamePlayManager>();
        bool started = manager != null && manager.CurrentSetup != null &&
                       manager.CurrentLevel != null && manager.CurrentLevel.level == level;
        if (!started)
        {
            _settledAt = -1;
            if (now - _requestedAt > TimeoutSeconds)
            {
                Debug.LogError($"[LevelThumbnailCapture] Night {level} didn't start within {TimeoutSeconds} s; skipped.");
                Advance(queue, captured: false);
            }
            return;
        }

        if (_settledAt < 0)
            _settledAt = now + SettleSeconds;
        if (now < _settledAt)
            return;

        bool ok = Shoot(manager, level);
        Advance(queue, ok);
    }

    static void Advance(List<int> queue, bool captured)
    {
        int level = queue[0];
        queue.RemoveAt(0);
        Save(QueueKey, queue);
        if (captured)
            Save(DoneKey, Load(DoneKey).Append(level));

        _settledAt = -1;
        _requestedAt = -1;

        if (queue.Count == 0)
        {
            EditorApplication.isPlaying = false;
            return;
        }

        GamePreference.selectedLevel = queue[0];
        SceneManager.LoadScene("GamePlay");
    }

    static bool Shoot(GamePlayManager manager, int level)
    {
        var player = Object.FindFirstObjectByType<PlayerController>();
        Camera cam = player != null && player.PlayerCamera != null ? player.PlayerCamera : Camera.main;
        if (cam == null)
        {
            Debug.LogError($"[LevelThumbnailCapture] Night {level}: no player camera.");
            return false;
        }

        LevelSceneSetup setup = manager.CurrentSetup;
        Transform view = setup.thumbnailView;
        // Only the spawn's room is switched on; a view elsewhere needs the rest of the house.
        if (view != null && CullingManager.Instance != null)
            CullingManager.Instance.EnableAllAreas();
        // The light hangs off the camera, so it follows the view. The night is left straight after.
        if (setup.thumbnailFlashlight && player != null)
            player.Flashlight.Equip();

        string path = PathFor(level);
        if (!Render(cam, path, view != null ? new Pose(view.position, view.rotation) : (Pose?)null))
            return false;

        Debug.Log($"[LevelThumbnailCapture] Night {level} -> {path}" + (view != null ? " (thumbnail view)" : ""));
        return true;
    }

    /// <summary>Renders the camera alone (no overlay HUD), optionally from another pose, to a thumbnail-sized PNG.</summary>
    public static bool Render(Camera cam, string path, Pose? view = null)
    {
        Transform camTransform = cam.transform;
        camTransform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
        if (view.HasValue)
            camTransform.SetPositionAndRotation(view.Value.position, view.Value.rotation);

        RenderTexture big = RenderTexture.GetTemporary(
            new RenderTextureDescriptor(Width * Supersample, Height * Supersample, GraphicsFormat.R8G8B8A8_SRGB, 24));
        RenderTexture small = RenderTexture.GetTemporary(
            new RenderTextureDescriptor(Width, Height, GraphicsFormat.R8G8B8A8_SRGB, 0));
        big.filterMode = FilterMode.Bilinear;
        RenderTexture previousActive = RenderTexture.active;
        var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false, false);

        try
        {
            cam.aspect = Aspect;
            var request = new RenderPipeline.StandardRequest();
            if (RenderPipeline.SupportsRenderRequest(cam, request))
            {
                request.destination = big;
                RenderPipeline.SubmitRenderRequest(cam, request);
            }
            else
            {
                RenderTexture previousTarget = cam.targetTexture;
                cam.targetTexture = big;
                cam.Render();
                cam.targetTexture = previousTarget;
            }

            // Bilinear at exactly half size averages each 2x2 block.
            Graphics.Blit(big, small);
            RenderTexture.active = small;
            texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            texture.Apply();

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, texture.EncodeToPNG());
            return true;
        }
        finally
        {
            RenderTexture.active = previousActive;
            cam.ResetAspect();
            camTransform.SetPositionAndRotation(position, rotation);
            RenderTexture.ReleaseTemporary(big);
            RenderTexture.ReleaseTemporary(small);
            Object.DestroyImmediate(texture);
        }
    }

    static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredEditMode)
            return;

        List<int> left = Load(QueueKey);
        List<int> done = Load(DoneKey);
        Clear();

        if (left.Count > 0)
            Debug.LogWarning($"[LevelThumbnailCapture] Stopped early; not captured: {string.Join(", ", left)}.");
        if (done.Count > 0)
            AssignThumbnails(done);
    }

    static void AssignThumbnails(List<int> levels)
    {
        AssetDatabase.Refresh();
        foreach (int level in levels)
        {
            string path = PathFor(level);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            LevelDefinition definition = LevelConfigLoader.Database.Get(level);
            if (sprite == null || definition == null)
            {
                Debug.LogError($"[LevelThumbnailCapture] Couldn't assign {path} to night {level}.");
                continue;
            }

            definition.thumbnail = sprite;
            EditorUtility.SetDirty(definition);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[LevelThumbnailCapture] Done: thumbnails assigned for nights {string.Join(", ", levels)}.");
    }

    public static string PathFor(int level) => $"{Folder}/Level {level:00}.png";

    static void Clear()
    {
        SessionState.EraseString(QueueKey);
        SessionState.EraseString(DoneKey);
        _settledAt = _requestedAt = -1;
    }

    static List<int> Load(string key) =>
        SessionState.GetString(key, "").Split(',').Where(s => s.Length > 0).Select(int.Parse).ToList();

    static void Save(string key, IEnumerable<int> levels) => SessionState.SetString(key, string.Join(",", levels));
}

/// <summary>Imports new captures as UI sprites; settings changed by hand afterwards are kept.</summary>
class LevelThumbnailImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(LevelThumbnailCapture.Folder + "/") || !assetImporter.importSettingsMissing)
            return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = 1024;
        importer.textureCompression = TextureImporterCompression.Compressed;
    }
}
