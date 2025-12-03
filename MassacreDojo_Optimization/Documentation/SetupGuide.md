# MassacreDojo パフォーマンス最適化学習プログラム
# セットアップ手順書

## 目次

1. [動作環境](#1-動作環境)
2. [Unityプロジェクトのセットアップ](#2-unityプロジェクトのセットアップ)
3. [初期設定](#3-初期設定)
4. [課題の開始方法](#4-課題の開始方法)
5. [テストの実行方法](#5-テストの実行方法)
6. [結果の送信方法](#6-結果の送信方法)
7. [トラブルシューティング](#7-トラブルシューティング)

---

## 1. 動作環境

### 必須環境

| 項目 | 要件 |
|------|------|
| Unity | 2022.3 LTS 以上 |
| OS | Windows 10/11, macOS 10.15以上 |
| メモリ | 8GB以上推奨 |
| GPU | DirectX 11対応 |

### 推奨環境

- Unity 2022.3.x LTS
- Visual Studio 2022 または VS Code
- Git（バージョン管理用）

---

## 2. Unityプロジェクトのセットアップ

### 2.1 プロジェクトを開く

1. Unity Hub を起動
2. 「Open」→「Add project from disk」を選択
3. `MassacreDojo_Optimization` フォルダを選択
4. Unity 2022.3.x で開く

### 2.2 初回起動時の確認

プロジェクトを開くと、以下のフォルダ構成が確認できます：

```
Assets/
├── _Project/
│   ├── Scripts/
│   │   ├── Core/           # ゲームコア
│   │   ├── Enemy/          # 敵システム
│   │   ├── Player/         # プレイヤー
│   │   ├── Exercises/      # 課題ファイル（元ファイル）
│   │   ├── Solutions/      # 解答（教員用）
│   │   └── Editor/         # エディタツール
│   ├── Prefabs/
│   ├── Scenes/
│   └── Resources/
└── StudentExercises/       # ← 課題展開後に作成される
```

### 2.3 必要なパッケージの確認

Window → Package Manager を開き、以下がインストールされていることを確認：

- **TextMeshPro** - UI表示用
- **Input System** - 入力制御用（オプション）

---

## 3. 初期設定

### 3.1 Exercise Window を開く

**メニュー**: `MassacreDojo` → `Exercise Window`

または **ショートカット**: `Ctrl + Shift + E`

![Exercise Window](./images/exercise_window.png)

### 3.2 LearningSettings の作成

初回起動時、LearningSettings が見つからない場合：

1. Exercise Window の「概要」タブを開く
2. 「LearningSettingsを作成」ボタンをクリック
3. `Assets/_Project/Resources/LearningSettings.asset` が作成される

### 3.3 学生情報の設定（結果送信用）

1. **メニュー**: `MassacreDojo` → `Submission Settings`
2. 以下を入力：
   - **学生ID**: 学籍番号
   - **氏名**: フルネーム
   - **サーバーURL**: 教員から提供されたURL
   - **APIキー**: 教員から提供されたキー
3. 「保存」をクリック

---

## 4. 課題の開始方法

### 4.1 課題ファイルの展開

**重要**: 課題を始める前に、必ず課題ファイルを展開してください。

1. Exercise Window を開く（`Ctrl + Shift + E`）
2. 「概要」タブの「課題ファイル」セクションを確認
3. **「課題ファイルを展開」** ボタンをクリック

![Deploy Exercises](./images/deploy_exercises.png)

4. 確認ダイアログで「OK」をクリック
5. `Assets/StudentExercises/` フォルダが作成される

### 4.2 展開されるファイル

```
Assets/StudentExercises/
├── Memory/
│   └── ZeroAllocation_Exercise.cs    # 課題1: メモリ最適化
├── CPU/
│   └── CPUOptimization_Exercise.cs   # 課題2: CPU最適化
└── Tradeoff/
    ├── NeighborCache_Exercise.cs     # 課題3-A: 近傍キャッシュ
    └── DecisionCache_Exercise.cs     # 課題3-B: AI判断キャッシュ
```

### 4.3 課題の進め方

1. Exercise Window で課題タブを選択（例：「課題1: メモリ」）
2. 各Stepの説明とヒントを確認
3. 該当する Exercise ファイルを開いて編集
4. `// TODO:` コメントの箇所を実装
5. チェックボックスで進捗を記録

---

## 5. テストの実行方法

### 5.1 シーンの準備

1. `Assets/_Project/Scenes/MainScene` を開く
2. Play ボタンでゲームを開始
3. 敵をスポーン（UI の「+100」ボタン等）

### 5.2 テストの実行

1. Exercise Window の「テスト」タブを開く
2. Play モード中に **「全テストを実行」** をクリック
3. 結果がウィンドウに表示される

### 5.3 個別テスト

特定の課題のみテストする場合：

- 「メモリテスト」ボタン → 課題1のテスト
- 「CPUテスト」ボタン → 課題2のテスト
- 「トレードオフテスト」ボタン → 課題3のテスト

### 5.4 テスト結果の見方

```
【課題1: メモリ最適化】
  [PASS] Step 1: オブジェクトプール - 再利用が正しく動作
  [PASS] Step 2: StringBuilder - 文字列生成が最適化されている
  [FAIL] Step 3: デリゲートキャッシュ - 毎回新しいインスタンスが生成されている
  [PASS] Step 4: コレクション再利用 - 再利用とClear()が正しく動作
```

- **[PASS]**: テスト成功
- **[FAIL]**: テスト失敗（実装を確認）
- **[SKIP]**: 該当コンポーネントが見つからない

---

## 6. 結果の送信方法

### 6.1 送信前の確認

1. 全てのテストが PASS していることを確認
2. 学生情報が正しく設定されていることを確認
3. Play モード中であることを確認

### 6.2 サーバーへ送信

1. Exercise Window の「テスト」タブを開く
2. 「結果送信」セクションで情報を確認
3. **「結果をサーバーに送信」** ボタンをクリック
4. 「送信完了」ダイアログが表示されれば成功

### 6.3 ローカル保存

サーバーに送信できない場合：

1. **「ローカル保存」** ボタンをクリック
2. `Assets/TestResult_学生ID_日時.json` が作成される
3. このファイルを教員に提出

---

## 7. トラブルシューティング

### 7.1 コンパイルエラーが発生する

**原因**: 課題ファイルが正しく展開されていない

**解決方法**:
1. `Assets/StudentExercises` フォルダを削除
2. Exercise Window で「課題ファイルを展開」を再実行

### 7.2 テストが全て SKIP になる

**原因**: 必要なコンポーネントがシーンに配置されていない

**解決方法**:
1. `MainScene` を開いているか確認
2. GameManager オブジェクトが存在するか確認
3. 課題用の Exercise コンポーネントがアタッチされているか確認

### 7.3 結果送信でエラーになる

**原因**: サーバー設定が正しくない

**解決方法**:
1. `MassacreDojo` → `Submission Settings` を開く
2. サーバーURL が正しいか確認（https:// で始まる）
3. APIキーが正しいか確認
4. インターネット接続を確認

### 7.4 LearningSettings が見つからない

**解決方法**:
1. Exercise Window の「概要」タブを開く
2. 「LearningSettingsを作成」ボタンをクリック

### 7.5 FPS が極端に低い

**原因**: 最適化が適用されていない、または敵が多すぎる

**解決方法**:
1. 敵の数を200体程度に減らす
2. LearningSettings で最適化オプションを確認
3. Profiler（`Window` → `Analysis` → `Profiler`）で原因を特定

---

## クイックリファレンス

### ショートカット

| ショートカット | 機能 |
|---------------|------|
| `Ctrl + Shift + E` | Exercise Window を開く |
| `Ctrl + P` | Play / Stop |
| `Ctrl + Shift + P` | Pause |

### メニュー

| メニュー | 機能 |
|---------|------|
| `MassacreDojo` → `Exercise Window` | 課題ウィンドウ |
| `MassacreDojo` → `Submission Settings` | 送信設定 |
| `Window` → `Analysis` → `Profiler` | プロファイラー |
| `Window` → `Analysis` → `Frame Debugger` | フレームデバッガー |

### 目標値

| 指標 | 目標 |
|------|------|
| FPS（500体） | 60以上 |
| GC Alloc | 1KB以下/frame |
| CPU Time | 15ms以下 |
| Draw Calls | 50-100 |

---

## サポート

問題が解決しない場合は、以下の情報と共に教員に連絡してください：

1. エラーメッセージのスクリーンショット
2. Console ウィンドウのログ
3. Unity バージョン
4. OS 情報

---

*最終更新: 2025年12月*
