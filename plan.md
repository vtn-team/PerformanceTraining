# パフォーマンスチューニング学習プロジェクト 設計書

## 1. プロジェクト概要

### 1.1 プロジェクト名
**MassacreDojo_Optimization**（千人斬り道場 - 最適化学習）

### 1.2 コンセプト
- シンプルな無双系アクションゲームを題材に、パフォーマンス最適化を学ぶ
- 問題を含むコードを学生が修正し、Profilerで効果を確認する体験型学習
- 全ての最適化機能は実装済みだが「無効化」状態で提供し、学生が有効化して効果を体験

### 1.3 技術スタック
- Unity 2022.3 LTS以上
- Universal Render Pipeline (URP)
- 比較用: DOTS (Entities 1.0+) - 別プロジェクト

### 1.4 教育目標

| 目標 | 内容 |
|------|------|
| メモリ効率の理解 | GCアロケーションの削減手法を習得 |
| CPU最適化の理解 | 計算量削減とキャッシュ戦略を習得 |
| トレードオフの判断 | メモリとCPUの相互関係を理解 |
| グラフィクス最適化 | Unity設定による描画効率化を習得 |
| 計測習慣の定着 | Profilerを使った計測・分析手法を習得 |

---

## 2. 課題設計

### 2.1 学習方法の分類

| カテゴリ | 学習方法 | 課題形式 |
|---------|---------|---------|
| メモリ最適化 | **コードを書き換える** | 穴埋め実装 |
| CPU計算キャッシュ | **コードを書き換える** | 穴埋め実装 |
| トレードオフ | **コードを書き換える + 考察** | 穴埋め実装 + 記述 |
| グラフィクス | **Unity設定を変更する** | 設定手順書に従う |
| DOTS | **別プロジェクトで比較** | 完成版を体験 |

### 2.2 時間配分（4時間コース）

| 時間 | 課題 | 内容 | 形式 |
|------|------|------|------|
| 0:00-0:30 | 準備 | プロジェクト確認、Profiler準備、初期計測 | - |
| 0:30-1:30 | 課題1 | ゼロアロケーション | 穴埋め |
| 1:30-2:30 | 課題2 | CPU計算キャッシュ | 穴埋め |
| 2:30-3:00 | 課題3 | メモリ↔CPUトレードオフ | 穴埋め+考察 |
| 3:00-3:30 | 課題4 | グラフィクス最適化 | 設定変更 |
| 3:30-4:00 | まとめ | 最終計測、振り返り、DOTS比較 | - |

### 2.3 プロジェクト構成
```
MassacreDojo_Optimization/     ← メインプロジェクト（学習用）
MassacreDojo_DOTS/             ← 比較用DOTSプロジェクト（完成版）
```

### 2.4 フォルダ構成
```
Assets/
├── _Project/
│   ├── Scenes/
│   │   └── MainGame.unity
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── GameManager.cs
│   │   │   ├── GameConstants.cs
│   │   │   └── LearningSettings.cs
│   │   ├── Player/
│   │   │   └── PlayerController.cs
│   │   ├── Enemy/
│   │   │   ├── Enemy.cs
│   │   │   ├── EnemySystem.cs           # メモリ問題を含む
│   │   │   ├── EnemyAIManager.cs        # CPU問題を含む
│   │   │   └── EnemyBehavior.cs         # トレードオフ問題を含む
│   │   ├── Exercises/                    # 学生が実装するファイル
│   │   │   ├── Memory/
│   │   │   │   └── ZeroAllocation_Exercise.cs
│   │   │   ├── CPU/
│   │   │   │   └── CPUOptimization_Exercise.cs
│   │   │   └── Tradeoff/
│   │   │       ├── TrigLUT_Exercise.cs
│   │   │       └── VisibilityMap_Exercise.cs
│   │   ├── Solutions/                    # 解答（教員用）
│   │   │   ├── Memory/
│   │   │   ├── CPU/
│   │   │   └── Tradeoff/
│   │   ├── Effects/
│   │   └── UI/
│   │       └── PerformanceMonitor.cs
│   ├── Settings/
│   │   └── GraphicsExercise/
│   ├── Prefabs/
│   ├── Documentation/
│   │   ├── StudentGuide.md
│   │   ├── ExerciseSheet.md
│   │   ├── GraphicsGuide.md
│   │   └── TeacherGuide.md
│   └── Art/
└── Resources/
    └── LearningSettings.asset
```

