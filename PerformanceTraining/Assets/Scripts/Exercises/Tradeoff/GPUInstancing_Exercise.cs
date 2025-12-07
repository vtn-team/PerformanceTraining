using System.Collections.Generic;
using UnityEngine;
using PerformanceTraining.Core;

namespace PerformanceTraining.Exercises.Tradeoff
{
    /// <summary>
    /// 【課題3: トレードオフ - GPU Instancing】
    ///
    /// ■ 実装項目: GPU Instancingで描画を最適化せよ
    ///
    /// 修正箇所は2つ：
    ///
    /// ① CollectInstanceData(): 描画データの収集
    ///    - 各キャラクターのTransformからMatrix4x4を作成
    ///    - 個別プロパティ（色など）をVector4配列に格納
    ///
    /// ② RenderInstanced(): 一括描画の実行
    ///    - Graphics.DrawMeshInstanced() を使用
    ///    - MaterialPropertyBlock で個別プロパティを設定
    ///
    /// 【トレードオフ】
    /// メモリ: Matrix4x4配列 + プロパティ配列（キャラクター数 × データサイズ）
    /// GPU: Draw Call削減（200回 → 1回）
    ///
    /// 【確認方法】
    /// Game View → Stats → Batches の数値を確認
    /// Window → Analysis → Frame Debugger で Draw Call を確認
    /// </summary>
    public class GPUInstancing_Exercise : MonoBehaviour
    {
        [Header("Instancing Settings")]
        [SerializeField] private Mesh _characterMesh;
        [SerializeField] private Material _instanceMaterial;
        [SerializeField] private bool _useInstancing = false;

        [Header("Debug")]
        [SerializeField] private int _lastInstanceCount;
        [SerializeField] private int _lastDrawCalls;

        private CharacterManager _characterManager;

        // インスタンシング用データ（事前確保でGC Alloc削減）
        private Matrix4x4[] _matrices;
        private Vector4[] _colors;
        private MaterialPropertyBlock _propertyBlock;

        private const int MAX_INSTANCES = 1023; // Graphics.DrawMeshInstanced の上限

        private void Awake()
        {
            _characterManager = FindObjectOfType<CharacterManager>();

            // 配列を事前確保
            _matrices = new Matrix4x4[MAX_INSTANCES];
            _colors = new Vector4[MAX_INSTANCES];
            _propertyBlock = new MaterialPropertyBlock();
        }

        private void Start()
        {
            // マテリアルとメッシュの自動取得（設定されていない場合）
            if (_characterMesh == null || _instanceMaterial == null)
            {
                AutoSetupMeshAndMaterial();
            }
        }

        private void LateUpdate()
        {
            if (!_useInstancing) return;
            if (_characterManager == null) return;

            // データ収集
            int count = CollectInstanceData();

            // 一括描画
            if (count > 0)
            {
                RenderInstanced(count);
            }

            _lastInstanceCount = count;
        }

        /// <summary>
        /// インスタンシング用のデータを収集する
        ///
        /// 【課題】この関数を実装してください
        /// </summary>
        /// <returns>収集したインスタンス数</returns>
        private int CollectInstanceData()
        {
            var characters = _characterManager.AliveCharacters;
            int count = Mathf.Min(characters.Count, MAX_INSTANCES);

            // TODO: 実装してください
            // 1. 各キャラクターのTransformからMatrix4x4を作成
            //    _matrices[i] = characters[i].transform.localToWorldMatrix;
            //
            // 2. 各キャラクターの色を取得（キャラクタータイプに応じて）
            //    _colors[i] = GetColorForCharacter(characters[i]);

            for (int i = 0; i < count; i++)
            {
                var character = characters[i];
                if (character == null) continue;

                // TODO: ここにMatrix4x4と色の設定を実装
            }

            return count;
        }

