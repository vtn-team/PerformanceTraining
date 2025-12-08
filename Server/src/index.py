"""
PerformanceTraining Score Server
統合Lambda関数 - zipアップロード用

エンドポイント:
- POST /submit       : スコア送信
- GET  /scores       : 全スコア取得
- GET  /scores/{udid}: UDID別スコア取得
- GET  /ranking/{ex} : 課題別ランキング取得

テーブルスキーマ:
- パーティションキー: udid (String) - 形式: {udid}#{exerciseId}
- ソートキーなし
"""
import json
import os
import boto3
from datetime import datetime
from decimal import Decimal

# DynamoDB設定
dynamodb = boto3.resource('dynamodb')
TABLE_NAME = os.environ.get('TABLE_NAME', 'PerformanceTrainingScores')
table = dynamodb.Table(TABLE_NAME)


class DecimalEncoder(json.JSONEncoder):
    """Decimal型をJSONシリアライズするためのエンコーダー"""
    def default(self, obj):
        if isinstance(obj, Decimal):
            return float(obj)
        return super(DecimalEncoder, self).default(obj)


def create_response(status_code, body):
    """共通レスポンス生成"""
    return {
        'statusCode': status_code,
        'headers': {
            'Content-Type': 'application/json',
            'Access-Control-Allow-Origin': '*',
            'Access-Control-Allow-Headers': 'Content-Type',
            'Access-Control-Allow-Methods': 'GET,POST,OPTIONS'
        },
        'body': json.dumps(body, cls=DecimalEncoder, ensure_ascii=False)
    }


def handler(event, context):
    """
    メインハンドラー
    API Gateway からのリクエストをルーティング
    """
    try:
        http_method = event.get('httpMethod', '')
        path = event.get('path', '')
        path_params = event.get('pathParameters') or {}

        # OPTIONSリクエスト（CORS preflight）
        if http_method == 'OPTIONS':
            return create_response(200, {})

        # ルーティング
        if http_method == 'POST' and path == '/submit':
            return handle_submit(event)

        elif http_method == 'GET' and path == '/scores':
            return handle_get_all_scores()

        elif http_method == 'GET' and path.startswith('/scores/'):
            udid = path_params.get('udid') or path.split('/scores/')[-1]
            return handle_get_user_scores(udid)

        elif http_method == 'GET' and path.startswith('/ranking/'):
            exercise_id = path_params.get('exerciseId') or path.split('/ranking/')[-1]
            return handle_get_ranking(exercise_id)

        else:
            return create_response(404, {'error': 'Not Found'})

    except Exception as e:
        print(f'Error: {str(e)}')
        return create_response(500, {'error': 'Internal server error'})