---

## 3. 課題内容

### 3.1 課題1: ゼロアロケーション（1時間）

#### 概要
Update内でのGCアロケーションをゼロにする4つの技法を学ぶ。

#### 仕込む問題

| Step | 問題 | 問題コード | 学習ポイント |
|------|------|-----------|-------------|
| 1 | オブジェクトプール | `Instantiate()`/`Destroy()`乱発 | プーリングによる再利用 |
| 2 | 文字列結合 | `"Score: " + score.ToString()` | StringBuilderの使用 |
| 3 | デリゲートキャッシュ | 毎フレーム`new Action()` | デリゲートの事前生成 |
| 4 | コレクション再利用 | 毎フレーム`new List<T>()` | Clear()による再利用 |

#### 学生の作業

**ファイル**: `Exercises/Memory/ZeroAllocation_Exercise.cs`
```
■ Step 1: オブジェクトプール（15分）
  - Stack<Enemy>でプール管理
  - Get() / Return() メソッドを実装
  
■ Step 2: 文字列キャッシュ（15分）
  - StringBuilderをフィールドで保持
  - Clear() + Append()で再利用
  
■ Step 3: デリゲートキャッシュ（15分）
  - Action/Funcをフィールドでキャッシュ
  - コンストラクタで一度だけ生成
  
■ Step 4: コレクション再利用（15分）
  - List<T>を事前確保
  - Clear()でリセットして再利用
```

#### 確認方法
- Profiler > CPU > GC Alloc を確認
- Before: 50KB+/frame → After: 1KB以下/frame

---

### 3.2 課題2: CPU計算キャッシュ（1時間）

#### 概要
CPU負荷の高い処理を最適化する3つの技法を学ぶ。

#### 仕込む問題

| Step | 問題 | 問題コード | 学習ポイント |
|------|------|-----------|-------------|
| 1 | 最近接検索 | O(n²)総当たり検索 | 空間分割（グリッド）でO(1)に |
| 2 | AI更新 | 全敵が毎フレーム更新 | 更新分散（Staggering） |
| 3 | 距離計算 | `Vector3.Distance()`使用 | `sqrMagnitude`で平方根削減 |

#### 学生の作業

**ファイル**: `Exercises/CPU/CPUOptimization_Exercise.cs`
```
■ Step 1: 空間分割（25分）
  - Dictionary<int, List<Enemy>>でセル管理
  - GetCellIndex(): 座標→セルインデックス変換
  - QueryNearby(): 周辺9セルからエンティティ取得
  
■ Step 2: 更新分散（20分）
  - frameCount % groupCount で更新グループ判定
  - 重い処理は分散、軽い処理は毎フレーム
  
■ Step 3: 距離計算（15分）
  - Vector3.Distance → sqrMagnitude に置き換え
  - 比較値も2乗する
```

#### 確認方法
- Profiler > CPU > Frame Time を確認
- Before: 40ms+ → After: 15ms以下

---

### 3.3 課題3: メモリ↔CPUトレードオフ（30分）

#### 概要
メモリとCPUのトレードオフを2つのパターンで体験する。

#### 3.3.1 三角関数LUT（15分）

**目的**: メモリを消費してCPU計算を削減する

**使用場面**（無双系ゲーム）:
- 敵の包囲行動（プレイヤーを囲む位置計算）
- 待機モーション（ボビング、揺れ）
- 旋回移動（弧を描いて接近）

**ファイル**: `Exercises/Tradeoff/TrigLUT_Exercise.cs`
```
■ 実装内容
  - Sin/Cosの値を事前計算して配列に格納
  - AngleToIndex(): 角度→インデックス変換
  - Sin()/Cos(): テーブル参照で値を返す
  
■ トレードオフ
  - メモリ: +2.8KB（360エントリの場合）
  - CPU: 2-5倍高速化
```

#### 3.3.2 可視性マップ（15分）

**目的**: メモリを消費してRaycast計算を削減する

