using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite itemIcon;
    public int maxStackSize = 10; // Giới hạn số lượng của RIÊNG loại item này

    // Định nghĩa loại vật phẩm để dễ xử lý logic sau này (Đạn, Tiêu dùng, Găng tay...)
    public enum ItemType { Bullet, Consumable, Equipment }
    public ItemType itemType;

    [Header("Nếu là đạn, liên kết với loại MagneticObject tương ứng")]
    public MagneticObject.ObjectType associatedObjectType;
}