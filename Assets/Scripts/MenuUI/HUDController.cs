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

    // Tên biến vẫn là "health..." dù giờ nó hiển thị ĐIỆN TÍCH, không phải máu.
    // CỐ Ý không đổi tên: đổi tên biến public sẽ xoá sạch mọi thứ đã kéo thả vào
    // Inspector, phải gán lại từ đầu. Không đáng để đánh đổi.
    [Header("Điện tích (Quá Tải) & Giáp")]
    [Tooltip("Image có Image Type = Filled. Giờ là THANH ĐIỆN TÍCH: đầy = sắp bay khỏi map.")]
    public Image healthFill;
    [Tooltip("Hiện mức nhiễm điện dạng phần trăm.")]
    public TMP_Text healthText;

    [Tooltip("Màu thanh điện lúc sạch điện (an toàn).")]
    public Color chargeSafeColor = new Color(0.3f, 0.9f, 1f);
    [Tooltip("Màu thanh điện lúc đầy (một cú chạm là bay khỏi đảo).")]
    public Color chargeDangerColor = new Color(1f, 0.3f, 0.1f);

    [Tooltip("(Tuỳ chọn) Hiện hệ số lực văng hiện tại, ví dụ \"x2.4\". " +
             "Đây là con số cho người chơi biết mình đang nguy hiểm cỡ nào.")]
    public TMP_Text knockbackText;

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

    [Tooltip("Đồng hồ đếm ngược tới lúc sống lại. Chỉ hiện khi đang nằm chờ.")]
    public TMP_Text respawnCountdownText;

    [Header("Khu chiếm đóng")]
    [Tooltip("Object chứa hai thanh tiến độ. Tự ẩn ngoài pha chiến đấu.")]
    public GameObject zoneGroup;

    [Tooltip("Image có Image Type = Filled, tiến độ chiếm của đội Đỏ.")]
    public Image zoneRedFill;

    [Tooltip("Image có Image Type = Filled, tiến độ chiếm của đội Xanh.")]
    public Image zoneBlueFill;

    [Tooltip("(Tuỳ chọn) Chữ báo ai đang giữ khu. Tự đổi màu theo đội đang chiếm.")]
    public TMP_Text zoneStatusText;

    [Tooltip("Màu chữ khi ĐỘI MÌNH đang chiếm khu. Xanh lá - cùng màu với viền đồng đội, " +
             "để bộ não học được một quy ước duy nhất: xanh lá = phe mình.")]
    public Color zoneCapturingColor = new Color(0.35f, 1f, 0.25f);

    [Tooltip("Màu chữ khi ĐỊCH đang chiếm khu.")]
    public Color zoneLosingColor = new Color(1f, 0.2f, 0.15f);

    [Header("Báo động khi mất khu")]
    [Tooltip("Phóng to thêm bao nhiêu phần ở đỉnh nhịp. 0.25 = to thêm 25%.")]
    public float zoneAlarmScaleAmount = 0.25f;

    [Tooltip("Biên độ rung, tính bằng pixel giao diện.")]
    public float zoneAlarmShakePixels = 6f;

    [Tooltip("Số nhịp mỗi giây. 2 nhịp/giây gần với nhịp tim lúc căng thẳng.")]
    public float zoneAlarmSpeed = 2f;

    [Tooltip("Màu chữ khi cả hai đội cùng đứng trong khu (tiến độ bị đóng băng).")]
    public Color zoneContestedColor = new Color(1f, 0.78f, 0.23f);

    [Tooltip("Màu chữ khi không ai đứng trong khu.")]
    public Color zoneEmptyColor = new Color(0.7f, 0.7f, 0.72f);

    [Tooltip("Dòng chữ lớn báo kết quả round / trận đấu.")]
    public TMP_Text announcementText;

    [Tooltip("Khung nền chứa dòng thông báo. Ẩn/hiện cùng lúc với dòng chữ, " +
             "để lúc không có thông báo thì không còn hộp trống nằm lại trên màn hình.\n" +
             "Bỏ trống ô này thì script tự hiểu khung nền chính là object CHA của announcementText.")]
    public GameObject announcementGroup;

    // Nhớ pha ở khung hình trước, để biết lúc nào vừa chuyển pha mà hiện thông báo
    private GameManager.GamePhase _lastPhase = GameManager.GamePhase.WaitingToStart;
    private bool _hasSeenPhase;

    // Vị trí và cỡ gốc của dòng chữ trạng thái khu, để trả về sau khi rung xong.
    // Đo một lần lúc chạy chứ không lưu ở Inspector, vì nó phụ thuộc layout thật.
    private Vector2 _zoneAlarmBasePos;
    private Vector3 _zoneAlarmBaseScale;
    private bool _zoneAlarmBaseCaptured;

    void Awake()
    {
        // Không gán tay thì mặc định khung nền là object cha của dòng chữ.
        // Lấy ở Awake (chạy 1 lần lúc khởi tạo) chứ không lấy trong Update, vì khi
        // khung nền bị tắt đi rồi thì transform.parent vẫn đọc được, nhưng lấy 1 lần
        // vẫn rẻ hơn và tránh phụ thuộc thứ tự bật/tắt.
        if (announcementGroup == null && announcementText != null && announcementText.transform.parent != null)
        {
            announcementGroup = announcementText.transform.parent.gameObject;
        }

        // Chốt an toàn: nếu khung nền lại đúng là gốc HUD thì tắt nó sẽ tắt luôn TOÀN BỘ HUD,
        // mà Update() bật lại ngay khung hình sau → màn hình nhấp nháy. Thà bỏ qua còn hơn.
        if (announcementGroup != null && announcementGroup == hudRoot)
        {
            Debug.LogWarning("[HUDController] announcementGroup đang trỏ vào hudRoot. " +
                             "Hãy bọc dòng thông báo trong một object riêng, nếu không khung nền sẽ không tự ẩn.", this);
            announcementGroup = null;
        }

        // Bắt đầu trận thì chưa có gì để báo — giấu sẵn đi cho sạch màn hình
        SetAnnouncement("");
    }

    void Update()
    {
        FPSMovement localPlayer = FPSMovement.Local;
        GameManager gm = GameManager.Instance;

        // HOST THOÁT GIỮA TRẬN -> coi như không có GameManager. Thêm 01/09.
        //
        // Khi Host rời đi, mạng sập trước còn object thì còn nằm đó thêm vài khung hình.
        // Trong khoảng đó gm khác null nhưng Runner bên trong đã chết, và dòng
        // "gm.PhaseTimer.RemainingTime(gm.Runner)" bên dưới sẽ ném lỗi MỖI KHUNG HÌNH.
        // Console ngập lỗi, khung hình tụt về gần 0 - nhìn từ ngoài đúng là "crash".
        //
        // Object.IsValid là cách Fusion tự nhận biết object đã rời khỏi mạng hay chưa.
        if (gm != null && (gm.Object == null || !gm.Object.IsValid)) gm = null;

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
        UpdateZoneAndRespawn(gm, localPlayer);
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

        // THANH ĐIỆN TÍCH. Khác thanh máu ở chỗ nó chạy NGƯỢC: đầy dần lên là xấu đi.
        float chargeRatio = health.ChargeRatio;

        if (healthFill != null)
        {
            healthFill.fillAmount = chargeRatio;

            // Đổi màu dần từ xanh sang đỏ để liếc một cái là biết mình đang ở đâu,
            // không phải đọc số. Nhiễm càng nặng màu càng gắt.
            healthFill.color = Color.Lerp(chargeSafeColor, chargeDangerColor, chargeRatio);
        }

        if (healthText != null) healthText.text = $"{Mathf.RoundToInt(chargeRatio * 100f)}%";

        // Hệ số lực văng: con số nói thẳng "một cú chạm bây giờ đẩy bạn xa gấp mấy lần"
        if (knockbackText != null) knockbackText.text = $"x{health.KnockbackMultiplier:F1}";

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

        // Ô thứ ba đếm đúng loại mà phím C rút ra - hiện đang tạm là TNT, xem
        // PlayerHotbarController.ThirdSlotType. Đọc chung một chỗ để HUD và phím không lệch nhau.
        if (spikeAmmoText != null)
            spikeAmmoText.text = inventory.CountOfType(PlayerHotbarController.ThirdSlotType).ToString();
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

    /// <summary>
    /// Hai thanh tiến độ chiếm khu, và đồng hồ đếm ngược hồi sinh.
    ///
    /// Đồng hồ hồi sinh xét theo NHÂN VẬT CỦA MÌNH, không theo người đang quan sát nhờ -
    /// bạn cần biết khi nào MÌNH sống lại, không phải khi nào đồng đội sống lại.
    /// </summary>
    private void UpdateZoneAndRespawn(GameManager gm, FPSMovement self)
    {
        // Đọc một lần, dùng cho cả đồng hồ hồi sinh lẫn việc xác định đội mình bên dưới
        PlayerHealth selfHealth = self.GetComponent<PlayerHealth>();

        // --- ĐẾM NGƯỢC HỒI SINH ---
        if (respawnCountdownText != null)
        {
            PlayerHealth myHealth = selfHealth;

            if (myHealth != null && myHealth.IsWaitingToRespawn)
            {
                respawnCountdownText.gameObject.SetActive(true);
                respawnCountdownText.text = $"Respawning in {Mathf.CeilToInt(myHealth.RespawnSecondsLeft)}s";
            }
            else
            {
                respawnCountdownText.gameObject.SetActive(false);
            }
        }

        // --- TIẾN ĐỘ CHIẾM KHU ---
        bool inCombat = gm != null && gm.Phase == GameManager.GamePhase.Combat;
        if (zoneGroup != null) zoneGroup.SetActive(inCombat);

        if (gm == null || gm.zoneProgressToWin <= 0f) return;

        float redRatio = Mathf.Clamp01(gm.RedZoneProgress / gm.zoneProgressToWin);
        float blueRatio = Mathf.Clamp01(gm.BlueZoneProgress / gm.zoneProgressToWin);

        if (zoneRedFill != null) zoneRedFill.fillAmount = redRatio;
        if (zoneBlueFill != null) zoneBlueFill.fillAmount = blueRatio;

        if (zoneStatusText == null) return;

        // Đọc thẳng từ ControlZone chứ không qua mạng: mỗi máy tự đếm ai đang đứng trong khu
        // từ vị trí vốn đã đồng bộ sẵn. Không tốn thêm byte nào.
        ControlZone zone = ControlZone.Instance;
        if (zone == null || !inCombat)
        {
            zoneStatusText.text = "";
            return;
        }

        zone.CountPlayersInside(out int red, out int blue);

        // NÓI THEO GÓC NHÌN CỦA NGƯỜI ĐANG CHƠI, không nói theo tên đội.
        //
        // "ĐỘI ĐỎ ĐANG CHIẾM" bắt người chơi phải nhớ mình thuộc đội nào rồi mới suy ra
        // là tin tốt hay tin xấu - mất một nhịp suy nghĩ giữa lúc đang đánh nhau.
        // "LOSING ZONE" thì hiểu ngay lập tức, không cần nghĩ.
        int myTeam = selfHealth != null ? selfHealth.Team : 0;

        int myCount = myTeam == 0 ? red : blue;
        int enemyCount = myTeam == 0 ? blue : red;

        bool alarming = false;

        if (myCount > 0 && enemyCount > 0)
        {
            zoneStatusText.text = "CONTESTED";
            zoneStatusText.color = zoneContestedColor;
        }
        else if (myCount > 0)
        {
            zoneStatusText.text = "CAPTURING";
            zoneStatusText.color = zoneCapturingColor;
        }
        else if (enemyCount > 0)
        {
            zoneStatusText.text = "LOSING ZONE";
            zoneStatusText.color = zoneLosingColor;
            alarming = true; // chỉ tin XẤU mới được rung, nếu không cảnh báo mất giá trị
        }
        else
        {
            zoneStatusText.text = "ZONE NEUTRAL";
            zoneStatusText.color = zoneEmptyColor;
        }

        ApplyZoneAlarm(alarming);
    }

    /// <summary>
    /// Phóng to và rung dòng chữ khi địch đang chiếm khu.
    ///
    /// CHỈ dùng cho tin xấu. Nếu cái gì cũng rung thì người chơi quen mắt và bỏ qua -
    /// cảnh báo chỉ có giá trị khi nó hiếm.
    ///
    /// Nhịp phóng to dùng hàm sin nên nó thở đều đặn; phần rung dùng số ngẫu nhiên nên
    /// nó giật thật. Hai loại chuyển động khác nhau chồng lên nhau mới ra cảm giác
    /// "báo động" chứ không phải "hiệu ứng trang trí".
    /// </summary>
    private void ApplyZoneAlarm(bool alarming)
    {
        if (zoneStatusText == null) return;

        RectTransform rt = zoneStatusText.rectTransform;

        // Ghi lại vị trí và cỡ gốc đúng một lần, để còn đường trả về
        if (!_zoneAlarmBaseCaptured)
        {
            _zoneAlarmBasePos = rt.anchoredPosition;
            _zoneAlarmBaseScale = rt.localScale;
            _zoneAlarmBaseCaptured = true;
        }

        if (!alarming)
        {
            rt.anchoredPosition = _zoneAlarmBasePos;
            rt.localScale = _zoneAlarmBaseScale;
            return;
        }

        float t = Time.unscaledTime * zoneAlarmSpeed * Mathf.PI * 2f;

        // Nhịp thở: 0 -> 1 -> 0. Dùng Abs(Sin) để nhịp nào cũng nảy lên, không có nhịp lép
        float beat = Mathf.Abs(Mathf.Sin(t));
        rt.localScale = _zoneAlarmBaseScale * (1f + beat * zoneAlarmScaleAmount);

        // Rung: chỉ rung mạnh ở đỉnh nhịp, để nó khớp với cú phóng to
        float shake = zoneAlarmShakePixels * beat;
        rt.anchoredPosition = _zoneAlarmBasePos + new Vector2(
            Random.Range(-shake, shake),
            Random.Range(-shake, shake));
    }

    private void UpdateRoundInfo(GameManager gm)
    {
        if (gm == null) return;

        if (redScoreText != null) redScoreText.text = gm.RedScore.ToString();
        if (blueScoreText != null) blueScoreText.text = gm.BlueScore.ToString();
        if (roundNumberText != null) roundNumberText.text = $"ROUND {gm.CurrentRound}";

        // TẮT HẲN NHÃN PHA KHI ĐANG CHIẾN ĐẤU.
        //
        // Lúc đánh nhau thì người chơi biết thừa là đang đánh nhau - dòng chữ "COMBAT"
        // không thêm thông tin gì, chỉ chiếm chỗ giữa màn hình và cạnh tranh sự chú ý với
        // những thứ thật sự cần nhìn (mức nhiễm điện, cực găng, vị trí địch).
        //
        // Tắt bằng SetActive chứ không phải gán chuỗi rỗng: chuỗi rỗng vẫn để lại một ô
        // trong suốt, nếu nhãn có nền hay viền thì cái nền đó vẫn hiện.
        //
        // Các pha khác vẫn hiện bình thường - lúc đó nhãn mới có việc để làm.
        if (phaseText != null)
        {
            bool showPhase = gm.Phase != GameManager.GamePhase.Combat;

            if (phaseText.gameObject.activeSelf != showPhase)
            {
                phaseText.gameObject.SetActive(showPhase);
            }

            if (showPhase) phaseText.text = GetPhaseName(gm.Phase);
        }

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
            case GameManager.GamePhase.WaitingToStart: return "GET READY";
            case GameManager.GamePhase.BuyPhase: return "BUY PHASE";
            case GameManager.GamePhase.Combat: return "COMBAT";
            case GameManager.GamePhase.RoundEnd: return "ROUND OVER";
            case GameManager.GamePhase.MatchEnd: return "MATCH OVER";
            default: return "";
        }
    }

    // --- THÔNG BÁO KẾT QUẢ ---

    private void UpdateAnnouncement(GameManager gm)
    {
        if (announcementText == null) return;

        if (gm == null)
        {
            SetAnnouncement("");

            // Quên pha đã thấy đi. Nếu không, khi GameManager xuất hiện trở lại mà vẫn
            // đang ở đúng pha cũ thì lệnh return bên dưới sẽ chặn, thông báo không hiện lại.
            _hasSeenPhase = false;
            return;
        }

        // Chỉ đổi nội dung đúng lúc vừa chuyển pha, không ghi đè mỗi khung hình
        if (_hasSeenPhase && gm.Phase == _lastPhase) return;

        _lastPhase = gm.Phase;
        _hasSeenPhase = true;

        switch (gm.Phase)
        {
            case GameManager.GamePhase.BuyPhase:
                // ROUND QUYẾT ĐỊNH: báo cho người chơi biết round này khác mọi round trước.
                //
                // Không có nó thì round 9 y hệt round 1 về mặt cảm giác, dù một bên chỉ
                // còn cách chức vô địch đúng một round. Toàn bộ độ căng của thể thức
                // "đấu tới 5 thắng" nằm ở chỗ người chơi BIẾT mình đang ở đâu trong đó.
                SetAnnouncement(GetMatchPointText(gm));
                break;

            case GameManager.GamePhase.RoundEnd:
                SetAnnouncement(GetRoundResultText(gm));
                break;

            case GameManager.GamePhase.MatchEnd:
                // Trận bị huỷ vì có người thoát -> nói rõ LÝ DO, không hiện đội thắng.
                //
                // Thiếu nhánh này thì trận huỷ sẽ hiện "BLUE TEAM WINS!" (vì MatchWinner
                // bằng -1, khác 0), tức là báo sai người thắng - vừa khó hiểu vừa gây ức chế
                // cho đội đang dẫn điểm.
                if (gm.MatchAbandoned)
                {
                    SetAnnouncement("MATCH CANCELLED\nA PLAYER LEFT THE GAME");
                }
                else
                {
                    SetAnnouncement(gm.MatchWinner == 0
                        ? "RED TEAM WINS!"
                        : "BLUE TEAM WINS!");
                }
                break;

            default:
                SetAnnouncement("");
                break;
        }
    }

    /// <summary>
    /// Đặt nội dung thông báo VÀ bật/tắt khung nền theo nó.
    /// Truyền chuỗi rỗng nghĩa là "không có gì để báo" → giấu luôn cả khung.
    ///
    /// Lưu ý: gán .text cho một TMP_Text đang nằm trong object bị tắt vẫn chạy bình thường,
    /// nên thứ tự gán chữ trước, tắt khung sau là an toàn.
    /// </summary>
    private void SetAnnouncement(string message)
    {
        bool hasMessage = !string.IsNullOrEmpty(message);

        if (announcementText != null) announcementText.text = message;
        if (announcementGroup != null) announcementGroup.SetActive(hasMessage);
    }

    /// <summary>
    /// Chữ hiện đầu round khi tỉ số đã tới mức quyết định. Trả về chuỗi rỗng ở round thường.
    ///
    /// Xét theo ĐỘI CỦA MÌNH chứ không nói chung chung, vì cùng một tỉ số 4-2 thì với
    /// một bên là "sắp vô địch" còn bên kia là "thua là hết" - hai cảm xúc trái ngược,
    /// không nên gộp thành một câu.
    /// </summary>
    private string GetMatchPointText(GameManager gm)
    {
        PlayerHealth myHealth = FPSMovement.Local != null
            ? FPSMovement.Local.GetComponent<PlayerHealth>()
            : null;

        if (myHealth == null) return "";

        int myScore = myHealth.Team == 0 ? gm.RedScore : gm.BlueScore;
        int enemyScore = myHealth.Team == 0 ? gm.BlueScore : gm.RedScore;

        // Thắng round này là đủ điều kiện vô địch chưa? Phải xét CẢ hai luật:
        // đủ số điểm quy định VÀ hơn đối thủ đủ cách biệt (Overtime dùng chung luật này).
        bool iCanWin = (myScore + 1) >= gm.pointsToWin && (myScore + 1 - enemyScore) >= gm.requiredLead;
        bool enemyCanWin = (enemyScore + 1) >= gm.pointsToWin && (enemyScore + 1 - myScore) >= gm.requiredLead;

        if (iCanWin && enemyCanWin) return "MATCH POINT - BOTH TEAMS";
        if (iCanWin) return "MATCH POINT - WIN TO TAKE IT ALL";
        if (enemyCanWin) return "ELIMINATION - LOSE AND IT IS OVER";

        return "";
    }

    private string GetRoundResultText(GameManager gm)
    {
        if (gm.LastRoundWinner == -1) return "DRAW - NO TEAM SCORED";

        // So đội thắng với đội của chính mình, để báo "THẮNG" hay "THUA"
        PlayerHealth myHealth = FPSMovement.Local != null
            ? FPSMovement.Local.GetComponent<PlayerHealth>()
            : null;

        if (myHealth == null)
        {
            return gm.LastRoundWinner == 0 ? "RED TEAM WINS THE ROUND" : "BLUE TEAM WINS THE ROUND";
        }

        return gm.LastRoundWinner == myHealth.Team ? "ROUND WON!" : "ROUND LOST";
    }
}