**使用場面**:
- 敵のプレイヤー視認判定
- AI意思決定（見えている敵への反応）

**ファイル**: `Exercises/Tradeoff/VisibilityMap_Exercise.cs`
```
■ 実装内容
  - 2Dグリッドで各セルの可視性を事前計算
  - セル間の可視性をbool配列で保持
  - IsVisible(): グリッド参照で可視性を返す
  
■ トレードオフ
  - メモリ: グリッドサイズ²のbool配列
  - CPU: Raycast完全不要（10-100倍高速）
  - 精度: 空間の離散化による誤差
```

#### 考察課題
```
Q1: 三角関数LUTのメモリ使用量を計算せよ
Q2: 可視性マップのメモリ使用量を計算せよ
Q3: それぞれの手法を使うべき場面と使わない方が良い場面を挙げよ
Q4: このゲームでの効果を予測し、実測値と比較せよ
```

---

### 3.4 課題4: グラフィクス最適化（30分）

#### 概要
Unity設定変更で解決するグラフィクス最適化。コード実装なし。

#### 課題内容

| 課題 | 問題 | 設定変更 | 時間 |
|------|------|---------|------|
| G-1 | バッチングが効かない | GPU Instancing有効化 | 10分 |
| G-2 | 遠距離も高ポリゴン | LODGroup設定 | 10分 |
| G-3 | オーバードロー過多 | パーティクル削減、カリング設定 | 10分 |

#### 学生の作業

**参照**: `Documentation/GraphicsGuide.md`
```
■ G-1: バッチング最適化
  - Material の Enable GPU Instancing を ON
  - Frame Debugger で Draw Calls 減少を確認
  
■ G-2: LOD設定
  - 敵Prefab に LODGroup コンポーネント追加
  - LOD 0/1/2/Culled の閾値設定
  
■ G-3: カリング・オーバードロー
  - パーティクル Max Particles を削減
  - Camera Far Clip Plane を調整
```

#### 確認方法
- Frame Debugger で Draw Calls を確認
- Game View > Stats で Triangles を確認

---

