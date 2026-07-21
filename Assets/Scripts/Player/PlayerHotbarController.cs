using UnityEngine;

public class PlayerHotbarController : MonoBehaviour
{
    [Header("Keybinds")]
    public KeyCode normalKey = KeyCode.Z;
    public KeyCode heavyKey = KeyCode.X;
    public KeyCode spikeKey = KeyCode.C;

    private InventorySystem inventory;
    private PlayerMagnetController magnetController;

    void Start()
    {
        inventory = GetComponent<InventorySystem>();
        magnetController = GetComponent<PlayerMagnetController>();
    }

    void Update()
    {
        if (Input.GetKeyDown(normalKey)) TryRetrieveAmmo(MagneticObject.ObjectType.Normal);
        if (Input.GetKeyDown(heavyKey)) TryRetrieveAmmo(MagneticObject.ObjectType.Heavy);
        if (Input.GetKeyDown(spikeKey)) TryRetrieveAmmo(MagneticObject.ObjectType.Spike);
    }

    private void TryRetrieveAmmo(MagneticObject.ObjectType type)
    {
        // Tìm ô chứa loại đạn mong muốn
        InventorySystem.InventorySlot slot = inventory.slots.Find(
            s => s.itemData != null && 
            s.itemData.associatedObjectType == type && 
            s.capturedObjects.Count > 0
        );

        if (slot != null)
        {
            // Lấy ra chính xác GameObject thực tế cuối cùng được nhặt (LIFO)
            int lastIndex = slot.capturedObjects.Count - 1;
            GameObject objToRetrieve = slot.capturedObjects[lastIndex];

            // Xóa khỏi danh sách túi đồ
            slot.capturedObjects.RemoveAt(lastIndex);
            slot.count--;

            if (slot.count <= 0)
            {
                inventory.slots.Remove(slot);
            }

            // 1. Mang vật thể ra tay
            objToRetrieve.transform.SetParent(null); // Giải phóng khỏi Player parent
            objToRetrieve.transform.position = magnetController.holdPoint.position;
            objToRetrieve.transform.rotation = magnetController.holdPoint.rotation;

            // 2. Kích hoạt lại hiển thị
            objToRetrieve.SetActive(true);

            // 3. Kích hoạt lại Vật lý
            Rigidbody rb = objToRetrieve.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = false; // Găng đang giữ nên tạm tắt trọng lực
            }

            MagneticObject magObj = objToRetrieve.GetComponent<MagneticObject>();
            if (magObj != null)
            {
                // Ép găng tay tóm chặt lấy nó, giữ nguyên polarity nguyên bản lúc nhặt
                magnetController.ForceGrabObject(magObj);
                Debug.Log($"Đã lôi {objToRetrieve.name} nguyên bản ra chiến đấu! Polarity hiện tại: {magObj.currentPolarity}");
            }
        }
        else
        {
            Debug.Log($"<color=red>Hết vật thể thuộc nhóm {type} trong túi!</color>");
        }
    }
    
}