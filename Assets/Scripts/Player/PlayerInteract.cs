using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    public Camera fpsCam;
    public float interactRange = 4f; 
    public KeyCode interactKey = KeyCode.F; 

    private InventorySystem inventory;
    private PlayerMagnetController magnetController; // Thêm tham chiếu đến găng tay

    void Start()
    {
        inventory = GetComponent<InventorySystem>();
        magnetController = GetComponent<PlayerMagnetController>(); // Lấy component găng tay
        
        if (fpsCam == null) fpsCam = Camera.main;
    }

    void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            TryInteract();
        }
    }

    void TryInteract()
    {
        // TRƯỜNG HỢP 1: NẾU ĐANG CÓ ĐỒ TRÊN TAY -> Cất luôn vào túi
        if (magnetController != null && magnetController.GetGrabbedObject() != null)
        {
            MagneticObject heldObj = magnetController.GetGrabbedObject();
            if (heldObj.itemData != null)
            {
                if (inventory.AddItem(heldObj.itemData, heldObj.gameObject))
                {
                    Debug.Log($"<color=green>Đã cất {heldObj.name} từ găng tay vào túi đồ!</color>");
                    magnetController.ClearGrabbedObjectWithoutReset(); // Xóa khỏi tay một cách âm thầm
                }
            }
            else
            {
                Debug.LogWarning($"Vật thể trên tay chưa có ItemData!");
            }
            return; // Dừng hàm tại đây, không quét Raycast nữa
        }

        // TRƯỜNG HỢP 2: NẾU TAY TRỐNG -> Quét Raycast dưới sàn
        Ray ray = fpsCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange))
        {
            if (hit.collider.CompareTag("Magnetic"))
            {
                MagneticObject magObj = hit.collider.GetComponent<MagneticObject>();
                if (magObj == null || magObj.isMovingAsBullet) return;

                // Lấy ItemData cấu hình loại đạn gán trên vật thể
                ItemData itemData = magObj.itemData;

                if (itemData != null)
                {
                    // Đẩy nguyên khối GameObject này vào Inventory
                    if (inventory.AddItem(itemData, hit.collider.gameObject))
                    {
                        Debug.Log($"<color=green>Đã hút {hit.collider.name} dưới sàn vào túi đồ!</color>");
                    }
                }
                else
                {
                    Debug.LogWarning($"Vật thể {hit.collider.name} chưa được gán ItemData để phân loại đạn!");
                }
            }
        }
    }
}