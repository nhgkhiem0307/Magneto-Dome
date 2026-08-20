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

    [Tooltip("Bao nhiêu giây quét lại một lần. Quét vài chục nút là việc rất nhẹ, " +
             "1 giây là thừa nhanh so với tốc độ người chơi bấm.")]
    public float rescanInterval = 1f;

    // Những thứ đã gắn rồi, để không gắn chồng hai ba lần -> một cú bấm kêu mấy tiếng.
    private readonly HashSet<Object> _hooked = new HashSet<Object>();

    private float _nextScanTime;

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

        Button[] buttons = scanWholeScene
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

        Toggle[] toggles = scanWholeScene
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
