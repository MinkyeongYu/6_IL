using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IL6.EditorBuild
{
    /// <summary>
    /// 메뉴: IL6 > Setup Project
    /// 한 번 실행으로 씬 생성 + 스프라이트 임포트 수정 + Build Settings 등록 완료.
    /// </summary>
    public static class ProjectSetup
    {
        private const string BootScenePath       = "Assets/Scenes/BootScene.unity";
        private const string OnboardingScenePath = "Assets/Scenes/OnboardingScene.unity";
        private const string SnowfieldScenePath  = "Assets/Scenes/SnowfieldScene.unity";

        [MenuItem("IL6/Setup Project", priority = 0)]
        public static void Setup()
        {
            FixSpriteImports();
            EnsureScenesDirectory();
            SetupBootScene();
            SetupOnboardingScene();
            RegisterBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ProjectSetup] 완료! Boot → Onboarding → Snowfield");
            EditorUtility.DisplayDialog("IL6 Setup",
                "프로젝트 설정 완료!\n\nBoot → Onboarding → Snowfield 씬 등록\n스프라이트 임포트 설정 적용\n\nIL6 > Build > Windows Standalone 으로 빌드하세요.",
                "확인");
        }

        // ── 스프라이트 임포트 강제 설정 ─────────────────────────────────────────
        [MenuItem("IL6/Fix Sprite Imports", priority = 1)]
        public static void FixSpriteImports()
        {
            string[] folders = { "Assets/Resources/Sprites" };
            int count = 0;

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", folders);
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite)
                { importer.textureType = TextureImporterType.Sprite; changed = true; }
                if (importer.spriteImportMode != SpriteImportMode.Single)
                { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
                if (!importer.alphaIsTransparency)
                { importer.alphaIsTransparency = true; changed = true; }
                if (importer.mipmapEnabled)
                { importer.mipmapEnabled = false; changed = true; }
                if (importer.filterMode != FilterMode.Point)
                { importer.filterMode = FilterMode.Point; changed = true; }

                if (changed)
                {
                    importer.SaveAndReimport();
                    count++;
                    Debug.Log($"[FixSprites] {path}");
                }
            }
            AssetDatabase.Refresh();
            Debug.Log($"[FixSprites] {count}개 스프라이트 임포트 설정 적용 완료");
            if (count > 0)
                EditorUtility.DisplayDialog("Fix Sprites", $"{count}개 스프라이트 임포트 설정 적용 완료.", "확인");
        }

        // ── Boot Scene ──────────────────────────────────────────────────────────
        private static void SetupBootScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var sessionGo = new GameObject("GameSession");
            sessionGo.AddComponent<GameSession>();
            var bootGo = new GameObject("BootController");
            bootGo.AddComponent<BootController>();
            EditorSceneManager.SaveScene(scene, BootScenePath);
            Debug.Log($"[ProjectSetup] BootScene → {BootScenePath}");
        }

        // ── Onboarding Scene ────────────────────────────────────────────────────
        private static void SetupOnboardingScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("OnboardingController");
            go.AddComponent<OnboardingController>();
            EditorSceneManager.SaveScene(scene, OnboardingScenePath);
            Debug.Log($"[ProjectSetup] OnboardingScene → {OnboardingScenePath}");
        }

        // ── Build Settings ──────────────────────────────────────────────────────
        private static void RegisterBuildSettings()
        {
            if (!File.Exists(SnowfieldScenePath))
                Debug.LogWarning($"[ProjectSetup] {SnowfieldScenePath} 없음");

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootScenePath,       true),
                new EditorBuildSettingsScene(OnboardingScenePath, true),
                new EditorBuildSettingsScene(SnowfieldScenePath,  File.Exists(SnowfieldScenePath)),
            };
            Debug.Log("[ProjectSetup] Build Settings 등록 완료");
        }

        private static void EnsureScenesDirectory()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
        }
    }
}
