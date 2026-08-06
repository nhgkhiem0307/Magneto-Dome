using Fusion;
using UnityEngine;

/// <summary>
/// Ví tiền của một người chơi. Tiền dùng để mua đồ ở Shop trong pha chuẩn bị.
///
/// Chỉ Host được cộng trừ tiền. Client chỉ đọc để hiển thị lên HUD.
/// Nếu để Client tự sửa thì ai cũng có thể tự cho mình tiền.
/// </summary>
public class PlayerEconomy : NetworkBehaviour
{
    [Header("Cấu hình kinh tế")]
    [Tooltip("Số tiền có sẵn ở round đầu tiên. GDD không quy định con số này, cần cân bằng thử.")]
    public int startingMoney = 500;

    [Tooltip("Trần ví. Thừa ra bao nhiêu cũng bị cắt bỏ.")]
    public int moneyCap = 750;

    [Tooltip("Thưởng cho đội thắng round.")]
    public int winReward = 150;

    [Tooltip("Thưởng an ủi cho đội thua round.")]
    public int loseReward = 100;

    [Networked, OnChangedRender(nameof(OnMoneyChanged))]
    public int Money { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            Money = Mathf.Min(startingMoney, moneyCap);
        }
    }

    /// <summary>Cộng tiền, tự động cắt phần vượt trần. Chỉ Host gọi.</summary>
    public void AddMoney(int amount)
    {
        if (!HasStateAuthority) return;

        Money = Mathf.Clamp(Money + amount, 0, moneyCap);
    }

    /// <summary>
    /// Thử trừ tiền để mua đồ. Trả về false nếu không đủ, và KHÔNG trừ gì cả.
    /// Chỉ Host gọi - mọi giao dịch phải do Host duyệt để chống gian lận.
    /// </summary>
    public bool TrySpend(int amount)
    {
        if (!HasStateAuthority) return false;
        if (amount < 0) return false;
        if (Money < amount) return false;

        Money -= amount;
        return true;
    }

    /// <summary>Thưởng cuối round. GameManager gọi vào.</summary>
    public void GiveRoundReward(bool won)
    {
        AddMoney(won ? winReward : loseReward);
    }

    private void OnMoneyChanged()
    {
        // Tạm thời chỉ in log. Khi làm HUD, đây là chỗ cập nhật con số tiền trên màn hình.
        if (HasInputAuthority)
        {
            Debug.Log($"<color=#FFD700>[TIỀN] Ví của bạn: ${Money}</color>");
        }
    }
}
