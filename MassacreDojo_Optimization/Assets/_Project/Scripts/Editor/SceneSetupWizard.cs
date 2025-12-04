using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MassacreDojo.Core;
using MassacreDojo.Enemy;
using MassacreDojo.Player;
using MassacreDojo.UI;

#if EXERCISES_DEPLOYED
using StudentExercises.Memory;
using StudentExercises.CPU;
using StudentExercises.Tradeoff;
#else
using MassacreDojo.Exercises.Memory;
using MassacreDojo.Exercises.CPU;
using MassacreDojo.Exercises.Tradeoff;
#endif

namespace MassacreDojo.Editor
{
    /// <summary>
    /// シーンの自動セットアップを行うウィザード
    /// </summary>
    public class SceneSetupWizard : EditorWindow
    {
        [MenuItem("MassacreDojo/Scene Setup Wizard")]
        public static void ShowWindow()
        {
            var window = GetWindow<SceneSetupWizard>("Scene Setup");
            window.minSize = new Vector2(400, 500);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("シーンセットアップウィザード", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("このウィザードは、学習用シーンを自動的にセットアップします。", MessageType.Info);

            EditorGUILayout.Space(20);

            if (GUILayout.Button("新しいシーンを作成してセットアップ", GUILayout.Height(40)))
            {
                CreateNewScene();
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("現在のシーンにセットアップ", GUILayout.Height(40)))
            {
                SetupCurrentScene();
            }

            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("個別セットアップ", EditorStyles.boldLabel);

            if (GUILayout.Button("LearningSettings を作成"))
            {
                CreateLearningSettings();
            }

            if (GUILayout.Button("フィールド（地面）を作成"))
            {
                CreateField();
            }

            if (GUILayout.Button("プレイヤーを作成"))
            {
                CreatePlayer();
            }

            if (GUILayout.Button("GameManagerを作成"))
            {
                CreateGameManager();
            }

            if (GUILayout.Button("カメラを設定"))
            {
                SetupCamera();
            }

            if (GUILayout.Button("ライトを設定"))
            {
                SetupLighting();
            }
        }

        private void CreateNewScene()
        {
            // 新しいシーンを作成
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // セットアップ実行
            SetupCurrentScene();

            // シーンを保存
            string path = "Assets/_Project/Scenes/MainGame.unity";
            EnsureDirectoryExists("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, path);

            Debug.Log($"シーンを作成しました: {path}");
        }

        private void SetupCurrentScene()
        {
            // LearningSettings確認
            CreateLearningSettings();

            // フィールド作成
            CreateField();

            // プレイヤー作成
            CreatePlayer();

            // GameManager作成
            CreateGameManager();

            // カメラ設定
            SetupCamera();

            // ライト設定
            SetupLighting();

            // SpawnUI作成
            CreateSpawnUI();

            Debug.Log("シーンのセットアップが完了しました！");
            EditorUtility.DisplayDialog("完了", "シーンのセットアップが完了しました。\n\nPlayモードで動作確認してください。", "OK");
        }

        private void CreateLearningSettings()
        {
            // Resourcesフォルダを確認
            EnsureDirectoryExists("Assets/Resources");

            // 既存のチェック
            var existing = Resources.Load<LearningSettings>("LearningSettings");
            if (existing != null)
            {
                Debug.Log("LearningSettings は既に存在します");
                return;
            }

            // 作成
            var settings = ScriptableObject.CreateInstance<LearningSettings>();
            AssetDatabase.CreateAsset(settings, "Assets/Resources/LearningSettings.asset");
            AssetDatabase.SaveAssets();

            Debug.Log("LearningSettings を作成しました");
        }

        private void CreateField()
        {
            // 既存のフィールドを検索
            var existingField = GameObject.Find("Field");
            if (existingField != null)
            {
                Debug.Log("Field は既に存在します");
                return;
            }

            // 地面を作成
            var field = GameObject.CreatePrimitive(PrimitiveType.Plane);
            field.name = "Field";
            field.transform.position = Vector3.zero;
            field.transform.localScale = new Vector3(10, 1, 10); // 100x100

            // マテリアル設定
            var renderer = field.GetComponent<Renderer>();
            var material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.3f, 0.5f, 0.3f);
            renderer.material = material;

            // マテリアルを保存
            EnsureDirectoryExists("Assets/_Project/Art/Materials");
            AssetDatabase.CreateAsset(material, "Assets/_Project/Art/Materials/FieldMaterial.mat");

            Debug.Log("Field を作成しました");
        }

        private void CreatePlayer()
        {
            // 既存のプレイヤーを検索
            var existingPlayer = GameObject.Find("Player");
            if (existingPlayer != null)
            {
                Debug.Log("Player は既に存在します");
                return;
            }

            // プレイヤーを作成
            var player = new GameObject("Player");
            player.transform.position = new Vector3(0, 0.5f, 0);

            // CharacterController追加
            var cc = player.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0, 1, 0);

            // ビジュアル（カプセル）
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(player.transform);
            visual.transform.localPosition = new Vector3(0, 1, 0);

            // Colliderを削除（CharacterControllerがあるため）
            DestroyImmediate(visual.GetComponent<CapsuleCollider>());

            // マテリアル
            var renderer = visual.GetComponent<Renderer>();
            var material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.2f, 0.4f, 0.8f);
            renderer.material = material;

