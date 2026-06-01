using System.IO;
using UnityEditor;
using UnityEngine;

namespace IL6.EditorBuild
{
    /// <summary>
    /// IL6 의 모든 절차적 스폰 GameObject 를 .prefab 자산으로 한 번에 만들어 저장.
    /// Unity 메뉴: IL6/Tools/Generate All Prefabs.
    ///
    /// 동작:
    ///   1. 빈 (0,0,0) 위치에 각 종류 GameObject 생성 (BuildingFactory / VillageStarter
    ///      / ProceduralSpawner 정적 메서드 호출).
    ///   2. PrefabUtility.SaveAsPrefabAsset 로 Assets/Prefabs/.../Name.prefab 저장.
    ///   3. 씬에서 즉시 Destroy.
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

            int n = 0;

            // === 건물 ===
            n += SaveOne(VillageStarter.SpawnCampfire(Vector3.zero), $"{BuildingsDir}/Campfire.prefab");
            n += SaveOne(VillageStarter.SpawnFence(Vector3.zero, 0f), $"{BuildingsDir}/Fence.prefab");
            n += SaveOne(VillageStarter.SpawnGate(Vector3.zero), $"{BuildingsDir}/Gate.prefab");
            n += SaveOne(BuildingFactory.SpawnBarricade(Vector3.zero), $"{BuildingsDir}/Barricade.prefab");
            n += SaveOne(BuildingFactory.SpawnHouse(Vector3.zero), $"{BuildingsDir}/House.prefab");
            n += SaveOne(BuildingFactory.SpawnStorage(Vector3.zero), $"{BuildingsDir}/Storage.prefab");
            n += SaveOne(BuildingFactory.SpawnFarm(Vector3.zero), $"{BuildingsDir}/Farm.prefab");
            n += SaveOne(BuildingFactory.SpawnWatchtower(Vector3.zero), $"{BuildingsDir}/Watchtower.prefab");
            n += SaveOne(BuildingFactory.SpawnInfirmary(Vector3.zero), $"{BuildingsDir}/Infirmary.prefab");
            n += SaveOne(BuildingFactory.SpawnHuntersHut(Vector3.zero), $"{BuildingsDir}/HuntersHut.prefab");

            // === 동물 — 모든 archetype ===
            foreach (var a in ProceduralSpawner._animals)
            {
                int hp = Mathf.Max(1, a.Hp);
                int yield = a.MeatMin == a.MeatMax ? a.MeatMin : a.MeatMin;
                var go = ProceduralSpawner.CreateOneAnimal(a, 0, 0, yield, hp);
                // 파일명 — Name 의 _proc 접미 제거
                string name = a.Name.Replace("_proc", "");
                n += SaveOne(go, $"{AnimalsDir}/{name}.prefab");
            }

            // === 동료(NPC) — 모든 archetype ===
            foreach (var arch in ProceduralSpawner._npcArchetypes)
            {
                var go = ProceduralSpawner.CreateNpcFromArchetype(arch, "Sample", Vector3.zero);
                n += SaveOne(go, $"{CompanionsDir}/{arch.Role}.prefab");
            }

            Debug.Log($"[PrefabGenerator] {n} prefabs saved.  Animals→{AnimalsDir}, Companions→{CompanionsDir}, Buildings→{BuildingsDir}");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static int SaveOne(GameObject go, string assetPath)
        {
            if (go == null) { Debug.LogWarning($"[PrefabGenerator] null GameObject for {assetPath}"); return 0; }
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
