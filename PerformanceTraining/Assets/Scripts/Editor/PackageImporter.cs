using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace PerformanceTraining.Editor
{
    /// <summary>
    /// S3+CloudFrontから.unitypackageをダウンロードしてインポートするエディタウィンドウ
    /// </summary>
    public class PackageImporter : EditorWindow
    {
        // CloudFrontのベースURL（必要に応じて変更）
        private const string BASE_URL = "https://your-distribution.cloudfront.net/packages/";

        private string _packageUrl = "";
        private string _statusMessage = "";
        private float _progress = 0f;
        private bool _isDownloading = false;

        [MenuItem("Tools/Package Importer")]
        public static void ShowWindow()
        {
            GetWindow<PackageImporter>("Package Importer");
        }

        private void OnGUI()
        {
            GUILayout.Label("Package Importer", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // URL入力
            GUILayout.Label("Package URL:");
            _packageUrl = EditorGUILayout.TextField(_packageUrl);

            GUILayout.Space(5);

            // プリセットボタン（必要に応じてカスタマイズ）
            GUILayout.Label("Presets:", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Exercise Pack"))
            {
                _packageUrl = BASE_URL + "exercise-pack.unitypackage";
            }
            if (GUILayout.Button("Assets Pack"))
            {
                _packageUrl = BASE_URL + "assets-pack.unitypackage";
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(15);

            // ダウンロードボタン
            EditorGUI.BeginDisabledGroup(_isDownloading || string.IsNullOrEmpty(_packageUrl));
            if (GUILayout.Button("Download & Import", GUILayout.Height(30)))
            {
                DownloadAndImportAsync(_packageUrl);
            }
            EditorGUI.EndDisabledGroup();

            // プログレスバー
            if (_isDownloading)
            {
                GUILayout.Space(10);
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(GUILayout.Height(20)), _progress, $"{(_progress * 100):F0}%");
            }

            // ステータスメッセージ
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                GUILayout.Space(10);
                EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);
            }
        }

        private async void DownloadAndImportAsync(string url)
        {
            _isDownloading = true;
            _progress = 0f;
            _statusMessage = "ダウンロード準備中...";
            Repaint();

            string tempPath = Path.Combine(Application.temporaryCachePath, "downloaded_package.unitypackage");

            try
            {
                // ダウンロード実行
                bool success = await DownloadFileAsync(url, tempPath);

                if (success && File.Exists(tempPath))
                {
                    var fileInfo = new FileInfo(tempPath);
                    _statusMessage = $"ダウンロード完了 ({fileInfo.Length / 1024:N0} KB)\nインポート中...";
                    Repaint();

                    // パッケージをインポート
                    AssetDatabase.ImportPackage(tempPath, true);
                    _statusMessage = "インポートダイアログを表示しました。";
                }
            }
            catch (Exception e)
            {
                _statusMessage = $"エラー: {e.Message}";
                Debug.LogError($"[PackageImporter] {e}");
            }
            finally
            {
                _isDownloading = false;
                _progress = 0f;

                // 一時ファイル削除
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }

                Repaint();
            }
        }

        private async Task<bool> DownloadFileAsync(string url, string outputPath)
        {
            using (var client = new WebClient())
            {
                client.DownloadProgressChanged += (sender, e) =>
                {
                    _progress = e.ProgressPercentage / 100f;
                    _statusMessage = $"ダウンロード中... {e.BytesReceived / 1024:N0} KB / {e.TotalBytesToReceive / 1024:N0} KB";

                    // メインスレッドでRepaint
                    EditorApplication.delayCall += Repaint;
                };

                try
                {
                    await client.DownloadFileTaskAsync(new Uri(url), outputPath);
                    return true;
                }
                catch (WebException we)
                {
                    if (we.Response is HttpWebResponse response)
                    {
                        _statusMessage = $"HTTP Error {(int)response.StatusCode}: {response.StatusDescription}";
                    }
                    else
                    {
                        _statusMessage = $"ネットワークエラー: {we.Message}";
                    }
                    return false;
                }
            }
        }
    }

    /// <summary>
    /// URLを直接指定してインポートするユーティリティ
    /// </summary>
    public static class PackageImporterUtility
    {
        /// <summary>
        /// URLからパッケージをダウンロードしてインポート
        /// </summary>
        public static void ImportFromUrl(string url)
        {
            string tempPath = Path.Combine(Application.temporaryCachePath, "downloaded_package.unitypackage");

            try
            {
                Debug.Log($"[PackageImporter] Downloading: {url}");

                using (var client = new WebClient())
                {
                    client.DownloadFile(url, tempPath);
                }

                if (File.Exists(tempPath))
                {
                    Debug.Log($"[PackageImporter] Download complete. Importing...");
                    AssetDatabase.ImportPackage(tempPath, true);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PackageImporter] Error: {e.Message}");
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }
            }
        }
    }
}
