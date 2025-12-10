using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PerformanceTraining.Core
{
    /// <summary>
    /// キャラクターの頭上に表示するUI（HP/名前）
    /// Screen Space Canvasを使用し、ワールド座標をスクリーン座標に変換して表示
    /// </summary>
    public class CharacterUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Character _character;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private Image _hpBarFill;
        [SerializeField] private Image _hpBarBackground;

        [Header("Settings")]
        [SerializeField] private float _maxVisibleDistance = 100f;
        [SerializeField] private float _fadeStartDistance = 80f;
        [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 2.0f, 0f);

        private Camera _mainCamera;
        private Transform _cameraTransform;
        private RectTransform _rectTransform;
        private Canvas _screenSpaceCanvas;
        private static Canvas _sharedScreenSpaceCanvas;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();

            // CanvasGroupを取得
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void Start()
        {
            _mainCamera = Camera.main;
            if (_mainCamera != null)
            {
                _cameraTransform = _mainCamera.transform;
            }

            // Screen Space Canvasに再ペアレント
            ReparentToScreenSpaceCanvas();

            // _characterがあればUIを更新（Initializeで設定済みの場合）
            if (_character != null)
            {
                UpdateUI();
            }
        }

        /// <summary>
        /// Screen Space Canvasを取得または作成し、自身を子として設定
        /// </summary>
        private void ReparentToScreenSpaceCanvas()
        {
            if (_sharedScreenSpaceCanvas == null)
            {
                // 既存のScreen Space Canvasを探す
                var existingCanvas = GameObject.Find("CharacterUICanvas");
                if (existingCanvas != null)
                {
                    _sharedScreenSpaceCanvas = existingCanvas.GetComponent<Canvas>();
                }

                // なければ作成
                if (_sharedScreenSpaceCanvas == null)
                {
                    var canvasObj = new GameObject("CharacterUICanvas");
                    _sharedScreenSpaceCanvas = canvasObj.AddComponent<Canvas>();
                    _sharedScreenSpaceCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    _sharedScreenSpaceCanvas.sortingOrder = 100;

                    // CanvasScaler設定（1920x1080基準、Height優先）
                    var scaler = canvasObj.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920f, 1080f);
                    scaler.matchWidthOrHeight = 1f; // Height優先

                    canvasObj.AddComponent<GraphicRaycaster>();
                    DontDestroyOnLoad(canvasObj);
                }
            }

            _screenSpaceCanvas = _sharedScreenSpaceCanvas;

            // 自身を再ペアレント
            transform.SetParent(_screenSpaceCanvas.transform, false);

            // RectTransform設定
            if (_rectTransform == null)
            {
                _rectTransform = gameObject.AddComponent<RectTransform>();
            }
            _rectTransform.anchorMin = Vector2.zero;
            _rectTransform.anchorMax = Vector2.zero;
            _rectTransform.pivot = new Vector2(0.5f, 0f);
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
            if (_character == null || !_character.IsAlive || _mainCamera == null)
            {
                SetVisible(false);
                return;
            }

            // キャラクターのワールド座標 + オフセット
            Vector3 worldPos = _character.transform.position + _worldOffset;

            // カメラの後ろにいる場合は非表示
            Vector3 toCharacter = worldPos - _cameraTransform.position;
            if (Vector3.Dot(_cameraTransform.forward, toCharacter) < 0)
            {
                SetVisible(false);
                return;
            }

            // ワールド座標をスクリーン座標に変換
            Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);

            // 画面外の場合は非表示
            if (screenPos.x < 0 || screenPos.x > Screen.width ||
                screenPos.y < 0 || screenPos.y > Screen.height)
            {
                SetVisible(false);
                return;
            }

            // CanvasScalerを考慮した座標変換（1920x1080基準、Height優先）
            float scaleFactor = Screen.height / 1080f;
            Vector2 canvasPos = new Vector2(screenPos.x / scaleFactor, screenPos.y / scaleFactor);

            // RectTransformの位置を更新
            _rectTransform.anchoredPosition = canvasPos;

            // カメラとの距離による表示制御
            float distance = Vector3.Distance(worldPos, _cameraTransform.position);

            if (distance > _maxVisibleDistance)
            {
                SetVisible(false);
            }
            else
            {
                SetVisible(true);

                // 名前とHP表示を更新
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

                // スケールは常に1
                _rectTransform.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// 名前テキストを更新
        /// </summary>
        private void UpdateNameText()
        {
            if (_nameText == null || _character == null) return;

            // TODO: この実装を最適化してください
            // 毎フレーム文字列結合によりGC Allocが発生します
            string displayText = _character.CharacterName + " HP:" +
                Mathf.RoundToInt(_character.Stats.currentHealth).ToString() + "/" +
                Mathf.RoundToInt(_character.Stats.maxHealth).ToString();
            _nameText.text = displayText;
        }

        private void SetVisible(bool visible)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.blocksRaycasts = visible;
            }
            gameObject.SetActive(visible);
        }

        private void OnCharacterDamaged(Character character, float damage)
        {
            UpdateUI();
        }

        private void OnCharacterDeath(Character character)
        {
            // キャラクター死亡時はUI自体を破棄
            Destroy(gameObject);
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
