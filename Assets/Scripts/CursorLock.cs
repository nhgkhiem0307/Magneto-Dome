using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trọng tài duy nhất quyết định con trỏ chuột bị khoá hay được thả.
///
/// VÌ SAO CẦN: trước đây có BỐN hệ thống cùng tự ý ghi Cursor.lockState —
/// SettingsUI, ShopUI, RadialMenuController và FPSMovement. Mỗi cái đóng lại đều
/// khoá chuột, bất kể có bảng nào khác đang mở hay không.
///
/// Kịch bản hỏng cụ thể: giữ Tab mở Radial Menu (chuột thả) -> bấm Escape mở Settings
/// -> đóng Settings -> nó khoá chuột, trong khi Radial Menu vẫn đang mở và người chơi
/// không rê chọn được nữa.
///
/// CÁCH LÀM: ai cần chuột thì ĐĂNG KÝ, xong thì HUỶ ĐĂNG KÝ. Chuột chỉ bị khoá khi
/// không còn ai đăng ký. Nhờ vậy đóng một bảng không thể cướp chuột của bảng khác.
/// </summary>
public static class CursorLock
{
    // Dùng UnityEngine.Object chứ không phải object thường, để phép so sánh với null
    // bắt được cả trường hợp component đã bị huỷ mà chưa kịp huỷ đăng ký.
    private static readonly HashSet<Object> _requesters = new HashSet<Object>();

    /// <summary>Con trỏ có đang được thả tự do không.</summary>
    public static bool IsFree => Cursor.lockState != CursorLockMode.Locked;

    /// <summary>Xin thả chuột ra. Gọi khi mở một bảng giao diện.</summary>
    public static void Request(Object who)
    {
        if (who == null) return;

        _requesters.Add(who);
        Apply();
    }

    /// <summary>Trả chuột lại. Gọi khi đóng bảng giao diện.</summary>
    public static void Release(Object who)
    {
        if (who == null) return;

        _requesters.Remove(who);
        Apply();
    }

    /// <summary>
    /// Xoá sạch mọi đăng ký. Gọi khi đổi scene hoặc rời trận —
    /// lúc đó mọi bảng giao diện đều đã bị huỷ theo scene cũ.
    /// </summary>
    public static void ReleaseAll()
    {
        _requesters.Clear();
        Apply();
    }

    /// <summary>
    /// Tính lại trạng thái con trỏ từ danh sách đăng ký.
    ///
    /// Ngoài menu (chưa có nhân vật) thì LUÔN thả chuột, vì lúc đó cả màn hình
    /// là giao diện. Trong trận thì chỉ thả khi có ít nhất một bảng đang mở.
    /// </summary>
    private static void Apply()
    {
        // Dọn những thứ đã bị huỷ. Cần vì đổi scene sẽ huỷ các bảng giao diện mà
        // không ai kịp gọi Release, và nếu để sót thì chuột sẽ không bao giờ khoá lại.
        _requesters.RemoveWhere(o => o == null);

        bool inMatch = FPSMovement.Local != null;
        bool free = !inMatch || _requesters.Count > 0;

        Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = free;
    }

    /// <summary>
    /// Ép tính lại mà không đổi danh sách đăng ký.
    /// FPSMovement gọi lúc nhân vật vừa sinh ra, để chuyển từ trạng thái menu sang trong trận.
    /// </summary>
    public static void Refresh() => Apply();
}
