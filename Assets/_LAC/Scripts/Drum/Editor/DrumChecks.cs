#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Threading.Tasks;
using LAC.Core;
using LAC.Enemies;
using LAC.Player;
using Mirror;
using UnityEditor;
using UnityEngine;

namespace LAC.Drum.Editor
{
    public static class DrumChecks
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static bool _running;
        public static string LastResult { get; private set; }
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
        private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

        [MenuItem("LAC/Tests/Check Drum (Play Mode - restarts run)")]
        public static async void Run()
        {
            if (_running || !Application.isPlaying || !NetworkServer.active) return;
            _running = true;
            LastResult = "RUNNING";
            GameObject hostile = null;
            GameObject friendly = null;
            try
            {
                var run = RunManager.Instance;
                run.ReportPlayerDown(); run.ReportPlayerDown(); run.RestartRun();
                var player = PlayerRegistry.All[0];
                var dash = player.GetComponent<PlayerDash>();
                var weapon = player.GetComponent<LAC.Combat.WeaponAuto>();
                weapon.enabled = false;
                Set(player.GetComponent<PlayerHealth>(), "_invulnerableUntil", Time.time + 90f);
                Vector2 center = run.DrumPosition;
                player.GetComponent<Rigidbody2D>().position = center;
                player.transform.position = center;
                Check(!run.TryActivateDrum(dash, center, center), "Không lướt vẫn kích hoạt");
                Set(dash, "_serverInvulnerableUntil", Time.time + 1f);
                run.ReportSoulCollected(19);
                Check(!run.TryActivateDrum(dash, center, center), "Thiếu Hồn vẫn kích hoạt");
                run.ReportSoulCollected(100);
                Check(run.DrumEnergy == run.DrumRules.SoulCost, "Vượt trần nạp");
                Check(!run.TryActivateDrum(dash, center + Vector2.right * 10, center + Vector2.right * 10), "Nhận yêu cầu từ xa");
                Check(!run.TryActivateDrum(dash, new Vector2(float.NaN, 0), center), "Nhận NaN");
                Check(DrumShockwave.Intersects(center + Vector2.left * 3, center + Vector2.right * 3, center, 1.15f), "Bỏ sót đoạn xuyên tâm");
                Check(!DrumShockwave.Intersects(center + Vector2.up * 3, center + new Vector2(3, 3), center, 1.15f), "Nhầm đoạn ngoài trống");
                hostile = new GameObject("TestEnemyShot", typeof(EnemyProjectileClearable));
                friendly = new GameObject("TestFriendlyShot", typeof(LAC.Combat.Projectile));
                friendly.GetComponent<LAC.Combat.Projectile>().enabled = false;
                var data = AssetDatabase.LoadAssetAtPath<EnemyData>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:EnemyData")[0]));
                var enemy = EnemySpawner.Instance.Spawn(data, center + Vector2.right * 5);
                Vector2 before = enemy.Position;
                Check(run.TryActivateDrum(dash, center, center), "Không kích hoạt khi hợp lệ");
                Check(enemy.IsStunned, "Không choáng");
                Check(!hostile.activeSelf && friendly.activeSelf, "Xoá nhầm phe đạn");
                Check(run.DrumEnergy == 0 && run.DrumActivations == 1, "Tiêu Hồn hoặc số lần sai");
                run.ReportSoulCollected(20);
                Check(!run.TryActivateDrum(dash, center, center), "Kích hoạt kép khi đang hồi");
                await Task.Delay(400);
                Check(enemy.IsStunned && Vector2.Distance(before, enemy.Position) > 1f, "Choáng/đẩy lùi không hoạt động");
                Vector2 held = enemy.Position;
                await Task.Delay(200);
                Check(Vector2.Distance(held, enemy.Position) < 0.05f, "Quái di chuyển khi choáng");
                await Task.Delay(550);
                Check(!enemy.IsStunned, "Không hết choáng");
                Check(run.DrumCooldownRemaining > 18f, "Hồi chiêu không đúng 20 giây");
                // Chờ đồng hồ thật để kiểm tra ready được host bật lại, không chỉ sửa trường private.
                while (Application.isPlaying && run.DrumCooldownRemaining > 0f) await Task.Delay(100);
                Check(Application.isPlaying, "Đã thoát Play");
                await Task.Delay(100);
                Check(run.DrumReady && run.DrumEnergy == 20, "Không sẵn sàng sau hồi");
                Vector2 start = center + Vector2.left * 3f;
                player.GetComponent<Rigidbody2D>().position = start; player.transform.position = start;
                typeof(PlayerMovement).GetProperty("Facing").SetValue(player.GetComponent<PlayerMovement>(), Vector2.right);
                typeof(PlayerDash).GetMethod("BeginLocalDash", Private).Invoke(dash, null);
                typeof(PlayerDash).GetMethod("CmdDash", Private).Invoke(dash, null);
                await Task.Delay(800);
                Check(run.DrumActivations == 2, "Cú lướt thực tế không chạm trống");
                run.ReportWaveCleared();
                Set(run, "_drumReadyAt", 0d); run.ReportSoulCollected(20); Set(dash, "_serverInvulnerableUntil", Time.time + 1f);
                Check(!run.TryActivateDrum(dash, center, center), "Kích hoạt khi chọn thẻ");
                run.ReportPlayerDown(); run.ReportPlayerDown(); run.RestartRun();
                Check(run.DrumEnergy == 0 && run.DrumActivations == 0 && run.DrumCooldownRemaining == 0, "Không reset khi chơi lại");
                weapon.enabled = true;
                LastResult = "ALL PASSED";
                Debug.Log("[DrumChecks] " + LastResult);
            }
            catch (Exception e) { LastResult = e.ToString(); Debug.LogException(e); }
            finally
            {
                if (hostile != null) UnityEngine.Object.Destroy(hostile);
                if (friendly != null) UnityEngine.Object.Destroy(friendly);
                _running = false;
            }
        }

        [MenuItem("LAC/Tests/Build Drum Network Client")]
        public static void BuildClient()
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/_LAC/Scenes/Arena.unity" },
                locationPathName = "Builds/DrumNetworkTests/DrumClient.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            Debug.Log($"[DrumNet] BUILD {report.summary.result}");
        }
    }
}
#endif
