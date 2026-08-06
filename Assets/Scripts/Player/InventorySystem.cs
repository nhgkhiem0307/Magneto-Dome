using Fusion;
using UnityEngine;

/// <summary>
/// Túi đồ của người chơi.
///
/// THAY ĐỔI LỚN so với bản cũ (30/07): trước đây túi lưu thẳng danh sách GameObject rồi
/// SetParent + SetActive(false) để ẩn đi. Cả hai thao tác đó đều KHÔNG dùng được với
/// NetworkObject - Fusion không hỗ trợ đổi cha giữa trận, và tắt hẳn một NetworkObject
/// sẽ làm hỏng vòng đời mô phỏng của nó.
///
/// Cách mới: túi chỉ lưu ID của các vật thể. Bản thân vật thể vẫn tồn tại trong thế giới,
/// chỉ bị tắt hình ảnh và va chạm đi (xem MagneticObject.IsStored). Nhờ vậy khi rút ra,
/// vật giữ nguyên đúng prefab, đúng điện tích, đúng sát thương - không phải tạo lại từ đầu.
/// </summary>
public class InventorySystem : NetworkBehaviour
{
    // Sức chứa tối đa của túi. NetworkArray bắt buộc phải khai báo kích thước cố định
    // ngay từ đầu, vì Fusion cần biết trước cần bao nhiêu băng thông.
    public const int Capacity = 16;

    // Số loại vật phẩm tiêu hao. Phải khớp với số phần tử của enum ConsumableType.
    public const int ConsumableTypeCount = 4;

    [Header("Vật phẩm tiêu hao")]
    [Tooltip("Số lượng tối đa mỗi loại món tiêu hao được mang theo.")]
    public int maxConsumableStack = 3;

    [Header("Lõi Tường Điện Từ")]
    [Tooltip("Prefab bức tường tạm. Bắt buộc có NetworkObject + EMBarrier.")]
    public NetworkObject emBarrierPrefab;

    [Tooltip("Thả tường cách người chơi bao nhiêu mét về phía trước.")]
    public float emBarrierDistance = 3f;

    // Danh sách ID các vật đang nằm trong túi. Ô trống mang giá trị mặc định.
    // Lưu ID chứ không lưu tham chiếu, vì tham chiếu không truyền qua mạng được.
    [Networked, Capacity(Capacity)]
    public NetworkArray<NetworkBehaviourId> StoredItems { get; }

    // Số lượng từng loại món tiêu hao, đánh chỉ mục theo enum ConsumableType.
    // Món tiêu hao không có thực thể trong thế giới nên chỉ cần đếm số là đủ.
    [Networked, Capacity(ConsumableTypeCount)]
    public NetworkArray<int> ConsumableCounts { get; }

    /// <summary>Số món đang có trong túi.</summary>
    public int Count
    {
        get
        {
            int n = 0;
            for (int i = 0; i < Capacity; i++)
            {
                if (GetItemAt(i) != null) n++;
            }
            return n;
        }
    }

    /// <summary>Lấy vật ở ô thứ i, trả về null nếu ô trống hoặc vật đã biến mất.</summary>
    public MagneticObject GetItemAt(int index)
    {
        if (index < 0 || index >= Capacity) return null;
        if (Runner == null) return null;

        NetworkBehaviourId id = StoredItems[index];
        if (!Runner.TryFindBehaviour(id, out MagneticObject obj)) return null;

        return obj;
    }

    /// <summary>Đếm số món thuộc một loại đạn cụ thể. HUD sẽ dùng hàm này.</summary>
    public int CountOfType(MagneticObject.ObjectType type)
    {
        int n = 0;
        for (int i = 0; i < Capacity; i++)
        {
            MagneticObject obj = GetItemAt(i);
            if (obj != null && obj.CurrentType == type) n++;
        }
        return n;
    }

    /// <summary>
    /// Cất một vật vào túi. Chỉ Host được gọi.
    /// Trả về false nếu túi đã đầy.
    /// </summary>
    public bool AddItem(MagneticObject obj)
    {
        if (!HasStateAuthority) return false;
        if (obj == null) return false;

        for (int i = 0; i < Capacity; i++)
        {
            if (GetItemAt(i) != null) continue;

            StoredItems.Set(i, obj.Id);
            obj.StoreInBag();

            Debug.Log($"<color=green>[TÚI] Đã cất {obj.name} ({obj.CurrentType}) vào ô {i}</color>");
            return true;
        }

        Debug.Log("<color=orange>[TÚI] Túi đã đầy!</color>");
        return false;
    }

    /// <summary>
    /// Lấy ra món CUỐI CÙNG thuộc loại chỉ định (kiểu LIFO, giống bản cũ).
    /// Chỉ Host được gọi. Trả về null nếu không còn món nào thuộc loại đó.
    /// </summary>
    public MagneticObject RemoveLastOfType(MagneticObject.ObjectType type)
    {
        if (!HasStateAuthority) return null;

        for (int i = Capacity - 1; i >= 0; i--)
        {
            MagneticObject obj = GetItemAt(i);
            if (obj == null || obj.CurrentType != type) continue;

            StoredItems.Set(i, default);
            return obj;
        }

        return null;
    }

    // ==================== VẬT PHẨM TIÊU HAO ====================

