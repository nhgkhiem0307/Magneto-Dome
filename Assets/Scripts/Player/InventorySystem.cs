using System;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    [Serializable]
    public class InventorySlot
    {
        public ItemData itemData;
        public int count;
        // Lưu trữ trực tiếp danh sách các GameObject thực tế đang bị ẩn đi
        public List<GameObject> capturedObjects = new List<GameObject>();

        public InventorySlot(ItemData item, GameObject obj)
        {
            itemData = item;
            count = 1;
            capturedObjects.Add(obj);
        }
    }

    public List<InventorySlot> slots = new List<InventorySlot>();

    // Hàm cất vật thể thực tế vào túi
    public bool AddItem(ItemData itemToAdd, GameObject actualObject)
    {
        // Tìm ô chứa loại đạn này còn chỗ trống
        InventorySlot slot = slots.Find(s => s.itemData == itemToAdd && s.count < itemToAdd.maxStackSize);

        if (slot != null)
        {
            slot.count++;
            slot.capturedObjects.Add(actualObject);
            
            // Thực hiện ẩn vật thể và chuyển nó thành con của Player
            HideAndParent(actualObject);
            return true;
        }

        // Tạo ô mới
        InventorySlot newSlot = new InventorySlot(itemToAdd, actualObject);
        slots.Add(newSlot);
        
        HideAndParent(actualObject);
        return true;
    }

    private void HideAndParent(GameObject obj)
    {
        // Chuyển vật thể thành con của Player để nó di chuyển theo Player (không bị trôi nổi trên map)
        obj.transform.SetParent(this.transform);
        obj.transform.localPosition = Vector3.zero; // Đưa về tâm Player cho gọn

        // Tắt vật lý vật thể đi để không va chạm lung tung trong túi
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; 
            rb.useGravity = false;
        }

        // Ẩn hoàn toàn vật thể khỏi thế giới game
        obj.SetActive(false);
    }
}