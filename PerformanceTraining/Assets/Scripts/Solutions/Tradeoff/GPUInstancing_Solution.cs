using System.Collections.Generic;
using UnityEngine;
using PerformanceTraining.Core;

namespace PerformanceTraining.Solutions.Tradeoff
{
    /// <summary>
    /// 【課題3: トレードオフ - GPU Instancing 解答】
    ///
    /// このファイルは教員用の解答です。
    /// 学生には見せないでください。
    ///
    /// 修正箇所①: CollectInstanceData()
    ///   - localToWorldMatrix で変換行列を取得
    ///   - キャラクタータイプに応じた色を設定
    ///
    /// 修正箇所②: RenderInstanced()
    ///   - MaterialPropertyBlock で色配列を設定
    ///   - Graphics.DrawMeshInstanced() で一括描画
    ///
    /// 【効果】
    /// Before: 200 Draw Calls（個別描画）
    /// After: 1 Draw Call（GPU Instancing）
    /// </summary>
    public class GPUInstancing_Solution : MonoBehaviour
    {
        [Header("Instancing Settings")]
        [SerializeField] private Mesh _characterMesh;
        [SerializeField] private Material _instanceMaterial;
        [SerializeField] private bool _useInstancing = true;

        [Header("Debug")]
        [SerializeField] private int _lastInstanceCount;

        private CharacterManager _characterManager;

        // インスタンシング用データ
        private Matrix4x4[] _matrices;
        private Vector4[] _colors;
        private MaterialPropertyBlock _propertyBlock;

        private const int MAX_INSTANCES = 1023;

        private void Awake()
        {
            _characterManager = FindObjectOfType<CharacterManager>();

            _matrices = new Matrix4x4[MAX_INSTANCES];
            _colors = new Vector4[MAX_INSTANCES];
            _propertyBlock = new MaterialPropertyBlock();
        }

        private void Start()
        {
            if (_characterMesh == null || _instanceMaterial == null)
            {
                AutoSetupMeshAndMaterial();
            }

            // 開始時に個別レンダラーを無効化
            if (_useInstancing)
            {
                SetIndividualRenderersEnabled(false);
            }
        }

        private void LateUpdate()
        {
            if (!_useInstancing) return;
            if (_characterManager == null) return;

            int count = CollectInstanceData();

            if (count > 0)
            {
                RenderInstanced(count);
            }

            _lastInstanceCount = count;
        }

        /// <summary>
        /// 【解答】インスタンシング用のデータを収集する
        /// </summary>
        private int CollectInstanceData()
        {
            var characters = _characterManager.AliveCharacters;
            int count = Mathf.Min(characters.Count, MAX_INSTANCES);

            for (int i = 0; i < count; i++)
            {
                var character = characters[i];
                if (character == null) continue;

                // 【解答】変換行列を取得
                _matrices[i] = character.transform.localToWorldMatrix;

                // 【解答】キャラクタータイプに応じた色を設定
                _colors[i] = GetColorForCharacter(character);
            }

            return count;
        }

        /// <summary>
        /// 【解答】GPU Instancingで一括描画する
        /// </summary>
        private void RenderInstanced(int count)
        {
            if (_characterMesh == null || _instanceMaterial == null) return;

            // 【解答】MaterialPropertyBlock に色配列を設定
            _propertyBlock.SetVectorArray("_Color", _colors);

            // 【解答】Graphics.DrawMeshInstanced() で一括描画
            Graphics.DrawMeshInstanced(
                _characterMesh,
                0,
                _instanceMaterial,
                _matrices,
                count,
                _propertyBlock
            );
        }

        private Vector4 GetColorForCharacter(Character character)
        {
            switch (character.Type)
            {
                case CharacterType.Warrior:
                    return new Vector4(1f, 0.3f, 0.3f, 1f);
                case CharacterType.Mage:
                    return new Vector4(0.3f, 0.3f, 1f, 1f);
                case CharacterType.Archer:
                    return new Vector4(0.3f, 1f, 0.3f, 1f);
                case CharacterType.Tank:
                    return new Vector4(1f, 1f, 0.3f, 1f);
                case CharacterType.Assassin:
                    return new Vector4(0.8f, 0.3f, 1f, 1f);
                default:
                    return new Vector4(1f, 1f, 1f, 1f);
            }
        }

        private void AutoSetupMeshAndMaterial()
        {
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
                    _instanceMaterial = new Material(meshRenderer.sharedMaterial);
                    _instanceMaterial.enableInstancing = true;
                }
            }
        }

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

        private void OnGUI()
        {
            if (!_useInstancing) return;

            GUILayout.BeginArea(new Rect(Screen.width - 220, 10, 210, 100));
            GUILayout.BeginVertical("box");
            GUILayout.Label("[Solution] GPU Instancing");
            GUILayout.Label($"Instances: {_lastInstanceCount}");
            GUILayout.Label("Mode: Instanced (1 Draw Call)");
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                _useInstancing = !_useInstancing;
                SetIndividualRenderersEnabled(!_useInstancing);
                Debug.Log($"[Solution] GPU Instancing: {(_useInstancing ? "ON" : "OFF")}");
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
    }
}
