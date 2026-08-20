using UnityEngine;

/// <summary>
/// Khu chiếm đóng - vùng người chơi phải đứng vào để thắng round.
///
/// ĐẶT MỘT OBJECT RỖNG VÀO GIỮA MAP RỒI GẮN SCRIPT NÀY LÊN.
///
/// CỐ Ý LÀ MONOBEHAVIOUR THUẦN, KHÔNG PHẢI NetworkBehaviour.
///
/// Script này không giữ trạng thái gì cả - nó chỉ trả lời câu hỏi "ai đang đứng trong khu".
/// Toàn bộ tiến độ chiếm nằm ở GameManager, vốn đã là NetworkObject sẵn.
///
/// Vì sao làm vậy: đặt một NetworkObject sẵn trong scene là thêm một chỗ có thể hỏng
/// (Fusion phải bake ID cho scene object). Tiến độ chỉ là hai con số float, nhét vào
/// GameManager rẻ hơn nhiều. Đúng nguyên tắc "chỉ đồng bộ thứ BẮT BUỘC" của project.
/// </summary>
public class ControlZone : MonoBehaviour
{
    /// <summary>Để GameManager tìm thấy mà không cần kéo thả Inspector, giống GameManager.Instance.</summary>
    public static ControlZone Instance { get; private set; }

    [Header("Kích thước vùng")]
    [Tooltip("Bán kính tính theo phương NGANG, đơn vị mét.")]
    public float radius = 7f;

    [Tooltip("Chiều cao vùng tính từ tâm lên và xuống. Cần vì khu chiếm ở trên cao, " +
             "người đứng dưới chân dốc không được tính là đang giữ.")]
    public float halfHeight = 3f;

    [Header("Hiển thị trong Editor")]
    public Color gizmoColor = new Color(1f, 0.85f, 0.2f, 0.25f);

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Người này có đang đứng trong khu không.
    ///
    /// Dùng hình TRỤ chứ không phải hình cầu: bán kính xét theo phương ngang, chiều cao
    /// xét riêng. Hình cầu sẽ khiến người đứng sát mép mà thấp hơn một chút bị loại oan,
    /// còn người nhảy cao ngay giữa tâm lại vẫn được tính.
    /// </summary>
    public bool Contains(Vector3 worldPosition)
    {
        Vector3 offset = worldPosition - transform.position;

        if (Mathf.Abs(offset.y) > halfHeight) return false;

        offset.y = 0f;
        return offset.sqrMagnitude <= radius * radius;
    }

    /// <summary>
    /// Đếm số người CÒN SỐNG của mỗi đội đang đứng trong khu.
    /// Người đã bị loại không giữ được khu - nếu không thì cái xác sẽ chiếm hộ.
    /// </summary>
    public void CountPlayersInside(out int redCount, out int blueCount)
    {
        redCount = 0;
        blueCount = 0;

        foreach (PlayerHealth p in PlayerHealth.AllPlayers)
        {
            if (p == null || !p.IsAlive) continue;
            if (!Contains(p.transform.position)) continue;

            if (p.Team == 0) redCount++;
            else blueCount++;
        }
    }

    // Vẽ vùng trong Scene view để căn vị trí cho dễ. Chỉ chạy trong Editor.
    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;

        // Vẽ hai vòng tròn trên dưới + đường nối, gợi ra hình trụ
        DrawCircle(transform.position + Vector3.up * halfHeight);
        DrawCircle(transform.position - Vector3.up * halfHeight);

        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
        for (int i = 0; i < 4; i++)
        {
            float a = i * Mathf.PI * 0.5f;
            Vector3 edge = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
            Gizmos.DrawLine(transform.position + edge + Vector3.up * halfHeight,
                            transform.position + edge - Vector3.up * halfHeight);
        }
    }

    private void DrawCircle(Vector3 center)
    {
        const int segments = 32;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float a = i / (float)segments * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
