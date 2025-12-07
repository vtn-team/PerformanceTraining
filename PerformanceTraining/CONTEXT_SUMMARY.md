# PerformanceTraining プロジェクト コンテキストサマリー

## プロジェクト概要

Unityを使用したパフォーマンス最適化学習用プロジェクト。
学生がProfilerを使いながら最適化を学べる3つの課題を提供。

- **旧名称**: MassacreDojo_Optimization
- **新名称**: PerformanceTraining
- **namespace**: PerformanceTraining

---

## フォルダ構造

```
PerformanceTraining/
└── Assets/
    ├── Scripts/
    │   ├── Core/                    # ゲームコア
    │   │   ├── Character.cs         # キャラクター基底クラス
    │   │   ├── CharacterManager.cs  # キャラクター管理
    │   │   ├── GameManager.cs       # ゲーム管理
    │   │   └── GameConstants.cs     # 定数定義
    │   ├── AI/                      # AIシステム
    │   ├── Exercises/               # 課題ファイル（学生用）
    │   │   ├── Memory/
    │   │   │   └── MemoryOptimization_Exercise.cs
    │   │   ├── CPU/
    │   │   │   └── CPUOptimization_Exercise.cs
    │   │   └── Tradeoff/
    │   │       └── GPUInstancing_Exercise.cs
    │   ├── Solutions/               # 解答ファイル（教員用）
    │   │   ├── Memory/
    │   │   ├── CPU/
    │   │   │   └── CPUOptimization_Solution.cs
    │   │   └── Tradeoff/
    │   │       └── GPUInstancing_Solution.cs
    │   └── Editor/
    │       └── ExerciseManagerWindow.cs  # 課題管理UI
    ├── Tests/
    │   └── PlayMode/
    │       └── Exercise1_MemoryTests.cs  # Unity Test Runner用
    ├── Prefabs/
    ├── Scenes/
    │   └── MainGame.unity
    └── Resources/
        └── LearningSettings.asset
```

---

## 課題一覧

### 課題1: メモリ最適化（GC Alloc削減）

**実装項目**: Profilerを見てGC Allocを削減せよ

**修正箇所**: 5箇所
- 文字列結合（+ 演算子、string.Format、$補間文字列）
- Instantiate/Destroy（Object Pool化が必要）

**攻略のヒント**:
- Window > Analysis > Profiler を開く
- CPU Usage → GC Alloc 列をソート
- 対象フォルダ: Scripts/Core/, Scripts/AI/

**目標値**: 50+ KB/frame → < 1 KB/frame

---

### 課題2: CPU最適化

**実装項目**: 適切な探索ロジックとせよ

**修正箇所**: 2箇所

#### ① 空間分割（O(n) → O(1)）
```csharp
// 実装するメソッド:
- UpdateSpatialGrid(): グリッドにキャラクターを登録
- GetCellIndex(): 座標からセルインデックスを計算
- GetNearbyCharacters(): 周辺9セルからキャラクターを取得

// ExecuteAttackSequence内で:
GetAllCharacters() → GetNearbyCharacters() に置き換え
```

#### ② 処理順序の最適化（パズル形式）
```csharp
// 最悪順序（課題初期状態）:
全取得 → 経路探索 → HPフィルタ → 距離フィルタ → 攻撃

// 最適順序（解答）:
近傍取得 → 距離フィルタ → HPフィルタ → 経路探索 → 攻撃
```

**目標値**: 40+ ms → < 16 ms (60fps)

---

### 課題3: トレードオフ（GPU Instancing）

**実装項目**: GPU Instancingで描画を最適化せよ

**修正箇所**: 2箇所

#### ① CollectInstanceData()
```csharp
// 変換行列を取得
_matrices[i] = character.transform.localToWorldMatrix;

// キャラクタータイプに応じた色を設定
_colors[i] = GetColorForCharacter(character);
```

#### ② RenderInstanced()
```csharp
// MaterialPropertyBlock に色配列を設定
_propertyBlock.SetVectorArray("_Color", _colors);

// 一括描画
Graphics.DrawMeshInstanced(_characterMesh, 0, _instanceMaterial, _matrices, count, _propertyBlock);
```

**確認方法**:
- Game View → Stats で Batches 数を確認
- Frame Debugger で Draw Call を確認
- Iキーで ON/OFF 切り替え

**目標値**: 200+ Draw Calls → 1 Draw Call

---

## アセンブリ定義（.asmdef）

```
PerformanceTraining.Runtime    (Assets/Scripts/)
PerformanceTraining.Editor     (Assets/Scripts/Editor/)
PerformanceTraining.Tests      (Assets/Tests/PlayMode/)
```

---

## ExerciseManagerWindow

**メニュー**: PerformanceTraining > Exercise Manager (Alt+E)

**機能**:
- 課題選択（Memory / CPU / Tradeoff）
- パフォーマンス目標の表示
- 攻略のヒント表示
- ソースコードを開く
- シーンを再生
- テストを実行（Unity Test Runner）
- Profilerを開く

---

## キャラクターシステム

### Character.cs
- `Die()`: HP 0 で `Destroy(gameObject)` を呼び出し
- `Attack()`: ターゲットへの攻撃
- `TakeDamage()`: ダメージ処理
- `Stats.currentHealth`: 現在HP
- `Id`: キャラクター識別子
- `Type`: CharacterType (Warrior, Mage, Archer, Tank, Assassin)

### CharacterManager.cs
- `AliveCharacters`: 生存キャラクターリスト
- `SpawnInitialCharacters()`: 初期スポーン
- `OnDeath`: 死亡イベント

---

## 定数（GameConstants.cs）

```csharp
INITIAL_ENEMY_COUNT = 200       // 初期キャラクター数
MAX_ENEMY_COUNT = 1000          // 最大キャラクター数
FIELD_SIZE = 100f               // フィールドサイズ
CELL_SIZE = 10f                 // 空間分割セルサイズ
GRID_SIZE = 10                  // グリッド数
AI_UPDATE_GROUPS = 10           // 更新分散グループ数
```

---

## 操作キー

| キー | 機能 |
|------|------|
| F1 | キャラクター50体追加 |
| F2 | ゲームリセット |
| F3 | 一時停止/再開 |
| F4 | 全最適化ON/OFF |
| F5 | ランダムバフ |
| F6 | 中央に範囲攻撃 |
| Space | 攻撃シーケンステスト（CPU課題） |
| I | GPU Instancing ON/OFF（課題3） |

---

## 残作業・検討事項

1. **テストの実装**: 各課題のUnity Test Runnerテスト
2. **古いTradeoffファイルの削除**: NeighborCache, DecisionCache, TrigLUT, VisibilityMapの削除検討
3. **シェーダー対応**: GPU Instancing用シェーダーの確認（_Colorプロパティ対応）
4. **ドキュメント**: SetupGuide.md, TeacherSetupGuide.md の最終確認

---

## 変更履歴

- namespace: MassacreDojo → PerformanceTraining
- フォルダ: Assets/_Project/* → Assets/* に移動
- ルートフォルダ: MassacreDojo_Optimization → PerformanceTraining
- 課題1: Object Pool追加（5箇所に）、実装項目を非表示化
- 課題2: 空間分割 + 処理順序パズル（2箇所）、負荷分散は削除
- 課題3: 各種キャッシュ → GPU Instancingに変更
