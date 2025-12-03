# グラフィクス最適化ガイド

## 概要

この課題では、Unity設定の変更によってグラフィクスパフォーマンスを最適化します。
コード実装は不要です。

---

## 課題G-1: GPU Instancing（10分）

### 目的
同じマテリアルを使用するオブジェクトの描画を効率化する。

### 問題
現在、敵オブジェクトは個別にDraw Callが発生しています。

### 手順

1. **敵のマテリアルを選択**
   - `Assets/_Project/Art/Materials/EnemyMaterial` を選択
   - または敵Prefabのマテリアルを直接選択

2. **GPU Instancingを有効化**
   - Inspector で `Enable GPU Instancing` にチェック

3. **確認**
   - `Window` > `Analysis` > `Frame Debugger` を開く
   - Draw Calls が減少していることを確認

### 期待される効果
- Draw Calls: 500+ → 50-100

### 注意点
- GPU Instancingはシェーダーがサポートしている必要があります
- Standard Shaderは対応しています
- カスタムシェーダーの場合は `#pragma multi_compile_instancing` が必要

---

## 課題G-2: LOD設定（10分）

### 目的
カメラからの距離に応じてモデルの詳細度を変更し、遠くのオブジェクトの描画負荷を削減する。

### 問題
現在、遠くの敵も近くの敵と同じポリゴン数で描画されています。

### 手順

1. **敵Prefabを開く**
   - `Assets/_Project/Prefabs/Enemy` をダブルクリック

2. **LODGroupコンポーネントを追加**
   - 敵のルートオブジェクトを選択
   - `Add Component` > `Rendering` > `LOD Group`

3. **LODレベルを設定**
   - LOD 0 (100%): 元のモデル（近距離）
   - LOD 1 (50%): 簡易モデル（中距離）
   - LOD 2 (25%): 最簡易モデル（遠距離）
   - Culled (10%以下): 非表示

4. **確認**
   - Game View > Stats で Triangles を確認
   - カメラを遠ざけると Triangles が減少することを確認

### LOD用モデルの作成（オプション）

プリミティブを使用している場合、LODモデルを以下のように作成できます：

```
LOD 0: Capsule (元のサイズ)
LOD 1: Capsule (スケール0.8)
LOD 2: Sphere (シンプルな形状)
Culled: 非表示
```

### 期待される効果
- 遠景のTriangles: 50%以下

---

## 課題G-3: カリング・オーバードロー（10分）

### 目的
不要な描画を削減し、GPU負荷を軽減する。

### 3-A: パーティクル最適化

1. **パーティクルシステムを選択**
   - 攻撃エフェクトなどのパーティクルを選択

2. **Max Particlesを削減**
   - Particle System > Main > Max Particles
   - 必要最小限の数に設定（例: 1000 → 100）

3. **シミュレーション空間を確認**
   - `Simulation Space` を `Local` に設定（可能な場合）

### 3-B: カメラ設定

1. **Cameraを選択**
   - Main Camera を選択

2. **Far Clip Planeを調整**
   - Camera > Clipping Planes > Far
   - フィールドサイズに合わせて調整（例: 1000 → 150）

3. **Occlusion Cullingを有効化**（オプション）
   - `Window` > `Rendering` > `Occlusion Culling`
   - Bakeボタンをクリック

### 3-C: オーバードローの確認

1. **Scene Viewでオーバードローを確認**
   - Scene View 左上のドロップダウン > `Overdraw`
   - 明るい部分がオーバードローが多い箇所

2. **問題箇所の特定と対処**
   - 半透明オブジェクトを減らす
   - パーティクルのサイズや数を調整

---

## 確認方法

### Frame Debugger

1. `Window` > `Analysis` > `Frame Debugger` を開く
2. `Enable` ボタンをクリック
3. 各Draw Callを確認

### Game View Stats

1. Game View の `Stats` ボタンをクリック
2. 以下を確認:
   - FPS
   - Batches (Draw Calls)
   - Tris (Triangles)
   - Verts (Vertices)

---

## チェックリスト

- [ ] GPU Instancing が有効
- [ ] LODGroup が設定済み
- [ ] パーティクルの Max Particles を削減
- [ ] Camera Far Clip Plane を調整
- [ ] Draw Calls が減少（500+ → 50-100）
- [ ] 遠景の Triangles が減少（50%以下）

---

## 計測結果を記録

| 項目 | Before | After | 改善率 |
|------|--------|-------|--------|
| Draw Calls | | | |
| Triangles | | | |
| FPS | | | |

---

## 追加の最適化（時間があれば）

### Static Batching
動かないオブジェクト（地面、壁など）は Static に設定することで
自動的にバッチングされます。

1. 静的オブジェクトを選択
2. Inspector 右上の `Static` にチェック

### Texture Atlasing
複数のテクスチャを1枚にまとめることで Draw Calls を削減できます。
URP では Sprite Atlas を使用します。

### シェーダーの最適化
- 不要なシェーダーバリアントを削除
- シンプルなシェーダーに置き換え
