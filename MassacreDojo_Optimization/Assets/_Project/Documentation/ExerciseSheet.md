# パフォーマンス最適化 学習課題シート（4時間コース）

## 目標: 敵500体でFPS 60を達成せよ

---

## 準備（30分）

### 環境確認
- [ ] Profilerを開く (`Window` > `Analysis` > `Profiler`)
- [ ] ゲームを実行、敵200体をスポーン
- [ ] パフォーマンスモニター（F5）を表示

### 初期値を記録

| 項目 | 値 |
|------|-----|
| FPS | |
| GC Alloc | KB/frame |
| CPU Time | ms |
| Draw Calls | |

---

## 課題1: ゼロアロケーション（1時間）

**ファイル**: `Scripts/Exercises/Memory/ZeroAllocation_Exercise.cs`

### Step 1: オブジェクトプール（15分）

**目的**: Instantiate/Destroyを避け、オブジェクトを再利用する

**実装内容**:
- [ ] `Stack<Enemy>` でプール管理を実装
- [ ] `GetFromPool()` メソッドを完成
- [ ] `ReturnToPool()` メソッドを完成

**ヒント**: Stackは `Push()` と `Pop()` で要素を追加・取得します

---

### Step 2: 文字列キャッシュ（15分）

**目的**: 文字列結合によるGCアロケーションを防ぐ

**実装内容**:
- [ ] `StringBuilder` をフィールドで保持
- [ ] `Clear()` でリセット
- [ ] `Append()` で文字列を追加

**Before (問題あり)**:
```csharp
return "Enemies: " + count.ToString() + " | Kills: " + kills.ToString();
```

**After (最適化)**:
```csharp
_sb.Clear();
_sb.Append("Enemies: ").Append(count).Append(" | Kills: ").Append(kills);
return _sb.ToString();
```

---

### Step 3: デリゲートキャッシュ（15分）

**目的**: 毎フレームの `new Action()` を防ぐ

**実装内容**:
- [ ] `Action<Enemy>` をフィールドでキャッシュ
- [ ] 初期化は一度だけ（Awakeまたは初回アクセス時）
- [ ] 以降はキャッシュを返す

---

### Step 4: コレクション再利用（15分）

**目的**: 毎回の `new List<T>()` を防ぐ

**実装内容**:
- [ ] `List<Enemy>` をフィールドで保持
- [ ] 初期容量を指定して初期化（例: 100）
- [ ] 使用前に `Clear()` でリセット

---

### 課題1 確認

LearningSettingsで以下をONにして確認:
- [ ] `useObjectPool` = true
- [ ] `useStringBuilder` = true
- [ ] `useDelegateCache` = true
- [ ] `useCollectionReuse` = true

**計測結果**:

| 項目 | Before | After |
|------|--------|-------|
| GC Alloc | KB | KB |

**目標**: 50KB+ → 1KB以下

---

## 課題2: CPU計算キャッシュ（1時間）

**ファイル**: `Scripts/Exercises/CPU/CPUOptimization_Exercise.cs`

### Step 1: 空間分割（25分）

**目的**: O(n²)の総当たり検索をO(1)に近づける

**実装内容**:
- [ ] `Dictionary<int, List<Enemy>>` でセル管理
- [ ] `GetCellIndex()`: 座標→セルインデックス変換
- [ ] `QueryNearbyEnemies()`: 周辺9セルから敵を取得

**計算式**:
```csharp
int x = (int)((position.x + FIELD_HALF_SIZE) / CELL_SIZE);
int z = (int)((position.z + FIELD_HALF_SIZE) / CELL_SIZE);
int index = z * GRID_WIDTH + x;
```

---

### Step 2: 更新分散（20分）

**目的**: 全敵が毎フレーム更新するのを避け、負荷を分散

**実装内容**:
- [ ] `frameCount % groupCount == group` で更新判定
- [ ] 重い処理のみ分散、軽い処理は毎フレーム

