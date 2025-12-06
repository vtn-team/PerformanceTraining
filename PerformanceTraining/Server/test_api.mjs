/**
 * MassacreDojo API テストスクリプト
 *
 * サーバーが正しく動作しているかをテストする
 */

import crypto from 'crypto';

// 設定（デプロイ後に変更してください）
const SERVER_URL = 'https://your-api-gateway-url.execute-api.ap-northeast-1.amazonaws.com/prod';
const API_KEY = 'your-api-key';

/**
 * CRC32を計算
 */
function computeCRC32(buffer) {
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

  let crc = 0xFFFFFFFF;
  for (const byte of buffer) {
    crc = (crc >>> 8) ^ table[(crc ^ byte) & 0xFF];
  }

  return (~crc) >>> 0;
}

/**
 * テストデータを作成
 */
function createTestData(studentId, studentName) {
  const timestamp = new Date().toISOString();

  const data = {
    studentId,
    studentName,
    timestamp,
    unityVersion: '2022.3.10f1',
    categories: [
      {
        categoryName: 'Memory',
        passCount: 4,
        failCount: 0,
        allPassed: true,
        items: [
          { itemName: 'ObjectPool', passed: true, message: '' },
          { itemName: 'StringBuilder', passed: true, message: '' },
          { itemName: 'DelegateCache', passed: true, message: '' },
          { itemName: 'CollectionReuse', passed: true, message: '' }
        ]
      },
      {
        categoryName: 'CPU',
        passCount: 3,
        failCount: 0,
        allPassed: true,
        items: [
          { itemName: 'SpatialPartition', passed: true, message: '' },
          { itemName: 'StaggeredUpdate', passed: true, message: '' },
          { itemName: 'SqrMagnitude', passed: true, message: '' }
        ]
      },
      {
        categoryName: 'Tradeoff',
        passCount: 2,
        failCount: 0,
        allPassed: true,
        items: [
          { itemName: 'NeighborCache', passed: true, message: '' },
          { itemName: 'DecisionCache', passed: true, message: '' }
        ]
      },
      {
        categoryName: 'Graphics',
        passCount: 2,
        failCount: 1,
        allPassed: false,
        items: [
          { itemName: 'GPUInstancing', passed: true, message: '' },
          { itemName: 'LOD', passed: true, message: '' },
          { itemName: 'Culling', passed: false, message: '未設定' }
        ]
      }
    ],
    performance: {
      fps: 62.5,
      cpuTimeMs: 14.2,
      gcAllocKB: 0.8,
      drawCalls: 95,
      enemyCount: 500
    }
  };

  // CRC32を計算
  let crcString = '';
  crcString += data.studentId;
  crcString += data.studentName;
  crcString += data.timestamp;
  crcString += data.unityVersion;

  const perf = data.performance;
  crcString += perf.fps.toFixed(2);
  crcString += perf.cpuTimeMs.toFixed(2);
  crcString += perf.gcAllocKB.toFixed(2);
  crcString += String(perf.drawCalls);
  crcString += String(perf.enemyCount);

  for (const cat of data.categories) {
    crcString += cat.categoryName;
    crcString += String(cat.passCount);
    crcString += String(cat.failCount);
    for (const item of cat.items) {
      crcString += item.itemName;
      crcString += String(item.passed);
    }
  }

  const crc32 = computeCRC32(Buffer.from(crcString, 'utf-8'));
  data.crc32 = crc32.toString(16).toUpperCase().padStart(8, '0');

  // 署名を生成
  const signatureBase = `${data.studentId}|${data.timestamp}|${data.crc32}`;
  data.signature = crypto
    .createHmac('sha256', API_KEY)
    .update(signatureBase)
    .digest('hex')
    .toLowerCase();

  return data;
}

/**
 * 送信テスト
 */
async function testSubmit() {
  console.log('=== 送信テスト ===');

  const data = createTestData('TEST001', 'テスト太郎');
  console.log('送信データ:');
  console.log(JSON.stringify(data, null, 2));

  try {
    const response = await fetch(`${SERVER_URL}/submit`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-API-Key': API_KEY
      },
      body: JSON.stringify(data)
    });

    console.log(`\nステータスコード: ${response.status}`);
    const responseData = await response.json();
    console.log('レスポンス:', JSON.stringify(responseData, null, 2));

    if (response.status === 200) {
      console.log('✓ 送信成功');
      return true;
    } else {
      console.log('✗ 送信失敗');
      return false;
    }
  } catch (error) {
    console.log(`✗ エラー: ${error.message}`);
    return false;
  }
}