            // PlayerController追加
            player.AddComponent<PlayerController>();

            Debug.Log("Player を作成しました");
        }

        private void CreateGameManager()
        {
            // 既存のGameManagerを検索
            var existingGM = GameObject.Find("GameManager");
            if (existingGM != null)
            {
                Debug.Log("GameManager は既に存在します");
                return;
            }

            // GameManagerオブジェクトを作成
            var gmObject = new GameObject("GameManager");

            // コンポーネント追加
            gmObject.AddComponent<GameManager>();
            gmObject.AddComponent<EnemySystem>();
            gmObject.AddComponent<EnemyAIManager>();
            gmObject.AddComponent<PerformanceMonitor>();

            // Exercise コンポーネント追加
            gmObject.AddComponent<ZeroAllocation_Exercise>();
            gmObject.AddComponent<CPUOptimization_Exercise>();
            gmObject.AddComponent<NeighborCache_Exercise>();
            gmObject.AddComponent<DecisionCache_Exercise>();

            // Player参照を設定
            var player = GameObject.Find("Player");
            if (player != null)
            {
                var gm = gmObject.GetComponent<GameManager>();
                var serializedObj = new SerializedObject(gm);
                var playerProp = serializedObj.FindProperty("playerTransform");
                playerProp.objectReferenceValue = player.transform;

                var enemySystemProp = serializedObj.FindProperty("enemySystem");
                enemySystemProp.objectReferenceValue = gmObject.GetComponent<EnemySystem>();

                var perfMonProp = serializedObj.FindProperty("performanceMonitor");
                perfMonProp.objectReferenceValue = gmObject.GetComponent<PerformanceMonitor>();

                serializedObj.ApplyModifiedProperties();
            }

            Debug.Log("GameManager を作成しました");
        }

        private void CreateSpawnUI()
        {
            // 既存のUIを検索
            var existingUI = GameObject.Find("SpawnUI");
            if (existingUI != null)
            {
                Debug.Log("SpawnUI は既に存在します");
                return;
            }

            // SpawnUIオブジェクト作成（IMGUIベースなのでシンプル）
            var spawnUIObject = new GameObject("SpawnUI");
            spawnUIObject.AddComponent<SpawnUI>();

            Debug.Log("SpawnUI を作成しました");
        }

        private void SetupCamera()
        {
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
                cameraObject.tag = "MainCamera";
            }

            // 見下ろし視点に設定
            mainCamera.transform.position = new Vector3(0, 30, -20);
            mainCamera.transform.rotation = Quaternion.Euler(60, 0, 0);
            mainCamera.fieldOfView = 60;
            mainCamera.farClipPlane = 200;

            Debug.Log("Camera を設定しました");
        }

        private void SetupLighting()
        {
            var light = GameObject.Find("Directional Light");
            if (light == null)
            {
                light = new GameObject("Directional Light");
                var lightComponent = light.AddComponent<Light>();
                lightComponent.type = LightType.Directional;
            }

            light.transform.rotation = Quaternion.Euler(50, -30, 0);

            // 環境光設定
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.4f, 0.4f, 0.5f);

            Debug.Log("Lighting を設定しました");
        }

        private void EnsureDirectoryExists(string path)
        {
            string[] folders = path.Split('/');
            string currentPath = folders[0];

            for (int i = 1; i < folders.Length; i++)
            {
                string newPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(newPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = newPath;
            }
        }
    }
}
