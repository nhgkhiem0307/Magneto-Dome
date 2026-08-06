using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bảng thông tin trong trận: máu, giáp, cực găng tay, tiền, đạn, đồng hồ, tỉ số.
///
/// ĐẶT TRÊN CANVAS trong scene gameplay, giống ShopUI và RadialMenuController.
///
/// Script này KHÔNG tính toán gì cả, chỉ đọc và hiển thị. Mọi con số đều đã có sẵn
/// trong các [Networked] của PlayerHealth, PlayerEconomy, GameManager...
/// Nhờ vậy không cần đồng bộ thêm thứ gì qua mạng.
///
/// Mọi ô tham chiếu đều được kiểm tra null, nên bạn có thể gán dần từng phần
/// mà không sợ lỗi. Ô nào bỏ trống thì mục đó đơn giản là không hiện.
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("Gốc HUD")]
    [Tooltip("Object chứa toàn bộ HUD. Tự ẩn khi chưa vào trận.")]
    public GameObject hudRoot;

    [Header("Máu & Giáp")]
    [Tooltip("Image có Image Type = Filled. Script điều khiển Fill Amount.")]
    public Image healthFill;
    public TMP_Text healthText;
    public Image armorFill;
    public TMP_Text armorText;
    [Tooltip("Object chứa thanh giáp. Tự ẩn khi không có giáp.")]
    public GameObject armorGroup;

    [Header("Cực găng tay")]
    [Tooltip("Ô màu báo cực đang mang. Đây là thông tin QUAN TRỌNG NHẤT của HUD game này.")]
    public Image polarityIndicator;
    public TMP_Text polarityText;
    public Color positiveColor = new Color(1f, 0.25f, 0.25f);
    public Color negativeColor = new Color(0.3f, 0.5f, 1f);

    [Header("Tâm ngắm")]
    public GameObject crosshair;

    [Header("Vòng đấu")]
    public TMP_Text phaseText;
    public TMP_Text timerText;
    public TMP_Text redScoreText;
    public TMP_Text blueScoreText;
    public TMP_Text roundNumberText;

    [Header("Kinh tế & Đạn")]
    public TMP_Text moneyText;
    public TMP_Text normalAmmoText;
    public TMP_Text heavyAmmoText;
    public TMP_Text spikeAmmoText;

    [Header("Buff Nước Tăng Lực")]
    public GameObject energyDrinkGroup;
    public TMP_Text energyDrinkTimerText;

    [Header("Thông báo")]
    [Tooltip("Hiện khi người chơi bị loại khỏi round.")]
    public GameObject deadPanel;

    [Tooltip("Dòng chữ lớn báo kết quả round / trận đấu.")]
    public TMP_Text announcementText;

    // Nhớ pha ở khung hình trước, để biết lúc nào vừa chuyển pha mà hiện thông báo
    private GameManager.GamePhase _lastPhase = GameManager.GamePhase.WaitingToStart;
    private bool _hasSeenPhase;

    void Update()
    {
        FPSMovement localPlayer = FPSMovement.Local;
        GameManager gm = GameManager.Instance;

        // Chưa vào trận thì ẩn hết đi
        if (localPlayer == null)
        {
            if (hudRoot != null) hudRoot.SetActive(false);
            return;
        }

        if (hudRoot != null) hudRoot.SetActive(true);

        // Đang quan sát đồng đội thì HUD hiện chỉ số CỦA HỌ, không phải máu 0 của xác mình.
        // Riêng tâm ngắm và bảng "đã bị loại" vẫn bám theo trạng thái thật của mình.
        FPSMovement displayed = SpectatorController.Watching != null
            ? SpectatorController.Watching
            : localPlayer;

        UpdateHealthAndArmor(displayed, localPlayer);
        UpdatePolarity(displayed);
        UpdateCrosshair(localPlayer);
        UpdateEconomyAndAmmo(displayed);
        UpdateEnergyDrink(displayed);
        UpdateRoundInfo(gm);
        UpdateAnnouncement(gm);
    }

    // --- MÁU & GIÁP ---

    // displayed = người đang được hiển thị chỉ số (mình, hoặc đồng đội đang quan sát)
    // self     = nhân vật của chính mình, dùng riêng cho bảng "đã bị loại"
    private void UpdateHealthAndArmor(FPSMovement displayed, FPSMovement self)
    {
        // Bảng báo bị loại phải xét THEO MÌNH, không theo người đang xem nhờ
        PlayerHealth myHealth = self.GetComponent<PlayerHealth>();
        if (deadPanel != null) deadPanel.SetActive(myHealth != null && !myHealth.IsAlive);

        PlayerHealth health = displayed.GetComponent<PlayerHealth>();
        if (health == null) return;

        float healthRatio = health.maxHealth > 0f ? health.CurrentHealth / health.maxHealth : 0f;

        if (healthFill != null) healthFill.fillAmount = Mathf.Clamp01(healthRatio);
        if (healthText != null) healthText.text = Mathf.CeilToInt(Mathf.Max(0f, health.CurrentHealth)).ToString();

        // Thanh giáp chỉ hiện khi thật sự có giáp, đỡ chiếm chỗ vô ích
        bool hasArmor = health.CurrentArmor > 0f;
        if (armorGroup != null) armorGroup.SetActive(hasArmor);

        float armorRatio = health.maxArmor > 0f ? health.CurrentArmor / health.maxArmor : 0f;

        if (armorFill != null) armorFill.fillAmount = Mathf.Clamp01(armorRatio);
        if (armorText != null) armorText.text = Mathf.CeilToInt(health.CurrentArmor).ToString();
    }

    // --- CỰC GĂNG TAY ---

    private void UpdatePolarity(FPSMovement player)
    {
        PlayerMagnetController magnet = player.GetComponent<PlayerMagnetController>();
        if (magnet == null) return;

        bool isPositive = magnet.currentGlovePolarity == MagneticObject.Polarity.Positive;

        if (polarityIndicator != null)
        {
            polarityIndicator.color = isPositive ? positiveColor : negativeColor;
        }

        if (polarityText != null)
        {
            polarityText.text = isPositive ? "+" : "-";
        }
    }

    // --- TÂM NGẮM ---

    private void UpdateCrosshair(FPSMovement player)
    {
        if (crosshair == null) return;

        PlayerHealth health = player.GetComponent<PlayerHealth>();
        bool alive = health == null || health.IsAlive;

        // Ẩn tâm ngắm khi chết, hoặc khi đang mở giao diện (lúc đó chuột dùng để bấm nút)
        crosshair.SetActive(alive && !NetworkRunnerHandler.IsCursorFree());
    }

    // --- TIỀN & ĐẠN ---

    private void UpdateEconomyAndAmmo(FPSMovement player)
    {
        PlayerEconomy economy = player.GetComponent<PlayerEconomy>();
        if (economy != null && moneyText != null)
        {
            moneyText.text = $"${economy.Money}";
        }

        InventorySystem inventory = player.GetComponent<InventorySystem>();
        if (inventory == null) return;

        if (normalAmmoText != null)
            normalAmmoText.text = inventory.CountOfType(MagneticObject.ObjectType.Normal).ToString();

        if (heavyAmmoText != null)
            heavyAmmoText.text = inventory.CountOfType(MagneticObject.ObjectType.Heavy).ToString();

        if (spikeAmmoText != null)
            spikeAmmoText.text = inventory.CountOfType(MagneticObject.ObjectType.Spike).ToString();
    }

    // --- BUFF ---

    private void UpdateEnergyDrink(FPSMovement player)
    {
        bool active = player.IsEnergyDrinkActive;

        if (energyDrinkGroup != null) energyDrinkGroup.SetActive(active);

        if (active && energyDrinkTimerText != null)
        {
            energyDrinkTimerText.text = $"{Mathf.CeilToInt(player.EnergyDrinkRemaining)}s";
        }
    }

    // --- VÒNG ĐẤU ---

    private void UpdateRoundInfo(GameManager gm)
    {
        if (gm == null) return;

        if (redScoreText != null) redScoreText.text = gm.RedScore.ToString();
        if (blueScoreText != null) blueScoreText.text = gm.BlueScore.ToString();
        if (roundNumberText != null) roundNumberText.text = $"ROUND {gm.CurrentRound}";

        if (phaseText != null) phaseText.text = GetPhaseName(gm.Phase);

        if (timerText != null)
        {
            float? remaining = gm.PhaseTimer.RemainingTime(gm.Runner);

            // Pha chiến đấu không giới hạn thời gian nên không có gì để đếm
            timerText.text = remaining.HasValue ? Mathf.CeilToInt(remaining.Value).ToString() : "";
        }
    }

    private string GetPhaseName(GameManager.GamePhase phase)
    {
        switch (phase)
        {
            case GameManager.GamePhase.WaitingToStart: return "CHUẨN BỊ VÀO TRẬN";
            case GameManager.GamePhase.BuyPhase: return "PHA MUA SẮM";
            case GameManager.GamePhase.Combat: return "CHIẾN ĐẤU";
            case GameManager.GamePhase.RoundEnd: return "KẾT THÚC ROUND";
            case GameManager.GamePhase.MatchEnd: return "KẾT THÚC TRẬN";
            default: return "";
        }
    }

    // --- THÔNG BÁO KẾT QUẢ ---

    private void UpdateAnnouncement(GameManager gm)
    {
        if (announcementText == null) return;

        if (gm == null)
        {
            announcementText.text = "";
            return;
        }

        // Chỉ đổi nội dung đúng lúc vừa chuyển pha, không ghi đè mỗi khung hình
        if (_hasSeenPhase && gm.Phase == _lastPhase) return;

        _lastPhase = gm.Phase;
        _hasSeenPhase = true;

        switch (gm.Phase)
        {
            case GameManager.GamePhase.RoundEnd:
                announcementText.text = GetRoundResultText(gm);
                break;

            case GameManager.GamePhase.MatchEnd:
                announcementText.text = gm.MatchWinner == 0
                    ? "ĐỘI ĐỎ VÔ ĐỊCH!"
                    : "ĐỘI XANH VÔ ĐỊCH!";
                break;

            default:
                announcementText.text = "";
                break;
        }
    }

    private string GetRoundResultText(GameManager gm)
    {
        if (gm.LastRoundWinner == -1) return "HOÀ! Không đội nào ghi điểm";

        // So đội thắng với đội của chính mình, để báo "THẮNG" hay "THUA"
        PlayerHealth myHealth = FPSMovement.Local != null
            ? FPSMovement.Local.GetComponent<PlayerHealth>()
            : null;

        if (myHealth == null)
        {
            return gm.LastRoundWinner == 0 ? "ĐỘI ĐỎ THẮNG ROUND" : "ĐỘI XANH THẮNG ROUND";
        }

        return gm.LastRoundWinner == myHealth.Team ? "THẮNG ROUND!" : "THUA ROUND";
    }
}
