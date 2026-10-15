using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Build BẢN NỘP chính thức bằng một lệnh — không phải nhớ tick/bỏ tick gì.
///
/// VÌ SAO CẦN: bản nộp khác bản test ở mấy ô dễ quên (Development Build, icon). Quên bỏ tick
/// Development Build là người chấm bấm nhầm F12 sẽ kết thúc trận ngay. Gom vào một hàm thì
/// build bao nhiêu lần cũng ra đúng một kiểu.
///
/// CÁCH DÙNG (chọn một):
///  - Trong Unity: menu  Magneto-Dome → Build bản nộp
///  - Không mở Unity: chạy Unity.exe -batchmode -quit -projectPath "..." -executeMethod BuildNop.Build
///
/// Nằm trong thư mục Editor nên KHÔNG bị đóng gói vào game.
/// </summary>
public static class BuildNop
{
    private const string IconPath = "Assets/Textures/GameIcon.png";

    /// <summary>Thư mục ra, tính từ thư mục project. Nằm NGOÀI project để Unity không import ngược lại.</summary>
    private const string OutputDir = "../Build_Nop";

    [MenuItem("Magneto-Dome/Build bản nộp")]
    public static void Build()
    {
        string outDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), OutputDir));
        string exePath = Path.Combine(outDir, PlayerSettings.productName + ".exe");

        // Không build đè lên thư mục đang có đồ: Unity ghi file mới nhưng KHÔNG xoá file cũ,
        // nên rác của bản trước (file .pdb gỡ lỗi...) sẽ nằm lại lẫn vào bản nộp.
        if (Directory.Exists(outDir) && Directory.EnumerateFileSystemEntries(outDir).Any())
        {
            Fail("Thư mục " + outDir + " đã có file. Đổi tên hoặc xoá nó trước rồi build lại.");
            return;
        }

        // 1. Icon game — ô "Default Icon" trong Player Settings.
        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        if (icon == null)
        {
            Fail("Không tìm thấy icon ở " + IconPath);
            return;
        }
        PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        AssetDatabase.SaveAssets();

        // 2. Tắt Development Build: phím test F9/F10/F12/K biến mất, không sinh file .pdb.
        EditorUserBuildSettings.development = false;

        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };

        Debug.Log("[BuildNop] Build " + scenes.Length + " scene ra " + exePath);
        BuildReport report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result != BuildResult.Succeeded)
        {
            Fail("Build thất bại: " + report.summary.result + ", " + report.summary.totalErrors + " lỗi");
            return;
        }

        Debug.Log("[BuildNop] XONG — " + (report.summary.totalSize / (1024 * 1024)) + " MB, mất "
                  + report.summary.totalTime.TotalSeconds.ToString("F0") + " giây");
    }

    private static void Fail(string message)
    {
        Debug.LogError("[BuildNop] " + message);

        // Chạy không giao diện thì phải trả mã lỗi, không thì dòng lệnh tưởng là thành công.
        if (Application.isBatchMode) EditorApplication.Exit(1);
    }
}
