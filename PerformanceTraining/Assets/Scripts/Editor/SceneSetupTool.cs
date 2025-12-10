using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using PerformanceTraining.Core;
using PerformanceTraining.AI;
using PerformanceTraining.Exercises.Tradeoff;
using TMPro;

namespace PerformanceTraining.Editor
{
    /// <summary>
    /// メインゲームシーンとプレハブのセットアップツール
    /// </summary>
    public class SceneSetupTool : EditorWindow
    {
        [MenuItem("PerformanceTraining/Scene Setup Tool")]
        public static void ShowWindow()
        {
            GetWindow<SceneSetupTool>("Scene Setup Tool");
        }

        private void OnGUI()
        {
            GUILayout.Label("PerformanceTraining Scene Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "このツールでメインゲームシーンとキャラクタープレハブを自動生成します。",
                MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("1. Create All Character Prefabs (Type-specific)", GUILayout.Height(30)))
            {
                CreateAllCharacterPrefabs();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("2. Create Attack Effect Prefab", GUILayout.Height(30)))
            {
                CreateAttackEffectPrefab();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("3. Create MainGame Scene", GUILayout.Height(30)))
            {
                CreateMainGameScene();
            }

            GUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "ボタンを順番にクリックしてセットアップを完了してください。",
                MessageType.None);
        }

        private const int VARIANTS_PER_TYPE = 3;

