/**
 * MassacreDojo テスト結果受信 Lambda関数
 *
 * AWS Lambda + API Gateway + DynamoDB 構成
 * テスト結果を受信し、CRC/署名を検証後、DynamoDBに保存する
 */

import { DynamoDBClient } from '@aws-sdk/client-dynamodb';
import {
  DynamoDBDocumentClient,
  PutCommand,
  QueryCommand,
  ScanCommand
} from '@aws-sdk/lib-dynamodb';
import crypto from 'crypto';

// DynamoDB設定
const client = new DynamoDBClient({});
const docClient = DynamoDBDocumentClient.from(client);

const TABLE_NAME = process.env.DYNAMODB_TABLE || 'MassacreDojoResults';
const API_KEY = process.env.API_KEY || 'default_key';
const VALID_API_KEYS = (process.env.VALID_API_KEYS || API_KEY).split(',');

// CORSヘッダー
const corsHeaders = {
  'Content-Type': 'application/json',
  'Access-Control-Allow-Origin': '*',
  'Access-Control-Allow-Headers': 'Content-Type,X-API-Key',
  'Access-Control-Allow-Methods': 'GET,POST,OPTIONS'
};

/**
 * メインハンドラー - テスト結果送信
 */
export const handler = async (event) => {
  try {
    // OPTIONSリクエスト（CORS preflight）
    if (event.httpMethod === 'OPTIONS') {
      return { statusCode: 200, headers: corsHeaders, body: '' };
    }

    // APIキー検証
    const requestApiKey = event.headers?.['X-API-Key'] || event.headers?.['x-api-key'] || '';
    if (!VALID_API_KEYS.includes(requestApiKey)) {
      return errorResponse(401, 'Invalid API Key');
    }

    // リクエストボディを解析
    const data = typeof event.body === 'string' ? JSON.parse(event.body) : event.body;

    // 必須フィールドの検証
    const requiredFields = ['studentId', 'studentName', 'timestamp', 'crc32', 'signature'];
    for (const field of requiredFields) {
      if (!data[field]) {
        return errorResponse(400, `Missing required field: ${field}`);
      }
    }

    // CRC32検証
    if (!verifyCRC32(data)) {
      return errorResponse(400, 'CRC32 verification failed');
    }

    // 署名検証
    if (!verifySignature(data, requestApiKey)) {
      return errorResponse(400, 'Signature verification failed');
    }

    // DynamoDBに保存
    const result = await saveToDynamoDB(data);

    if (result.success) {
      return {
        statusCode: 200,
        headers: corsHeaders,
        body: JSON.stringify({
          success: true,
          message: 'Test results saved successfully',
          recordId: result.recordId
        })
      };
    } else {
      return errorResponse(500, result.error);
    }

  } catch (error) {
    console.error('Error:', error);
    if (error instanceof SyntaxError) {
      return errorResponse(400, `Invalid JSON: ${error.message}`);
    }
    return errorResponse(500, `Internal server error: ${error.message}`);
  }
};

/**
 * 結果取得ハンドラー（教員用）
 */
export const getResultsHandler = async (event) => {
  try {
    // APIキー検証
    const requestApiKey = event.headers?.['X-API-Key'] || event.headers?.['x-api-key'] || '';
    if (!VALID_API_KEYS.includes(requestApiKey)) {
      return errorResponse(401, 'Invalid API Key');
    }

    const studentId = event.queryStringParameters?.studentId;

    let items;
    if (studentId) {
      // 特定学生の結果を取得
      const response = await docClient.send(new QueryCommand({
        TableName: TABLE_NAME,
        KeyConditionExpression: 'pk = :pk',
        ExpressionAttributeValues: { ':pk': studentId },
        ScanIndexForward: false
      }));
      items = response.Items || [];
    } else {
      // 全件取得
      const response = await docClient.send(new ScanCommand({
        TableName: TABLE_NAME
      }));
      items = response.Items || [];
    }

    return {
      statusCode: 200,
      headers: corsHeaders,
      body: JSON.stringify({
        success: true,
        count: items.length,
        results: items
      })
    };

  } catch (error) {
    console.error('Error:', error);
    return errorResponse(500, error.message);
  }
};

/**
 * 統計情報取得ハンドラー（教員用）
 */
