using System;
using System.IO;
using UnityEngine;

namespace IL6
{
    [Serializable]
    public class SaveFileV1
    {
        public int version = 1;
        public int currentDay = 1;
        public ResourceSnapshot resources = new();
        public uint weatherRng = 42;
    }

    public static class SaveLoad
    {
        public const string FileName = "save_v1.json";

        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static void Save(SaveFileV1 data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: false);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveLoad] save failed: {ex}");
            }
        }

        public static SaveFileV1 Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return null;
                string json = File.ReadAllText(FilePath);
                var parsed = JsonUtility.FromJson<SaveFileV1>(json);
                if (parsed == null || parsed.version != 1) return null;
                return parsed;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveLoad] load failed: {ex}");
                return null;
            }
        }

        public static void Clear()
        {
            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveLoad] clear failed: {ex}");
            }
        }
    }
}
