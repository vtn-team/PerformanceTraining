# MassacreDojo - パフォーマンス最適化学習プロジェクト

## プロジェクト概要

敵が大量に出現するバトルロイヤル形式のゲームをベースに、パフォーマンスボトルネックを意図的に仕込み、学生がそれを最適化する課題を提供する。

### ゲーム仕様
- **ジャンル**: 無双系バトルロイヤル
- **キャラクター数**: 100〜1000体
- **ゲームルール**: キャラクターがお互いを殺し合い、最後の1人が勝者
- **プレイヤー介入**:
  - 特定キャラクターへのバフ付与
  - フィールド範囲攻撃で敵を倒す

---

## 実装タスク

### Phase 1: コアゲームシステム

#### 1.1 Character システム
- [x] Character クラス作成
  - [x] 6種類のキャラクタータイプ（Warrior, Assassin, Tank, Mage, Ranger, Berserker）
  - [x] ランダムな名前生成
  - [x] ランダムなパラメータ（±20%変動）
  - [x] 戦闘システム（攻撃、ダメージ、死亡処理）
  - [x] バフシステム
- [x] CharacterSpawner 作成
- [x] CharacterManager 作成（全キャラクター管理）

#### 1.2 AI システム（ビヘイビアツリー）
- [x] BehaviorTree 基底クラス（MonoBehaviour継承、インスペクタ対応）
- [x] Node 基底クラス
- [x] 複合ノード:
  - [x] Selector（OR条件）
  - [x] Sequence（AND条件）
- [x] アクションノード:
  - [x] SearchNode（索敵）
  - [x] AttackNode（攻撃）
  - [x] ChaseNode（追跡）
  - [x] FleeNode（逃走）
  - [x] WanderNode（徘徊）
  - [x] IdleNode（待機）
- [x] 条件ノード:
  - [x] IsLowHealthNode
  - [x] HasTargetNode
  - [x] IsInAttackRangeNode
- [x] CharacterAI（キャラクタータイプ別パラメータ調整）

#### 1.3 バトルシステム
- [x] ダメージ計算（Character.CalculateDamage）
- [x] 死亡処理（Character.Die, CharacterManager.HandleCharacterDeath）
- [x] 勝敗判定（CharacterManager.OnBattleRoyaleWinner）

#### 1.4 プレイヤー介入システム
- [x] バフシステム（Character.ApplyBuff, GameManager.BuffRandomCharacter）
- [x] 範囲攻撃システム（CharacterManager.DealAreaDamage, GameManager.PerformAreaAttack）
- [ ] UI（選択、発動）

#### 1.5 シーン・UI
- [ ] メインゲームシーン作成
- [ ] PerformanceMonitor UI
- [ ] ゲーム状態表示UI

---

### Phase 2: パフォーマンスボトルネック実装

意図的に非効率なコードを実装し、学生が最適化する課題を作成する。

#### 2.1 メモリ課題
| 課題 | ボトルネック実装 | 最適化手法 |
|------|------------------|------------|
| オブジェクトプール | 毎回 Instantiate/Destroy | Stack による再利用 |
| StringBuilder | 文字列結合（+ 演算子） | StringBuilder 再利用 |

#### 2.2 CPU/効率課題
| 課題 | ボトルネック実装 | 最適化手法 |
|------|------------------|------------|
| ソートアルゴリズム | バブルソート O(n²) | クイックソート / LINQ最適化 |
| DrawCallバッチング | 個別描画 | GPU Instancing / SRP Batcher |

#### 2.3 トレードオフ課題（検討中）
| 候補 | メモリコスト | CPU削減効果 | 備考 |
|------|--------------|-------------|------|
| 距離キャッシュ | 中 | 高 | N体問題の距離計算 |
| 視界キャッシュ | 低〜中 | 中 | Raycast削減 |
| AI決定キャッシュ | 低 | 中 | 毎フレームAI計算削減 |

**検討ポイント**:
- 1つに絞りたい
- 効果が明確に見える
- 実装難易度が適切

---

### Phase 3: 学習UI・テスト

#### 3.1 Exercise Manager Window
- [x] プロジェクト起動時に自動表示
- [x] 課題選択UI
- [x] ソースコード・シーンへの導線
- [x] テスト実行・結果表示

#### 3.2 テストシステム
- [ ] 各課題の自動テスト
- [ ] パフォーマンス計測
- [ ] 結果表示

---

## ファイル構成（現在）