        /// <summary>
        /// GPU Instancingで一括描画する
        ///
        /// 【課題】この関数を実装してください
        /// </summary>
        /// <param name="count">描画するインスタンス数</param>
        private void RenderInstanced(int count)
        {
            if (_characterMesh == null || _instanceMaterial == null) return;

            // TODO: 実装してください
            // 1. MaterialPropertyBlock に色配列を設定
            //    _propertyBlock.SetVectorArray("_Color", _colors);
            //
            // 2. Graphics.DrawMeshInstanced() で一括描画
            //    Graphics.DrawMeshInstanced(_characterMesh, 0, _instanceMaterial, _matrices, count, _propertyBlock);
        }

        /// <summary>
        /// キャラクタータイプに応じた色を取得
        /// </summary>
        private Vector4 GetColorForCharacter(Character character)
        {
            switch (character.Type)
            {
                case CharacterType.Warrior:
                    return new Vector4(1f, 0.3f, 0.3f, 1f); // 赤
                case CharacterType.Mage:
                    return new Vector4(0.3f, 0.3f, 1f, 1f); // 青
                case CharacterType.Ranger:
                    return new Vector4(0.3f, 1f, 0.3f, 1f); // 緑
                case CharacterType.Tank:
                    return new Vector4(1f, 1f, 0.3f, 1f); // 黄
                case CharacterType.Assassin:
                    return new Vector4(0.8f, 0.3f, 1f, 1f); // 紫
                default:
                    return new Vector4(1f, 1f, 1f, 1f); // 白
            }
        }

        /// <summary>
        /// メッシュとマテリアルを自動設定
        /// </summary>
        private void AutoSetupMeshAndMaterial()
        {
            // 最初のキャラクターからメッシュとマテリアルを取得
            if (_characterManager != null && _characterManager.AliveCharacters.Count > 0)
            {
                var firstChar = _characterManager.AliveCharacters[0];
                var meshFilter = firstChar.GetComponentInChildren<MeshFilter>();
                var meshRenderer = firstChar.GetComponentInChildren<MeshRenderer>();

                if (meshFilter != null && _characterMesh == null)
                {
                    _characterMesh = meshFilter.sharedMesh;
                }

                if (meshRenderer != null && _instanceMaterial == null)
                {
                    // GPU Instancing対応マテリアルを作成
                    _instanceMaterial = new Material(meshRenderer.sharedMaterial);
                    _instanceMaterial.enableInstancing = true;
                }
            }
        }

        /// <summary>
        /// 個別MeshRendererの表示/非表示を切り替え
        /// </summary>
        public void SetIndividualRenderersEnabled(bool enabled)
        {
            if (_characterManager == null) return;

            foreach (var character in _characterManager.AliveCharacters)
            {
                if (character == null) continue;
                var renderer = character.GetComponentInChildren<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.enabled = enabled;
                }
            }
        }

        // ================================================================
        // テスト・デバッグ用
        // ================================================================

        private void OnGUI()
        {
            if (!_useInstancing) return;

            GUILayout.BeginArea(new Rect(Screen.width - 220, 10, 210, 100));
            GUILayout.BeginVertical("box");
            GUILayout.Label("GPU Instancing");
            GUILayout.Label($"Instances: {_lastInstanceCount}");
            GUILayout.Label($"Mode: {(_useInstancing ? "Instanced" : "Individual")}");
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void Update()
        {
            // Iキーでインスタンシングの切り替え
            if (Input.GetKeyDown(KeyCode.I))
            {
                _useInstancing = !_useInstancing;
                SetIndividualRenderersEnabled(!_useInstancing);
                Debug.Log($"GPU Instancing: {(_useInstancing ? "ON" : "OFF")}");
            }
        }

        public bool UseInstancing
        {
            get => _useInstancing;
            set
            {
                _useInstancing = value;
                SetIndividualRenderersEnabled(!value);
            }
        }

        public int LastInstanceCount => _lastInstanceCount;
    }
}
