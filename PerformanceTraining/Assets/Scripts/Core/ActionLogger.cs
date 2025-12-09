using UnityEngine;
using System.Text;

namespace PerformanceTraining.Core
{
    /// <summary>
    /// 【回答例】行動ログシステム - StringBuilderによる効率的な文字列構築
    ///
    /// ポイント:
    /// - StringBuilderをstaticで保持し再利用（GC Alloc削減）
    /// - リングバッファで古いログを自動破棄
    /// - 遅延更新で必要時のみ文字列を構築
    /// </summary>
    public class ActionLogger
    {
        // ログ表示用のStringBuilder（再利用してGC Allocを削減）
        private static readonly StringBuilder _logBuilder = new StringBuilder(1024);

        // ログエントリを保持するリングバッファ
        private const int MAX_LOG_ENTRIES = 10;
        private readonly string[] _logEntries = new string[MAX_LOG_ENTRIES];
        private int _logIndex = 0;
        private int _logCount = 0;

        // ログ表示用の文字列（更新時のみ再構築）
        private string _actionLogString = "";
        private bool _logDirty = false;

        /// <summary>
        /// 行動ログ文字列を取得
        /// </summary>
        public string LogString => _actionLogString;

        /// <summary>
        /// ログが更新されたか確認し、必要なら文字列を再構築
        /// </summary>
        public void UpdateIfDirty()
        {
            if (_logDirty)
            {
                BuildLogString();
                _logDirty = false;
            }
        }

        /// <summary>
        /// 行動ログにエントリを追加
        /// リングバッファで古いログを自動的に破棄
        /// </summary>
        private void AddEntry(string entry)
        {
            _logEntries[_logIndex] = entry;
            _logIndex = (_logIndex + 1) % MAX_LOG_ENTRIES;
            _logCount = Mathf.Min(_logCount + 1, MAX_LOG_ENTRIES);
            _logDirty = true;
        }

        /// <summary>
        /// 行動ログ文字列を構築（StringBuilderを再利用）
        /// </summary>
        private void BuildLogString()
        {
            _logBuilder.Clear();
            _logBuilder.Append("=== Action Log ===\n");

            // リングバッファから新しい順にログを取得
            int startIndex = (_logIndex - _logCount + MAX_LOG_ENTRIES) % MAX_LOG_ENTRIES;
            for (int i = _logCount - 1; i >= 0; i--)
            {
                int index = (startIndex + i) % MAX_LOG_ENTRIES;
                if (_logEntries[index] != null)
                {
                    _logBuilder.Append(_logEntries[index]).Append('\n');
                }
            }

            _actionLogString = _logBuilder.ToString();
        }

        /// <summary>
        /// キルログを追加
        /// </summary>
        public void LogKill(Character killer, Character victim)
        {
            _logBuilder.Clear();
            _logBuilder.Append('[')
                       .Append(Time.time.ToString("F1"))
                       .Append("s] ")
                       .Append(killer.CharacterName)
                       .Append(" defeated ")
                       .Append(victim.CharacterName);

            AddEntry(_logBuilder.ToString());
        }

        /// <summary>
        /// 死亡ログを追加
        /// </summary>
        public void LogDeath(Character character)
        {
            _logBuilder.Clear();
            _logBuilder.Append('[')
                       .Append(Time.time.ToString("F1"))
                       .Append("s] ")
                       .Append(character.CharacterName)
                       .Append(" (")
                       .Append(character.Type)
                       .Append(") was eliminated");

            AddEntry(_logBuilder.ToString());
        }

        /// <summary>
        /// スポーンログを追加
        /// </summary>
        public void LogSpawn(Character character)
        {
            _logBuilder.Clear();
            _logBuilder.Append('[')
                       .Append(Time.time.ToString("F1"))
                       .Append("s] ")
                       .Append(character.CharacterName)
                       .Append(" (")
                       .Append(character.Type)
                       .Append(") joined the battle");

            AddEntry(_logBuilder.ToString());
        }

        /// <summary>
        /// ログをクリア
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < MAX_LOG_ENTRIES; i++)
            {
                _logEntries[i] = null;
            }
            _logIndex = 0;
            _logCount = 0;
            _actionLogString = "";
            _logDirty = false;
        }
    }
}