        /// <summary>
        /// 全キャラクタータイプのプレハブとPrefabListを作成
        /// 各タイプに3種類のバリエーションを作成
        /// </summary>
        private static void CreateAllCharacterPrefabs()
        {
            string prefabDir = "Assets/Prefabs/Characters";

            // ディレクトリ作成
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder(prefabDir))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Characters");
            }

            var types = (CharacterType[])System.Enum.GetValues(typeof(CharacterType));
            int totalPrefabs = types.Length * VARIANTS_PER_TYPE;

            // 確認ダイアログ
            if (!EditorUtility.DisplayDialog(
                "Create All Character Prefabs",
                $"This will create {totalPrefabs} prefabs ({VARIANTS_PER_TYPE} variants per type) and a CharacterPrefabList asset. Continue?",
                "Yes", "No"))
            {
                return;
            }

            // 各タイプに対してバリエーションを格納
            var prefabsByType = new System.Collections.Generic.List<GameObject>[types.Length];

            // 各タイプのプレハブを作成（3バリエーション）
            for (int i = 0; i < types.Length; i++)
            {
                CharacterType type = types[i];
                prefabsByType[i] = new System.Collections.Generic.List<GameObject>();

                for (int variant = 0; variant < VARIANTS_PER_TYPE; variant++)
                {
                    var prefab = CreateCharacterPrefabForType(type, variant, prefabDir);
                    prefabsByType[i].Add(prefab);
                }
            }

            // CharacterPrefabListを作成
            CreateCharacterPrefabListAsset(types, prefabsByType);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Created {totalPrefabs} character prefabs and CharacterPrefabList");
            EditorUtility.DisplayDialog("Success", $"Created {totalPrefabs} character prefabs ({VARIANTS_PER_TYPE} per type) and CharacterPrefabList!", "OK");
        }

        /// <summary>
        /// 指定タイプ・バリエーションのキャラクタープレハブを作成
        /// </summary>
        private static GameObject CreateCharacterPrefabForType(CharacterType type, int variant, string prefabDir)
        {
            string prefabPath = $"{prefabDir}/Character_{type}_{variant + 1:D2}.prefab";

            // プレハブ用GameObjectを作成
            GameObject characterObj = new GameObject($"Character_{type}_{variant + 1:D2}");

            // コンポーネント追加
            characterObj.AddComponent<Character>();
            characterObj.AddComponent<CharacterAI>();

            // 簡易的な見た目（Capsule）
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(characterObj.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            // Collider削除
            DestroyImmediate(visual.GetComponent<CapsuleCollider>());

            // タイプ別・バリエーション別の色を設定
            var renderer = visual.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Standard"));
            mat.color = GetColorForType(type, variant);
            renderer.material = mat;

            // CharacterUI（HP/名前表示）を作成
            CreateCharacterUIOnObject(characterObj);

            // プレハブとして保存
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(characterObj, prefabPath);

            // 一時オブジェクト削除
            DestroyImmediate(characterObj);

            Debug.Log($"Created prefab: {prefabPath}");
            return prefab;
        }

        /// <summary>
        /// キャラクタータイプとバリエーションに対応する色を取得
        /// </summary>
        private static Color GetColorForType(CharacterType type, int variant = 0)
        {
            // バリエーションによる明度調整（0: 標準, 1: 明るめ, 2: 暗め）
            float brightnessOffset = variant switch
            {
                0 => 0f,
                1 => 0.15f,
                2 => -0.15f,
                _ => 0f
            };

            Color baseColor = type switch
            {
                CharacterType.Warrior => new Color(0.8f, 0.2f, 0.2f),    // 赤 - 戦士
                CharacterType.Assassin => new Color(0.3f, 0.3f, 0.3f),   // 黒/グレー - 暗殺者
                CharacterType.Tank => new Color(0.6f, 0.4f, 0.2f),       // 茶色 - タンク
                CharacterType.Mage => new Color(0.5f, 0.2f, 0.8f),       // 紫 - 魔法使い
                CharacterType.Ranger => new Color(0.2f, 0.6f, 0.2f),     // 緑 - レンジャー
                CharacterType.Berserker => new Color(0.9f, 0.4f, 0.1f),  // オレンジ - バーサーカー
                _ => Color.white
            };

            // 明度調整を適用
            return new Color(
                Mathf.Clamp01(baseColor.r + brightnessOffset),
                Mathf.Clamp01(baseColor.g + brightnessOffset),
                Mathf.Clamp01(baseColor.b + brightnessOffset)
            );
        }

        /// <summary>
        /// CharacterPrefabList ScriptableObjectを作成（複数バリエーション対応）
        /// </summary>
        private static void CreateCharacterPrefabListAsset(CharacterType[] types, System.Collections.Generic.List<GameObject>[] prefabsByType)
        {
            string assetPath = "Assets/Resources/CharacterPrefabList.asset";

            // Resourcesフォルダ作成
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            // ScriptableObject作成
            var prefabList = ScriptableObject.CreateInstance<CharacterPrefabList>();

            // エントリを設定
            var entriesField = typeof(CharacterPrefabList).GetField("_prefabEntries",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var defaultField = typeof(CharacterPrefabList).GetField("_defaultPrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (entriesField != null && defaultField != null)
            {
                var entries = new CharacterPrefabEntry[types.Length];
                for (int i = 0; i < types.Length; i++)
                {
                    entries[i] = new CharacterPrefabEntry
                    {
                        type = types[i],
                        prefabs = new System.Collections.Generic.List<GameObject>(prefabsByType[i])
                    };
                }
                entriesField.SetValue(prefabList, entries);

                // デフォルトは最初のタイプの最初のプレハブ
                if (prefabsByType.Length > 0 && prefabsByType[0].Count > 0)
                {
                    defaultField.SetValue(prefabList, prefabsByType[0][0]);
                }
            }

            // アセットとして保存
            AssetDatabase.CreateAsset(prefabList, assetPath);
            Debug.Log($"Created CharacterPrefabList at: {assetPath}");
        }

        /// <summary>
        /// 単一キャラクタープレハブを作成（レガシー/フォールバック用）
        /// </summary>
        private static void CreateCharacterPrefab()
        {
            string prefabDir = "Assets/Prefabs/Characters";
            string prefabPath = prefabDir + "/Character.prefab";

            // ディレクトリ作成
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder(prefabDir))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Characters");
            }

            // 既存チェック
            if (File.Exists(prefabPath))
            {
                if (!EditorUtility.DisplayDialog(
                    "Confirm Overwrite",
                    "Character prefab already exists. Overwrite?",
                    "Yes", "No"))
                {
                    return;
                }
            }

            // プレハブ用GameObjectを作成
            GameObject characterObj = new GameObject("Character");

            // コンポーネント追加
            characterObj.AddComponent<Character>();
            characterObj.AddComponent<CharacterAI>();

            // 簡易的な見た目（Capsule）
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(characterObj.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            // Collider削除（Characterに別途追加する場合）
            DestroyImmediate(visual.GetComponent<CapsuleCollider>());

            // CharacterUI（HP/名前表示）を作成
            CreateCharacterUIOnObject(characterObj);

            // プレハブとして保存
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(characterObj, prefabPath);

            // 一時オブジェクト削除
            DestroyImmediate(characterObj);

            // 選択
            Selection.activeObject = prefab;

            Debug.Log($"Character prefab created at: {prefabPath}");
            EditorUtility.DisplayDialog("Success", "Character prefab created!", "OK");
        }

        /// <summary>
        /// キャラクターにUI（HP/名前）を追加
        /// uGUI Canvas + TextMeshPro + Image を使用
        /// </summary>
        private static void CreateCharacterUIOnObject(GameObject characterObj)
        {
            // UIルートオブジェクト（Canvas）
            GameObject canvasObj = new GameObject("CharacterCanvas");
            canvasObj.transform.SetParent(characterObj.transform);
            canvasObj.transform.localPosition = new Vector3(0f, 1.5f, 0f);

            // Canvas設定（World Space）
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            // RectTransform設定
            var canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(200f, 60f);
            canvasRect.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            // CanvasGroup（フェード用）
            var canvasGroup = canvasObj.AddComponent<CanvasGroup>();

            // CharacterUIコンポーネント追加
            var characterUI = canvasObj.AddComponent<CharacterUI>();

            // 名前テキスト（TextMeshProUGUI）
            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(canvasObj.transform);
            var nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;
            nameRect.localPosition = new Vector3(0f, 10f, 0f);
            nameRect.sizeDelta = new Vector2(200f, 30f);
            nameRect.localScale = Vector3.one;

            var nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = "Name";
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.fontSize = 24;
            nameText.color = Color.white;
            nameText.enableWordWrapping = false;
            nameText.overflowMode = TextOverflowModes.Overflow;

            // HPバー背景（Image）
            GameObject hpBarBgObj = new GameObject("HPBarBackground");
            hpBarBgObj.transform.SetParent(canvasObj.transform);
            var hpBarBgRect = hpBarBgObj.AddComponent<RectTransform>();
            hpBarBgRect.anchorMin = new Vector2(0f, 0f);
            hpBarBgRect.anchorMax = new Vector2(1f, 0.5f);
            hpBarBgRect.offsetMin = new Vector2(10f, 5f);
            hpBarBgRect.offsetMax = new Vector2(-10f, -5f);
            hpBarBgRect.localPosition = new Vector3(0f, -10f, 0f);
            hpBarBgRect.sizeDelta = new Vector2(180f, 15f);
            hpBarBgRect.localScale = Vector3.one;

            var hpBarBgImage = hpBarBgObj.AddComponent<Image>();
            hpBarBgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // HPバーFill（Image）
            GameObject hpBarFillObj = new GameObject("HPBarFill");
            hpBarFillObj.transform.SetParent(hpBarBgObj.transform);
            var hpBarFillRect = hpBarFillObj.AddComponent<RectTransform>();
            hpBarFillRect.anchorMin = Vector2.zero;
            hpBarFillRect.anchorMax = Vector2.one;
            hpBarFillRect.offsetMin = new Vector2(2f, 2f);
            hpBarFillRect.offsetMax = new Vector2(-2f, -2f);
            hpBarFillRect.localPosition = Vector3.zero;
            hpBarFillRect.localScale = Vector3.one;

            var hpBarFillImage = hpBarFillObj.AddComponent<Image>();
            hpBarFillImage.color = Color.green;
            hpBarFillImage.type = Image.Type.Filled;
            hpBarFillImage.fillMethod = Image.FillMethod.Horizontal;
            hpBarFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            hpBarFillImage.fillAmount = 1f;

            // CharacterUIにリファレンスを設定
            SerializedObject characterUISO = new SerializedObject(characterUI);
            characterUISO.FindProperty("_canvas").objectReferenceValue = canvas;
            characterUISO.FindProperty("_canvasGroup").objectReferenceValue = canvasGroup;
            characterUISO.FindProperty("_nameText").objectReferenceValue = nameText;
            characterUISO.FindProperty("_hpBarFill").objectReferenceValue = hpBarFillImage;
            characterUISO.FindProperty("_hpBarBackground").objectReferenceValue = hpBarBgImage;
            characterUISO.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 攻撃エフェクトプレハブを作成（Object Pool最適化対象）
        /// </summary>
        private static void CreateAttackEffectPrefab()
        {
            string prefabDir = "Assets/Prefabs/Effects";
            string prefabPath = prefabDir + "/AttackEffect.prefab";

            // ディレクトリ作成
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder(prefabDir))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Effects");
            }

            // 既存チェック
            if (File.Exists(prefabPath))
            {
                if (!EditorUtility.DisplayDialog(
                    "Confirm Overwrite",
                    "AttackEffect prefab already exists. Overwrite?",
                    "Yes", "No"))
                {
                    return;
                }
            }

            // エフェクトオブジェクト作成（シンプルな赤いSphere）
            GameObject effectObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            effectObj.name = "AttackEffect";
            effectObj.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

            // Collider削除
            DestroyImmediate(effectObj.GetComponent<SphereCollider>());

            // マテリアル設定（赤いエミッシブ）
            var renderer = effectObj.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(1f, 0.3f, 0.1f);
            mat.SetColor("_EmissionColor", new Color(1f, 0.3f, 0.1f) * 2f);
            mat.EnableKeyword("_EMISSION");
            renderer.material = mat;

            // プレハブとして保存
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(effectObj, prefabPath);

            // 一時オブジェクト削除
            DestroyImmediate(effectObj);

            // 選択
            Selection.activeObject = prefab;

            Debug.Log($"AttackEffect prefab created at: {prefabPath}");
            EditorUtility.DisplayDialog("Success", "AttackEffect prefab created!", "OK");
        }

        private static void CreateMainGameScene()
        {
            string sceneDir = "Assets/Scenes";
            string scenePath = sceneDir + "/MainGame.unity";

            // ディレクトリ作成
            if (!AssetDatabase.IsValidFolder(sceneDir))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            // 既存チェック
            if (File.Exists(scenePath))
            {
                if (!EditorUtility.DisplayDialog(
                    "Confirm Overwrite",
                    "MainGame scene already exists. Overwrite?",
                    "Yes", "No"))
                {
                    return;
                }
            }

            // 新しいシーンを作成
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // CharacterPrefabListを取得
            string prefabListPath = "Assets/Resources/CharacterPrefabList.asset";
            CharacterPrefabList prefabList = AssetDatabase.LoadAssetAtPath<CharacterPrefabList>(prefabListPath);

            // フォールバック用の単一プレハブを取得
            string fallbackPrefabPath = "Assets/Prefabs/Characters/Character.prefab";
            GameObject fallbackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fallbackPrefabPath);

            // どちらもない場合はエラー
            if (prefabList == null && fallbackPrefab == null)
            {
                // タイプ別プレハブがあるか確認（新形式: バリエーション付き）
                string warriorPath = "Assets/Prefabs/Characters/Character_Warrior_01.prefab";
                fallbackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(warriorPath);

                if (fallbackPrefab == null)
                {
                    EditorUtility.DisplayDialog(
                        "Error",
                        "No character prefabs found! Please create them first.",
                        "OK");
                    return;
                }
            }

            // AttackEffect Prefabを取得
            string attackEffectPath = "Assets/Prefabs/Effects/AttackEffect.prefab";
            GameObject attackEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(attackEffectPath);

            if (attackEffectPrefab == null)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "AttackEffect prefab not found! Please create it first.",
                    "OK");
                return;
            }

            // GameManager作成
            GameObject gameManagerObj = new GameObject("GameManager");
            var gameManager = gameManagerObj.AddComponent<GameManager>();

            // CharacterManager作成
            GameObject characterManagerObj = new GameObject("CharacterManager");
            characterManagerObj.transform.SetParent(gameManagerObj.transform);
            var characterManager = characterManagerObj.AddComponent<CharacterManager>();

            // CharacterSpawner作成
            GameObject spawnerObj = new GameObject("CharacterSpawner");
            spawnerObj.transform.SetParent(characterManagerObj.transform);
            var spawner = spawnerObj.AddComponent<CharacterSpawner>();

            // SpawnerにPrefabを設定
            SerializedObject spawnerSO = new SerializedObject(spawner);
            if (prefabList != null)
            {
                spawnerSO.FindProperty("_prefabList").objectReferenceValue = prefabList;
            }
            if (fallbackPrefab != null)
            {
                spawnerSO.FindProperty("_characterPrefab").objectReferenceValue = fallbackPrefab;
            }
            spawnerSO.FindProperty("_attackEffectPrefab").objectReferenceValue = attackEffectPrefab;
            spawnerSO.ApplyModifiedPropertiesWithoutUndo();

            // GameManagerにCharacterManagerを設定
            SerializedObject gameManagerSO = new SerializedObject(gameManager);
            gameManagerSO.FindProperty("characterManager").objectReferenceValue = characterManager;
            gameManagerSO.ApplyModifiedPropertiesWithoutUndo();

            // フィールド（床）作成 - FIELD_SIZE に合わせてスケール
            // Unity Plane は 10x10 units なので、1000x1000 にするには 100x100 スケール
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Field";
            floor.transform.position = Vector3.zero;
            float planeScale = GameConstants.FIELD_SIZE / 10f; // 1000 / 10 = 100
            floor.transform.localScale = new Vector3(planeScale, 1f, planeScale);

            // マテリアル設定
            var floorRenderer = floor.GetComponent<MeshRenderer>();
            var floorMat = new Material(Shader.Find("Standard"));
            floorMat.color = new Color(0.3f, 0.5f, 0.3f); // 緑っぽい色
            floorRenderer.material = floorMat;

            // Directional Light調整
            var lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                    light.intensity = 1.2f;
                }
            }

            // カメラ調整（上からの俯瞰）+ CameraController追加
            var cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var camera in cameras)
            {
                // フィールドサイズに合わせてカメラ位置調整
                float camHeight = GameConstants.FIELD_SIZE * 0.6f;
                camera.transform.position = new Vector3(0f, camHeight, -camHeight * 0.5f);
                camera.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
                camera.fieldOfView = 60f;
                camera.farClipPlane = GameConstants.FIELD_SIZE * 3f;

                // CameraController追加
                if (camera.GetComponent<CameraController>() == null)
                {
                    camera.gameObject.AddComponent<CameraController>();
                }
            }

            // GPUInstancing_Exercise作成（課題3用）
            GameObject gpuInstancingObj = new GameObject("GPUInstancing_Exercise");
            gpuInstancingObj.transform.SetParent(gameManagerObj.transform);
            gpuInstancingObj.AddComponent<GPUInstancing_Exercise>();

            // シーンを保存
            EditorSceneManager.SaveScene(newScene, scenePath);

            Debug.Log($"MainGame scene created at: {scenePath}");
            EditorUtility.DisplayDialog("Success", "MainGame scene created!", "OK");
        }

        [MenuItem("PerformanceTraining/Quick Setup (Create All)")]
        public static void QuickSetup()
        {
            CreateAllCharacterPrefabs();
            CreateAttackEffectPrefab();
            CreateMainGameScene();
        }

        /// <summary>
        /// 現在のシーンにGPUInstancing_Exerciseコンポーネントを追加
        /// </summary>
        [MenuItem("PerformanceTraining/Add GPUInstancing_Exercise to Scene")]
        public static void AddGPUInstancingExerciseToScene()
        {
            // 既存のコンポーネントを確認
            var existing = Object.FindAnyObjectByType<GPUInstancing_Exercise>();
            if (existing != null)
            {
                EditorUtility.DisplayDialog("Info", "GPUInstancing_Exercise already exists in the scene.", "OK");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            // GameManagerを探す（親として使用）
            var gameManager = Object.FindAnyObjectByType<GameManager>();
            GameObject parent = gameManager != null ? gameManager.gameObject : null;

            // 新しいGameObjectを作成
            GameObject gpuInstancingObj = new GameObject("GPUInstancing_Exercise");
            if (parent != null)
            {
                gpuInstancingObj.transform.SetParent(parent.transform);
            }

            // コンポーネントを追加
            gpuInstancingObj.AddComponent<GPUInstancing_Exercise>();

            // シーンを汚しマークにする
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            // 選択
            Selection.activeGameObject = gpuInstancingObj;

            Debug.Log("GPUInstancing_Exercise added to scene.");
            EditorUtility.DisplayDialog("Success", "GPUInstancing_Exercise component added to scene.\nDon't forget to save the scene!", "OK");
        }
    }
}
