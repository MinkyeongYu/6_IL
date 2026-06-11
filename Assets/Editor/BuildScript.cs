using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace IL6.EditorBuild
{
    /// <summary>
    /// CLI 빌드 — Unity.exe -batchmode -quit -executeMethod IL6.EditorBuild.BuildScript.BuildWebGL.
    /// 결과: <repo>/Build/WebGL/.
    /// </summary>
    public static class BuildScript
    {
        private const string OutputDirWebGL = "Build/WebGL";
        private const string OutputDirWin = "Build/Windows";
        private const string PortableExeName = "6IL_v0.2.0_portable.exe";

        [MenuItem("IL6/Build/WebGL")]
        public static void BuildWebGL()
        {
            string outDir = Path.GetFullPath(OutputDirWebGL);
            Directory.CreateDirectory(outDir);

            var scenes = GetScenePaths();
            if (scenes.Length == 0)
            {
                Debug.LogError("[BuildScript] No enabled scenes in Build Settings.");
                EditorApplication.Exit(2);
                return;
            }

            // WebGL 옵션 — 가급적 빠른 부팅 + 호환성
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled; // 로컬 정적 서버 호환
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
            PlayerSettings.runInBackground = true;

            var opts = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outDir,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None,
            };

            Debug.Log($"[BuildScript] Building WebGL → {outDir}  with {scenes.Length} scene(s)");
            var report = BuildPipeline.BuildPlayer(opts);
            int code = report.summary.totalErrors == 0 ? 0 : 1;
            Debug.Log($"[BuildScript] WebGL build complete. errors={report.summary.totalErrors}  result={report.summary.result}");
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        [MenuItem("IL6/Build/Windows Standalone")]
        public static void BuildWindows()
        {
            string outDir = Path.GetFullPath(OutputDirWin);
            Directory.CreateDirectory(outDir);
            string exe = Path.Combine(outDir, "IL6.exe");

            var scenes = GetScenePaths();
            if (scenes.Length == 0)
            {
                Debug.LogError("[BuildScript] No enabled scenes.");
                EditorApplication.Exit(2);
                return;
            }

            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);

            var opts = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = exe,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };

            Debug.Log($"[BuildScript] Building Windows → {exe}");
            var report = BuildPipeline.BuildPlayer(opts);
            int code = report.summary.totalErrors == 0 ? 0 : 1;
            Debug.Log($"[BuildScript] Windows build complete. errors={report.summary.totalErrors}");

            if (report.summary.totalErrors == 0)
            {
                File.SetLastWriteTimeUtc(exe, DateTime.UtcNow);

                // 프로젝트 루트에 portable exe 복사
                string repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string dest = Path.Combine(repoRoot, PortableExeName);

                // 이전 portable exe 삭제
                foreach (var old in Directory.GetFiles(repoRoot, "InisLand_v*_portable.exe"))
                    File.Delete(old);
                foreach (var old in Directory.GetFiles(repoRoot, "6IL_v*_portable.exe"))
                    File.Delete(old);
                foreach (var old in Directory.GetDirectories(repoRoot, "6IL_v*_portable_Data"))
                    Directory.Delete(old, recursive: true);

                File.Copy(exe, dest, overwrite: true);
                File.SetLastWriteTimeUtc(dest, DateTime.UtcNow);
                CopyRuntimeFile(outDir, repoRoot, "UnityPlayer.dll");
                CopyRuntimeFile(outDir, repoRoot, "UnityCrashHandler64.exe");
                CopyRuntimeDirectory(Path.Combine(outDir, "MonoBleedingEdge"), Path.Combine(repoRoot, "MonoBleedingEdge"));
                CopyRuntimeDirectory(Path.Combine(outDir, "IL6_Data"), Path.Combine(repoRoot, Path.GetFileNameWithoutExtension(PortableExeName) + "_Data"));
                Debug.Log($"[BuildScript] Portable exe → {dest}");
            }

            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        private static string[] GetScenePaths()
        {
            // EditorBuildSettings 의 활성 씬, 없으면 모든 .unity 파일을 fallback
            var enabled = new System.Collections.Generic.List<string>();
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (s.enabled && !string.IsNullOrEmpty(s.path)) enabled.Add(s.path);
            }
            if (enabled.Count > 0) return enabled.ToArray();

            var fallback = new System.Collections.Generic.List<string>();
            string[] guids = AssetDatabase.FindAssets("t:Scene");
            foreach (var g in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (p.StartsWith("Assets/")) fallback.Add(p);
            }
            return fallback.ToArray();
        }

        private static void CopyRuntimeFile(string sourceDir, string destDir, string fileName)
        {
            string source = Path.Combine(sourceDir, fileName);
            if (!File.Exists(source)) return;

            string dest = Path.Combine(destDir, fileName);
            File.Copy(source, dest, overwrite: true);
            File.SetLastWriteTimeUtc(dest, DateTime.UtcNow);
        }

        private static void CopyRuntimeDirectory(string sourceDir, string destDir)
        {
            if (!Directory.Exists(sourceDir)) return;
            if (Directory.Exists(destDir)) Directory.Delete(destDir, recursive: true);

            Directory.CreateDirectory(destDir);
            foreach (string dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                string relative = MakeRelativePath(sourceDir, dir);
                Directory.CreateDirectory(Path.Combine(destDir, relative));
            }

            foreach (string file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                string relative = MakeRelativePath(sourceDir, file);
                string dest = Path.Combine(destDir, relative);
                string parent = Path.GetDirectoryName(dest);
                if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
                File.Copy(file, dest, overwrite: true);
            }
        }

        private static string MakeRelativePath(string root, string path)
        {
            string normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string normalizedPath = Path.GetFullPath(path);
            return normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase)
                ? normalizedPath.Substring(normalizedRoot.Length)
                : Path.GetFileName(path);
        }
    }
}