## 4. 学習課題シート
```
╔══════════════════════════════════════════════════════════════════════════════╗
║              パフォーマンス最適化 学習課題（4時間コース）                         ║
║              目標: 敵500体でFPS 60を達成せよ                                    ║
╠══════════════════════════════════════════════════════════════════════════════╣
║                                                                              ║
║ 【準備】(30分)                                                                ║
║   □ Profilerを開く (Window > Analysis > Profiler)                            ║
║   □ ゲーム実行、敵200体スポーン                                                ║
║   □ 初期値を記録:                                                            ║
║     FPS[    ] GC Alloc[    ]KB CPU[    ]ms DrawCalls[    ]                  ║
║                                                                              ║
╠══════════════════════════════════════════════════════════════════════════════╣
║ 【課題1】ゼロアロケーション (1時間)                                             ║
║   ファイル: Exercises/Memory/ZeroAllocation_Exercise.cs                       ║
╠══════════════════════════════════════════════════════════════════════════════╣
║                                                                              ║
║ ■ Step 1: オブジェクトプール (15分)                                            ║
║   □ Stack<Enemy> でプール管理を実装                                           ║
║   □ Get() / Return() を完成                                                  ║
║                                                                              ║
║ ■ Step 2: 文字列結合 (15分)                                                   ║
║   □ StringBuilder をフィールドで保持                                          ║
║   □ Clear() + Append() で再利用                                              ║
║                                                                              ║
║ ■ Step 3: デリゲートキャッシュ (15分)                                          ║
║   □ Action/Func をフィールドでキャッシュ                                       ║
║   □ コンストラクタで一度だけ生成                                               ║
║                                                                              ║
║ ■ Step 4: コレクション再利用 (15分)                                            ║
║   □ List<T> を事前確保                                                       ║
║   □ Clear() でリセットして再利用                                              ║
║                                                                              ║
║ → 確認: GC Alloc が 50KB → 1KB以下 になること                                 ║
║ → 記録: GC Alloc [    ]KB → [    ]KB                                        ║
║                                                                              ║
╠══════════════════════════════════════════════════════════════════════════════╣
║ 【課題2】CPU計算キャッシュ (1時間)                                              ║
║   ファイル: Exercises/CPU/CPUOptimization_Exercise.cs                         ║
╠══════════════════════════════════════════════════════════════════════════════╣
║                                                                              ║
║ ■ Step 1: 空間分割 (25分)                                                     ║
║   □ Dictionary<int, List<Enemy>> でセル管理                                  ║
║   □ GetCellIndex() / QueryNearby() を完成                                    ║
║                                                                              ║
║ ■ Step 2: 更新分散 (20分)                                                     ║
║   □ frameCount % groupCount で分散判定                                       ║
║   □ 重い処理のみ分散、軽い処理は毎フレーム                                      ║
║                                                                              ║
║ ■ Step 3: 距離計算 (15分)                                                     ║
║   □ Vector3.Distance → sqrMagnitude に書き換え                               ║
║   □ 比較値も2乗する                                                          ║
║                                                                              ║
║ → 確認: CPU時間が 40ms → 15ms以下 になること                                   ║
║ → 記録: CPU [    ]ms → [    ]ms                                             ║
║                                                                              ║
╠══════════════════════════════════════════════════════════════════════════════╣
║ 【課題3】メモリ↔CPUトレードオフ (30分)                                          ║
║   ファイル: Exercises/Tradeoff/TrigLUT_Exercise.cs                            ║
║           Exercises/Tradeoff/VisibilityMap_Exercise.cs                       ║
╠══════════════════════════════════════════════════════════════════════════════╣
║                                                                              ║
║ ■ 3-A: 三角関数LUT (15分)                                                     ║
║   □ Sin/Cos テーブルを事前計算して配列に格納                                    ║
║   □ AngleToIndex() で角度→インデックス変換                                    ║
║   メモリ増加: [    ]KB  CPU削減: [    ]ms → [    ]ms                         ║
║                                                                              ║
║ ■ 3-B: 可視性マップ (15分)                                                    ║
║   □ 2Dグリッドでセル間の可視性を事前計算                                        ║
║   □ IsVisible() でグリッド参照                                               ║
║   メモリ増加: [    ]KB  Raycast削減: [    ]回 → [    ]回                      ║
║                                                                              ║
║ ■ 考察                                                                       ║
║   Q: それぞれの手法を使うべき場面は？                                           ║
║   _______________________________________________________________            ║
║   _______________________________________________________________            ║
║                                                                              ║
╠══════════════════════════════════════════════════════════════════════════════╣
║ 【課題4】グラフィクス最適化 (30分)                                              ║
║   参照: Documentation/GraphicsGuide.md                                        ║
╠══════════════════════════════════════════════════════════════════════════════╣
║                                                                              ║
║ ■ G-1: バッチング (10分)                                                      ║
║   □ Material の Enable GPU Instancing を ON                                  ║
║   □ Draw Calls: [    ] → [    ]                                             ║
║                                                                              ║
║ ■ G-2: LOD設定 (10分)                                                        ║
║   □ 敵Prefab に LODGroup を追加                                              ║
║   □ Triangles: [    ] → [    ]                                              ║
║                                                                              ║
║ ■ G-3: カリング・オーバードロー (10分)                                          ║
║   □ パーティクル Max Particles を削減                                         ║
║   □ Camera Far Clip Plane を調整                                            ║
║                                                                              ║
╠══════════════════════════════════════════════════════════════════════════════╣
║ 【最終確認】(30分)                                                             ║
╠══════════════════════════════════════════════════════════════════════════════╣
║                                                                              ║
║ 全最適化適用後の計測結果:                                                       ║
║                                                                              ║
║   | 項目        | Before | After | 改善率 |                                   ║
║   |-------------|--------|-------|--------|                                   ║
║   | FPS         |        |       |        |                                   ║
║   | GC Alloc    |        |       |        |                                   ║
║   | CPU Time    |        |       |        |                                   ║
║   | Draw Calls  |        |       |        |                                   ║
║   | Triangles   |        |       |        |                                   ║
║                                                                              ║
║ □ 敵500体で FPS 60 を達成できたか: Yes / No                                    ║
║ □ 敵1000体での FPS: [    ]                                                   ║
║                                                                              ║
╚══════════════════════════════════════════════════════════════════════════════╝
```

