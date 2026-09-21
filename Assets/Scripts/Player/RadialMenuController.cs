using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menu vòng tròn chọn nhanh vật phẩm tiêu hao. Giữ Tab để mở, rê chuột chọn, thả phím để dùng.
///
/// ĐẶT TRÊN CANVAS trong scene gameplay, KHÔNG đặt trên prefab nhân vật.
/// (Bản cũ đặt trên nhân vật, nhưng prefab không tham chiếu được tới object trong scene
/// nên mọi ô UI đều rỗng và menu chưa từng chạy được.)
///
/// Đây là MonoBehaviour thường: việc chọn món bằng góc chuột hoàn toàn cục bộ.
/// Chỉ khi thả phím mới gửi một yêu cầu lên Host qua InventorySystem.
///
/// Theo thiết kế: KHÔNG làm chậm thời gian, đòi hỏi phản xạ người chơi.
/// </summary>
public class RadialMenuController : MonoBehaviour
{
    [System.Serializable]
    public class RadialSlotUI
    {
        [Tooltip("Ô này tương ứng với món tiêu hao nào.")]
        public ConsumableType consumableType;

        public RectTransform slotRect;   // để phóng to khi rê chuột tới
        public CanvasGroup canvasGroup;  // để làm mờ khi hết hàng
        public Image slotImage;          // nền ô (wedge), dùng để ĐỔI SPRITE theo trạng thái
        public Image iconImage;          // icon lấy từ ItemData
        public TMP_Text countText;       // số lượng còn lại

        [Header("Sprite Swap cho nền ô")]
        [Tooltip("Trạng thái bình thường - còn hàng, không rê chuột tới. VD: wedge_topleft_normal")]
        public Sprite bgNormal;

        [Tooltip("Đang rê chuột tới VÀ còn hàng. VD: wedge_topleft_selected")]
        public Sprite bgSelected;

        [Tooltip("Đã hết hàng. VD: wedge_topleft_empty")]
        public Sprite bgEmpty;
    }

    [Header("UI")]
    [Tooltip("Object gốc của menu vòng tròn. Sẽ tự bật/tắt.")]
    public GameObject radialMenuUI;

    public List<RadialSlotUI> slots = new List<RadialSlotUI>();

    [Header("Nguồn icon")]
    [Tooltip("Kéo 4 ItemData của các món tiêu hao vào đây để lấy icon. Thứ tự không quan trọng.")]
    public List<ItemData> itemDataSource = new List<ItemData>();

    [Header("Hub ở giữa vòng tròn")]
    [Tooltip("Text tên món đang rê chuột tới. Đặt bên trong hub ở giữa radial menu.\n" +
             "Không rê vào ô nào thì tự để trống.")]
    public TMP_Text hubNameText;

    [Tooltip("Text số lượng, ghi dạng 'x2'. Hết hàng thì tự để TRỐNG (không ghi 'x0').")]
    public TMP_Text hubCountText;

    [Tooltip("Màu chữ khi món đó CÒN dùng được. Mặc định #2BE8FF (xanh lơ phát sáng).")]
    public Color hubAvailableColor = new Color(0.1686f, 0.9098f, 1f, 1f);

    [Tooltip("Độ mờ của tên món khi ĐÃ HẾT hàng. Lúc đó chữ giữ đúng màu bạn đặt sẵn " +
             "trong Inspector của ô text, chỉ bị mờ bớt đi.")]
    [Range(0f, 1f)]
    public float hubEmptyAlpha = 0.45f;

    [Header("Hiệu ứng thị giác")]
    public float hoverScale = 1.25f;
    public float scaleSpeed = 15f;

    [Tooltip("Độ mờ của ICON khi hết hàng. (Nền ô không dùng độ mờ nữa - nó đã có sprite " +
             "riêng cho trạng thái hết hàng.)")]
    public float dimmedAlpha = 0.4f;

    [Tooltip("Độ mờ của cả ô khi KHÔNG rê chuột tới.")]
    public float normalAlpha = 0.85f;

    [Header("Tông màu ICON")]
    public Color normalColor = Color.white;
    public Color dimmedColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    [Header("Phím tắt")]
    public KeyCode menuKey = KeyCode.Tab;

    [Tooltip("Chuột phải rê ra xa tâm quá bấy nhiêu pixel mới tính là đã chọn một ô.")]
    public float deadZoneRadius = 40f;

    private bool _isMenuOpen;
    private int _selectedIndex = -1;

    // Màu gốc của ô tên món, chụp lại lúc khởi động.
    //
    // Phải lưu lại vì mỗi khung hình menu mở là màu chữ bị ghi đè (sáng lên khi còn hàng),
    // nên đọc hubNameText.color lúc đó chỉ ra màu của khung hình trước, không còn là
    // "màu mặc định" bạn đặt trong Inspector nữa.
    private Color _hubNameBaseColor = Color.white;

    void Start()
    {
        if (radialMenuUI != null) radialMenuUI.SetActive(false);
        if (hubNameText != null) _hubNameBaseColor = hubNameText.color;
        ApplyIcons();
    }

