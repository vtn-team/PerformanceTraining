using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.Collections.Generic;
using System.IO;
using PerformanceTraining.Core;
using PerformanceTraining.AI;

namespace PerformanceTraining.Editor
{
    /// <summary>
    /// CharacterPrefabListにpolyperfectアセットのプレハブを自動アサインするツール
    /// polyperfectモデルをベースにCharacterコンポーネント付きプレハブを生成する
    /// </summary>
    public static class PrefabListSetup
    {
        private const string POLYPERFECT_PATH = "Assets/polyperfect/Low Poly Animated People/- Prefabs";
        private const string PREFAB_LIST_PATH = "Assets/Resources/CharacterPrefabList.asset";
        private const string OUTPUT_PREFAB_PATH = "Assets/Prefabs/Characters/Generated";

        // 各タイプに割り当てるプレハブ名（計30体、各タイプ5体）
        private static readonly Dictionary<CharacterType, string[]> PrefabAssignments = new Dictionary<CharacterType, string[]>
        {
            { CharacterType.Warrior, new[] {
                "man_knight", "man_soldier", "woman_soldier", "man_viking", "woman_viking"
            }},
            { CharacterType.Assassin, new[] {
                "man_ninja", "woman_ninja", "man_pirate", "woman_pirate", "man_punk"
            }},
            { CharacterType.Tank, new[] {
                "man_sumo", "woman_sumo", "man_construction_worker", "woman_construction_worker", "man_hazard"
            }},
            { CharacterType.Mage, new[] {
                "man_wizard", "man_claus", "woman_claus", "man_scientist", "woman_scientist"
            }},
            { CharacterType.Ranger, new[] {
                "man_explorer", "woman_explorer", "man_cowboy", "woman_cowgirl", "man_farm"
            }},
            { CharacterType.Berserker, new[] {
                "man_boxer", "woman_boxer", "man_metalhead", "woman_metalhead", "man_weightlifter"
            }}
        };

        [MenuItem("Tools/Performance Training/Setup Character Prefab List")]
        public static void SetupPrefabList()
        {
            // 出力フォルダを作成
            if (!AssetDatabase.IsValidFolder(OUTPUT_PREFAB_PATH))
            {
                string parent = Path.GetDirectoryName(OUTPUT_PREFAB_PATH).Replace("\\", "/");
                string folderName = Path.GetFileName(OUTPUT_PREFAB_PATH);
                AssetDatabase.CreateFolder(parent, folderName);
            }

            // CharacterPrefabListアセットをロード
            var prefabList = AssetDatabase.LoadAssetAtPath<CharacterPrefabList>(PREFAB_LIST_PATH);
            if (prefabList == null)
            {
                Debug.LogError($"CharacterPrefabList not found at: {PREFAB_LIST_PATH}");
                return;
            }

            // SerializedObjectでアクセス
            var serializedObject = new SerializedObject(prefabList);
            var entriesProperty = serializedObject.FindProperty("_prefabEntries");
            var defaultPrefabProperty = serializedObject.FindProperty("_defaultPrefab");

            // エントリをクリアして再作成
            entriesProperty.ClearArray();

            int totalAssigned = 0;
            GameObject firstPrefab = null;

            foreach (var kvp in PrefabAssignments)
            {
                CharacterType type = kvp.Key;
                string[] modelNames = kvp.Value;

                // 新しいエントリを追加
                entriesProperty.InsertArrayElementAtIndex(entriesProperty.arraySize);
                var entryProperty = entriesProperty.GetArrayElementAtIndex(entriesProperty.arraySize - 1);

                // タイプを設定
                entryProperty.FindPropertyRelative("type").enumValueIndex = (int)type;

                // プレハブリストを取得
                var prefabsProperty = entryProperty.FindPropertyRelative("prefabs");
                prefabsProperty.ClearArray();

                int variantIndex = 1;
                foreach (var modelName in modelNames)
                {
                    string modelPath = $"{POLYPERFECT_PATH}/{modelName}.prefab";
                    var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);

                    if (modelPrefab != null)
                    {
                        // キャラクタープレハブを生成
                        var characterPrefab = CreateCharacterPrefab(modelPrefab, type, variantIndex);

                        if (characterPrefab != null)
                        {
                            prefabsProperty.InsertArrayElementAtIndex(prefabsProperty.arraySize);
                            prefabsProperty.GetArrayElementAtIndex(prefabsProperty.arraySize - 1).objectReferenceValue = characterPrefab;
                            totalAssigned++;

                            if (firstPrefab == null)
                            {
                                firstPrefab = characterPrefab;
                            }

                            Debug.Log($"Created: {type}_{variantIndex:D2} <- {modelName}");
                            variantIndex++;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Model not found: {modelPath}");
                    }
                }
            }

            // デフォルトプレハブを設定
            if (firstPrefab != null)
            {
                defaultPrefabProperty.objectReferenceValue = firstPrefab;
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(prefabList);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"CharacterPrefabList setup complete! Created and assigned {totalAssigned} prefabs.");
        }

