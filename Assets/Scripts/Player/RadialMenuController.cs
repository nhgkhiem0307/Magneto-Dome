using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RadialMenuController : MonoBehaviour
{
    [System.Serializable]
    public class RadialSlotUI
    {
        public string itemName = "Item";
        public RectTransform slotRect;     // RectTransform để xử lý Zoom phóng to
        public CanvasGroup canvasGroup;   // CanvasGroup để xử lý làm mờ (Alpha)
        public Image slotImage;           // <-- THÊM DÒNG NÀY: Để chỉnh màu nền ô UI
        public Text countText;            // UI Text hiển thị số lượng
        public int itemQuantity = 0;      // Số lượng vật phẩm
    }

    [Header("UI Settings")]
    public GameObject radialMenuUI; 
    public List<RadialSlotUI> slots = new List<RadialSlotUI>();

    [Header("Visual Feedback Settings")]
    public float hoverScale = 1.25f;      
    public float scaleSpeed = 15f;       
    public float dimmedAlpha = 0.4f;      // Độ mờ Alpha khi hết đồ
    public float normalAlpha = 0.85f;     

    [Header("Color Tint Settings")] // <-- THÊM MỤC NÀY NỮA
    public Color normalColor = Color.white;                     // Tông màu gốc khi có đồ
    public Color dimmedColor = new Color(0.4f, 0.4f, 0.4f, 1f); // Tông màu xám tối khi hết đồ

    [Header("Keybind Settings")]
    public KeyCode menuKey = KeyCode.Tab;

    private bool isMenuOpen = false;
    private int selectedItemIndex = -1; 

    void Start()
    {
        if (radialMenuUI != null) 
            radialMenuUI.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(menuKey)) OpenMenu();
        if (Input.GetKeyUp(menuKey)) CloseMenuAndUseItem();

        if (isMenuOpen)
        {
            CalculateSelectedSector();
            UpdateSlotsVisuals();
        }
    }

    void OpenMenu()
    {
        isMenuOpen = true;
        if (radialMenuUI != null) radialMenuUI.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void CloseMenuAndUseItem()
    {
        isMenuOpen = false;
        if (radialMenuUI != null) radialMenuUI.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (selectedItemIndex != -1 && selectedItemIndex < slots.Count)
        {
            if (slots[selectedItemIndex].itemQuantity > 0)
            {
                UseItemFromRadialMenu(selectedItemIndex);
            }
        }
    }

    void CalculateSelectedSector()
    {
        Vector2 mousePos = new Vector2(Input.mousePosition.x - (Screen.width / 2f), Input.mousePosition.y - (Screen.height / 2f));
        
        if (mousePos.magnitude > 40f && slots.Count > 0) 
        {
            float angle = Mathf.Atan2(mousePos.y, mousePos.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            float sectorAngle = 360f / slots.Count;
            selectedItemIndex = Mathf.FloorToInt(angle / sectorAngle); 
        }
        else
        {
            selectedItemIndex = -1; 
        }
    }

    // HÀM XỬ LÝ HIỆU ỨNG THỊ GIÁC (ZOOM, ALPHA & TÔNG MÀU TỐI)
    void UpdateSlotsVisuals()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            RadialSlotUI slot = slots[i];
            if (slot.slotRect == null) continue;

            bool isAvailable = slot.itemQuantity > 0;
            bool isHovered = (i == selectedItemIndex) && isAvailable;

            // Update UI Text hiển thị số lượng
            if (slot.countText != null)
            {
                slot.countText.text = isAvailable ? slot.itemQuantity.ToString() : "0";
            }

            // 1. Xử lý Độ Mờ Trong Suốt (Alpha)
            if (slot.canvasGroup != null)
            {
                float targetAlpha = isAvailable ? (isHovered ? 1f : normalAlpha) : dimmedAlpha;
                slot.canvasGroup.alpha = Mathf.Lerp(slot.canvasGroup.alpha, targetAlpha, Time.deltaTime * scaleSpeed);
            }

            // 2. Xử lý Tông Màu Tối (Color Tint) <-- THÊM MỚI
            if (slot.slotImage != null)
            {
                Color targetColor = isAvailable ? normalColor : dimmedColor;
                slot.slotImage.color = Color.Lerp(slot.slotImage.color, targetColor, Time.deltaTime * scaleSpeed);
            }

            // 3. Xử lý Phóng To (Zoom)
            Vector3 targetScale = isHovered ? Vector3.one * hoverScale : Vector3.one;
            slot.slotRect.localScale = Vector3.Lerp(slot.slotRect.localScale, targetScale, Time.deltaTime * scaleSpeed);
        }
    }

    void UseItemFromRadialMenu(int index)
    {
        slots[index].itemQuantity--;
        Debug.Log($"<color=cyan>Đã dùng: {slots[index].itemName}</color>");
    }
}