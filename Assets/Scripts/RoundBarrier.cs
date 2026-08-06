using UnityEngine;

/// <summary>
/// Rào chắn giữ người chơi trong khu vực xuất phát suốt pha chuẩn bị,
/// và tự hạ xuống khi bắt đầu chiến đấu. Giống rào spawn của Valorant.
///
/// CỐ Ý là MonoBehaviour thường, KHÔNG cần NetworkObject.
///
/// Lý do: trạng thái đóng/mở của rào suy ra được hoàn toàn từ pha hiện tại của
/// GameManager, mà pha đó đã là [Networked] rồi. Mỗi máy tự đọc pha rồi tự bật/tắt
/// là khớp nhau tuyệt đối, không cần truyền thêm gì qua mạng.
///
/// Đây là cùng nguyên tắc đã dùng cho vật cầm trên tay: chỉ đồng bộ thứ BẮT BUỘC,
/// thứ nào suy ra được tại chỗ thì tính tại chỗ.
/// </summary>
public class RoundBarrier : MonoBehaviour
{
    [Tooltip("Các collider chặn đường. Để trống thì tự tìm trên object này và các object con.")]
    public Collider[] barrierColliders;

    [Tooltip("Phần nhìn thấy được của rào. Để trống thì tự tìm.")]
    public Renderer[] barrierRenderers;

    // Trạng thái đang áp dụng. Dùng để chỉ đụng vào component khi thật sự có thay đổi,
    // thay vì ghi đè mỗi khung hình cho phí.
    private bool _isBlocking;
    private bool _hasAppliedOnce;

    void Awake()
    {
        if (barrierColliders == null || barrierColliders.Length == 0)
        {
            barrierColliders = GetComponentsInChildren<Collider>();
        }

        if (barrierRenderers == null || barrierRenderers.Length == 0)
        {
            barrierRenderers = GetComponentsInChildren<Renderer>();
        }
    }

    void Update()
    {
        bool shouldBlock = ShouldBlock();

        if (_hasAppliedOnce && shouldBlock == _isBlocking) return;

        _isBlocking = shouldBlock;
        _hasAppliedOnce = true;
        ApplyState(shouldBlock);
    }

    private bool ShouldBlock()
    {
        GameManager gm = GameManager.Instance;

        // Chưa có GameManager - ví dụ khi bạn bấm Play thẳng vào TestScene để test nhanh.
        // Lúc đó MỞ rào, để không bị nhốt trong góc mà không hiểu vì sao.
        if (gm == null) return false;

        return gm.Phase == GameManager.GamePhase.WaitingToStart
            || gm.Phase == GameManager.GamePhase.BuyPhase;
    }

    private void ApplyState(bool blocking)
    {
        foreach (Collider c in barrierColliders)
        {
            if (c != null) c.enabled = blocking;
        }

        foreach (Renderer r in barrierRenderers)
        {
            if (r != null) r.enabled = blocking;
        }

        if (!blocking)
        {
            Debug.Log("<color=lime>[RÀO] Rào chắn đã hạ - xông lên!</color>");
        }
    }
}
