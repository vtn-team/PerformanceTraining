# DOTS比較プロジェクト

## 概要

このプロジェクトは、従来のMonoBehaviourベースのアプローチと
Unity DOTS (Data-Oriented Technology Stack) の
パフォーマンス差を比較するためのものです。

---

## DOTSとは

Unity DOTSは以下の技術で構成されています:

| 技術 | 説明 |
|------|------|
| **ECS** (Entities) | Entity-Component-Systemアーキテクチャ |
| **Job System** | マルチスレッド並列処理 |
| **Burst Compiler** | C#をネイティブコードに変換 |

---

## プロジェクト構成

```
MassacreDojo_DOTS/
├── Assets/
│   ├── _Project/
│   │   ├── Scripts/
│   │   │   ├── Components/     # ECSコンポーネント
│   │   │   │   └── EnemyComponents.cs
│   │   │   ├── Systems/        # ECSシステム
│   │   │   │   ├── EnemySpawnSystem.cs
│   │   │   │   ├── EnemyMovementSystem.cs
│   │   │   │   ├── EnemyAISystem.cs
│   │   │   │   └── SpatialHashSystem.cs
│   │   │   ├── Authoring/      # MonoBehaviour→Entity変換
│   │   │   │   ├── EnemyAuthoring.cs
│   │   │   │   ├── SpawnerAuthoring.cs
│   │   │   │   └── PlayerAuthoring.cs
│   │   │   └── UI/
│   │   │       └── PerformanceMonitor_DOTS.cs
│   │   └── Prefabs/
│   └── Documentation/
│       └── DOTS_Comparison.md
```

---

## DOTSが高速な理由

### 1. Burst Compiler
- C#コードをLLVMを通じてネイティブコードに変換
- SIMD命令の自動活用
- 10-100倍の高速化が可能

### 2. Job System
- マルチスレッド並列処理
- CPUコアを最大限活用
- データ競合を防ぐ安全な設計

### 3. データ指向設計
- メモリレイアウトの最適化
- キャッシュヒット率の向上
- 構造体（struct）ベースでGC不要

---

## 比較計測の方法

### 従来手法（最適化前）
1. MassacreDojo_Optimization プロジェクトを開く
2. LearningSettings で全最適化をOFF
3. 敵を500/1000/2000体スポーンしてFPS計測

### 従来手法（最適化後）
1. 課題1-4を全て完了
2. LearningSettings で全最適化をON
3. 同様に計測

### DOTS版
1. MassacreDojo_DOTS プロジェクトを開く
2. 敵を500/1000/2000体スポーンしてFPS計測

---

## 計測結果を記録

| 項目 | 従来手法（最適化前） | 従来手法（最適化後） | DOTS |
|------|---------------------|---------------------|------|
| FPS (500体) | | | |
| FPS (1000体) | | | |
| FPS (2000体) | | | |
| GC Alloc/frame | | | |
| CPU Time (ms) | | | |

---

## DOTS導入の判断基準

### DOTSを選ぶべき場合

| 条件 | 理由 |
|------|------|
| エンティティ数が数千〜数万 | DOTSの恩恵が大きい |
| パフォーマンスが最重要 | 他の要素を犠牲にできる |
| チームにDOTS経験者がいる | 学習コストを抑えられる |
| 長期運用プロジェクト | 投資対効果が高い |

### 従来手法で十分な場合

| 条件 | 理由 |
|------|------|
| エンティティ数が数百以下 | 最適化で十分対応可能 |
| 開発期間が短い | 学習コストを避けたい |
| チームがMonoBehaviourに慣れている | 生産性を維持したい |
| 既存アセットを多く使う | DOTS非対応が多い |

---

## コード比較

### 移動処理の比較

#### 従来手法
```csharp
// MonoBehaviour
public class Enemy : MonoBehaviour
{
    public float speed = 3f;

    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }
}
```

#### DOTS版
```csharp
// ECSシステム（Burst + 並列処理）
[BurstCompile]
public partial struct MoveEnemiesJob : IJobEntity
{
    public float DeltaTime;

    public void Execute(ref LocalTransform transform, in EnemyMovement movement)
    {
        transform.Position += movement.Direction * movement.Speed * DeltaTime;
    }
}
```

---

## 考察課題

```
Q1: 従来手法（最適化後）とDOTSで、どの程度の差があったか?
    FPS差: ___________

Q2: DOTSの導入コスト（学習曲線、コード量）をどう評価するか?
    _______________________________________________________________

Q3: このゲームにDOTSは必要だったか?理由とともに答えよ。
    _______________________________________________________________

Q4: DOTSを導入すべきプロジェクトの条件を3つ挙げよ。
    1. _______________________________________________________________
    2. _______________________________________________________________
    3. _______________________________________________________________
```

---

## 参考資料

- [Unity DOTS公式ドキュメント](https://docs.unity3d.com/Packages/com.unity.entities@latest)
- [Burst Compiler User Guide](https://docs.unity3d.com/Packages/com.unity.burst@latest)
- [Job System Cookbook](https://github.com/stella3d/job-system-cookbook)
