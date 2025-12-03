# 教員用セットアップガイド

## 概要

このドキュメントでは、教員向けのサーバー構築と授業運用について説明します。

---

## 1. サーバーセットアップ（AWS Lambda + DynamoDB）

### 1.1 前提条件

- AWSアカウント
- AWS CLI インストール済み
- AWS SAM CLI インストール済み
- Node.js 18以上

### 1.2 AWS CLI の設定

```bash
aws configure
# AWS Access Key ID: [your-access-key]
# AWS Secret Access Key: [your-secret-key]
# Default region name: ap-northeast-1
# Default output format: json
```

### 1.3 サーバーのデプロイ

```bash
cd Server

# 依存関係インストール
npm install

# ビルド
sam build

# デプロイ（初回は対話形式）
sam deploy --guided
```

**対話形式での入力例:**

```
Stack Name [sam-app]: massacre-dojo-server
AWS Region [ap-northeast-1]: ap-northeast-1
Parameter ApiKey []: your-secret-api-key-2024
Parameter TableName [MassacreDojoResults]: MassacreDojoResults
Confirm changes before deploy [y/N]: y
Allow SAM CLI IAM role creation [Y/n]: Y
Disable rollback [y/N]: N
Save arguments to configuration file [Y/n]: Y
```

### 1.4 デプロイ後の確認

デプロイ完了後、以下の情報が出力されます：

```
Outputs:
  SubmitUrl: https://xxxxxxxxxx.execute-api.ap-northeast-1.amazonaws.com/prod/submit
  ResultsUrl: https://xxxxxxxxxx.execute-api.ap-northeast-1.amazonaws.com/prod/results
  StatisticsUrl: https://xxxxxxxxxx.execute-api.ap-northeast-1.amazonaws.com/prod/statistics
```

**学生に配布する情報:**
- `SubmitUrl` の URL
- `ApiKey` に設定した値

---

## 2. 学生への配布物

### 2.1 配布するもの

1. **Unityプロジェクト** (`MassacreDojo_Optimization` フォルダ)
2. **サーバーURL** (SubmitUrl)
3. **APIキー**
4. **セットアップ手順書** (`Documentation/SetupGuide.md`)

### 2.2 配布方法の例

```
配布フォルダ/
├── MassacreDojo_Optimization/   # Unityプロジェクト
├── SetupGuide.pdf              # セットアップ手順（PDF化）
└── ServerInfo.txt              # サーバー情報
```

**ServerInfo.txt の内容:**
```
サーバーURL: https://xxxxxxxxxx.execute-api.ap-northeast-1.amazonaws.com/prod/submit
APIキー: your-secret-api-key-2024
```

---

## 3. 結果の確認方法

### 3.1 全学生の結果を取得

```bash
curl -H "X-API-Key: your-secret-api-key-2024" \
  "https://xxxxxxxxxx.execute-api.ap-northeast-1.amazonaws.com/prod/results"
```

### 3.2 特定学生の結果を取得

```bash
curl -H "X-API-Key: your-secret-api-key-2024" \
  "https://xxxxxxxxxx.execute-api.ap-northeast-1.amazonaws.com/prod/results?studentId=S12345"
```

### 3.3 統計情報を取得

```bash
curl -H "X-API-Key: your-secret-api-key-2024" \
  "https://xxxxxxxxxx.execute-api.ap-northeast-1.amazonaws.com/prod/statistics"
```

**統計情報の例:**
```json
{
  "success": true,
  "statistics": {
    "totalSubmissions": 45,
    "uniqueStudents": 30,
    "allPassedCount": 22,
    "allPassedRate": 73.3,
    "categoryStats": {
      "Memory": {"pass": 120, "fail": 0},
      "CPU": {"pass": 85, "fail": 5},
      "Tradeoff": {"pass": 55, "fail": 5}
    },
    "fpsAverage": 58.2,
    "fpsMax": 72.5,
    "fpsMin": 35.8
  }
}
```

### 3.4 DynamoDB で直接確認

```bash
aws dynamodb scan --table-name MassacreDojoResults --output table
```

---

## 4. 授業運用

### 4.1 タイムテーブル例（4時間）

| 時間 | 内容 |
|------|------|
| 0:00-0:15 | イントロダクション、環境確認 |
| 0:15-0:30 | 課題ファイル展開、初期計測 |
| 0:30-1:30 | **課題1: メモリ最適化** |
| 1:30-1:45 | 休憩 |
| 1:45-2:45 | **課題2: CPU最適化** |
| 2:45-3:15 | **課題3: トレードオフ** |
| 3:15-3:45 | **課題4: グラフィクス** |
| 3:45-4:00 | 最終テスト、結果送信、まとめ |

### 4.2 進捗確認

授業中に統計APIを定期的に確認：

```bash
watch -n 30 'curl -s -H "X-API-Key: your-key" "https://xxx/prod/statistics" | jq .statistics'
```

### 4.3 トラブル対応

**よくある問題:**

| 問題 | 対応 |
|------|------|
| 送信できない | APIキー、URLを再確認 |
| コンパイルエラー | 課題ファイル再展開を指示 |
| FPSが上がらない | Profilerで原因特定を支援 |

---

## 5. サーバー管理

### 5.1 ログの確認

```bash
sam logs -n MassacreDojo-SubmitResult --stack-name massacre-dojo-server --tail
```

### 5.2 コスト確認

AWS Cost Explorer で確認。通常の授業使用では無料枠内。

### 5.3 データのエクスポート

```bash
aws dynamodb scan --table-name MassacreDojoResults \
  --output json > results_backup.json
```

### 5.4 データの削除（授業終了後）

```bash
# テーブル内容のみ削除
aws dynamodb scan --table-name MassacreDojoResults \
  --projection-expression "pk, sk" \
  --output json | jq -c '.Items[]' | while read item; do
  aws dynamodb delete-item --table-name MassacreDojoResults --key "$item"
done

# スタック全体を削除
sam delete --stack-name massacre-dojo-server
```

---

## 6. カスタマイズ

### 6.1 APIキーを複数クラス用に設定

`template.yaml` の環境変数を変更：

```yaml
VALID_API_KEYS: "class-a-key,class-b-key,class-c-key"
```

### 6.2 課題内容の変更

1. `Assets/_Project/Scripts/Exercises/` 内のファイルを編集
2. 対応する Solution ファイルも更新
3. ExerciseWindow.cs の表示内容を更新
4. ExerciseTestRunner.cs のテストを更新

### 6.3 評価基準の変更

`lambda_function.mjs` の `saveToDynamoDB` 関数でスコア計算をカスタマイズ可能。

---

## 7. セキュリティ注意事項

1. **APIキーは学生に公開される** - 機密情報は含めない
2. **結果データは改ざん検知可能** - CRC32 + HMAC-SHA256
3. **他人の結果は閲覧不可** - 学生からは自分の送信のみ
4. **授業終了後はデータ削除推奨**

---

## 連絡先

技術的な問題が発生した場合は、以下を確認してください：

1. CloudWatch Logs でエラーログを確認
2. DynamoDB でデータ状態を確認
3. API Gateway でリクエストログを確認