    // Settings mở ra thì Radial Menu tự đóng - chỉ một bảng trên màn hình tại một thời điểm.
    void OnEnable() { SettingsUI.Opened += OnSettingsOpened; }
    void OnDisable() { SettingsUI.Opened -= OnSettingsOpened; }

    private void OnSettingsOpened()
    {
        // Truyền null = HUỶ, không dùng món nào. Người chơi bấm Esc là muốn thoát ra,
        // chứ không phải xác nhận món đang rê chuột tới - dùng mất một bình thuốc chỉ vì
        // bấm Esc thì rất ức chế.
        //
        // Sau đó thả Tab ra cũng không sao: nhánh GetKeyUp bên dưới chỉ chạy khi menu
        // còn mở, mà menu đã đóng rồi.
        if (_isMenuOpen) CloseMenu(null);
    }

    void Update()
    {
        InventorySystem inventory = GetLocalInventory();

        // Chưa vào trận thì đóng menu lại nếu đang mở
        if (inventory == null)
        {
            if (_isMenuOpen) CloseMenu(null);
            return;
        }

        if (Input.GetKeyDown(menuKey)) TryOpenMenu();
        if (Input.GetKeyUp(menuKey) && _isMenuOpen) CloseMenu(inventory);

        if (_isMenuOpen)
        {
            CalculateSelectedSector();
            UpdateSlotsVisuals(inventory);
        }
    }

    private void TryOpenMenu()
    {
        // Đang mở giao diện khác (ví dụ Shop) thì không chồng menu lên nhau.
        // Con trỏ đang tự do nghĩa là có UI nào đó đang chiếm chuột rồi.
        if (NetworkRunnerHandler.IsCursorFree()) return;

        _isMenuOpen = true;
        _selectedIndex = -1;

        if (radialMenuUI != null) radialMenuUI.SetActive(true);

        // Thả chuột ra để rê chọn. Việc này cũng tự động khoá xoay camera và chặn
        // hai nút chuột không cho thành lệnh bắn - xem NetworkRunnerHandler.IsCursorFree().
        CursorLock.Request(this);
    }

    private void CloseMenu(InventorySystem inventory)
    {
        _isMenuOpen = false;
        if (radialMenuUI != null) radialMenuUI.SetActive(false);

        CursorLock.Release(this);

        if (inventory == null) return;
        if (_selectedIndex < 0 || _selectedIndex >= slots.Count) return;

        RadialSlotUI slot = slots[_selectedIndex];

        // Hết hàng thì thôi, khỏi làm phiền Host
        if (inventory.GetConsumableCount(slot.consumableType) <= 0) return;

        // Gửi yêu cầu lên Host. Host mới là bên trừ số lượng và áp dụng hiệu ứng.
        inventory.RequestUseConsumable(slot.consumableType);
    }

    // Xác định đang rê chuột vào múi nào của vòng tròn
    private void CalculateSelectedSector()
    {
        Vector2 mouseFromCenter = new Vector2(
            Input.mousePosition.x - (Screen.width / 2f),
            Input.mousePosition.y - (Screen.height / 2f));

        // Còn quanh quẩn giữa tâm thì coi như chưa chọn gì, để người chơi huỷ được
        if (mouseFromCenter.magnitude <= deadZoneRadius || slots.Count == 0)
        {
            _selectedIndex = -1;
            return;
        }

        float angle = Mathf.Atan2(mouseFromCenter.y, mouseFromCenter.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        float sectorAngle = 360f / slots.Count;
        _selectedIndex = Mathf.FloorToInt(angle / sectorAngle);
    }

    private void UpdateSlotsVisuals(InventorySystem inventory)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            RadialSlotUI slot = slots[i];
            if (slot.slotRect == null) continue;

            // Số lượng lấy THẲNG từ túi đồ, không còn là con số nhập tay ở Inspector như bản cũ
            int quantity = inventory.GetConsumableCount(slot.consumableType);

            bool isAvailable = quantity > 0;
            bool isHovered = (i == _selectedIndex) && isAvailable;

            if (slot.countText != null) slot.countText.text = quantity.ToString();

            // 1. Độ mờ CẢ Ô - giờ chỉ để làm nổi ô đang rê chuột tới.
            //
            // Trước đây hết hàng thì dìm alpha cả ô xuống dimmedAlpha. Không dùng cách đó
            // nữa vì giờ mỗi ô đã có hẳn một sprite riêng cho trạng thái hết hàng
            // (wedge_..._empty): dìm mờ cả ô thì chính cái ảnh vừa đổi sang cũng mờ theo,
            // công vẽ ảnh riêng thành ra phí. Việc "báo cho người chơi biết đã hết" giờ do
            // sprite nền + icon bị dìm màu đảm nhiệm.
            if (slot.canvasGroup != null)
            {
                float targetAlpha = isHovered ? 1f : normalAlpha;
                slot.canvasGroup.alpha = Mathf.Lerp(slot.canvasGroup.alpha, targetAlpha, Time.deltaTime * scaleSpeed);
            }

            // 2. Nền ô: ĐỔI SPRITE theo trạng thái, không đổi màu nữa.
            //
            // Ba trạng thái: hết hàng -> bgEmpty, đang rê tới -> bgSelected, còn lại -> bgNormal.
            // Chỉ gán khi sprite thực sự khác: gán lại mỗi khung hình sẽ bắt Unity dựng lại
            // toàn bộ lưới của Canvas, phí công vô ích ở một UI mỗi giây đổi vài lần.
            if (slot.slotImage != null)
            {
                Sprite targetSprite = isAvailable
                    ? (isHovered ? slot.bgSelected : slot.bgNormal)
                    : slot.bgEmpty;

                if (targetSprite != null && slot.slotImage.sprite != targetSprite)
                {
                    slot.slotImage.sprite = targetSprite;
                }
            }

            // 3. Icon: dìm màu khi hết hàng.
            //
            // Phần dìm màu chuyển từ NỀN sang ICON - nền đã có sprite riêng lo việc đó rồi,
            // còn icon thì chỉ có một ảnh duy nhất nên vẫn cần dìm bằng màu.
            if (slot.iconImage != null)
            {
                Color targetColor = isAvailable
                    ? normalColor
                    : new Color(dimmedColor.r, dimmedColor.g, dimmedColor.b, dimmedAlpha);

                slot.iconImage.color = Color.Lerp(slot.iconImage.color, targetColor, Time.deltaTime * scaleSpeed);
            }

            // 4. Phóng to ô đang chọn
            Vector3 targetScale = isHovered ? Vector3.one * hoverScale : Vector3.one;
            slot.slotRect.localScale = Vector3.Lerp(slot.slotRect.localScale, targetScale, Time.deltaTime * scaleSpeed);
        }

