using UnityEditor;
using UnityEngine;
using System.IO;

[InitializeOnLoad]
public static class SwUtilsInstaller
{
    private const string PackageName = "com.lavinse.swutils";
    private const string InstalledVersionKey = "SwUtils_InstalledVersion";
    private const string CurrentVersion = "1.0.2"; // package.json 버전과 동일하게 유지

    private static readonly string SourcePath = $"Packages/{PackageName}/Samples";
    private static readonly string TargetPath = "Assets/SwUtils/Samples";

    static SwUtilsInstaller()
    {
        string installedVersion = SessionState.GetString(InstalledVersionKey, "");
        if (installedVersion == CurrentVersion) return;

        CopyPrefabs();
    }

    private static void CopyPrefabs()
    {
        if (!Directory.Exists(SourcePath))
        {
            Debug.LogWarning($"[SwUtils] 소스 폴더를 찾을 수 없습니다: {SourcePath}");
            return;
        }

        if (!Directory.Exists(TargetPath))
        {
            Directory.CreateDirectory(TargetPath);
        }

        string[] files = Directory.GetFiles(SourcePath, "*", SearchOption.AllDirectories);
        bool copied = false;

        foreach (string file in files)
        {
            // .meta 파일은 Unity가 자동 생성하므로 스킵
            if (file.EndsWith(".meta")) continue;

            string fileName = Path.GetFileName(file);
            string targetFile = Path.Combine(TargetPath, fileName);

            if (File.Exists(targetFile))
            {
                Debug.Log($"[SwUtils] 이미 존재하여 스킵: {fileName}");
                continue;
            }

            File.Copy(file, targetFile);
            copied = true;
            Debug.Log($"[SwUtils] 복사 완료: {fileName} → {TargetPath}");
        }

        if (copied)
        {
            AssetDatabase.Refresh();
            Debug.Log($"[SwUtils] 프리팹이 {TargetPath} 에 복사되었습니다.");
        }

        SessionState.SetString(InstalledVersionKey, CurrentVersion);
    }

    [MenuItem("Tools/SwUtils/프리팹 재설치")]
    private static void ReinstallPrefabs()
    {
        if (Directory.Exists(TargetPath))
        {
            Directory.Delete(TargetPath, true);
            File.Delete(TargetPath + ".meta");
        }

        CopyPrefabs();
        Debug.Log("[SwUtils] 프리팹 재설치 완료");
    }
}