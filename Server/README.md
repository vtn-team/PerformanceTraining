# PerformanceTraining Score Server

AWS Lambda + API Gateway + DynamoDB を使用したサーバーレスAPIです。

## ファイル構成

```
Server/
├── template.yaml           # AWS SAM テンプレート
├── README.md               # このファイル
└── src/
    └── lambda_function.py  # Lambda関数（統合版）
```

## デプロイ方法

### 方法1: AWS SAM CLI を使用（推奨）

```bash
cd Server

# ビルド
sam build

# デプロイ（初回）
sam deploy --guided

# デプロイ（2回目以降）
sam deploy
```

### 方法2: zipアップロード（AWS Console）

1. **zipファイルの作成**
   ```bash
   cd Server/src
   zip lambda_function.zip lambda_function.py
   ```

2. **AWS Consoleでの設定**
   - Lambda関数を作成（Python 3.11）
   - 作成したzipファイルをアップロード
   - ハンドラを `lambda_function.lambda_handler` に設定
   - 環境変数 `TABLE_NAME` に DynamoDB テーブル名を設定
   - DynamoDB へのアクセス権限を付与

3. **DynamoDBテーブルの作成**
   - テーブル名: `PerformanceTrainingScores`
   - パーティションキー: `pk` (String)
   - ソートキー: `sk` (String)
   - GSI名: `ExerciseIndex` (sk がパーティションキー)

4. **API Gatewayの設定**
   - REST API を作成
   - 以下のエンドポイントを設定：
     - `POST /submit` → Lambda関数
     - `GET /scores` → Lambda関数
     - `GET /scores/{userName}` → Lambda関数
     - `GET /ranking/{exerciseId}` → Lambda関数
   - CORSを有効化
   - デプロイしてURLを取得

---

## API エンドポイント

### スコア送信
```
POST /submit
```

Request Body:
```json
{
    "userName": "山田太郎",
    "exerciseId": "Memory",
    "score": 85,
    "details": {
        "testsPassed": 4,
        "totalTests": 5,
        "gcAllocBytes": 1024
    }
}
```

Response:
```json
{
    "message": "Score submitted successfully",
    "userName": "山田太郎",
    "exerciseId": "Memory",
    "score": 85
}
```

### 全スコア取得
```
GET /scores
```

Response:
```json
{
    "users": [
        {
            "userName": "山田太郎",
            "scores": [
                {
                    "exerciseId": "Memory",
                    "score": 85,
                    "details": {...},
                    "updatedAt": "2024-01-01T00:00:00Z"
                }
            ]
        }
    ]
}
```

### ユーザー別スコア取得
```
GET /scores/{userName}
```

### ランキング取得
```
GET /ranking/{exerciseId}
```

Response:
```json
{
    "exerciseId": "Memory",
    "ranking": [
        {"rank": 1, "userName": "山田太郎", "score": 95, ...},
        {"rank": 2, "userName": "鈴木花子", "score": 85, ...}
    ]
}
```

---

## ローカルテスト

```bash
# SAM CLIでローカル起動
sam local start-api

# テストリクエスト
curl -X POST http://localhost:3000/submit \
  -H "Content-Type: application/json" \
  -d '{"userName":"test","exerciseId":"Memory","score":80}'

curl http://localhost:3000/scores
curl http://localhost:3000/ranking/Memory
```

## 削除

```bash
sam delete
```
