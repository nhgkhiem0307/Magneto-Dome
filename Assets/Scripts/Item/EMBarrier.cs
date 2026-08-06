using Fusion;
using UnityEngine;

/// <summary>
/// Bức tường tạm do Lõi Tường Điện Từ (EM Barrier Core) sinh ra.
/// Tự biến mất sau một khoảng thời gian.
///
/// Khác với RoundBarrier (rào chắn đầu round): rào đó đặt sẵn trong scene và chỉ bật/tắt,
/// còn cái này được Host sinh ra giữa trận nên bắt buộc phải là NetworkObject.
/// </summary>
public class EMBarrier : NetworkBehaviour
{
    [Tooltip("Tồn tại bao nhiêu giây rồi tự biến mất.")]
    public float lifetime = 10f;

    [Networked] private TickTimer LifeTimer { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            LifeTimer = TickTimer.CreateFromSeconds(Runner, lifetime);
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Chỉ Host được quyền xoá. Dùng Runner.Despawn chứ không phải Destroy,
        // để mọi máy cùng xoá, không ai bị sót lại bức tường ma.
        if (!HasStateAuthority) return;

        if (LifeTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
        }
    }
}