---

## 5. 成果物

### 5.1 評価指標

| 指標 | 初期状態（目安） | 目標（課題完了後） |
|------|-----------------|-------------------|
| FPS (敵500体) | 15-25 | 60+ |
| GC Alloc/frame | 50KB+ | <1KB |
| CPU Time | 40ms+ | <15ms |
| Draw Calls | 500+ | 50-100 |
| Triangles (遠景) | 変化なし | 50%以下 |

### 5.2 学生の成果物

| 成果物 | 内容 |
|--------|------|
| 実装コード | 4つの穴埋め課題ファイル |
| 計測記録 | Before/After の数値記録 |
| 考察記述 | トレードオフ課題の考察回答 |
| 設定変更 | グラフィクス最適化のUnity設定 |

### 5.3 習得スキル

| スキル | 内容 |
|--------|------|
| オブジェクトプール | Instantiate/Destroyの代替手法 |
| ゼロアロケーション | StringBuilder、デリゲートキャッシュ、コレクション再利用 |
| 空間分割 | グリッドベースの近傍検索 |
| 更新分散 | フレームスパイク平滑化 |
| トレードオフ判断 | メモリとCPUの相互関係の理解 |
| Profiler活用 | 計測・分析・改善のサイクル |
| Unity設定 | GPU Instancing、LOD、カリング |

---

## 6. DOTS比較プロジェクト

### 6.1 目的
従来手法（MonoBehaviour）とDOTSのパフォーマンス差を体験し、技術選定の判断基準を学ぶ。

### 6.2 プロジェクト構成
```
MassacreDojo_DOTS/
├── Assets/
│   ├── _Project/
│   │   ├── Scenes/
│   │   │   └── MainGame_DOTS.unity
│   │   ├── Scripts/
│   │   │   ├── Components/
│   │   │   │   ├── EnemyTag.cs
│   │   │   │   ├── EnemyStats.cs
│   │   │   │   ├── EnemyMovement.cs
│   │   │   │   └── SpatialCell.cs
│   │   │   ├── Systems/
│   │   │   │   ├── EnemySpawnSystem.cs
│   │   │   │   ├── EnemyMovementSystem.cs
│   │   │   │   ├── EnemyAISystem.cs
│   │   │   │   └── SpatialHashSystem.cs
│   │   │   ├── Authoring/
│   │   │   │   ├── EnemyAuthoring.cs
│   │   │   │   └── SpawnerAuthoring.cs
│   │   │   └── UI/
│   │   │       └── PerformanceMonitor_DOTS.cs
│   │   └── Prefabs/
│   └── Documentation/
│       └── DOTS_Comparison.md
```

### 6.3 比較計測

| 項目 | 従来手法（最適化前） | 従来手法（最適化後） | DOTS |
|------|---------------------|---------------------|------|
| FPS (500体) | | | |
| FPS (1000体) | | | |
| FPS (2000体) | | | |
| GC Alloc/frame | | | |
| CPU Time (ms) | | | |

### 6.4 DOTSが高速な理由

| 要因 | 説明 |
|------|------|
| Burst Compiler | C#をネイティブコードに変換 |
| Job System | マルチスレッド並列処理 |
| データ指向設計 | キャッシュ効率の最大化 |
| ゼロアロケーション | 構造体ベースでGC不要 |

### 6.5 DOTS導入の判断基準

| 観点 | DOTSを選ぶべき場合 | 従来手法で十分な場合 |
|------|-------------------|---------------------|
| エンティティ数 | 数千〜数万 | 数百以下 |
| パフォーマンス要件 | 最重要 | 他の要素も重要 |
| チーム経験 | DOTS経験者がいる | MonoBehaviourに慣れている |
| 開発期間 | 十分にある | 短期間 |
| 保守性 | 長期運用 | 短期プロジェクト |

### 6.6 学習のポイント
```
【考察課題】

Q1: 従来手法（最適化後）とDOTSで、どの程度の差があったか？
    FPS差: ___________

Q2: DOTSの導入コスト（学習曲線、コード量）をどう評価するか？
    _______________________________________________________________

Q3: このゲームにDOTSは必要だったか？理由とともに答えよ。
    _______________________________________________________________
```

