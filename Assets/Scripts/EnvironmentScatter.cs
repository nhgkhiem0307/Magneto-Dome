using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Công cụ RẢI trang trí: cỏ, bụi, hoa, đá nhỏ lên bề mặt đảo.
///
/// CÁCH DÙNG:
///   1. Tạo Empty GameObject, đặt vào giữa đảo, gắn script này lên
///   2. Kéo các prefab cỏ/bụi vào danh sách Prefabs
///   3. Chuột phải lên tên component (trong Inspector) -> "Rải trang trí"
///   4. Không ưng thì -> "Xoá hết" rồi rải lại. Mỗi lần rải ra một kiểu khác nhau.
///
/// Chỉ chạy trong Editor. Vật rải ra được LƯU THẲNG VÀO SCENE, không sinh lúc chạy game,
/// nên không tốn hiệu năng lúc chơi và mọi máy đều thấy giống nhau mà không cần đồng bộ.
/// </summary>
public class EnvironmentScatter : MonoBehaviour
{
    [Header("Rải cái gì")]
    [Tooltip("Danh sách prefab sẽ được rải. Bỏ nhiều loại vào cho tự nhiên.")]
    public GameObject[] prefabs;

    [Tooltip("Số lượng muốn rải. Cỏ thì 300-800, bụi rậm 50-150, đá nhỏ 30-80.")]
    public int count = 400;

    [Header("Rải ở đâu")]
    [Tooltip("Bán kính vùng rải, tính từ vị trí của object này.")]
    public float radius = 30f;

    [Tooltip("Bắn tia từ độ cao này xuống để tìm mặt đất. Phải cao hơn đỉnh đảo.")]
    public float raycastHeight = 60f;

    [Tooltip("Chỉ rải lên các layer này. Bỏ chọn layer của người chơi và vật thể từ tính.")]
    public LayerMask surfaceLayers = ~0;

    [Tooltip("Dốc hơn bao nhiêu độ thì KHÔNG rải. 35 độ là hợp lý - cỏ không mọc trên vách đá.")]
    [Range(0f, 90f)]
    public float maxSlopeAngle = 35f;

    [Header("Vùng CẤM mọc")]
    [Tooltip("Những vòng tròn sẽ không có gì được rải vào. Dùng để chừa trống khu chiếm đóng, " +
             "điểm hồi sinh, lối đi chính - những nơi cần tầm nhìn thoáng để chơi.\n\n" +
             "Để trống ô Center thì lấy vị trí của chính object này làm tâm.")]
    public ExclusionZone[] exclusions;

    /// <summary>Một vòng tròn cấm rải. Bán kính tính theo phương ngang, không xét độ cao.</summary>
    [System.Serializable]
    public class ExclusionZone
    {
        [Tooltip("Tâm vùng cấm. Để trống thì dùng vị trí của object đang rải.")]
        public Transform center;

        [Tooltip("Bán kính vùng cấm, tính bằng mét.")]
        public float radius = 13f;

        [Tooltip("Ghi chú cho dễ nhớ vùng này là gì. Không ảnh hưởng gì tới việc rải.")]
        public string note = "Khu chiếm đóng";
    }

    [Header("Biến thể cho tự nhiên")]
    [Tooltip("Khoảng phóng to thu nhỏ ngẫu nhiên. (0.8, 1.3) nghĩa là từ 80% tới 130%.")]
    public Vector2 scaleRange = new Vector2(0.8f, 1.3f);

    [Tooltip("Bật: vật nghiêng theo độ dốc mặt đất. Hợp với đá. " +
             "Tắt: luôn đứng thẳng - hợp với cỏ và cây, vì chúng mọc hướng lên trời.")]
    public bool alignToSlope = false;

    [Tooltip("Ấn chìm xuống đất bao nhiêu mét, để gốc không bị hở chân lơ lửng.")]
    public float sinkIntoGround = 0.05f;

    [Header("QUAN TRỌNG")]
    [Tooltip("Xoá mọi Collider trên vật rải ra. GẦN NHƯ LUÔN PHẢI BẬT.\n\n" +
             "Cỏ có collider sẽ chặn đường đạn và làm nó lệch hướng - đúng cái lỗi vừa mất " +
             "cả buổi để sửa. Trang trí thì chỉ cần nhìn thấy, không cần chạm được.")]
    public bool stripColliders = true;

    [Tooltip("Đánh dấu Static để Unity gộp chúng lại khi dựng ánh sáng và vẽ. " +
             "Hàng trăm bụi cỏ động sẽ làm tụt khung hình.")]
    public bool markStatic = true;

    [Tooltip("TẮT đổ bóng cho vật rải ra. NÊN ĐỂ BẬT ô này.\n\n" +
             "Đổ bóng là thứ ĐẮT NHẤT: mỗi vật phải được vẽ thêm một lần nữa vào bản đồ bóng, " +
             "nên 800 bụi cỏ có bóng tốn gần gấp đôi 800 bụi cỏ không bóng.\n\n" +
             "Và bóng của cỏ lowpoly trông như nhiễu hạt lấm tấm chứ không đẹp. " +
             "Tắt đi vừa nhanh hơn vừa sạch hơn.")]
    public bool disableShadowCasting = true;

#if UNITY_EDITOR
    [ContextMenu("Rải trang trí")]
    private void Scatter()
    {
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("[RẢI] Chưa có prefab nào trong danh sách.");
            return;
        }

