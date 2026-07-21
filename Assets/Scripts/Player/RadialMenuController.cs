using UnityEngine;

public class RadialMenuController : MonoBehaviour
{
    [Header("UI Settings")]
    public GameObject radialMenuUI; 
    
    [Header("Keybind Settings")]
    [Tooltip("Phím bấm để mở vòng tròn chọn nhanh (Mặc định là Tab)")]
    public KeyCode menuKey = KeyCode.Tab;

    private bool isMenuOpen = false;
    private int selectedItemIndex = -1; // -1 nghĩa là chưa trỏ vào ô nào

    void Start()
    {
        // Đảm bảo UI được ẩn đi khi bắt đầu game
        if (radialMenuUI != null) 
            radialMenuUI.SetActive(false);
    }

    void Update()
    {
        // 1. Khi nhấn GIỮ phím Tab
        if (Input.GetKeyDown(menuKey))
        {
            OpenMenu();
        }

        // 2. Khi THẢ phím Tab ra
        if (Input.GetKeyUp(menuKey))
        {
            CloseMenuAndUseItem();
        }

        // 3. Tính toán vị trí chuột để chọn vật phẩm khi Menu đang mở
        if (isMenuOpen)
        {
            CalculateSelectedSector();
        }
    }

    void OpenMenu()
    {
        isMenuOpen = true;
        if (radialMenuUI != null) radialMenuUI.SetActive(true);

        // Giải phóng con trỏ chuột để người chơi xoay chọn vật phẩm
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void CloseMenuAndUseItem()
    {
        isMenuOpen = false;
        if (radialMenuUI != null) radialMenuUI.SetActive(false);

        // Khóa con trỏ chuột lại giữa màn hình cho chế độ góc nhìn thứ nhất (FPS)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Nếu người chơi trỏ chuột vào một ô hợp lệ thì kích hoạt sử dụng vật phẩm đó
        if (selectedItemIndex != -1)
        {
            UseItemFromRadialMenu(selectedItemIndex);
        }
    }

    void CalculateSelectedSector()
    {
        // Lấy tọa độ chuột tương đối so với tâm màn hình
        Vector2 mousePos = new Vector2(Input.mousePosition.x - (Screen.width / 2f), Input.mousePosition.y - (Screen.height / 2f));
        
        // Chuột phải di chuyển ra xa tâm một chút (khoảng cách > 40 pixel) thì mới tính là đang chọn ô
        if (mousePos.magnitude > 40f) 
        {
            // Tính góc xoay từ 0 đến 360 độ dựa trên vị trí chuột
            float angle = Mathf.Atan2(mousePos.y, mousePos.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            // Giả định vòng tròn chia đều làm 4 Sector (ô vật phẩm), mỗi ô rộng 90 độ
            // Ô 0: góc 0 -> 90 | Ô 1: góc 90 -> 180 | Ô 2: góc 180 -> 270 | Ô 3: góc 270 -> 360
            selectedItemIndex = Mathf.FloorToInt(angle / 90f); 
        }
        else
        {
            selectedItemIndex = -1; // Chuột quá sát tâm màn hình
        }
    }

    void UseItemFromRadialMenu(int index)
    {
        // Xử lý logic dùng vật phẩm tương ứng tại đây
        // Ví dụ: 
        // index 0 -> Uống nước tăng lực (Tăng tốc độ di chuyển trong FPSMovement)
        // index 1 -> Sử dụng bình hồi máu (Tăng máu trong PlayerHealth)
        
        Debug.Log($"<color=cyan>Đang kích hoạt sử dụng vật phẩm hỗ trợ tại Ô số: {index}</color>");
    }
}