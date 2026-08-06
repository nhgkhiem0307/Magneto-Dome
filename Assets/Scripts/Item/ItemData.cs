using UnityEngine;

/// <summary>
/// Bốn món tiêu hao mua từ Shop, cất trong túi và lấy ra bằng Radial Menu.
///
/// Đây là enum chứ không phải tham chiếu ScriptableObject, vì túi đồ cần đồng bộ
/// qua mạng — mà Fusion chỉ truyền được số, không truyền được tham chiếu asset.
/// Số thứ tự dùng làm chỉ mục trong mảng đếm của InventorySystem nên
/// PHẢI liên tục từ 0 và không được đổi thứ tự về sau.
///
/// Shield Armor cố ý KHÔNG nằm đây: theo GDD nó cộng giáp ngay lúc mua,
/// không vào túi và không hiện trong Radial Menu.
/// </summary>
public enum ConsumableType
{
    EnergyDrink = 0,      // Giảm 15% cooldown Dash
    Bandage = 1,          // Hồi tối đa 20 HP, không quá 50% lượng máu đã mất
    GasolineCanister = 2, // Biến 1 vật Normal thành thùng TNT
    EMBarrierCore = 3,    // Thả lõi tạo tường chắn 10 giây
}

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

    // --- PHẦN DÙNG CHO SHOP (thêm 30/07) ---
    //
    // ItemData giờ đảm nhiệm phần HIỂN THỊ của cửa hàng: tên, icon, giá tiền.
    // Còn phần logic chạy qua mạng thì dùng enum ConsumableType ở trên.
    // Tách vậy vì ScriptableObject không truyền qua mạng được.

    [Header("Shop")]
    [Tooltip("Giá bán trong cửa hàng.")]
    public int price;

    [Tooltip("Chỉ điền khi Item Type = Consumable. Cho biết đây là món tiêu hao nào.")]
    public ConsumableType consumableType;
}