# MassacreDojo サーバーセットアップガイド

テスト結果を受信・管理するAWS Lambda + DynamoDB サーバーのセットアップ手順です。

## アーキテクチャ

```
Unity Client
    │
    ▼ POST /submit (JSON + CRC + Signature)
┌─────────────────┐
│   API Gateway   │
└────────┬────────┘
         │
         ▼
┌─────────────────┐     ┌─────────────────┐
│  Lambda Function │────▶│    DynamoDB     │
│  (Node.js 20.x) │     │  (Results Table) │
└─────────────────┘     └─────────────────┘
```

## ファイル構成

```
Server/
├── lambda_function.mjs   # Lambda関数（メイン）
├── template.yaml         # AWS SAM テンプレート
├── package.json          # Node.js依存関係
├── test_api.mjs         # APIテストスクリプト
└── README.md            # このファイル
```

## 前提条件

- AWS CLI がインストール済み
- AWS SAM CLI がインストール済み
- Node.js 18以上
- AWSアカウントと適切な権限

## セットアップ手順

### 1. AWS SAM CLI のインストール

```bash
# Windows (Chocolatey)
choco install aws-sam-cli

# macOS
brew install aws-sam-cli

# pip
pip install aws-sam-cli
```

### 2. 依存関係のインストール

```bash
cd Server
npm install
```

### 3. デプロイ

```bash
# ビルド
sam build

# デプロイ（初回は --guided オプションで対話形式）
sam deploy --guided
```

対話形式で以下を設定:
- **Stack Name**: `massacre-dojo-server`
- **AWS Region**: `ap-northeast-1` (東京)
- **ApiKey**: 任意のAPIキー（学生に配布）
- **TableName**: `MassacreDojoResults`

### 4. デプロイ後の確認

デプロイ完了後、以下のURLが出力されます:

```
Outputs:
  ApiEndpoint: https://xxxxxxxxxx.execute-api.ap-northeast-1.amazonaws.com/prod
  SubmitUrl: https://xxxxxxxxxx.execute-api.ap-northeast-1.amazonaws.com/prod/submit
  ResultsUrl: https://xxxxxxxxxx.execute-api.ap-northeast-1.amazonaws.com/prod/results
  StatisticsUrl: https://xxxxxxxxxx.execute-api.ap-northeast-1.amazonaws.com/prod/statistics
```

## API仕様

### POST /submit - テスト結果送信

学生がテスト結果を送信するエンドポイント。

**Headers:**
```
Content-Type: application/json
X-API-Key: {your-api-key}
```

**Request Body:**
```json
{
  "studentId": "S12345",
  "studentName": "山田太郎",
  "timestamp": "2025-12-02T10:30:00Z",
  "unityVersion": "2022.3.10f1",
  "categories": [
    {
      "categoryName": "Memory",
      "passCount": 4,
      "failCount": 0,
      "allPassed": true,
      "items": [
        {"itemName": "ObjectPool", "passed": true, "message": ""}
      ]
    }
  ],
  "performance": {
    "fps": 60.5,
    "cpuTimeMs": 12.3,
    "gcAllocKB": 0.5,
    "drawCalls": 85,
    "enemyCount": 500
  },
  "crc32": "A1B2C3D4",
  "signature": "hmac-sha256-signature"
}
```

**Response (成功):**
```json
{
  "success": true,
  "message": "Test results saved successfully",
  "recordId": "S12345_2025-12-02T10:30:00Z"
}
```

**Response (エラー):**
```json
{
  "success": false,
  "error": "CRC32 verification failed"
}
```

### GET /results - 結果取得（教員用）

**全件取得:**
```
GET /results
X-API-Key: {your-api-key}
```

**特定学生の結果:**
```
GET /results?studentId=S12345
X-API-Key: {your-api-key}
```

**Response:**
```json
{
  "success": true,
  "count": 25,
  "results": [...]
}
```

### GET /statistics - 統計情報（教員用）

```
GET /statistics
X-API-Key: {your-api-key}
```

**Response:**
```json
{
  "success": true,
  "statistics": {
    "totalSubmissions": 50,
    "uniqueStudents": 25,
    "allPassedCount": 18,
    "allPassedRate": 72.0,
    "categoryStats": {
      "Memory": {"pass": 100, "fail": 0},
      "CPU": {"pass": 75, "fail": 25}
    },
    "fpsAverage": 58.5,
    "fpsMax": 75.2,
    "fpsMin": 42.1
  }
}
```

## セキュリティ

### CRC32検証

データ整合性を確認。送信データから計算したCRC32と送信されたCRC32を比較。

### HMAC-SHA256署名

改ざん防止。APIキーを秘密鍵として署名を生成・検証。

```
署名対象: {studentId}|{timestamp}|{crc32}
署名: HMAC-SHA256(署名対象, APIキー)
```

### タイミング攻撃対策

`crypto.timingSafeEqual()` を使用して署名比較を行い、タイミング攻撃を防止。

## Unity側設定

1. `MassacreDojo > Submission Settings` を開く
2. サーバーURL: `https://xxxxxxxxxx.execute-api.ap-northeast-1.amazonaws.com/prod/submit`
3. APIキー: デプロイ時に設定したキー
4. 学生ID・氏名を入力

## ローカルテスト

### SAM Local でローカル実行

```bash
# ローカルAPI起動
sam local start-api

# 別ターミナルでテスト
node test_api.mjs
```

### テストスクリプトの実行

```bash
# test_api.mjs の SERVER_URL と API_KEY を編集後:
node test_api.mjs
```

## 運用

### ログ確認

```bash
sam logs -n MassacreDojo-SubmitResult --stack-name massacre-dojo-server --tail
```

### テーブル内容確認

```bash
aws dynamodb scan --table-name MassacreDojoResults
```

### スタック削除

```bash
sam delete --stack-name massacre-dojo-server
```

## DynamoDB テーブル構造

**Primary Key:**
- Partition Key (pk): studentId
- Sort Key (sk): timestamp

**Attributes:**
| 属性名 | 型 | 説明 |
|--------|------|------|
| recordId | String | 一意のレコードID |
| studentId | String | 学生ID |
| studentName | String | 学生名 |
| categories | List | テスト結果（配列） |
| performance | Map | パフォーマンス指標 |
| totalPass | Number | 合計パス数 |
| totalFail | Number | 合計失敗数 |
| allPassed | Boolean | 全テストパスしたか |
| receivedAt | String | サーバー受信時刻 |
| crc32 | String | CRCチェック値 |
| signature | String | HMAC署名 |

## コスト見積もり

- **Lambda**: 無料枠内（月100万リクエスト無料）
- **API Gateway**: 無料枠内（月100万リクエスト無料）
- **DynamoDB**: オンデマンド課金（少量なら月数円〜数十円）

30人クラス × 10回送信 = 300リクエスト/授業
→ **無料枠内で運用可能**

## トラブルシューティング

### CRC検証失敗
- クライアント側とサーバー側のCRC計算ロジックが一致しているか確認
- 浮動小数点の精度（小数点2桁）を確認

### 署名検証失敗
- APIキーがクライアントとサーバーで一致しているか確認
- タイムスタンプのフォーマット（ISO 8601）を確認

### CORS エラー
- API GatewayのCORS設定を確認
- `Access-Control-Allow-Origin` ヘッダーを確認

### Lambda タイムアウト
- DynamoDB接続のコールドスタート時間を考慮
- タイムアウト設定を30秒に設定済み