        int placed = 0;
        int attempts = 0;
        int maxAttempts = count * 10; // tránh lặp vô tận khi vùng rải toàn vách dốc

        float slopeDot = Mathf.Cos(maxSlopeAngle * Mathf.Deg2Rad);

        while (placed < count && attempts < maxAttempts)
        {
            attempts++;

            // Lấy điểm ngẫu nhiên trong hình tròn, rồi bắn tia thẳng xuống tìm mặt đất
            Vector2 flat = Random.insideUnitCircle * radius;
            Vector3 from = transform.position + new Vector3(flat.x, raycastHeight, flat.y);

            // Lọc vùng cấm TRƯỚC khi bắn tia - bắn tia là phần tốn nhất, bỏ sớm được thì bỏ
            if (IsInsideExclusion(from)) continue;

            if (!Physics.Raycast(from, Vector3.down, out RaycastHit hit,
                                 raycastHeight * 3f, surfaceLayers)) continue;

            // Quá dốc thì bỏ qua - cỏ không mọc trên vách đá
            if (hit.normal.y < slopeDot) continue;

            GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
            if (prefab == null) continue;

            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
            if (obj == null) continue;

            obj.transform.position = hit.point - Vector3.up * sinkIntoGround;

            // Xoay quanh trục đứng cho mỗi cái một hướng, khỏi nhìn ra là bản sao.
            Quaternion spin = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            // GIỮ NGUYÊN GÓC GỐC CỦA PREFAB, chỉ xoay THÊM lên trên nó.
            //
            // Model FBX xuất từ Blender thường mang sẵn rotation -90 độ trục X, vì Blender
            // dùng hệ Z-up còn Unity dùng Y-up. Góc đó nằm trong transform của prefab.
            // Gán thẳng rotation = spin sẽ XOÁ MẤT phần chỉnh trục đó -> model nằm ngang.
            //
            // Nhân thêm vào thì vừa giữ được hướng đúng của model, vừa xoay ngẫu nhiên được.
            Quaternion baseRotation = prefab.transform.rotation;

            obj.transform.rotation = alignToSlope
                ? Quaternion.FromToRotation(Vector3.up, hit.normal) * spin * baseRotation
                : spin * baseRotation;

            obj.transform.localScale = prefab.transform.localScale * Random.Range(scaleRange.x, scaleRange.y);

            if (stripColliders)
            {
                foreach (Collider c in obj.GetComponentsInChildren<Collider>(true))
                {
                    DestroyImmediate(c);
                }
            }

            if (disableShadowCasting)
            {
                foreach (Renderer r in obj.GetComponentsInChildren<Renderer>(true))
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }

            if (markStatic) GameObjectUtility.SetStaticEditorFlags(obj, StaticEditorFlags.BatchingStatic);

            Undo.RegisterCreatedObjectUndo(obj, "Rải trang trí");
            placed++;
        }

        Debug.Log($"<color=lime>[RẢI] Đã đặt {placed} vật trang trí (thử {attempts} lần).</color>");

        if (placed < count)
        {
            Debug.LogWarning($"[RẢI] Chỉ đặt được {placed}/{count}. " +
                             "Có thể vùng rải quá dốc, bán kính quá nhỏ, vùng cấm che gần hết, hoặc sai Surface Layers.");
        }
    }

    /// <summary>
    /// Điểm này có nằm trong vùng cấm nào không.
    ///
    /// Chỉ xét khoảng cách THEO PHƯƠNG NGANG. Vùng cấm là một cái ống đứng vô tận,
    /// không phải hình cầu - nếu xét cả độ cao thì chỗ đất trũng trong khu chiếm đóng
    /// sẽ lọt ra ngoài bán kính và vẫn mọc cỏ.
    /// </summary>
    private bool IsInsideExclusion(Vector3 worldPoint)
    {
        if (exclusions == null) return false;

        foreach (ExclusionZone zone in exclusions)
        {
            if (zone == null || zone.radius <= 0f) continue;

            Vector3 center = zone.center != null ? zone.center.position : transform.position;

            float dx = worldPoint.x - center.x;
            float dz = worldPoint.z - center.z;

            if (dx * dx + dz * dz <= zone.radius * zone.radius) return true;
        }

        return false;
    }

    [ContextMenu("Xoá hết")]
    private void ClearAll()
    {
        int n = transform.childCount;

        for (int i = n - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(transform.GetChild(i).gameObject);
        }

        Debug.Log($"<color=orange>[RẢI] Đã xoá {n} vật trang trí.</color>");
    }

    // Vẽ vùng rải (xanh lá) và các vùng cấm (đỏ) trong Scene view để căn cho dễ
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 1f, 0.4f, 0.8f);
        DrawCircle(transform.position, radius);

        if (exclusions == null) return;

        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.9f);

        foreach (ExclusionZone zone in exclusions)
        {
            if (zone == null || zone.radius <= 0f) continue;

            Vector3 center = zone.center != null ? zone.center.position : transform.position;
            DrawCircle(center, zone.radius);
        }
    }

    private void DrawCircle(Vector3 center, float r)
    {
        const int seg = 48;
        Vector3 prev = center + new Vector3(r, 0f, 0f);

        for (int i = 1; i <= seg; i++)
        {
            float a = i / (float)seg * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
#endif
}
