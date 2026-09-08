using System.IO;
using System.Linq;
using LAC.Menu;
using LAC.Net;
using LAC.Player;
using Mirror;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LAC.Editor
{
    public static class LobbySetup
    {
        private const string Arena = "Assets/_LAC/Scenes/Arena.unity";
        private const string Lobby = "Assets/_LAC/Scenes/ManCho.unity";
        private const string Folder = "Assets/_LAC/Prefabs/UI/Menu";

        [MenuItem("LAC/Coop/1. Create Lobby Assets")]
        public static void CreateAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Thoát Play mode trước khi dựng sảnh.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            Scene arena = EditorSceneManager.OpenScene(Arena, OpenSceneMode.Additive);
            NetworkManagerLAC source = arena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<NetworkManagerLAC>()).Single();
            GameObject network = Object.Instantiate(source.gameObject);
            network.name = "LobbyNetwork";
            var manager = network.GetComponent<NetworkManagerLAC>();
            var serialized = new SerializedObject(manager);
            serialized.FindProperty("_autoStartHost").boolValue = false;
            serialized.FindProperty("_useLobby").boolValue = true;
            serialized.FindProperty("autoCreatePlayer").boolValue = false;
            serialized.FindProperty("maxConnections").intValue = 2;
            serialized.FindProperty("offlineScene").stringValue = Lobby;
            serialized.FindProperty("onlineScene").stringValue = "";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            foreach (NetworkManagerHUD hud in network.GetComponents<NetworkManagerHUD>()) Object.DestroyImmediate(hud);
            GameObject networkPrefab = PrefabUtility.SaveAsPrefabAsset(network, Folder + "/LobbyNetwork.prefab");
            Object.DestroyImmediate(network);
            EditorSceneManager.CloseScene(arena, true);

            var ui = new GameObject("LobbyView", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(LobbyView));
            serialized = new SerializedObject(ui.GetComponent<LobbyView>());
            serialized.FindProperty("_characters").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CharacterRegistry>("Assets/_LAC/Data/Characters/CharacterRegistry.asset");
            serialized.FindProperty("_font").objectReferenceValue = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            GameObject viewPrefab = PrefabUtility.SaveAsPrefabAsset(ui, Folder + "/LobbyView.prefab");
            Object.DestroyImmediate(ui);

            Scene lobby = EditorSceneManager.OpenScene(Lobby, OpenSceneMode.Single);
            if (!lobby.GetRootGameObjects().Any(g => g.GetComponent<NetworkManagerLAC>() != null)) PrefabUtility.InstantiatePrefab(networkPrefab, lobby);
            if (!lobby.GetRootGameObjects().Any(g => g.GetComponent<LobbyView>() != null)) PrefabUtility.InstantiatePrefab(viewPrefab, lobby);
            EditorSceneManager.SaveScene(lobby);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Lobby, true), new EditorBuildSettingsScene(Arena, true) }
                .Concat(EditorBuildSettings.scenes.Where(s => s.path != Lobby && s.path != Arena)).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("[LobbySetup] Đã dựng sảnh, prefab mạng và UI; ManCho là scene đầu tiên.");
        }

        [MenuItem("LAC/Coop/2. Build Smoke Player")]
        public static void BuildSmokePlayer()
        {
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { Lobby, Arena },
                locationPathName = "Builds/CoopSmoke/LAC.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.InvalidOperationException("Build thất bại: " + report.summary.result);
            Debug.Log("[LobbySetup] Build thành công: Builds/CoopSmoke/LAC.exe");
        }
    }
}