**例**:
```csharp
// グループ0: フレーム0,10,20...で更新
// グループ1: フレーム1,11,21...で更新
return frameCount % 10 == group;
```

---

### Step 3: 距離計算（15分）

**目的**: 平方根計算を避けて高速化

**実装内容**:
- [ ] `Vector3.Distance()` → `sqrMagnitude` に置き換え
- [ ] 比較時は閾値も2乗する

**Before**:
```csharp
if (Vector3.Distance(a, b) < 5f)
```

**After**:
```csharp
if ((a - b).sqrMagnitude < 25f)  // 5² = 25
```

---

### 課題2 確認

**計測結果**:

| 項目 | Before | After |
|------|--------|-------|
| CPU Time | ms | ms |

**目標**: 40ms+ → 15ms以下

---

## 課題3: メモリ↔CPUトレードオフ（30分）

### 3-A: 三角関数LUT（15分）

**ファイル**: `Scripts/Exercises/Tradeoff/TrigLUT_Exercise.cs`

**実装内容**:
- [ ] `float[]` でSin/Cosテーブルを事前計算
- [ ] `AngleToIndex()`: 角度→インデックス変換
- [ ] `Sin()` / `Cos()`: テーブル参照で値を返す

**トレードオフ**:
- メモリ: +2.8KB
- CPU: 2-5倍高速化

---

### 3-B: 可視性マップ（15分）

**ファイル**: `Scripts/Exercises/Tradeoff/VisibilityMap_Exercise.cs`

**実装内容**:
- [ ] 2Dグリッドで可視性を事前計算
- [ ] `IsVisible()`: グリッド参照で可視性を返す

**トレードオフ**:
- メモリ: グリッドサイズ² バイト
- CPU: Raycast完全不要

---

### 考察課題

**Q1**: 三角関数LUTのメモリ使用量を計算せよ

計算式: _________________________________________________

答え: _________ バイト

---

**Q2**: 可視性マップのメモリ使用量を計算せよ（50x50グリッドの場合）

計算式: _________________________________________________

答え: _________ バイト

---

**Q3**: それぞれの手法を使うべき場面と使わない方が良い場面を挙げよ

**LUTを使うべき場面**:
- _________________________________________________

**LUTを使わない方が良い場面**:
- _________________________________________________

**可視性マップを使うべき場面**:
- _________________________________________________

**可視性マップを使わない方が良い場面**:
- _________________________________________________

---

## 課題4: グラフィクス最適化（30分）

**参照**: `Documentation/GraphicsGuide.md`

### G-1: バッチング（10分）
- [ ] MaterialのEnable GPU InstancingをON
- [ ] Draw Calls: ______ → ______

### G-2: LOD設定（10分）
- [ ] 敵PrefabにLODGroupを追加
- [ ] Triangles: ______ → ______

### G-3: カリング・オーバードロー（10分）
- [ ] パーティクル Max Particles を削減
- [ ] Camera Far Clip Plane を調整

---

## 最終確認（30分）

### 全最適化適用後の計測結果

| 項目 | Before | After | 改善率 |
|------|--------|-------|--------|
| FPS | | | |
| GC Alloc | | | |
| CPU Time | | | |
| Draw Calls | | | |
| Triangles | | | |

### 達成確認
- [ ] 敵500体で FPS 60 を達成できたか: Yes / No
- [ ] 敵1000体での FPS: ______

---

## 自己評価

### 理解度チェック

| 項目 | よく理解 | 理解 | 要復習 |
|------|---------|------|--------|
| オブジェクトプール | | | |
| StringBuilder | | | |
| デリゲートキャッシュ | | | |
| 空間分割 | | | |
| 更新分散 | | | |
| sqrMagnitude | | | |
| トレードオフ判断 | | | |
| Profiler活用 | | | |

### 感想・質問

_________________________________________________________

_________________________________________________________

_________________________________________________________