export const getStatisticsHandler = async (event) => {
  try {
    // APIキー検証
    const requestApiKey = event.headers?.['X-API-Key'] || event.headers?.['x-api-key'] || '';
    if (!VALID_API_KEYS.includes(requestApiKey)) {
      return errorResponse(401, 'Invalid API Key');
    }

    // 全データ取得
    const response = await docClient.send(new ScanCommand({
      TableName: TABLE_NAME
    }));
    const items = response.Items || [];

    if (items.length === 0) {
      return {
        statusCode: 200,
        headers: corsHeaders,
        body: JSON.stringify({
          success: true,
          statistics: {
            totalSubmissions: 0,
            uniqueStudents: 0
          }
        })
      };
    }

    // 統計計算
    const uniqueStudents = new Set();
    let allPassedCount = 0;
    const categoryStats = {};
    const fpsValues = [];

    for (const item of items) {
      uniqueStudents.add(item.studentId || '');

      if (item.allPassed) {
        allPassedCount++;
      }

      // カテゴリ別統計
      for (const cat of (item.categories || [])) {
        const catName = cat.categoryName || '';
        if (!categoryStats[catName]) {
          categoryStats[catName] = { pass: 0, fail: 0 };
        }
        categoryStats[catName].pass += cat.passCount || 0;
        categoryStats[catName].fail += cat.failCount || 0;
      }

      // FPS統計
      if (item.performance?.fps) {
        fpsValues.push(Number(item.performance.fps));
      }
    }

    const statistics = {
      totalSubmissions: items.length,
      uniqueStudents: uniqueStudents.size,
      allPassedCount,
      allPassedRate: items.length > 0 ? (allPassedCount / items.length) * 100 : 0,
      categoryStats,
      fpsAverage: fpsValues.length > 0 ? fpsValues.reduce((a, b) => a + b, 0) / fpsValues.length : 0,
      fpsMax: fpsValues.length > 0 ? Math.max(...fpsValues) : 0,
      fpsMin: fpsValues.length > 0 ? Math.min(...fpsValues) : 0
    };

    return {
      statusCode: 200,
      headers: corsHeaders,
      body: JSON.stringify({
        success: true,
        statistics
      })
    };

  } catch (error) {
    console.error('Error:', error);
    return errorResponse(500, error.message);
  }
};

/**
 * エラーレスポンスを生成
 */
function errorResponse(statusCode, message) {
  return {
    statusCode,
    headers: corsHeaders,
    body: JSON.stringify({
      success: false,
      error: message
    })
  };
}

/**
 * CRC32を検証
 */
function verifyCRC32(data) {
  try {
    const originalCrc = data.crc32 || '';

    // CRC計算用の文字列を構築
    let crcString = '';
    crcString += String(data.studentId || '');
    crcString += String(data.studentName || '');
    crcString += String(data.timestamp || '');
    crcString += String(data.unityVersion || '');

    // パフォーマンスデータ
    const perf = data.performance || {};
    if (perf) {
      crcString += Number(perf.fps || 0).toFixed(2);
      crcString += Number(perf.cpuTimeMs || 0).toFixed(2);
      crcString += Number(perf.gcAllocKB || 0).toFixed(2);
      crcString += String(perf.drawCalls || 0);
      crcString += String(perf.enemyCount || 0);
    }

    // カテゴリデータ
    const categories = data.categories || [];
    for (const cat of categories) {
      crcString += String(cat.categoryName || '');
      crcString += String(cat.passCount || 0);
      crcString += String(cat.failCount || 0);

      const items = cat.items || [];
      for (const item of items) {
        crcString += String(item.itemName || '');
        crcString += String(item.passed || false);
      }
    }

    // CRC32計算
    const calculatedCrc = computeCRC32(Buffer.from(crcString, 'utf-8'));
    const calculatedCrcHex = calculatedCrc.toString(16).toUpperCase().padStart(8, '0');

    return calculatedCrcHex === originalCrc.toUpperCase();

  } catch (error) {
    console.error('CRC verification error:', error);
    return false;
  }
}

/**
 * CRC32を計算
 */
function computeCRC32(buffer) {
  // CRC32テーブルを生成
  const table = new Uint32Array(256);
  const polynomial = 0xEDB88320;

  for (let i = 0; i < 256; i++) {
    let crc = i;
    for (let j = 0; j < 8; j++) {
      if (crc & 1) {
        crc = (crc >>> 1) ^ polynomial;
      } else {
        crc >>>= 1;
      }
    }
    table[i] = crc >>> 0;
  }

  // CRC計算
  let crc = 0xFFFFFFFF;
  for (const byte of buffer) {
    crc = (crc >>> 8) ^ table[(crc ^ byte) & 0xFF];
  }

  return (~crc) >>> 0;
}

/**
 * HMAC-SHA256署名を検証
 */
function verifySignature(data, apiKey) {
  try {
    const originalSignature = data.signature || '';

    // 署名対象の文字列
    const signatureBase = `${data.studentId || ''}|${data.timestamp || ''}|${data.crc32 || ''}`;

    // HMAC-SHA256計算
    const calculatedSignature = crypto
      .createHmac('sha256', apiKey)
      .update(signatureBase)
      .digest('hex')
      .toLowerCase();

    // タイミング攻撃対策
    return crypto.timingSafeEqual(
      Buffer.from(calculatedSignature),
      Buffer.from(originalSignature.toLowerCase())
    );

  } catch (error) {
    console.error('Signature verification error:', error);
    return false;
  }
}

/**
 * DynamoDBにテスト結果を保存
 */
async function saveToDynamoDB(data) {
  try {
    const studentId = data.studentId || '';
    const timestamp = data.timestamp || '';
    const recordId = `${studentId}_${timestamp}`;

    // 合計スコアを計算
    let totalPass = 0;
    let totalFail = 0;
    for (const cat of (data.categories || [])) {
      totalPass += cat.passCount || 0;
      totalFail += cat.failCount || 0;
    }

    const item = {
      ...data,
      recordId,
      pk: studentId,
      sk: timestamp,
      receivedAt: new Date().toISOString(),
      totalPass,
      totalFail,
      totalScore: totalPass,
      allPassed: totalFail === 0
    };

    await docClient.send(new PutCommand({
      TableName: TABLE_NAME,
      Item: item
    }));

    return { success: true, recordId };

  } catch (error) {
    console.error('DynamoDB save error:', error);
    return { success: false, error: error.message };
  }
}