/**
 * 結果取得テスト
 */
async function testGetResults() {
  console.log('\n=== 結果取得テスト ===');

  try {
    const response = await fetch(`${SERVER_URL}/results`, {
      headers: { 'X-API-Key': API_KEY }
    });

    console.log(`ステータスコード: ${response.status}`);

    if (response.status === 200) {
      const data = await response.json();
      console.log(`取得件数: ${data.count || 0}`);
      console.log('✓ 取得成功');
      return true;
    } else {
      const data = await response.text();
      console.log(`レスポンス: ${data}`);
      console.log('✗ 取得失敗');
      return false;
    }
  } catch (error) {
    console.log(`✗ エラー: ${error.message}`);
    return false;
  }
}

/**
 * 統計情報取得テスト
 */
async function testGetStatistics() {
  console.log('\n=== 統計情報取得テスト ===');

  try {
    const response = await fetch(`${SERVER_URL}/statistics`, {
      headers: { 'X-API-Key': API_KEY }
    });

    console.log(`ステータスコード: ${response.status}`);

    if (response.status === 200) {
      const data = await response.json();
      const stats = data.statistics || {};
      console.log(`総送信数: ${stats.totalSubmissions || 0}`);
      console.log(`ユニーク学生数: ${stats.uniqueStudents || 0}`);
      console.log(`全パス率: ${(stats.allPassedRate || 0).toFixed(1)}%`);
      console.log('✓ 取得成功');
      return true;
    } else {
      const data = await response.text();
      console.log(`レスポンス: ${data}`);
      console.log('✗ 取得失敗');
      return false;
    }
  } catch (error) {
    console.log(`✗ エラー: ${error.message}`);
    return false;
  }
}

/**
 * 不正CRCテスト
 */
async function testInvalidCRC() {
  console.log('\n=== 不正CRCテスト ===');

  const data = createTestData('TEST002', '不正テスト');
  data.crc32 = '00000000'; // 不正なCRC

  try {
    const response = await fetch(`${SERVER_URL}/submit`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-API-Key': API_KEY
      },
      body: JSON.stringify(data)
    });

    console.log(`ステータスコード: ${response.status}`);
    const responseData = await response.text();
    console.log(`レスポンス: ${responseData}`);

    if (response.status === 400) {
      console.log('✓ 正しく拒否された');
      return true;
    } else {
      console.log('✗ 拒否されるべきだった');
      return false;
    }
  } catch (error) {
    console.log(`✗ エラー: ${error.message}`);
    return false;
  }
}

/**
 * 不正APIキーテスト
 */
async function testInvalidApiKey() {
  console.log('\n=== 不正APIキーテスト ===');

  const data = createTestData('TEST003', '不正キーテスト');

  try {
    const response = await fetch(`${SERVER_URL}/submit`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-API-Key': 'invalid_key'
      },
      body: JSON.stringify(data)
    });

    console.log(`ステータスコード: ${response.status}`);
    const responseData = await response.text();
    console.log(`レスポンス: ${responseData}`);

    if (response.status === 401) {
      console.log('✓ 正しく拒否された');
      return true;
    } else {
      console.log('✗ 拒否されるべきだった');
      return false;
    }
  } catch (error) {
    console.log(`✗ エラー: ${error.message}`);
    return false;
  }
}

/**
 * メイン実行
 */
async function main() {
  console.log('MassacreDojo API テスト');
  console.log('='.repeat(50));
  console.log(`サーバー: ${SERVER_URL}`);
  console.log('='.repeat(50));

  const results = [];

  results.push(['送信テスト', await testSubmit()]);
  results.push(['結果取得テスト', await testGetResults()]);
  results.push(['統計情報取得テスト', await testGetStatistics()]);
  results.push(['不正CRCテスト', await testInvalidCRC()]);
  results.push(['不正APIキーテスト', await testInvalidApiKey()]);

  console.log('\n' + '='.repeat(50));
  console.log('テスト結果サマリー');
  console.log('='.repeat(50));

  let passed = 0;
  let failed = 0;

  for (const [name, result] of results) {
    const status = result ? '✓ PASS' : '✗ FAIL';
    console.log(`  ${status}: ${name}`);
    if (result) passed++;
    else failed++;
  }

  console.log(`\n合計: ${passed} PASS / ${failed} FAIL`);
}

main().catch(console.error);