        UpdateHub(inventory);
    }

    /// <summary>
    /// Cập nhật hub ở giữa vòng tròn: tên món và số lượng của ô ĐANG RÊ CHUỘT TỚI.
    ///
    /// Lưu ý khác với các ô xung quanh: hub bám theo _selectedIndex thô, KHÔNG xét còn hàng
    /// hay không. Rê vào một món đã hết thì vẫn phải đọc được tên nó - nếu để trống thì
    /// người chơi tưởng chuột chưa trỏ trúng ô nào.
    /// </summary>
    private void UpdateHub(InventorySystem inventory)
    {
        if (hubNameText == null && hubCountText == null) return;

        // Đang quanh quẩn giữa tâm (vùng huỷ chọn) -> hub để trống
        if (_selectedIndex < 0 || _selectedIndex >= slots.Count)
        {
            if (hubNameText != null) hubNameText.text = "";
            if (hubCountText != null) hubCountText.text = "";
            return;
        }

        RadialSlotUI slot = slots[_selectedIndex];
        int quantity = inventory.GetConsumableCount(slot.consumableType);
        bool isAvailable = quantity > 0;

        if (hubNameText != null)
        {
            ItemData data = FindItemData(slot.consumableType);
            hubNameText.text = (data != null && !string.IsNullOrEmpty(data.itemName))
                ? data.itemName
                : slot.consumableType.ToString(); // chưa gán ItemData thì đỡ bằng tên enum

            // Còn dùng được -> màu sáng #2BE8FF. Hết -> giữ nguyên màu gốc bạn đặt trong
            // Inspector, chỉ mờ bớt đi (_hubNameBaseColor lưu lại từ lúc Start).
            hubNameText.color = isAvailable
                ? hubAvailableColor
                : new Color(_hubNameBaseColor.r, _hubNameBaseColor.g, _hubNameBaseColor.b, hubEmptyAlpha);
        }

        if (hubCountText != null)
        {
            // Hết hàng thì KHÔNG ghi gì. Ghi "x0" vừa thừa vừa dễ bị liếc nhầm thành "còn 0
            // nhưng vẫn bấm được".
            hubCountText.text = isAvailable ? $"x{quantity}" : "";
            hubCountText.color = hubAvailableColor;
        }
    }

    // Lấy icon từ ItemData, để chỉ phải gán ảnh một lần duy nhất và dùng chung với Shop
    private void ApplyIcons()
    {
        foreach (RadialSlotUI slot in slots)
        {
            if (slot.iconImage == null) continue;

            ItemData data = FindItemData(slot.consumableType);
            if (data == null || data.itemIcon == null)
            {
                slot.iconImage.enabled = false;
                continue;
            }

            slot.iconImage.sprite = data.itemIcon;
            slot.iconImage.enabled = true;
        }
    }

    private ItemData FindItemData(ConsumableType type)
    {
        foreach (ItemData data in itemDataSource)
        {
            if (data != null
                && data.itemType == ItemData.ItemType.Consumable
                && data.consumableType == type)
            {
                return data;
            }
        }
        return null;
    }

    // Túi đồ của chính nhân vật mình
    private InventorySystem GetLocalInventory()
    {
        if (FPSMovement.Local == null) return null;
        return FPSMovement.Local.GetComponent<InventorySystem>();
    }
}
