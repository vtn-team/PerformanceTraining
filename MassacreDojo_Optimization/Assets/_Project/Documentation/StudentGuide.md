# パフォーマンス最適化 学習ガイド

## はじめに

このプロジェクトでは、シンプルな無双系アクションゲーム「千人斬り道場」を題材に、
Unityにおけるパフォーマンス最適化の基本を学びます。

### 学習目標

| 目標 | 内容 |
|------|------|
| メモリ効率の理解 | GCアロケーションの削減手法を習得 |
| CPU最適化の理解 | 計算量削減とキャッシュ戦略を習得 |
| トレードオフの判断 | メモリとCPUの相互関係を理解 |
| 計測習慣の定着 | Profilerを使った計測・分析手法を習得 |

### 最終目標

**敵500体でFPS 60を達成する**

---

## 操作方法

### プレイヤー操作
- **WASD**: 移動
- **マウス移動**: 向きを変える
- **左クリック**: 攻撃
- **スペース**: 回避

### デバッグ操作
- **F1**: 敵50体追加
- **F2**: ゲームリセット
- **F3**: 一時停止/再開
- **F4**: 全最適化ON/OFF切替
- **F5**: パフォーマンスモニター表示切替
- **F6**: 詳細情報表示切替

---

## Profilerの使い方

### 開き方
1. `Window` > `Analysis` > `Profiler` を選択

### 確認すべき項目

| 項目 | 確認場所 | 目標値 |
|------|---------|--------|
| GC Alloc | CPU > GC Alloc列 | < 1KB/frame |
| Frame Time | CPU > 総時間 | < 16.67ms (60FPS) |
| 関数別時間 | CPU > Hierarchy | ボトルネック特定 |

### Deep Profile
より詳細な計測が必要な場合は、Profilerウィンドウの「Deep Profile」を有効にします。
ただし、Deep Profileは非常に重いため、計測時のみ有効にしてください。

---

## 課題一覧

### 課題1: ゼロアロケーション（1時間）

**目標**: Update内でのGCアロケーションをゼロにする

**ファイル**: `Scripts/Exercises/Memory/ZeroAllocation_Exercise.cs`

| Step | 問題 | 学習ポイント |
|------|------|-------------|
| 1 | Instantiate/Destroy乱発 | オブジェクトプール |
| 2 | 文字列結合 | StringBuilder |
| 3 | 毎回new Action | デリゲートキャッシュ |
| 4 | 毎回new List | コレクション再利用 |

**確認方法**:
- Profiler > CPU > GC Alloc を確認
- Before: 50KB+/frame → After: 1KB以下/frame

---

### 課題2: CPU計算キャッシュ（1時間）

**目標**: CPU負荷の高い処理を最適化する

**ファイル**: `Scripts/Exercises/CPU/CPUOptimization_Exercise.cs`

| Step | 問題 | 学習ポイント |
|------|------|-------------|
| 1 | O(n²)総当たり検索 | 空間分割（グリッド） |
| 2 | 全敵が毎フレーム更新 | 更新分散（Staggering） |
| 3 | Vector3.Distance使用 | sqrMagnitude |

**確認方法**:
- Profiler > CPU > Frame Time を確認
- Before: 40ms+ → After: 15ms以下

---

### 課題3: メモリ↔CPUトレードオフ（30分）

**目標**: メモリとCPUのトレードオフを理解する

**ファイル**:
- `Scripts/Exercises/Tradeoff/TrigLUT_Exercise.cs`
- `Scripts/Exercises/Tradeoff/VisibilityMap_Exercise.cs`

#### 3-A: 三角関数LUT
- メモリを使ってCPU計算を削減
- メモリ: +2.8KB、CPU: 2-5倍高速化

#### 3-B: 可視性マップ
- メモリを使ってRaycastを削減
- メモリ: グリッドサイズ²、CPU: 10-100倍高速化

---

### 課題4: グラフィクス最適化（30分）

**目標**: Unity設定変更でグラフィクス最適化

詳細は `GraphicsGuide.md` を参照してください。

---

## よくある間違いと対処

| 間違い | 症状 | 対処 |
|--------|------|------|
| プールのReturn忘れ | メモリリーク | OnDisable/OnDestroyでReturn |
| StringBuilder未クリア | 文字列が連結される | Clear()を最初に呼ぶ |
| sqrMagnitude比較ミス | 距離判定がおかしい | 閾値も2乗する |
| 空間分割の範囲外 | エラー/検索漏れ | クランプ処理を追加 |

---

## チェックリスト

### 課題1完了チェック
- [ ] オブジェクトプールが機能している
- [ ] StringBuilderで文字列を構築している
- [ ] デリゲートがキャッシュされている
- [ ] リストが再利用されている
- [ ] GC Alloc < 1KB/frame

### 課題2完了チェック
- [ ] 空間分割が機能している
- [ ] 更新分散が機能している
- [ ] sqrMagnitudeを使用している
- [ ] Frame Time < 15ms

### 課題3完了チェック
- [ ] 三角関数LUTが機能している
- [ ] 可視性マップが機能している
- [ ] トレードオフを理解している

### 最終確認
- [ ] 敵500体でFPS 60達成
- [ ] 全最適化が有効な状態で計測

---

## 参考資料

- [Unity Profiler公式ドキュメント](https://docs.unity3d.com/Manual/Profiler.html)
- [Unity最適化のベストプラクティス](https://docs.unity3d.com/Manual/BestPracticeGuides.html)
