# MassacreDojo クイックスタートガイド

## 5分でセットアップ

### Step 1: プロジェクトを開く

Unity Hub で `MassacreDojo_Optimization` フォルダを開く

### Step 2: Exercise Window を開く

```
メニュー: MassacreDojo → Exercise Window
ショートカット: Ctrl + Shift + E
```

### Step 3: 課題ファイルを展開

1. 「概要」タブを開く
2. **「課題ファイルを展開」** をクリック
3. `Assets/StudentExercises/` に課題ファイルが作成される

### Step 4: 学生情報を設定（送信する場合）

```
メニュー: MassacreDojo → Submission Settings
```

- 学生ID、氏名を入力
- サーバーURL、APIキーを入力（教員から提供）

### Step 5: 課題を開始

1. Exercise Window で課題タブを選択
2. `Assets/StudentExercises/` 内のファイルを編集
3. `// TODO:` の箇所を実装

### Step 6: テスト実行

1. `MainScene` を開いて Play
2. Exercise Window の「テスト」タブ
3. **「全テストを実行」** をクリック

### Step 7: 結果送信

Play モード中に **「結果をサーバーに送信」** をクリック

---

## 課題ファイル一覧

| ファイル | 課題 |
|---------|------|
| `Memory/ZeroAllocation_Exercise.cs` | 課題1: メモリ最適化 |
| `CPU/CPUOptimization_Exercise.cs` | 課題2: CPU最適化 |
| `Tradeoff/NeighborCache_Exercise.cs` | 課題3-A: 近傍キャッシュ |
| `Tradeoff/DecisionCache_Exercise.cs` | 課題3-B: AI判断キャッシュ |

---

## 困ったときは

| 問題 | 解決方法 |
|------|---------|
| コンパイルエラー | 課題ファイルを再展開 |
| テストが SKIP | MainScene を開いて Play |
| 送信エラー | Submission Settings を確認 |

詳細は `Documentation/SetupGuide.md` を参照