    /// <summary>Số lượng đang có của một món tiêu hao. Radial Menu và HUD đọc hàm này.</summary>
    public int GetConsumableCount(ConsumableType type)
    {
        int index = (int)type;
        if (index < 0 || index >= ConsumableTypeCount) return 0;

        return ConsumableCounts[index];
    }

    /// <summary>
    /// Cộng một món tiêu hao vào túi (Shop gọi sau khi trừ tiền xong).
    /// Trả về false nếu đã đầy kho loại đó, để Shop không trừ tiền oan.
    /// </summary>
    public bool AddConsumable(ConsumableType type)
    {
        if (!HasStateAuthority) return false;

        int index = (int)type;
        if (index < 0 || index >= ConsumableTypeCount) return false;

        if (ConsumableCounts[index] >= maxConsumableStack)
        {
            Debug.Log($"<color=orange>[TÚI] Đã mang tối đa {maxConsumableStack} món {type}</color>");
            return false;
        }

        ConsumableCounts.Set(index, ConsumableCounts[index] + 1);
        Debug.Log($"<color=green>[TÚI] Nhận {type}, hiện có {ConsumableCounts[index]}</color>");
        return true;
    }

    /// <summary>
    /// Radial Menu gọi vào đây khi người chơi chọn xong một món.
    /// Gửi yêu cầu lên Host chứ không tự dùng, để client không thể tự chế ra vật phẩm.
    /// </summary>
    public void RequestUseConsumable(ConsumableType type)
    {
        if (!HasInputAuthority) return;

        RPC_UseConsumable((int)type);
    }

    // Truyền int thay vì enum cho chắc ăn với bộ sinh mã của Fusion.
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_UseConsumable(int typeIndex)
    {
        UseConsumable((ConsumableType)typeIndex);
    }

    /// <summary>
    /// Dùng một món tiêu hao: trừ số lượng rồi áp dụng hiệu ứng.
    /// Chỉ Host được gọi. Trả về false nếu hết hàng hoặc dùng không có tác dụng.
    /// </summary>
    public bool UseConsumable(ConsumableType type)
    {
        if (!HasStateAuthority) return false;

        int index = (int)type;
        if (index < 0 || index >= ConsumableTypeCount) return false;
        if (ConsumableCounts[index] <= 0) return false;

        // Thử áp dụng hiệu ứng TRƯỚC khi trừ số lượng.
        // Nếu dùng mà không có tác dụng (ví dụ máu đang đầy mà xài băng gạc)
        // thì không nuốt mất món đồ của người chơi.
        if (!ApplyConsumableEffect(type)) return false;

        ConsumableCounts.Set(index, ConsumableCounts[index] - 1);
        Debug.Log($"<color=cyan>[TÚI] Đã dùng {type}, còn lại {ConsumableCounts[index]}</color>");

        // Chỉ người dùng món đồ mới nghe, không phải cả phòng
        if (HasInputAuthority) AudioManager.UseItem();

        return true;
    }

    // Hiệu ứng của từng món. Trả về false nếu dùng lúc này là vô nghĩa.
    private bool ApplyConsumableEffect(ConsumableType type)
    {
        switch (type)
        {
            case ConsumableType.EnergyDrink:
                return ApplyEnergyDrink();

            case ConsumableType.Bandage:
                return ApplyBandage();

            case ConsumableType.GasolineCanister:
                return ApplyGasolineCanister();

            case ConsumableType.EMBarrierCore:
                return ApplyEMBarrierCore();

            default:
                return false;
        }
    }

    // Nước tăng lực: giảm cooldown Dash trong một khoảng thời gian, không cộng dồn.
    private bool ApplyEnergyDrink()
    {
        FPSMovement movement = GetComponent<FPSMovement>();
        if (movement == null) return false;

        return movement.ApplyEnergyDrink();
    }

    // Băng gạc: hồi tối đa 20 HP, nhưng không quá 50% lượng máu ĐÃ MẤT.
    // Ví dụ mất 20 HP (còn 80) -> hồi 50% của 20 = 10 HP -> thành 90 HP.
    private bool ApplyBandage()
    {
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health == null) return false;

        return health.ApplyBandage();
    }

    // Chai xăng: cầm lên tay. Cú bấm chuột trái kế tiếp vào một vật thường
    // sẽ biến vật đó thành thùng TNT.
    private bool ApplyGasolineCanister()
    {
        PlayerMagnetController magnet = GetComponent<PlayerMagnetController>();
        if (magnet == null) return false;

        return magnet.EquipGasoline();
    }

    // Lõi tường điện từ: thả một bức tường tạm ngay trước mặt.
    private bool ApplyEMBarrierCore()
    {
        if (emBarrierPrefab == null)
        {
            Debug.LogError("[TÚI] Chưa gán 'Em Barrier Prefab' trong Inspector của InventorySystem!");
            return false;
        }

        // Đặt trước mặt, ngang tầm chân người chơi.
        // transform.forward chính là hướng thân người đang quay, đã đồng bộ sẵn qua mạng.
        Vector3 spawnPosition = transform.position + transform.forward * emBarrierDistance;
        Quaternion spawnRotation = Quaternion.LookRotation(transform.forward, Vector3.up);

        Runner.Spawn(emBarrierPrefab, spawnPosition, spawnRotation, Object.InputAuthority);

        Debug.Log("<color=cyan>[TƯỜNG ĐIỆN TỪ] Đã thả tường chắn trước mặt</color>");
        return true;
    }
}
