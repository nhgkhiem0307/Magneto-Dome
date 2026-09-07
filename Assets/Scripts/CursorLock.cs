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
    /// Xoá sạch mọi đăng ký VÀ thả chuột ra. Gọi khi đổi scene hoặc rời trận —
    /// lúc đó mọi bảng giao diện đều đã bị huỷ theo scene cũ.
    ///
    /// ⚠️ CỐ Ý KHÔNG GỌI Apply(). Đây là lỗi đã sửa ngày 01/09 và rất dễ vô tình khôi phục.
    ///
    /// Apply() hỏi "còn nhân vật không?" để quyết định. Nhưng lúc ReturnToMenu() gọi hàm
    /// này thì nhân vật VẪN CÒN SỐNG - nó chỉ bị huỷ vài khung hình sau, khi scene thật sự
    /// đổi. Nên Apply() thấy inMatch = true, mà danh sách đăng ký thì vừa bị xoá sạch,
    /// và nó kết luận: KHOÁ CHUỘT LẠI.
    ///
    /// Kết quả là một hàm tên "thả hết" lại đi khoá chuột, rồi MenuScene load xong mà
    /// không ai tính lại nữa - người chơi vào menu với con trỏ bị khoá, không bấm được nút
    /// nào. Bấm Escape thì mở Settings, Settings đăng ký xin chuột, và chuột được thả -
    /// đó là lý do Escape "chữa" được lỗi này.
    ///
    /// Đổi scene thì LUÔN LUÔN phải thả chuột, không có ngoại lệ nào cần hỏi thêm.
    /// </summary>
    public static void ReleaseAll()
    {
        _requesters.Clear();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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
