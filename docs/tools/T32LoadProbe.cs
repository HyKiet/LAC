// Chạy bằng Unity-MCP script-execute (full code), className=T32LoadProbe, methodName=Run.
// File ngoài Assets: không thêm harness vào game hoặc sửa scene/chỉ số asset.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LAC.Cards;
using LAC.Combat;
using LAC.Core;
using LAC.Enemies;
using LAC.Player;
using LAC.VFX;
using Mirror;
using Newtonsoft.Json;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;

public static class T32LoadProbe
{
    public static string Run() => Begin(false);
    public static string RunImpacts() => Begin(true);
    private static string Begin(bool impacts)
    {
        if (!Application.isPlaying || !NetworkServer.active || PlayerRegistry.Count != 1)
            throw new InvalidOperationException("T-32 cần Arena host một người trong Play mode.");
        if (UnityEngine.Object.FindAnyObjectByType<T32LoadRunner>() != null)
            throw new InvalidOperationException("Fixture cũ còn chạy; thoát Play mode trước lượt tiếp.");
        var go = new GameObject("T32LoadProbe");
        try
        {
            go.AddComponent<T32LoadRunner>().Begin(impacts);
            return "Đã dựng fixture 40 quái, 200 đạn, 10 sóng; warmup 3 s, đo 30 s. "
                + "Không gọi thêm Unity tools trong cửa sổ đo; fixture giữ hình sau khi xuất kết quả.";
        }
        catch { UnityEngine.Object.DestroyImmediate(go); throw; }
    }
}

