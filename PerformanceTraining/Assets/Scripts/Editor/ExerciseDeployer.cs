using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace PerformanceTraining.Editor
{
    /// <summary>
    /// 課題ファイルを展開するユーティリティ
    /// 学生が準備できたタイミングで課題ファイルをAssets直下に展開する
    /// </summary>
    public static class ExerciseDeployer
    {
        // 展開先フォルダ名
        private const string EXERCISE_FOLDER_NAME = "StudentExercises";

        // 展開時に追加するスクリプティング定義シンボル
        private const string EXERCISES_DEPLOYED_SYMBOL = "EXERCISES_DEPLOYED";

        // ソースファイルのパス
        private static readonly string SourcePath = "Assets/Scripts/Exercises";

        // 展開先パス
        private static readonly string DestinationPath = $"Assets/{EXERCISE_FOLDER_NAME}";

        /// <summary>
        /// 課題ファイルが既に展開されているか確認
        /// </summary>
        public static bool IsDeployed()
        {
            return AssetDatabase.IsValidFolder(DestinationPath);
        }

        /// <summary>
        /// 全課題ファイルを展開
        /// </summary>
        public static bool DeployAllExercises()
        {
            if (IsDeployed())
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "課題フォルダが既に存在します",
                    $"'{EXERCISE_FOLDER_NAME}' フォルダが既に存在します。\n上書きしますか？",
                    "上書き", "キャンセル");

                if (!overwrite) return false;

                // 既存フォルダを削除
                AssetDatabase.DeleteAsset(DestinationPath);
            }

            try
            {
                // フォルダ構造を作成
                AssetDatabase.CreateFolder("Assets", EXERCISE_FOLDER_NAME);
                AssetDatabase.CreateFolder(DestinationPath, "Memory");
                AssetDatabase.CreateFolder(DestinationPath, "CPU");
                AssetDatabase.CreateFolder(DestinationPath, "Tradeoff");

                int deployedCount = 0;

                // Memory課題
                deployedCount += DeployExerciseFile("Memory", "ZeroAllocation_Exercise.cs");

                // CPU課題
                deployedCount += DeployExerciseFile("CPU", "CPUOptimization_Exercise.cs");

                // Tradeoff課題
                deployedCount += DeployExerciseFile("Tradeoff", "NeighborCache_Exercise.cs");
                deployedCount += DeployExerciseFile("Tradeoff", "DecisionCache_Exercise.cs");
                deployedCount += DeployExerciseFile("Tradeoff", "TrigLUT_Exercise.cs");
                deployedCount += DeployExerciseFile("Tradeoff", "VisibilityMap_Exercise.cs");

                AssetDatabase.Refresh();

                // スクリプティング定義シンボルを追加
                AddScriptingDefineSymbol(EXERCISES_DEPLOYED_SYMBOL);

                EditorUtility.DisplayDialog(
                    "展開完了",
                    $"{deployedCount} 個の課題ファイルを展開しました。\n\n" +
                    $"展開先: {DestinationPath}\n\n" +
                    "これらのファイルを編集して課題を完成させてください。",
                    "OK");

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"課題ファイルの展開に失敗しました: {e.Message}");
                EditorUtility.DisplayDialog("エラー", $"展開に失敗しました:\n{e.Message}", "OK");
                return false;
            }
        }

        /// <summary>
        /// 個別の課題ファイルを展開
        /// </summary>
        private static int DeployExerciseFile(string category, string fileName)
        {
            string sourcePath = $"{SourcePath}/{category}/{fileName}";
            string destPath = $"{DestinationPath}/{category}/{fileName}";

            if (!File.Exists(sourcePath))
            {
                Debug.LogWarning($"ソースファイルが見つかりません: {sourcePath}");
                return 0;
            }

            // ファイルを読み込み
            string content = File.ReadAllText(sourcePath);

            // 名前空間を変更（学生用）
            content = content.Replace(
                "namespace PerformanceTraining.Exercises",
                "namespace StudentExercises");

            // ファイルを書き込み
            File.WriteAllText(destPath, content);

            Debug.Log($"展開: {fileName}");
            return 1;
        }

        /// <summary>
        /// 特定カテゴリの課題のみ展開
        /// </summary>
        public static bool DeployCategory(string category)
        {
            string categoryPath = $"{DestinationPath}/{category}";

            if (!AssetDatabase.IsValidFolder(DestinationPath))
            {
                AssetDatabase.CreateFolder("Assets", EXERCISE_FOLDER_NAME);
            }

            if (!AssetDatabase.IsValidFolder(categoryPath))
            {
                AssetDatabase.CreateFolder(DestinationPath, category);
            }

            string sourceCategoryPath = $"{SourcePath}/{category}";
            if (!Directory.Exists(sourceCategoryPath))
            {
                Debug.LogError($"ソースカテゴリが見つかりません: {sourceCategoryPath}");
                return false;
            }

            int count = 0;
            foreach (var file in Directory.GetFiles(sourceCategoryPath, "*.cs"))
            {
                string fileName = Path.GetFileName(file);
                if (fileName.Contains("_Exercise"))
                {
                    count += DeployExerciseFile(category, fileName);
                }
            }

            AssetDatabase.Refresh();
            return count > 0;
        }

        /// <summary>
        /// 展開した課題フォルダを削除
        /// </summary>
        public static bool RemoveDeployedExercises()
        {
            if (!IsDeployed())
            {
                EditorUtility.DisplayDialog("情報", "展開された課題フォルダはありません。", "OK");
                return false;
            }

            bool confirm = EditorUtility.DisplayDialog(
                "確認",
                $"'{EXERCISE_FOLDER_NAME}' フォルダを削除しますか？\n" +
                "学生の作業内容が失われます。",
                "削除", "キャンセル");

            if (!confirm) return false;

            AssetDatabase.DeleteAsset(DestinationPath);
            AssetDatabase.Refresh();

            // スクリプティング定義シンボルを削除
            RemoveScriptingDefineSymbol(EXERCISES_DEPLOYED_SYMBOL);

            EditorUtility.DisplayDialog("完了", "課題フォルダを削除しました。", "OK");
            return true;
        }

        /// <summary>
        /// 展開先フォルダのパスを取得
        /// </summary>
        public static string GetDeploymentPath()
        {
            return DestinationPath;
        }

        /// <summary>
        /// 展開状態の詳細を取得
        /// </summary>
        public static Dictionary<string, bool> GetDeploymentStatus()
        {
            var status = new Dictionary<string, bool>();

            status["Memory/ZeroAllocation_Exercise.cs"] =
                File.Exists($"{DestinationPath}/Memory/ZeroAllocation_Exercise.cs");
            status["CPU/CPUOptimization_Exercise.cs"] =
                File.Exists($"{DestinationPath}/CPU/CPUOptimization_Exercise.cs");
            status["Tradeoff/NeighborCache_Exercise.cs"] =
                File.Exists($"{DestinationPath}/Tradeoff/NeighborCache_Exercise.cs");
            status["Tradeoff/DecisionCache_Exercise.cs"] =
                File.Exists($"{DestinationPath}/Tradeoff/DecisionCache_Exercise.cs");
            status["Tradeoff/TrigLUT_Exercise.cs"] =
                File.Exists($"{DestinationPath}/Tradeoff/TrigLUT_Exercise.cs");
            status["Tradeoff/VisibilityMap_Exercise.cs"] =
                File.Exists($"{DestinationPath}/Tradeoff/VisibilityMap_Exercise.cs");

            return status;
        }

        /// <summary>
        /// スクリプティング定義シンボルを追加
        /// </summary>
        private static void AddScriptingDefineSymbol(string symbol)
        {
            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            PlayerSettings.GetScriptingDefineSymbols(namedTarget, out string[] definesArray);
            var defines = string.Join(";", definesArray);

            if (!definesArray.Contains(symbol))
            {
                defines = string.IsNullOrEmpty(defines) ? symbol : $"{defines};{symbol}";
                PlayerSettings.SetScriptingDefineSymbols(namedTarget, defines);
                Debug.Log($"Added scripting define symbol: {symbol}");
            }
        }

        /// <summary>
        /// スクリプティング定義シンボルを削除
        /// </summary>
        private static void RemoveScriptingDefineSymbol(string symbol)
        {
            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            PlayerSettings.GetScriptingDefineSymbols(namedTarget, out string[] definesArray);

            var symbolList = definesArray.ToList();
            if (symbolList.Remove(symbol))
            {
                PlayerSettings.SetScriptingDefineSymbols(namedTarget, string.Join(";", symbolList));
                Debug.Log($"Removed scripting define symbol: {symbol}");
            }
        }

        /// <summary>
        /// 課題が展開されているかシンボルで確認
        /// </summary>
        public static bool IsExercisesDeployedSymbolDefined()
        {
            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            PlayerSettings.GetScriptingDefineSymbols(namedTarget, out string[] definesArray);
            return definesArray.Contains(EXERCISES_DEPLOYED_SYMBOL);
        }
    }
}
