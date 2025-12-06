using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PerformanceTraining.Core
{
    /// <summary>
    /// キャラクターの頭上に表示するUI（HP/名前）
    /// カメラとの距離に応じて表示/非表示を切り替え
    /// uGUI Canvas + TextMeshPro使用
    /// </summary>
    public class CharacterUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Character _character;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private Image _hpBarFill;
        [SerializeField] private Image _hpBarBackground;

        [Header("Settings")]
        [SerializeField] private float _maxVisibleDistance = 30f;
        [SerializeField] private float _fadeStartDistance = 20f;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 1.5f, 0f);

        private Camera _mainCamera;
        private Transform _cameraTransform;

        private void Awake()
        {
            // 自身のCharacterコンポーネントを取得
            if (_character == null)
            {
                _character = GetComponentInParent<Character>();
            }

            // CanvasGroupを取得
            if (_canvasGroup == null && _canvas != null)
            {
                _canvasGroup = _canvas.GetComponent<CanvasGroup>();
            }
        }

        private void Start()
        {
            _mainCamera = Camera.main;
            if (_mainCamera != null)
            {
                _cameraTransform = _mainCamera.transform;
            }

            // 初期化
            if (_character != null)
            {
                UpdateUI();

                // イベント登録
                _character.OnDamaged += OnCharacterDamaged;
                _character.OnDeath += OnCharacterDeath;
            }
        }

        private void OnDestroy()
        {
            if (_character != null)
            {
                _character.OnDamaged -= OnCharacterDamaged;
                _character.OnDeath -= OnCharacterDeath;
            }
        }

        private void LateUpdate()
        {
            if (_character == null || !_character.IsAlive || _cameraTransform == null)
            {
                SetVisible(false);
                return;
            }

            // 位置をキャラクターの頭上に設定
            transform.position = _character.transform.position + _offset;

            // カメラの方を向く（Y軸ビルボード - 上下には傾けない）
            Vector3 lookDir = transform.position - _cameraTransform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }

            // カメラとの距離による表示制御
            float distance = Vector3.Distance(transform.position, _cameraTransform.position);

            if (distance > _maxVisibleDistance)
            {
                // 距離が遠すぎる場合は非表示
                SetVisible(false);
            }
            else
            {
                SetVisible(true);

                // TODO: パフォーマンス課題 - 毎フレーム文字列結合によるGC Alloc
                // 最適化: StringBuilderを使用、または変更時のみ更新
                UpdateNameText();

                // フェード処理
                if (_canvasGroup != null)
                {
                    if (distance > _fadeStartDistance)
                    {
                        float fadeRange = _maxVisibleDistance - _fadeStartDistance;
                        float fadeAmount = 1f - (distance - _fadeStartDistance) / fadeRange;
                        _canvasGroup.alpha = Mathf.Clamp01(fadeAmount);
                    }
                    else
                    {
                        _canvasGroup.alpha = 1f;
                    }
                }
            }
        }

        /// <summary>
        /// 名前テキストを毎フレーム更新（ボトルネック）
        /// </summary>
        private void UpdateNameText()
        {
            if (_nameText == null || _character == null) return;

            // ボトルネック: string.Format による文字列生成（毎フレーム新しい文字列を生成）
            string typeStr = _character.Type.ToString();
            string stateStr = _character.State.ToString();
            int hpPercent = (int)(_character.HealthPercent * 100);

            // 複数回の文字列結合でGC Allocを増やす
            string displayName = _character.CharacterName;
            displayName = displayName + " [" + typeStr + "]";
            displayName = string.Format("{0} ({1}%)", displayName, hpPercent);

            _nameText.text = displayName;
        }

        private void SetVisible(bool visible)
        {
            if (_canvas != null)
            {
                _canvas.enabled = visible;
            }
        }

        private void OnCharacterDamaged(Character character, float damage)
        {
            UpdateUI();
        }

        private void OnCharacterDeath(Character character)
        {
            SetVisible(false);
        }

        /// <summary>
        /// UIを更新
        /// </summary>
        public void UpdateUI()
        {
            if (_character == null) return;

            // HP更新
            if (_hpBarFill != null)
            {
                _hpBarFill.fillAmount = _character.HealthPercent;

                // HPに応じて色を変更
                if (_character.HealthPercent > 0.5f)
                {
                    _hpBarFill.color = Color.green;
                }
                else if (_character.HealthPercent > 0.25f)
                {
                    _hpBarFill.color = Color.yellow;
                }
                else
                {
                    _hpBarFill.color = Color.red;
                }
            }

            // 名前更新
            if (_nameText != null)
            {
                _nameText.text = _character.CharacterName;
            }
        }

        /// <summary>
        /// UIを初期化（プレハブ生成時に呼び出し）
        /// </summary>
        public void Initialize(Character character)
        {
            _character = character;

            if (_character != null)
            {
                // イベント登録
                _character.OnDamaged += OnCharacterDamaged;
                _character.OnDeath += OnCharacterDeath;

                UpdateUI();
            }
        }

        /// <summary>
        /// 最大表示距離を設定
        /// </summary>
        public void SetMaxVisibleDistance(float distance)
        {
            _maxVisibleDistance = distance;
            _fadeStartDistance = distance * 0.66f;
        }
    }
}