```
Assets/_Project/
├── Scripts/
│   ├── Core/
│   │   ├── Character.cs           ✅ 作成済み
│   │   ├── CharacterSpawner.cs    ✅ 作成済み
│   │   ├── CharacterManager.cs    ✅ 作成済み
│   │   ├── CameraController.cs    ✅ 作成済み
│   │   ├── GameManager.cs         ✅ 更新済み
│   │   ├── GameConstants.cs       ✅ 既存（フィールド100x100）
│   │   └── LearningSettings.cs    ✅ 既存
│   ├── AI/
│   │   ├── CharacterAI.cs         ✅ 作成済み
│   │   └── BehaviorTree/
│   │       ├── BehaviorTree.cs    ✅ 作成済み
│   │       ├── Node.cs            ✅ 作成済み
│   │       ├── NodeState.cs       ✅ 作成済み
│   │       ├── Selector.cs        ✅ 作成済み
│   │       ├── Sequence.cs        ✅ 作成済み
│   │       └── Nodes/
│   │           ├── SearchNode.cs   ✅ 作成済み
│   │           ├── AttackNode.cs   ✅ 作成済み
│   │           └── CombatNodes.cs  ✅ 作成済み（Chase, Flee, Wander, Idle等）
│   ├── Exercises/
│   │   ├── Memory/                ⬜ 再実装予定
│   │   ├── CPU/                   ⬜ 再実装予定
│   │   └── Tradeoff/              ⬜ 再実装予定
│   └── Editor/
│       ├── ExerciseManagerWindow.cs ✅ 既存
│       └── SceneSetupTool.cs      ✅ 作成済み
├── Prefabs/
│   └── Characters/                ⬜ 未作成（SceneSetupToolで自動生成可能）
├── Scenes/
│   └── MainGame.unity             ⬜ 未作成（SceneSetupToolで自動生成可能）
└── Resources/
    └── LearningSettings.asset     ✅ 既存
```

---

## シーンセットアップ手順

### Step 1: プレハブ作成
1. メニュー `MassacreDojo > Scene Setup Tool` を開く
2. 「**1. Create Character Prefab**」をクリック
3. `Assets/_Project/Prefabs/Characters/Character.prefab` が作成される

### Step 2: シーン作成
1. 同じウィンドウで「**2. Create MainGame Scene**」をクリック
2. `Assets/_Project/Scenes/MainGame.unity` が作成される
3. 以下が自動設定される：
   - GameManager + CharacterManager + CharacterSpawner 階層
   - 100x100 サイズのフィールド
   - カメラ（CameraController付き、クリックでキャラジャンプ）
   - ライティング

### Step 3: ゲーム実行
1. 作成されたシーンを開く
2. Playボタンでゲーム開始
3. 200体のキャラクターが自動スポーン
4. AIが自動で戦闘開始

### 既存シーンへの追加（手動）
既にシーンがある場合：
1. 空のGameObject「GameManager」を作成
2. `GameManager`コンポーネントを追加
3. 子オブジェクト「CharacterManager」を作成し、`CharacterManager`コンポーネントを追加
4. 孫オブジェクト「CharacterSpawner」を作成し、`CharacterSpawner`コンポーネントを追加
5. CharacterSpawnerのInspectorで`Character Prefab`を設定
6. GameManagerのInspectorで`Character Manager`を設定
7. Main Cameraに`CameraController`を追加

---

## 次のステップ

1. ~~Character クラス実装~~ ✅
2. ~~AI ビヘイビアツリー実装~~ ✅
3. ~~CharacterSpawner 実装~~ ✅
4. ~~メインシーン作成~~ ✅
5. **ボトルネックコード実装** ← 現在
6. 課題UI調整

---

## カメラ操作

| キー/操作 | 機能 |
|-----------|------|
| WASD / 矢印キー | カメラ移動 |
| Shift + 移動 | 高速移動（3倍速） |
| マウスホイール | ズームイン/アウト |
| 画面端にマウス | エッジスクロール |
| 左クリック | 次のキャラクターにジャンプ |
| 右クリック | 前のキャラクターにジャンプ |

---

## デバッグ操作

ゲーム実行中に以下のキーで操作可能：

| キー | 機能 |
|------|------|
| F1 | キャラクター50体追加 |
| F2 | ゲームリセット |
| F3 | 一時停止/再開 |
| F4 | 全最適化ON/OFF切り替え |
| F5 | ランダムなキャラクターにバフ付与 |
| F6 | 中央に範囲攻撃 |

---

## 更新履歴

| 日付 | 内容 |
|------|------|
| 2025-12-06 | 初版作成、方針決定 |
| 2025-12-06 | CharacterSpawner, CharacterManager作成、GameManager更新 |
| 2025-12-06 | SceneSetupTool作成（シーン・プレハブ自動生成） |
| 2025-12-06 | CameraController追加（移動、ズーム、クリックでキャラジャンプ） |