public sealed class T32LoadRunner : MonoBehaviour
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly FrameTiming[] _timing = new FrameTiming[1];
    private readonly List<double> _wall = new List<double>(32768);
    private readonly List<double> _loop = new List<double>(32768);
    private readonly List<double> _main = new List<double>(32768);
    private readonly List<double> _gpu = new List<double>(32768);
    private readonly List<double> _gc = new List<double>(32768);
    private ObjectPool<Projectile> _bulletPool;
    private ObjectPool<SoundWave> _soundPool;
    private ProfilerRecorder _loopRecorder;
    private ProfilerRecorder _gcRecorder;
    private PlayerCharacter _player;
    private PlayerHealth _health;
    private PlayerInputReader _input;
    private WeaponAuto _weapon;
    private WaveManager _waves;
    private CharacterId _previousCharacter;
    private bool _previousInput, _previousWeapon, _previousWaves, _previousBackground;
    private int _previousVsync, _previousTarget;
    private float _previousScale, _previousInvulnerability;
    private double _started;
    private ulong _lastTimingTimestamp;
    private int _launches, _minEnemies = int.MaxValue, _minBullets = int.MaxValue;
    private int _minSounds = int.MaxValue;
    private bool _impacts, _ready, _finished;

    public void Begin(bool impacts)
    {
        _impacts = impacts;
        _player = PlayerRegistry.All[0];
        _health = _player.GetComponent<PlayerHealth>();
        _input = _player.GetComponent<PlayerInputReader>();
        _weapon = _player.GetComponent<WeaponAuto>();
        _waves = UnityEngine.Object.FindAnyObjectByType<WaveManager>();
        _previousCharacter = _player.CharacterId;
        _previousInput = _input.enabled; _previousWeapon = _weapon.enabled;
        _previousWaves = _waves.enabled;
        _previousInvulnerability = (float)typeof(PlayerHealth).GetField("_invulnerableUntil", Private).GetValue(_health);
        _previousScale = Time.timeScale; _previousVsync = QualitySettings.vSyncCount;
        _previousTarget = Application.targetFrameRate; _previousBackground = Application.runInBackground;
        _ready = true;

        var run = RunManager.Instance;
        if (!run.IsOver) run.ReportPlayerDown();
        run.RestartRun();
        CardSelectionController.Instance.GetComponent<CardSelectionView>().Hide();
        HitStop.Cancel();
        Time.timeScale = 1f;
        Application.runInBackground = true;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
        _waves.enabled = false; _input.enabled = false; _weapon.enabled = false;
        _player.SetCharacter(CharacterId.ThachSanh);
        typeof(PlayerHealth).GetField("_invulnerableUntil", Private).SetValue(_health, Time.time + 10000f);
        PoolRegistry.ReleaseAll();
        EnemySpawner.Instance.ResetRun();
        var data = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_LAC/Data/Enemies/CoHon.asset");
        for (int i = 0; i < 40; i++)
        {
            float angle = i * Mathf.PI * 2f / 40f;
            var position = (Vector2)_player.transform.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 6f;
            var enemy = EnemySpawner.Instance.Spawn(data, position);
            // Chỉ đổi component của fixture; không đổi EnemyData. Để hit-feedback chạy mà không hụt tải quái.
            typeof(Enemy).GetField("_health", Private).SetValue(enemy, 1000000f);
        }
        var projectile = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LAC/Prefabs/Projectiles/Projectile.prefab").GetComponent<Projectile>();
        var wave = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LAC/Prefabs/VFX/SoundWave.prefab").GetComponent<SoundWave>();
        _bulletPool = new ObjectPool<Projectile>(projectile, transform, 200, 256);
        _soundPool = new ObjectPool<SoundWave>(wave, transform, 10, 32);
        Refill();
        _loopRecorder = ProfilerRecorder.StartNew(new ProfilerCategory("PlayerLoop"), "PlayerLoop", 1);
        _gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
        if (!_loopRecorder.Valid || !_gcRecorder.Valid)
            throw new InvalidOperationException("Profiler recorder không sẵn sàng.");
        _started = Time.realtimeSinceStartupAsDouble;
        Debug.Log("[T32] START " + (impacts ? "impacts" : "flight") + " 40 enemies / 200 projectiles / 10 waves.");
    }

    private void Refill()
    {
        Vector2 center = _player.transform.position;
        // Không giữ slot -> instance: pool có thể đưa instance của slot cũ sang slot mới.
        // Duy trì tải theo CountActive thật, tránh alias khiến fixture hụt đối tượng.
        while (_bulletPool.CountActive < 200)
        {
            int i = _launches % 200;
            float angle = (_launches++ * 137.50776f) * Mathf.Deg2Rad;
            Vector2 radial = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float radius = 3.5f + i % 8 * .45f;
            Vector2 position = center + radial * radius;
            var bullet = _bulletPool.Get(position, Quaternion.identity);
            Vector2 direction = _impacts ? -radial : new Vector2(-radial.y, radial.x);
            bullet.Launch(_bulletPool, direction, 6f, _impacts ? .05f : 0f, 2f, 1);
        }
        while (_soundPool.CountActive < 10)
        {
            int i = _soundPool.CountActive;
            var position = center + new Vector2(Mathf.Cos(i * 2.4f), Mathf.Sin(i * 2.4f)) * (i % 3) * 1.3f;
            var sound = _soundPool.Get(position, Quaternion.identity);
            sound.Play(_soundPool, .25f, 4f + i % 3, new Color(.612f, .812f, .753f, 1f));
        }
    }

    private void LateUpdate()
    {
        if (!_ready || _finished || _bulletPool == null) return;
        if (!NetworkServer.active || RunManager.Instance.State != RunState.WaveActive)
        {
            Debug.LogError("[T32] Fixture mất host hoặc rời WaveActive; bỏ kết quả.");
            _finished = true;
            return;
        }
        Refill();
        double elapsed = Time.realtimeSinceStartupAsDouble - _started;
        FrameTimingManager.CaptureFrameTimings();
        if (elapsed < 3f) return;
        _minEnemies = Math.Min(_minEnemies, EnemyRegistry.Count);
        _minBullets = Math.Min(_minBullets, _bulletPool.CountActive);
        _minSounds = Math.Min(_minSounds, _soundPool.CountActive);
        _wall.Add(Time.unscaledDeltaTime * 1000d);
        _loop.Add(_loopRecorder.LastValue / 1000000d);
        _gc.Add(_gcRecorder.LastValue);
        if (FrameTimingManager.GetLatestTimings(1, _timing) > 0
            && _timing[0].frameStartTimestamp != _lastTimingTimestamp)
        {
            _lastTimingTimestamp = _timing[0].frameStartTimestamp;
            _main.Add(Math.Max(0d, _timing[0].cpuMainThreadFrameTime - _timing[0].cpuMainThreadPresentWaitTime));
            if (_timing[0].gpuFrameTime > 0d) _gpu.Add(_timing[0].gpuFrameTime);
        }
        if (elapsed >= 33d) Finish();
    }

    private void Finish()
    {
        _finished = true;
        var result = new Result
        {
            scenario = _impacts ? "impacts" : "flight",
            environment = "Unity Editor Play Mode; synthetic host fixture, not standalone benchmark",
            unity = Application.unityVersion, cpu = SystemInfo.processorType,
            gpu = SystemInfo.graphicsDeviceName, api = SystemInfo.graphicsDeviceType.ToString(),
            width = Screen.width, height = Screen.height, samples = _wall.Count,
            warmupSeconds = 3, measuredSeconds = Time.realtimeSinceStartupAsDouble - _started - 3d,
            minEnemies = _minEnemies, minProjectiles = _minBullets, minWaves = _minSounds,
            launches = _launches, physicsStep = Time.fixedDeltaTime,
            frameTimingEnabled = FrameTimingManager.IsFeatureEnabled(),
            wallMs = Summarize(_wall), playerLoopMs = Summarize(_loop),
            cpuMainWithoutPresentWaitMs = Summarize(_main), gpuMs = Summarize(_gpu),
            gcBytesPerFrame = Summarize(_gc),
            estimatedFps = 1000d / Summarize(_wall).mean,
            wallFramesOverBudget = _wall.FindAll(value => value > 1000d / 60d).Count,
            enemyDataUnchanged = !EditorUtility.IsDirty(AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_LAC/Data/Enemies/CoHon.asset"))
        };
        string json = JsonConvert.SerializeObject(result, Formatting.Indented);
        string path = "D:/LAC-main/docs/measurements/T32-" + result.scenario + "-" + Screen.width + "x" + Screen.height + ".json";
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, json);
        _loopRecorder.Dispose(); _gcRecorder.Dispose();
        Time.timeScale = 0f;
        Debug.Log("[T32] RESULT " + JsonConvert.SerializeObject(result));
    }

    private void OnDestroy()
    {
        if (!_ready) return;
        _loopRecorder.Dispose(); _gcRecorder.Dispose();
        if (!Application.isPlaying) return;
        _bulletPool?.ReleaseAll(); _soundPool?.ReleaseAll();
        _bulletPool?.Clear(); _soundPool?.Clear();
        if (_waves != null) _waves.enabled = _previousWaves;
        if (_input != null) _input.enabled = _previousInput;
        if (_weapon != null) _weapon.enabled = _previousWeapon;
        if (_player != null) _player.SetCharacter(_previousCharacter);
        if (_health != null) typeof(PlayerHealth).GetField("_invulnerableUntil", Private).SetValue(_health, _previousInvulnerability);
        QualitySettings.vSyncCount = _previousVsync; Application.targetFrameRate = _previousTarget;
        Application.runInBackground = _previousBackground; Time.timeScale = _previousScale;
    }

    private static Distribution Summarize(List<double> values)
    {
        var result = new Distribution { count = values.Count };
        if (values.Count == 0) return result;
        var sorted = values.ToArray(); Array.Sort(sorted);
        foreach (double value in values) result.mean += value;
        result.mean /= values.Count;
        result.p50 = sorted[(int)((sorted.Length - 1) * .5d)];
        result.p95 = sorted[(int)((sorted.Length - 1) * .95d)];
        result.p99 = sorted[(int)((sorted.Length - 1) * .99d)];
        result.max = sorted[sorted.Length - 1];
        return result;
    }

    [Serializable] private sealed class Distribution
    {
        public int count;
        public double mean, p50, p95, p99, max;
    }
    [Serializable] private sealed class Result
    {
        public string scenario, environment, unity, cpu, gpu, api;
        public int width, height, samples, warmupSeconds, minEnemies, minProjectiles, minWaves, launches, wallFramesOverBudget;
        public double measuredSeconds, estimatedFps;
        public float physicsStep;
        public bool frameTimingEnabled, enemyDataUnchanged;
        public Distribution wallMs, playerLoopMs, cpuMainWithoutPresentWaitMs, gpuMs, gcBytesPerFrame;
    }
}
