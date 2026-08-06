using Fusion;
using UnityEngine;

/// <summary>
/// Phím nhặt đồ. Có hai trường hợp:
///   - Đang cầm vật trên tay  -> cất luôn vào túi
///   - Tay trống              -> quét phía trước, nhặt vật thể từ tính vào túi
/// </summary>
public class PlayerInteract : NetworkBehaviour
{
    public Camera fpsCam;
    public float interactRange = 4f;

    // Phím này được NetworkRunnerHandler đọc để đóng gói vào input gửi qua mạng,
    // nên vẫn chỉnh được ở Inspector như trước.
    public KeyCode interactKey = KeyCode.F;

    [Networked] private NetworkButtons PreviousButtons { get; set; }

    private InventorySystem inventory;
    private PlayerMagnetController magnetController;
    private PlayerHealth health;

    public override void Spawned()
    {
        inventory = GetComponent<InventorySystem>();
        magnetController = GetComponent<PlayerMagnetController>();
        health = GetComponent<PlayerHealth>();

        if (fpsCam == null) fpsCam = GetComponentInChildren<Camera>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData input)) return;

        NetworkButtons pressed = input.Buttons.GetPressed(PreviousButtons);
        PreviousButtons = input.Buttons;

        // Chỉ Host xử lý: nhặt đồ làm thay đổi trạng thái vật thể trong thế giới
        if (!HasStateAuthority) return;

        if (health != null && !health.IsAlive) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsCombatAllowed()) return;

        if (!pressed.IsSet((int)InputButton.Interact)) return;

        // Dựng lại hướng ngắm từ góc nhìn gửi kèm input.
        // Không dùng fpsCam.transform.forward được, vì trên Host camera của người chơi khác
        // đã bị tắt và không xoay theo chuột của họ.
        Quaternion aimRotation = Quaternion.Euler(input.Pitch, input.Yaw, 0f);
        Vector3 aimDirection = aimRotation * Vector3.forward;
        Vector3 aimOrigin = fpsCam != null ? fpsCam.transform.position : transform.position + Vector3.up * 1.6f;

        TryInteract(aimOrigin, aimDirection);
    }

    private void TryInteract(Vector3 aimOrigin, Vector3 aimDirection)
    {
        if (inventory == null || magnetController == null) return;

        // TRƯỜNG HỢP 1: ĐANG CÓ ĐỒ TRÊN TAY -> Cất luôn vào túi
        MagneticObject heldObj = magnetController.GetGrabbedObject();
        if (heldObj != null)
        {
            if (inventory.AddItem(heldObj))
            {
                // Xoá khỏi tay một cách âm thầm - AddItem đã lo phần tắt vật đi rồi
                magnetController.ClearGrabbedObjectWithoutReset();
                Debug.Log($"<color=green>[NHẶT] Đã cất {heldObj.name} từ tay vào túi</color>");
            }
            return;
        }

        // TRƯỜNG HỢP 2: TAY TRỐNG -> Quét phía trước tìm vật thể
        if (!Physics.Raycast(aimOrigin, aimDirection, out RaycastHit hit, interactRange)) return;
        if (!hit.collider.CompareTag("Magnetic")) return;

        MagneticObject magObj = hit.collider.GetComponent<MagneticObject>();
        if (magObj == null) return;

        // Đang bay với tư cách đạn thì không nhặt được
        if (magObj.isMovingAsBullet) return;

        if (inventory.AddItem(magObj))
        {
            Debug.Log($"<color=green>[NHẶT] Đã nhặt {magObj.name} dưới sàn vào túi</color>");
        }
    }
}
