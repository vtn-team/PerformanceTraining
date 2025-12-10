using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace PerformanceTraining.Tests.Editor
{
    /// <summary>
    /// PlayModeテスト実行時にMainGameシーンをBuild Settingsに追加する
    /// これによりテスト中のSceneManager.LoadSceneAsyncが動作する
    /// </summary>
    [InitializeOnLoad]
    public class TestSceneSetup : ICallbacks
    {
        private const string MAIN_GAME_SCENE_PATH = "Assets/Scenes/MainGame.unity";
        private static EditorBuildSettingsScene[] _originalScenes;

        static TestSceneSetup()
        {
            // テストのコールバックに登録
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new TestSceneSetup());
        }

        public int callbackOrder => 0;

        public void RunStarted(ITestAdaptor testsToRun)
        {
            // テスト開始時: 現在のBuild Settingsを保存
            _originalScenes = EditorBuildSettings.scenes;

            // MainGameシーンがBuild Settingsにあるか確認
            bool hasMainGame = _originalScenes.Any(s => s.path == MAIN_GAME_SCENE_PATH);

            if (!hasMainGame)
            {
                // MainGameシーンを追加
                var sceneList = new List<EditorBuildSettingsScene>(_originalScenes);
                sceneList.Add(new EditorBuildSettingsScene(MAIN_GAME_SCENE_PATH, true));
                EditorBuildSettings.scenes = sceneList.ToArray();
                Debug.Log($"[TestSceneSetup] Added {MAIN_GAME_SCENE_PATH} to Build Settings for tests");
            }
        }

        public void RunFinished(ITestResultAdaptor testResults)
        {
            // テスト終了時: Build Settingsを元に戻す
            if (_originalScenes != null)
            {
                EditorBuildSettings.scenes = _originalScenes;
                Debug.Log("[TestSceneSetup] Restored original Build Settings");
            }
        }

        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }
    }
}
