using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tự gắn tiếng click vào MỌI nút trong scene, khỏi phải kéo thả từng nút một.
///
/// ĐẶT MỘT CÁI LÊN CANVAS của mỗi scene có giao diện (MenuScene và TestScene).
///
/// Vì sao cần: gán tay ở ô On Click() của từng Button thì phải nhớ làm cho nút mới,
/// và nút được sinh ra lúc chạy (danh sách phòng chẳng hạn) thì không gán trước được.
/// Script này quét lại định kỳ nên nút sinh sau cũng có tiếng.
/// </summary>
public class ButtonClickSound : MonoBehaviour
{
    [Header("Phạm vi")]
    [Tooltip("Bật: quét toàn bộ scene. Tắt: chỉ quét các object con của object này.")]
    public bool scanWholeScene = true;

    [Tooltip("Gắn tiếng cho cả Toggle (ô tích) nữa, không riêng Button.")]
    public bool includeToggles = true;

    [Header("Quét lại")]
    [Tooltip("Quét lại định kỳ để bắt những nút được sinh ra lúc chạy - ví dụ từng dòng " +
             "trong danh sách phòng. Tắt nếu scene không có nút động nào.")]
    public bool rescanPeriodically = true;

    [Tooltip("Bao nhiêu giây quét lại một lần.\n\n" +
             "⚠️ ĐỪNG TIN LỜI CŨ 'quét vài chục nút là việc rất nhẹ'. Cái đắt KHÔNG PHẢI " +
             "số nút, mà là FindObjectsByType phải DUYỆT TOÀN BỘ SCENE (kể cả object đang " +
             "tắt) để tìm ra chúng. Trong scene gameplay có cả bản đồ thì mỗi lần quét là " +
             "một cú khựng hình - cứ 1 giây giật một cái, rất giống lỗi mạng.\n\n" +
             "Chỉ bật Rescan ở scene có nút SINH RA LÚC CHẠY (danh sách phòng ở MenuScene). " +
             "TestScene không có nút nào như vậy: Shop, Radial, Settings đều tồn tại sẵn " +
             "từ lúc mở scene và được lần quét đầu ở Start() bắt hết rồi.")]
    public float rescanInterval = 1f;

    // Những thứ đã gắn rồi, để không gắn chồng hai ba lần -> một cú bấm kêu mấy tiếng.
    private readonly HashSet<Object> _hooked = new HashSet<Object>();

    private float _nextScanTime;

    // Lần quét đầu tiên đã chạy chưa. Chỉ lần đó mới được duyệt toàn scene — xem Scan().
    private bool _didFirstScan;

    private void Start()
    {
        Scan();
    }

    private void Update()
    {
        if (!rescanPeriodically) return;
        if (Time.unscaledTime < _nextScanTime) return;

        _nextScanTime = Time.unscaledTime + Mathf.Max(0.1f, rescanInterval);
        Scan();
    }

    /// <summary>
    /// Quét và gắn tiếng cho những nút chưa được gắn.
    /// Gọi tay được, ví dụ ngay sau khi vừa dựng xong danh sách phòng.
    /// </summary>
    public void Scan()
    {
        // Dọn những thứ đã bị huỷ, nếu không HashSet phình mãi qua các lần đổi scene
        _hooked.RemoveWhere(o => o == null);

        // CHỈ LẦN QUÉT ĐẦU MỚI DUYỆT TOÀN SCENE. Sửa 01/09 - đây là lỗi hiệu năng thật.
        //
        // FindObjectsByType với FindObjectsInactive.Include phải đi qua MỌI object trong
        // scene, kể cả đang tắt. TestScene có hơn 4000 vật trang trí, mà lại có TỚI HAI
        // component này cùng chạy - thành 4 lượt duyệt toàn scene mỗi giây. Đủ để hạ FPS
        // trung bình và gây giật đều đặn mỗi giây một cái, rất giống lag mạng.
        //
        // Vì sao chuyển sang quét cây con vẫn ĐỦ: thứ duy nhất cần quét lại định kỳ là nút
        // SINH RA LÚC CHẠY - tức mấy dòng danh sách phòng ở MenuScene. Chúng được tạo ra
        // dưới Canvas, mà script này nằm trên chính Canvas đó. Nút nằm sẵn trong scene từ
        // đầu (Shop, Radial, Settings) thì lần quét đầu ở Start() đã bắt hết rồi.
        bool deepScan = scanWholeScene && !_didFirstScan;
        _didFirstScan = true;

        Button[] buttons = deepScan
            ? FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            : GetComponentsInChildren<Button>(true);

        foreach (Button b in buttons)
        {
            if (b == null || _hooked.Contains(b)) continue;

            _hooked.Add(b);

            // AddListener CỘNG THÊM chứ không ghi đè, nên mọi hành động đã gán sẵn
            // ở ô On Click() trong Inspector vẫn chạy bình thường.
            b.onClick.AddListener(AudioManager.ButtonClick);
        }

        if (!includeToggles) return;

        Toggle[] toggles = deepScan
            ? FindObjectsByType<Toggle>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            : GetComponentsInChildren<Toggle>(true);

        foreach (Toggle t in toggles)
        {
            if (t == null || _hooked.Contains(t)) continue;

            _hooked.Add(t);

            // onValueChanged truyền vào một bool, mà ButtonClick() không nhận tham số,
            // nên phải bọc qua lambda bỏ giá trị đó đi.
            t.onValueChanged.AddListener(_ => AudioManager.ButtonClick());
        }
    }
}
