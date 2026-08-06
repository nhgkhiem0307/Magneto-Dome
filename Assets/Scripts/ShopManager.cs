using Fusion;
using UnityEngine;

/// <summary>
/// Xử lý giao dịch mua bán của MỘT người chơi. Gắn trên prefab nhân vật.
///
/// Vì sao không phải một Manager chung cho cả trận? Vì tiền và túi đồ là của riêng
/// từng người. Đặt trên nhân vật thì mỗi giao dịch tự biết là của ai, khỏi phải
/// truyền kèm danh tính.
///
/// Luồng mua: Client bấm nút -> gửi RPC lên Host -> Host kiểm tra tiền và pha chơi
/// -> Host trừ tiền và phát đồ. Client KHÔNG bao giờ tự trừ tiền mình.
/// </summary>
public class ShopManager : NetworkBehaviour
{
    [Header("Danh mục hàng")]
    [Tooltip("Thứ tự trong mảng này CHÍNH LÀ mã số món hàng gửi qua mạng. " +
             "Đừng đảo thứ tự sau khi đã chạy thử, nếu không nút bấm sẽ mua nhầm món.")]
    public ItemData[] catalogue;

    private PlayerEconomy economy;
    private InventorySystem inventory;
    private PlayerHealth health;

    public override void Spawned()
    {
        economy = GetComponent<PlayerEconomy>();
        inventory = GetComponent<InventorySystem>();
        health = GetComponent<PlayerHealth>();
    }

    /// <summary>Số món hàng đang bày bán. UI dùng để dựng danh sách nút.</summary>
    public int CatalogueSize => catalogue != null ? catalogue.Length : 0;

    /// <summary>Lấy thông tin một món để hiển thị. Trả về null nếu chỉ số sai.</summary>
    public ItemData GetItem(int index)
    {
        if (catalogue == null || index < 0 || index >= catalogue.Length) return null;
        return catalogue[index];
    }

    /// <summary>UI gọi hàm này khi người chơi bấm nút mua.</summary>
    public void RequestBuy(int itemIndex)
    {
        // Chỉ chủ nhân vật mới được mua đồ cho mình
        if (!HasInputAuthority) return;

        RPC_RequestBuy(itemIndex);
    }

    // Gửi từ máy người chơi lên Host. Host là bên duy nhất được phép quyết định.
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestBuy(int itemIndex)
    {
        TryBuy(itemIndex);
    }

    private void TryBuy(int itemIndex)
    {
        if (!HasStateAuthority) return;

        ItemData item = GetItem(itemIndex);
        if (item == null) return;

        // CHỈ mua được trong pha chuẩn bị.
        // Kiểm tra ở Host chứ không tin vào UI của client - client có thể bị sửa.
        if (GameManager.Instance != null
            && GameManager.Instance.Phase != GameManager.GamePhase.BuyPhase)
        {
            Debug.Log("<color=orange>[SHOP] Chỉ mua được trong pha chuẩn bị</color>");
            return;
        }

        if (economy == null) return;

        if (economy.Money < item.price)
        {
            Debug.Log($"<color=orange>[SHOP] Không đủ tiền mua {item.itemName} (cần ${item.price}, có ${economy.Money})</color>");
            return;
        }

        // Thử phát đồ TRƯỚC rồi mới trừ tiền.
        // Nếu làm ngược lại, người chơi mua giáp lúc đã đầy sẽ mất tiền mà không được gì.
        if (!GiveItem(item))
        {
            Debug.Log($"<color=orange>[SHOP] Không nhận thêm được {item.itemName} lúc này</color>");
            return;
        }

        economy.TrySpend(item.price);
        Debug.Log($"<color=lime>[SHOP] Đã mua {item.itemName} với giá ${item.price}. Còn ${economy.Money}</color>");

        // Chỉ người mua nghe tiếng, không phải cả phòng
        if (HasInputAuthority) AudioManager.Buy();
    }

    // Phát món hàng. Trả về false nếu người chơi không nhận thêm được nữa.
    private bool GiveItem(ItemData item)
    {
        // Equipment = Shield Armor: cộng thẳng vào thanh giáp, không vào túi.
        // Theo GDD đây là món duy nhất không nằm trong Radial Menu.
        if (item.itemType == ItemData.ItemType.Equipment)
        {
            return health != null && health.AddArmor();
        }

        return inventory != null && inventory.AddConsumable(item.consumableType);
    }
}