---

## 7. 実装タスク一覧

### 7.1 教員側準備（事前）

| タスクID | タスク名 | 内容 | 想定時間 |
|---------|---------|------|---------|
| T-01 | プロジェクト基盤 | フォルダ構成、LearningSettings、GameManager | 2h |
| T-02 | 計測ツール | PerformanceMonitor | 2h |
| T-03 | 敵システム本体 | EnemySystem.cs（メモリ問題を含む） | 3h |
| T-04 | 敵AI本体 | EnemyAIManager.cs（CPU問題を含む） | 3h |
| T-05 | 敵行動本体 | EnemyBehavior.cs（トレードオフ問題を含む） | 2h |
| T-06 | 穴埋め課題 | 全Exerciseファイル | 4h |
| T-07 | 解答 | 全Solutionファイル | 2h |
| T-08 | グラフィクス設定 | 問題状態の設定、手順書 | 2h |
| T-09 | ドキュメント | 学生ガイド、教員ガイド | 2h |
| T-10 | DOTS比較PJ | 別プロジェクト構築 | 8h |

**合計**: 約30時間

### 7.2 ファイル一覧

| ファイル | 種類 | 用途 |
|---------|------|------|
| `LearningSettings.cs` | 設定管理 | 課題完了フラグ管理 |
| `GameConstants.cs` | 定数 | ゲーム設定値 |
| `GameManager.cs` | コア | ゲーム状態管理 |
| `PerformanceMonitor.cs` | UI | 計測値表示 |
| `EnemySystem.cs` | 問題本体 | メモリ問題4種を含む |
| `EnemyAIManager.cs` | 問題本体 | CPU問題3種を含む |
| `EnemyBehavior.cs` | 問題本体 | トレードオフ問題を含む |
| `ZeroAllocation_Exercise.cs` | 穴埋め課題 | メモリ最適化 |
| `CPUOptimization_Exercise.cs` | 穴埋め課題 | CPU最適化 |
| `TrigLUT_Exercise.cs` | 穴埋め課題 | 三角関数LUT |
| `VisibilityMap_Exercise.cs` | 穴埋め課題 | 可視性マップ |
| `*_Solution.cs` | 解答 | 教員用 |
| `GraphicsGuide.md` | 手順書 | グラフィクス最適化 |
| `StudentGuide.md` | ガイド | 学生用説明書 |
| `TeacherGuide.md` | ガイド | 教員用説明書 |

---

## 8. 補足資料

### 8.1 Profiler確認ポイント

| 課題 | 確認項目 | 場所 |
|------|---------|------|
| メモリ | GC Alloc | Profiler > CPU > GC Alloc列 |
| メモリ | メモリスパイク | Profiler > Memory > GCグラフ |
| CPU | Frame Time | Profiler > CPU > 総時間 |
| CPU | 関数別時間 | Profiler > CPU > Hierarchy |
| GPU | Draw Calls | Frame Debugger / Stats |
| GPU | Triangles | Game View > Stats |
| GPU | Overdraw | Scene View > Overdraw |

### 8.2 よくある間違いと対処

| 間違い | 症状 | 対処 |
|--------|------|------|
| プールのReturn忘れ | メモリリーク | OnDisable/OnDestroyでReturn |
| StringBuilder未クリア | 文字列が連結される | Clear()を最初に呼ぶ |
| sqrMagnitude比較ミス | 距離判定がおかしい | 閾値も2乗する |
| 空間分割の範囲外 | エラー/検索漏れ | クランプ処理を追加 |
| LOD設定の閾値 | 切り替えが見える | 滑らかな閾値設定 |

### 8.3 発展課題（時間が余った場合）

| 課題 | 内容 | 難易度 |
|------|------|--------|
| プール拡張 | 自動縮小機能の追加 | ★★☆ |
| 空間分割拡張 | 動的セルサイズ調整 | ★★★ |
| LUT拡張 | 線形補間による精度向上 | ★★☆ |
| 可視性マップ拡張 | 動的更新対応 | ★★★ |
| 敵2000体チャレンジ | 追加最適化の発見 | ★★★ |