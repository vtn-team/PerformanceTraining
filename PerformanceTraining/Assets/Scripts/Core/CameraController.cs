using UnityEngine;
using System.Collections;

#pragma warning disable 0414 // 将来の拡張用フィールド

namespace PerformanceTraining.Core
{
    /// <summary>
    /// カメラコントローラー
    /// WASDまたは矢印キーでスクロール、マウスホイールでズーム
    /// クリックで次のキャラクターにジャンプ
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 50f;
        [SerializeField] private float _fastMoveMultiplier = 3f;
        [SerializeField] private float _edgeScrollSpeed = 30f;
        [SerializeField] private float _edgeScrollThreshold = 20f;
        [SerializeField] private bool _enableEdgeScroll = false;

        [Header("Zoom")]
        [SerializeField] private float _zoomSpeed = 20f;
        [SerializeField] private float _minHeight = 3f;
        [SerializeField] private float _maxHeight = 500f;

        [Header("Initial Position")]
        [SerializeField] private Vector3 _initialPosition = Vector3.zero;
        [SerializeField] private float _initialZoom = 20f;

        [Header("Bounds")]
        [SerializeField] private bool _clampToBounds = true;

        [Header("Character Focus")]
        [SerializeField] private float _focusZoomHeight = 50f;

        [Header("Settings")]
        [SerializeField] private LearningSettings _learningSettings;

        private Camera _camera;
        private Vector3 _targetPosition;
        private int _currentCharacterIndex = -1;
        private CharacterManager _characterManager;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            // LearningSettingsを取得
            if (_learningSettings == null)
            {
                _learningSettings = Resources.Load<LearningSettings>("LearningSettings");
            }

            // 課題モードに応じた初期ズーム設定
            SetInitialZoom();
        }

        /// <summary>
        /// 初期位置・ズームを設定
        /// </summary>
        private void SetInitialZoom()
        {
            float backOffset = _initialZoom * 0.5f;
            _targetPosition = new Vector3(_initialPosition.x, _initialZoom, _initialPosition.z - backOffset);
            transform.position = _targetPosition;
        }

        private void Start()
        {
            // CharacterManagerを取得
            if (GameManager.Instance != null)
            {
                _characterManager = GameManager.Instance.CharacterManager;
            }
        }

        private void Update()
        {
            HandleKeyboardMovement();
            HandleEdgeScrolling();
            HandleZoom();
            HandleCharacterJump();
            ApplyMovement();
        }

        private void HandleCharacterJump()
        {
            // Zキーで次のキャラクター
            if (Input.GetKeyDown(KeyCode.Z))
            {
                JumpToNextCharacter();
            }
            // Xキーで前のキャラクター
            if (Input.GetKeyDown(KeyCode.X))
            {
                JumpToPreviousCharacter();
            }
        }

        /// <summary>
        /// 次のキャラクターにジャンプ
        /// </summary>
        public void JumpToNextCharacter()
        {
            if (_characterManager == null)
            {
                if (GameManager.Instance != null)
                    _characterManager = GameManager.Instance.CharacterManager;
                if (_characterManager == null) return;
            }

            var aliveCharacters = _characterManager.AliveCharacters;
            if (aliveCharacters == null || aliveCharacters.Count == 0) return;

            _currentCharacterIndex++;
            if (_currentCharacterIndex >= aliveCharacters.Count)
            {
                _currentCharacterIndex = 0;
            }

            FocusOnCharacter(aliveCharacters[_currentCharacterIndex]);
        }

        /// <summary>
        /// 前のキャラクターにジャンプ
        /// </summary>
        public void JumpToPreviousCharacter()
        {
            if (_characterManager == null)
            {
                if (GameManager.Instance != null)
                    _characterManager = GameManager.Instance.CharacterManager;
                if (_characterManager == null) return;
            }

            var aliveCharacters = _characterManager.AliveCharacters;
            if (aliveCharacters == null || aliveCharacters.Count == 0) return;

            _currentCharacterIndex--;
            if (_currentCharacterIndex < 0)
            {
                _currentCharacterIndex = aliveCharacters.Count - 1;
            }

            FocusOnCharacter(aliveCharacters[_currentCharacterIndex]);
        }

        /// <summary>
        /// 指定キャラクターにフォーカス（追従なし、位置移動のみ）
        /// </summary>
        public void FocusOnCharacter(Character character)
        {
            if (character == null || !character.IsAlive) return;

            Vector3 charPos = character.transform.position;

            // カメラ位置を計算（キャラクターの少し後ろ上方）
            // 現在の高さを維持
            float height = _targetPosition.y;
            float backOffset = height * 0.5f;

            _targetPosition = new Vector3(charPos.x, height, charPos.z - backOffset);

            Debug.Log($"Focus: {character.CharacterName} ({character.Type}) - HP: {character.HealthPercent * 100:F0}%");
        }

        private void HandleKeyboardMovement()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            if (horizontal == 0 && vertical == 0) return;

            float speed = _moveSpeed;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                speed *= _fastMoveMultiplier;
            }

            // カメラの向きに基づいて移動方向を計算
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            Vector3 movement = (forward * vertical + right * horizontal) * speed * Time.unscaledDeltaTime;
            _targetPosition += movement;
        }

        private void HandleEdgeScrolling()
        {
            if (!_enableEdgeScroll) return;
            if (!Application.isFocused) return;

            Vector3 mousePos = Input.mousePosition;
            Vector3 movement = Vector3.zero;

            // 画面端でスクロール
            if (mousePos.x < _edgeScrollThreshold)
            {
                movement.x = -1;
            }
            else if (mousePos.x > Screen.width - _edgeScrollThreshold)
            {
                movement.x = 1;
            }

            if (mousePos.y < _edgeScrollThreshold)
            {
                movement.z = -1;
            }
            else if (mousePos.y > Screen.height - _edgeScrollThreshold)
            {
                movement.z = 1;
            }

            if (movement != Vector3.zero)
            {
                Vector3 forward = transform.forward;
                Vector3 right = transform.right;
                forward.y = 0;
                right.y = 0;
                forward.Normalize();
                right.Normalize();

                Vector3 edgeMovement = (forward * movement.z + right * movement.x) * _edgeScrollSpeed * Time.unscaledDeltaTime;
                _targetPosition += edgeMovement;
            }
        }

        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) < 0.01f) return;

            float zoomAmount = scroll * _zoomSpeed * (_targetPosition.y / 50f);
            _targetPosition.y -= zoomAmount;
            _targetPosition.y = Mathf.Clamp(_targetPosition.y, _minHeight, _maxHeight);
        }

        private void ApplyMovement()
        {
            // フィールド範囲内にクランプ
            if (_clampToBounds)
            {
                float halfSize = GameConstants.FIELD_HALF_SIZE;
                _targetPosition.x = Mathf.Clamp(_targetPosition.x, -halfSize, halfSize);
                _targetPosition.z = Mathf.Clamp(_targetPosition.z, -halfSize, halfSize);
            }

            // スムーズに移動
            transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.unscaledDeltaTime * 10f);
        }

        /// <summary>
        /// 指定位置にカメラを移動
        /// </summary>
        public void FocusOn(Vector3 position)
        {
            _targetPosition = new Vector3(position.x, _targetPosition.y, position.z - _targetPosition.y * 0.5f);
        }

        /// <summary>
        /// フィールド全体を見渡せる位置に移動
        /// </summary>
        public void ViewAll()
        {
            _targetPosition = new Vector3(0, _maxHeight * 0.8f, -_maxHeight * 0.4f);
        }
    }
}
