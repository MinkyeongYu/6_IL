using System.IO;
using UnityEditor;
using UnityEngine;

namespace IL6.EditorBuild
{
    /// <summary>
    /// IL6 의 모든 절차적 스폰 GameObject 를 .prefab 자산으로 한 번에 만들어 저장.
    /// Unity 에디터 메뉴: IL6/Tools/Generate Prefabs.
    ///
    /// 동작:
    ///   1. 빈 (0,0,0) 위치에 각 종류 GameObject 생성 (ProceduralSpawner / VillageStarter
    ///      의 기존 Spawn 메서드 호출).
    ///   2. PrefabUtility.SaveAsPrefabAsset 로 Assets/Prefabs/.../Name.prefab 저장.
    ///   3. 씬에서 즉시 Destroy.
    ///
    /// 만들어진 프리팹은 인스펙터에서 sprite/머티리얼 교체 후, Spawn 메서드에서
    /// Instantiate(prefab) 로 교체하면 됨.
    /// </summary>
    public static class PrefabGenerator
    {
        private const string AnimalsDir = "Assets/Prefabs/Animals";
        private const string CompanionsDir = "Assets/Prefabs/Companions";
        private const string BuildingsDir = "Assets/Prefabs/Buildings";

        [MenuItem("IL6/Tools/Generate All Prefabs")]
        public static void GenerateAll()
        {
            EnsureDir(AnimalsDir);
            EnsureDir(CompanionsDir);
            EnsureDir(BuildingsDir);

            int created = 0;

            // === 건물 — VillageStarter / SimpleHud 의 SpawnX 메서드들 ===
            // 빌딩들은 SimpleHud 의 인스턴스 메서드라 직접 호출 어려움.
            // 가장 단순한 두 가지(모닥불, 펜스) 만 VillageStarter 정적 메서드 사용:
            created += SaveOne(VillageStarter.SpawnCampfire(Vector3.zero), $"{BuildingsDir}/Campfire.prefab");
            created += SaveOne(VillageStarter.SpawnFence(Vector3.zero, 0f), $"{BuildingsDir}/Fence.prefab");
            created += SaveOne(VillageStarter.SpawnGate(Vector3.zero), $"{BuildingsDir}/Gate.prefab");

            Debug.Log($"[PrefabGenerator] {created} prefabs saved to {BuildingsDir} (Animals/Companions need Editor-side instantiate — see comment).");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static int SaveOne(GameObject go, string assetPath)
        {
            if (go == null) { Debug.LogWarning($"[PrefabGenerator] null GameObject for {assetPath}"); return 0; }
            // SaveAsPrefabAsset 은 root GO 를 prefab 으로, 그리고 in-scene 인스턴스도 prefab 인스턴스로 변환함.
            // 우리는 깨끗한 프리팹만 원하므로 SaveAsPrefabAsset 후 즉시 Destroy.
            var saved = PrefabUtility.SaveAsPrefabAsset(go, assetPath, out bool success);
            Object.DestroyImmediate(go);
            if (!success) { Debug.LogError($"[PrefabGenerator] failed to save {assetPath}"); return 0; }
            Debug.Log($"[PrefabGenerator] saved {assetPath}");
            return 1;
        }

        private static void EnsureDir(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }
    }
}