def handle_submit(event):
    """
    スコア送信処理

    Request Body:
    {
        "udid": "string",
        "userName": "string",
        "exerciseId": "Memory" | "CPU" | "Tradeoff",
        "score": number,
        "testsPassed": number,
        "totalTests": number,
        "executionTimeMs": number,
        "gcAllocBytes": number
    }
    """
    try:
        body = json.loads(event.get('body', '{}'))

        udid = body.get('udid', '').strip()
        user_name = body.get('userName', '').strip()
        exercise_id = body.get('exerciseId', '').strip()
        score = body.get('score', 0)
        tests_passed = body.get('testsPassed', 0)
        total_tests = body.get('totalTests', 0)
        execution_time_ms = body.get('executionTimeMs', 0)
        gc_alloc_bytes = body.get('gcAllocBytes', 0)

        # バリデーション
        if not udid:
            return create_response(400, {'error': 'udid is required'})

        if not user_name:
            return create_response(400, {'error': 'userName is required'})

        if not exercise_id:
            return create_response(400, {'error': 'exerciseId is required'})

        valid_exercises = ['Memory', 'CPU', 'Tradeoff']
        if exercise_id not in valid_exercises:
            return create_response(400, {'error': f'exerciseId must be one of {valid_exercises}'})

        # タイムスタンプ
        timestamp = datetime.utcnow().isoformat() + 'Z'

        # 複合キー: {udid}#{exerciseId}
        composite_key = f'{udid}#{exercise_id}'

        # 既存レコードを確認
        response = table.get_item(Key={'udid': composite_key})

        created_at = timestamp
        if 'Item' in response:
            created_at = response['Item'].get('createdAt', timestamp)
            existing_score = float(response['Item'].get('score', 0))
            # スコアが低い場合は更新しない
            if score <= existing_score:
                return create_response(200, {
                    'message': 'Score not updated (existing score is higher or equal)',
                    'existingScore': existing_score,
                    'submittedScore': score
                })

        # DynamoDBアイテム（フラット構造）
        item = {
            'udid': composite_key,
            'rawUdid': udid,
            'userName': user_name,
            'exerciseId': exercise_id,
            'score': Decimal(str(score)),
            'testsPassed': tests_passed,
            'totalTests': total_tests,
            'executionTimeMs': Decimal(str(execution_time_ms)),
            'gcAllocBytes': gc_alloc_bytes,
            'updatedAt': timestamp,
            'createdAt': created_at
        }

        table.put_item(Item=item)

        return create_response(200, {
            'message': 'Score submitted successfully',
            'userName': user_name,
            'exerciseId': exercise_id,
            'score': score
        })

    except json.JSONDecodeError:
        return create_response(400, {'error': 'Invalid JSON'})


def handle_get_all_scores():
    """全ユーザーのスコアを取得"""
    response = table.scan()
    items = response.get('Items', [])

    # ユーザーごとにグループ化
    users = {}
    for item in items:
        user = item.get('userName')
        if user not in users:
            users[user] = {'userName': user, 'scores': []}
        users[user]['scores'].append({
            'exerciseId': item.get('exerciseId'),
            'score': item.get('score'),
            'testsPassed': item.get('testsPassed', 0),
            'totalTests': item.get('totalTests', 0),
            'executionTimeMs': item.get('executionTimeMs', 0),
            'gcAllocBytes': item.get('gcAllocBytes', 0),
            'updatedAt': item.get('updatedAt')
        })

    return create_response(200, {'users': list(users.values())})


def handle_get_user_scores(udid):
    """特定UDIDのスコアを取得"""
    # スキャンしてrawUdidでフィルタリング
    response = table.scan(
        FilterExpression='rawUdid = :udid',
        ExpressionAttributeValues={':udid': udid}
    )
    items = response.get('Items', [])

    scores = []
    user_name = None
    for item in items:
        if user_name is None:
            user_name = item.get('userName', '')
        scores.append({
            'exerciseId': item.get('exerciseId'),
            'score': item.get('score'),
            'testsPassed': item.get('testsPassed', 0),
            'totalTests': item.get('totalTests', 0),
            'executionTimeMs': item.get('executionTimeMs', 0),
            'gcAllocBytes': item.get('gcAllocBytes', 0),
            'updatedAt': item.get('updatedAt')
        })

    return create_response(200, {
        'udid': udid,
        'userName': user_name or '',
        'scores': scores
    })


def handle_get_ranking(exercise_id):
    """課題別ランキングを取得"""
    # スキャンしてexerciseIdでフィルタリング
    response = table.scan(
        FilterExpression='exerciseId = :eid',
        ExpressionAttributeValues={':eid': exercise_id}
    )
    items = response.get('Items', [])

    # スコア降順でソート
    sorted_items = sorted(items, key=lambda x: float(x.get('score', 0)), reverse=True)

    ranking = []
    for i, item in enumerate(sorted_items, start=1):
        ranking.append({
            'rank': i,
            'userName': item.get('userName'),
            'score': item.get('score'),
            'testsPassed': item.get('testsPassed', 0),
            'totalTests': item.get('totalTests', 0),
            'updatedAt': item.get('updatedAt')
        })

    return create_response(200, {
        'exerciseId': exercise_id,
        'ranking': ranking
    })
