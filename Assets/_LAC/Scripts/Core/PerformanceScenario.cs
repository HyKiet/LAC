#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LAC.Combat;
using LAC.Enemies;
using LAC.Player;
using LAC.VFX;
using Mirror;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace LAC.Core
{
    // Chỉ cài trong build phát triển khi có cờ tường minh; không tham gia ván bình thường.
    public sealed class PerformanceScenario : MonoBehaviour
    {
        private ObjectPool<Projectile> _shots;
        private ObjectPool<SoundWave> _waves;
        private ObjectPool<InstrumentWave> _instrumentPool;
        private InstrumentWaveData[] _instruments;
        private int _instrumentSerial;
        private PlayerCharacter _player;
        private PlayerMovement _movement;
        private WeaponAuto _weapon;
        private WaveManager _manager;
        private readonly List<double> _frames = new List<double>(30000);
        private readonly List<double> _main = new List<double>(30000);
        private ProfilerRecorder _mainRecorder;
        private ProfilerRecorder _gcRecorder;
        private long _gcBytes;
        private int _minEnemies = int.MaxValue, _minShots = int.MaxValue;
        private int _serial;
        private float _began, _nextWave;
        private bool _ready;
        private int _oldVsync, _oldTarget;
        private int _renderedFrames;
        public static string LastResult { get; private set; } = "Not run";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--lac-perf") >= 0) Begin();
        }
        public static void Begin()
        {
            if (FindAnyObjectByType<PerformanceScenario>() != null) return;
            new GameObject("T32Performance", typeof(PerformanceScenario));
        }
        private void Start()
        {
            LastResult="WARMUP";
            Application.runInBackground=true;
            _oldVsync=QualitySettings.vSyncCount; _oldTarget=Application.targetFrameRate;
            QualitySettings.vSyncCount=0; Application.targetFrameRate=240;
            RenderPipelineManager.endFrameRendering+=CountRenderedFrame;
        }
        private void CountRenderedFrame(ScriptableRenderContext context, Camera[] cameras)
        {
            if (_ready && Time.unscaledTime-_began >= 10 && cameras.Length>0) _renderedFrames++;
        }
        private void Setup()
        {
            _manager=FindAnyObjectByType<WaveManager>(); _manager.enabled=false;
            _player=PlayerRegistry.All[0];
            _movement=_player.GetComponent<PlayerMovement>(); _movement.enabled=false;
            _weapon=_player.GetComponent<WeaponAuto>(); _weapon.enabled=false;
            typeof(PlayerHealth).GetField("_invulnerableUntil",BindingFlags.Instance|BindingFlags.NonPublic)
                .SetValue(_player.GetComponent<PlayerHealth>(),float.MaxValue);
            var spawner=EnemySpawner.Instance; spawner.ResetRun(); spawner.BeginWave(RunManager.Instance.CurrentWave);
            var catalog=Resources.Load<EnemyCatalogData>("Enemies/EnemyCatalog");
            for(int i=0;i<40;i++)
            {
                float angle=i*Mathf.PI*2/40;
                spawner.Spawn(catalog.Get(i%5),new Vector2(Mathf.Cos(angle)*7,Mathf.Sin(angle)*5));
            }
            var data=Resources.Load<PerformanceTestData>("Tests/Performance");
            _shots=PoolRegistry.Get(data.Projectile,200,256);
            _waves=PoolRegistry.Get(data.Wave,16,64);
            _instruments = data.Instruments;
            if (_instruments != null && _instruments.Length > 0)
                _instrumentPool = PoolRegistry.Get(_instruments[0].Prefab, 24, 256);
            _mainRecorder=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1);
            _gcRecorder=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);
            _began=Time.unscaledTime; _ready=true;
        }
        private void FixedUpdate()
        {
            if(!_ready) return;
            float t=Time.time*.5f;
            _player.GetComponent<Rigidbody2D>().MovePosition(new Vector2(Mathf.Cos(t)*5,Mathf.Sin(t)*3.5f));
        }
        private void LateUpdate()
        {
            if(!_ready)
            {
                if(NetworkServer.active && PlayerRegistry.Count>0 && Time.timeSinceLevelLoad>2) Setup();
                return;
            }
            while(_shots.CountActive<200)
            {
                int n=_serial++;
                float y=-5+(n%40)*.25f;
                bool right=n%2==0;
                var shot=_shots.Get(new Vector2(right?-8:8,y),Quaternion.identity);
                shot.Launch(_shots,right?Vector2.right:Vector2.left,5,0,5,100);
            }
            if(Time.time>=_nextWave)
            {
                _nextWave=Time.time+.2f;
                for(int i=0;i<2;i++) _waves.Get(_player.transform.position,Quaternion.identity)
                    .Play(_waves,.2f,4,new Color32(79,166,148,255));
                if (_instrumentPool != null)
                    for (int i = 0; i < 2; i++)
                    {
                        var profile = _instruments[_instrumentSerial++ % _instruments.Length];
                        _instrumentPool.Get(_player.transform.position, Quaternion.identity)
                            .Play(_instrumentPool, profile, Vector2.right, 4, 55, 12);
                    }
            }
            float elapsed=Time.unscaledTime-_began;
            if(elapsed<10) return;
            LastResult="MEASURING";
            _frames.Add(Time.unscaledDeltaTime*1000.0);
            if(_mainRecorder.Valid) _main.Add(_mainRecorder.LastValue/1000000.0);
            if(_gcRecorder.Valid) _gcBytes+=_gcRecorder.LastValue;
            _minEnemies=Mathf.Min(_minEnemies,EnemyRegistry.Count);
            _minShots=Mathf.Min(_minShots,_shots.CountActive);
            if(elapsed>=40) Finish();
        }
        private void Finish()
        {
            double total=0; foreach(double frame in _frames) total+=frame;
            _frames.Sort(); _main.Sort();
            double p95=_frames[(int)((_frames.Count-1)*.95)];
            double cpu=_main.Count>0?_main[(int)((_main.Count-1)*.95)]:-1;
            bool pass=_minEnemies>=40 && _minShots>=200 && p95<=1000.0/60 && _renderedFrames>=_frames.Count*.9;
            string result=JsonUtility.ToJson(new Result(SystemInfo.processorType,SystemInfo.graphicsDeviceName,
                Screen.width,Screen.height,_frames.Count,total/_frames.Count,p95,cpu,
                _gcBytes/(double)_frames.Count,_minEnemies,_minShots,_renderedFrames,pass),true);
            Directory.CreateDirectory("Logs");
            string path=Path.GetFullPath(Application.isEditor?"Logs/T32-editor.json":"Logs/T32-standalone.json");
            File.WriteAllText(path,result);
            LastResult=result; Debug.Log("[T32] "+path+"\n"+result);
            enabled=false;
            Cleanup();
        }
        [Serializable]
        private sealed class Result
        {
            [SerializeField] private string cpu,gpu;
            [SerializeField] private int width,height,samples,minEnemies,minPlayerProjectiles,renderedFrames;
            [SerializeField] private double meanMs,p95Ms,mainThreadP95Ms,gcBytesPerFrame;
            [SerializeField] private bool passed;
            public Result(string c,string g,int w,int h,int n,double mean,double p95,double main,double gc,int enemies,int shots,int rendered,bool pass)
            {cpu=c;gpu=g;width=w;height=h;samples=n;meanMs=mean;p95Ms=p95;mainThreadP95Ms=main;gcBytesPerFrame=gc;minEnemies=enemies;minPlayerProjectiles=shots;renderedFrames=rendered;passed=pass;}
        }
        private void Cleanup()
        {
            _mainRecorder.Dispose(); _gcRecorder.Dispose();
            RenderPipelineManager.endFrameRendering-=CountRenderedFrame;
            QualitySettings.vSyncCount=_oldVsync; Application.targetFrameRate=_oldTarget;
            if(_movement!=null) _movement.enabled=true;
            if(_weapon!=null) _weapon.enabled=true;
            if(_manager!=null) _manager.enabled=true;
        }
        private void OnDestroy() { if(enabled) Cleanup(); }
    }
}
#endif