        /// <summary>
        /// polyperfectモデルからCharacterプレハブを生成
        /// </summary>
        private static GameObject CreateCharacterPrefab(GameObject modelPrefab, CharacterType type, int variantIndex)
        {
            string prefabName = $"Character_{type}_{variantIndex:D2}";
            string prefabPath = $"{OUTPUT_PREFAB_PATH}/{prefabName}.prefab";

            // 既存のプレハブがあれば削除
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                AssetDatabase.DeleteAsset(prefabPath);
            }

            // ルートGameObjectを作成
            var root = new GameObject(prefabName);

            // Characterコンポーネントを追加
            root.AddComponent<Character>();

            // CharacterAIコンポーネントを追加
            root.AddComponent<CharacterAI>();

            // モデルを子として配置
            var modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
            modelInstance.name = "Visual";
            modelInstance.transform.SetParent(root.transform);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;

            // polyperfectのスクリプトを無効化/削除（競合を防ぐ）
            RemovePolyperfectScripts(modelInstance);

            // CharacterUIは共通プレハブを使用するため、ここでは追加しない

            // プレハブとして保存
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

            // 一時オブジェクトを破棄
            Object.DestroyImmediate(root);

            return prefab;
        }

        /// <summary>
        /// polyperfectアセットのスクリプトを削除（CharacterAIと競合するため）
        /// </summary>
        private static void RemovePolyperfectScripts(GameObject obj)
        {
            // 全ての子オブジェクトを含めてスクリプトを探す
            var allComponents = obj.GetComponentsInChildren<MonoBehaviour>(true);

            // 削除対象リスト（イテレーション中に削除すると問題が起きるため）
            var toRemove = new List<MonoBehaviour>();

            foreach (var component in allComponents)
            {
                if (component == null) continue;

                // Polyperfect名前空間のスクリプトは全て削除
                string namespaceName = component.GetType().Namespace ?? "";
                string typeName = component.GetType().Name;

                if (namespaceName.Contains("Polyperfect") ||
                    typeName.Contains("Wander") ||
                    typeName.Contains("People_") ||
                    typeName.Contains("Common_") ||
                    typeName.Contains("Animal_"))
                {
                    toRemove.Add(component);
                }
            }

            // 削除実行
            foreach (var component in toRemove)
            {
                if (component != null)
                {
                    Object.DestroyImmediate(component);
                }
            }

            // NavMeshAgentも削除
            var navAgents = obj.GetComponentsInChildren<NavMeshAgent>(true);
            foreach (var agent in navAgents)
            {
                Object.DestroyImmediate(agent);
            }

            // Rigidbodyも削除（物理演算の競合を防ぐ）
            var rigidbodies = obj.GetComponentsInChildren<Rigidbody>(true);
            foreach (var rb in rigidbodies)
            {
                Object.DestroyImmediate(rb);
            }

            // Colliderも削除（独自のコリジョンを使うため）
            var colliders = obj.GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders)
            {
                Object.DestroyImmediate(col);
            }
        }

        private const string CHARACTER_UI_PREFAB_PATH = "Assets/Prefabs/UI/CharacterUI.prefab";

        [MenuItem("Tools/Performance Training/Create CharacterUI Prefab")]
        public static void CreateCharacterUIPrefab()
        {
            // 出力フォルダを作成
            string uiFolder = "Assets/Prefabs/UI";
            if (!AssetDatabase.IsValidFolder(uiFolder))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
            }

            // 既存のプレハブがあれば削除
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CHARACTER_UI_PREFAB_PATH) != null)
            {
                AssetDatabase.DeleteAsset(CHARACTER_UI_PREFAB_PATH);
            }

            // CharacterUI用のルートオブジェクトを作成
            var uiRoot = new GameObject("CharacterUI");
            uiRoot.transform.localPosition = Vector3.zero;
            uiRoot.transform.localRotation = Quaternion.identity;
            uiRoot.transform.localScale = Vector3.one;

            // RectTransformを追加
            var rectTransform = uiRoot.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(100f, 30f);
            rectTransform.localScale = Vector3.one;

            // CanvasGroup
            var canvasGroup = uiRoot.AddComponent<CanvasGroup>();

            // HPBar Background
            var hpBgObj = new GameObject("HPBarBackground");
            hpBgObj.transform.SetParent(uiRoot.transform, false);
            hpBgObj.transform.localScale = Vector3.one;
            var hpBgRect = hpBgObj.AddComponent<RectTransform>();
            hpBgRect.anchoredPosition = new Vector2(0f, 0f);
            hpBgRect.sizeDelta = new Vector2(100f, 10f);
            hpBgRect.localScale = Vector3.one;
            var hpBgImage = hpBgObj.AddComponent<Image>();
            hpBgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // HPBar Fill
            var hpFillObj = new GameObject("HPBarFill");
            hpFillObj.transform.SetParent(hpBgObj.transform, false);
            hpFillObj.transform.localScale = Vector3.one;
            var hpFillRect = hpFillObj.AddComponent<RectTransform>();
            hpFillRect.anchorMin = Vector2.zero;
            hpFillRect.anchorMax = Vector2.one;
            hpFillRect.offsetMin = Vector2.zero;
            hpFillRect.offsetMax = Vector2.zero;
            hpFillRect.localScale = Vector3.one;
            var hpFillImage = hpFillObj.AddComponent<Image>();
            hpFillImage.color = Color.green;
            hpFillImage.type = Image.Type.Filled;
            hpFillImage.fillMethod = Image.FillMethod.Horizontal;
            hpFillImage.fillOrigin = 0;

            // Name Text
            var nameTextObj = new GameObject("NameText");
            nameTextObj.transform.SetParent(uiRoot.transform, false);
            nameTextObj.transform.localScale = Vector3.one;
            var nameTextRect = nameTextObj.AddComponent<RectTransform>();
            nameTextRect.anchoredPosition = new Vector2(0f, 12f);
            nameTextRect.sizeDelta = new Vector2(100f, 14f);
            nameTextRect.localScale = Vector3.one;
            var nameText = nameTextObj.AddComponent<TextMeshProUGUI>();
            nameText.text = "Name";
            nameText.fontSize = 12;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = Color.white;

            // CharacterUI コンポーネントを追加
            var characterUI = uiRoot.AddComponent<CharacterUI>();

            // SerializedObjectを使用してprivateフィールドに値を設定
            var so = new SerializedObject(characterUI);
            so.FindProperty("_canvasGroup").objectReferenceValue = canvasGroup;
            so.FindProperty("_nameText").objectReferenceValue = nameText;
            so.FindProperty("_hpBarFill").objectReferenceValue = hpFillImage;
            so.FindProperty("_hpBarBackground").objectReferenceValue = hpBgImage;
            so.ApplyModifiedProperties();

            // プレハブとして保存
            var prefab = PrefabUtility.SaveAsPrefabAsset(uiRoot, CHARACTER_UI_PREFAB_PATH);

            // 一時オブジェクトを破棄
            Object.DestroyImmediate(uiRoot);

            Debug.Log($"CharacterUI prefab created at: {CHARACTER_UI_PREFAB_PATH}");
        }

        [MenuItem("Tools/Performance Training/List Available Prefabs")]
        public static void ListAvailablePrefabs()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { POLYPERFECT_PATH });
            Debug.Log($"Found {guids.Length} prefabs in {POLYPERFECT_PATH}:");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var fileName = Path.GetFileNameWithoutExtension(path);
                Debug.Log($"  - {fileName}");
            }
        }

        [MenuItem("Tools/Performance Training/Clear Generated Prefabs")]
        public static void ClearGeneratedPrefabs()
        {
            if (AssetDatabase.IsValidFolder(OUTPUT_PREFAB_PATH))
            {
                var guids = AssetDatabase.FindAssets("t:Prefab", new[] { OUTPUT_PREFAB_PATH });
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    AssetDatabase.DeleteAsset(path);
                }
                Debug.Log($"Cleared {guids.Length} generated prefabs.");
            }
        }
    }
}
