using Fusion;
using UnityEngine;

/// <summary>
/// Ba phím tắt rút đạn từ túi ra tay.
/// Lấy theo thứ tự LIFO - món nhặt sau cùng ra trước, giống bản cũ.
/// </summary>
public class PlayerHotbarController : NetworkBehaviour
{
    [Header("Keybinds")]
    // Ba phím này được NetworkRunnerHandler đọc để đóng gói vào input gửi qua mạng,
    // nên vẫn chỉnh được ở Inspector như trước.
    public KeyCode normalKey = KeyCode.Z;
    public KeyCode heavyKey = KeyCode.X;
    public KeyCode spikeKey = KeyCode.C;

    /// <summary>
    /// Ô thứ ba (phím C, số đạn thứ ba trên HUD) chứa loại vật nào.
    ///
    /// ⚠️ TẠM THỜI LÀ TNT, không phải Spike: map chưa có vật Spike nào, còn TNT cất vào túi
    /// thì trước đây KHÔNG phím nào rút ra được - nằm chết trong túi, chiếm chỗ tới hết round.
    ///
    /// Có vật Spike rồi thì đổi dòng này về Spike là xong, HUDController đọc chung ô này.
    /// Tên biến spikeKey / spikeAmmoText / HotbarSpike cố ý GIỮ NGUYÊN: đổi tên field public
    /// là Unity quên sạch giá trị đã gán trong Inspector.
    /// </summary>
    public const MagneticObject.ObjectType ThirdSlotType = MagneticObject.ObjectType.TNT;

    [Networked] private NetworkButtons PreviousButtons { get; set; }

    private InventorySystem inventory;
    private PlayerMagnetController magnetController;
    private PlayerHealth health;

    public override void Spawned()
    {
        inventory = GetComponent<InventorySystem>();
        magnetController = GetComponent<PlayerMagnetController>();
        health = GetComponent<PlayerHealth>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData input)) return;

        NetworkButtons pressed = input.Buttons.GetPressed(PreviousButtons);
        PreviousButtons = input.Buttons;

        // Chỉ Host được rút đồ ra: nó thay đổi trạng thái của vật thể trong thế giới.
        if (!HasStateAuthority) return;

        // Chết rồi thì không rút đồ được
        if (health != null && !health.IsAlive) return;

        // Ngoài pha chiến đấu cũng không rút được
        if (GameManager.Instance != null && !GameManager.Instance.IsCombatAllowed()) return;

        if (pressed.IsSet((int)InputButton.HotbarNormal)) TryRetrieveAmmo(MagneticObject.ObjectType.Normal);
        if (pressed.IsSet((int)InputButton.HotbarHeavy)) TryRetrieveAmmo(MagneticObject.ObjectType.Heavy);
        if (pressed.IsSet((int)InputButton.HotbarSpike)) TryRetrieveAmmo(ThirdSlotType);
    }

    private void TryRetrieveAmmo(MagneticObject.ObjectType type)
    {
        if (inventory == null || magnetController == null) return;

        // Đang cầm sẵn đồ trên tay thì không rút thêm, tránh làm rơi mất món đang cầm
        if (magnetController.GetGrabbedObject() != null)
        {
            Debug.Log("<color=orange>[TÚI] Đang cầm đồ trên tay rồi, không rút thêm được</color>");
            return;
        }

        MagneticObject obj = inventory.RemoveLastOfType(type);

        if (obj == null)
        {
            Debug.Log($"<color=red>[TÚI] Hết vật thể thuộc nhóm {type}!</color>");
            return;
        }

        // Bật vật lại và đưa tới đúng vị trí tay, rồi ép găng tay tóm lấy
        obj.TakeOutOfBag(magnetController.holdPoint.position);
        magnetController.ForceGrabObject(obj);

        Debug.Log($"<color=cyan>[TÚI] Đã rút {obj.name} ({type}) ra tay. Điện tích: {obj.currentPolarity}</color>");
    }
}
