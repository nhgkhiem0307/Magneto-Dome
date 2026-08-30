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
        public Image slotImage;          // nền ô, dùng để đổi tông màu
        public Image iconImage;          // icon lấy từ ItemData
        public TMP_Text countText;       // số lượng còn lại
    }

    [Header("UI")]
    [Tooltip("Object gốc của menu vòng tròn. Sẽ tự bật/tắt.")]
    public GameObject radialMenuUI;

    public List<RadialSlotUI> slots = new List<RadialSlotUI>();

    [Header("Nguồn icon")]
    [Tooltip("Kéo 4 ItemData của các món tiêu hao vào đây để lấy icon. Thứ tự không quan trọng.")]
    public List<ItemData> itemDataSource = new List<ItemData>();

    [Header("Hiệu ứng thị giác")]
    public float hoverScale = 1.25f;
    public float scaleSpeed = 15f;
    public float dimmedAlpha = 0.4f;
    public float normalAlpha = 0.85f;

    [Header("Tông màu")]
    public Color normalColor = Color.white;
    public Color dimmedColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    [Header("Phím tắt")]
    public KeyCode menuKey = KeyCode.Tab;

    [Tooltip("Chuột phải rê ra xa tâm quá bấy nhiêu pixel mới tính là đã chọn một ô.")]
    public float deadZoneRadius = 40f;

    private bool _isMenuOpen;
    private int _selectedIndex = -1;

    void Start()
    {
        if (radialMenuUI != null) radialMenuUI.SetActive(false);
        ApplyIcons();
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

            // 1. Độ mờ
            if (slot.canvasGroup != null)
            {
                float targetAlpha = isAvailable ? (isHovered ? 1f : normalAlpha) : dimmedAlpha;
                slot.canvasGroup.alpha = Mathf.Lerp(slot.canvasGroup.alpha, targetAlpha, Time.deltaTime * scaleSpeed);
            }

            // 2. Tông màu - hết hàng thì xám đi
            if (slot.slotImage != null)
            {
                Color targetColor = isAvailable ? normalColor : dimmedColor;
                slot.slotImage.color = Color.Lerp(slot.slotImage.color, targetColor, Time.deltaTime * scaleSpeed);
            }

            // 3. Phóng to ô đang chọn
            Vector3 targetScale = isHovered ? Vector3.one * hoverScale : Vector3.one;
            slot.slotRect.localScale = Vector3.Lerp(slot.slotRect.localScale, targetScale, Time.deltaTime * scaleSpeed);
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
