using Fusion;
using UnityEngine;

/// <summary>
/// Điều khiển toàn bộ vòng đời một trận đấu: các pha của round, tính điểm,
/// hồi sinh, và điều kiện thắng chung cuộc.
///
/// Chỉ Host chạy phần logic. Các máy khác chỉ đọc trạng thái [Networked] để hiển thị.
/// HUD sau này sẽ móc thẳng vào các giá trị công khai ở đây.
/// </summary>
public class GameManager : NetworkBehaviour
{
    // Để HUD và các script khác lấy được GameManager mà không cần kéo thả Inspector
    public static GameManager Instance { get; private set; }

    public enum GamePhase
    {
        WaitingToStart, // vừa vào scene, chờ mọi người sẵn sàng
        BuyPhase,       // 15 giây chuẩn bị, chưa được đánh nhau
        Combat,         // đang chiến đấu
        RoundEnd,       // hết round, đóng băng vài giây để xem kết quả
        MatchEnd        // hết trận
    }

    [Header("Thời lượng các pha (giây)")]
    public float buyPhaseDuration = 15f;
    public float roundEndDuration = 5f;
    public float matchEndDuration = 8f;
    [Tooltip("Chờ một nhịp sau khi vào scene để mọi nhân vật kịp spawn xong.")]
    public float warmupDuration = 2f;

    [Tooltip("Giới hạn giờ pha chiến đấu. BẮT BUỘC phải có trong chế độ Quá Tải: " +
             "vì không ai chết vì hết máu, hai bên cùng né rìa vực thì round kéo dài vô tận. " +
             "Hết giờ thì đội có TỔNG ĐIỆN TÍCH thấp hơn được xử thắng.")]
    public float combatDuration = 90f;

    [Header("Điều kiện thắng")]
    [Tooltip("Số round cần thắng để vô địch.")]
    public int pointsToWin = 5;
    [Tooltip("Khoảng cách tối thiểu với đối thủ. Không đủ thì vào Overtime, đấu tiếp tới khi đủ.")]
    public int requiredLead = 2;

    [Header("KillZone")]
    [Tooltip("Rơi xuống thấp hơn độ cao này là chết. Map là đảo lơ lửng nên cần cái này.")]
    public float killZoneY = -20f;

    // --- TRẠNG THÁI ĐỒNG BỘ ---
    [Networked, OnChangedRender(nameof(OnPhaseChanged))]
    public GamePhase Phase { get; set; }

    // TickTimer chạy theo tick mạng nên mọi máy đếm ngược khớp nhau tuyệt đối.
    // HUD đọc PhaseTimer.RemainingTime(Runner) là ra số giây còn lại.
    [Networked] public TickTimer PhaseTimer { get; set; }

    [Networked] public int RedScore { get; set; }
    [Networked] public int BlueScore { get; set; }
    [Networked] public int CurrentRound { get; set; }

    // Đội thắng round vừa rồi: 0 = Đỏ, 1 = Xanh, -1 = chưa có
    [Networked] public int LastRoundWinner { get; set; }

    // Đội vô địch: 0 = Đỏ, 1 = Xanh, -1 = chưa xong
    [Networked] public int MatchWinner { get; set; }

    public override void Spawned()
    {
        Instance = this;

        if (HasStateAuthority)
        {
            RedScore = 0;
            BlueScore = 0;
            CurrentRound = 0;
            LastRoundWinner = -1;
            MatchWinner = -1;

            // Chờ một nhịp cho mọi nhân vật spawn xong rồi mới bắt đầu round đầu tiên
            Phase = GamePhase.WaitingToStart;
            PhaseTimer = TickTimer.CreateFromSeconds(Runner, warmupDuration);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this) Instance = null;
    }

    public override void FixedUpdateNetwork()
    {
        // Toàn bộ luật chơi do Host quyết định. Client chỉ nhận kết quả.
        if (!HasStateAuthority) return;

        // KillZone kiểm tra liên tục trong lúc đánh nhau
        if (Phase == GamePhase.Combat)
        {
            CheckKillZone();
        }

        switch (Phase)
        {
            case GamePhase.WaitingToStart:
                if (PhaseTimer.Expired(Runner)) StartNewRound();
                break;

            case GamePhase.BuyPhase:
                if (PhaseTimer.Expired(Runner)) StartCombat();
                break;

            case GamePhase.Combat:
                // Hết giờ trước khi có ai rơi -> phân thắng bại bằng tổng điện tích.
                // Xét TRƯỚC CheckRoundOver để hết giờ là chốt ngay, không chờ thêm tick nào.
                if (PhaseTimer.Expired(Runner))
                {
                    EndRoundByCharge();
                    break;
                }

                CheckRoundOver();
                break;

            case GamePhase.RoundEnd:
                if (PhaseTimer.Expired(Runner)) OnRoundEndFinished();
                break;

            case GamePhase.MatchEnd:
                // Cho người chơi vài giây xem tỉ số chung cuộc rồi mới đưa về menu.
                //
                // Chỉ Host gọi. Việc tắt Runner sẽ khiến các máy Client nhận OnShutdown,
                // và chúng tự quay về menu ở đó - không cần gửi thêm tin gì.
                if (PhaseTimer.Expired(Runner))
                {
                    PhaseTimer = TickTimer.None; // tránh gọi lại ở tick sau

                    if (NetworkRunnerHandler.Instance != null)
                    {
                        NetworkRunnerHandler.Instance.ReturnToMenu();
                    }
                }
                break;
        }
    }

    // --- CHUYỂN PHA ---

    private void StartNewRound()
    {
        CurrentRound++;

        // Hồi sinh toàn bộ, kể cả người đang sống, để ai cũng về đúng điểm xuất phát
        RespawnAllPlayers();

        // Trả cả bản đồ về nguyên trạng: mọi vật thể về đúng chỗ cũ, sạch điện,
        // thùng TNT đã nổ thì sống lại. Round mới bắt đầu từ một sân chơi y hệt round trước.
        ResetWorldObjects();

        Phase = GamePhase.BuyPhase;
        PhaseTimer = TickTimer.CreateFromSeconds(Runner, buyPhaseDuration);

        Debug.Log($"<color=cyan><b>=== ROUND {CurrentRound} - PHA CHUẨN BỊ ({buyPhaseDuration}s) ===</b></color>");
    }

    private void StartCombat()
    {
        Phase = GamePhase.Combat;

        // Chế độ Quá Tải: pha chiến đấu CÓ giới hạn giờ.
        // Trước đây để TickTimer.None (vô hạn) là được, vì đằng nào cũng có người
        // hết máu. Giờ không còn cái chết vì hết máu nữa nên phải có đồng hồ.
        PhaseTimer = TickTimer.CreateFromSeconds(Runner, combatDuration);

        Debug.Log($"<color=lime><b>=== BẮT ĐẦU CHIẾN ĐẤU ({combatDuration}s) ===</b></color>");
    }

    private void EndRound(int winnerTeam)
    {
        LastRoundWinner = winnerTeam;

        if (winnerTeam == 0) RedScore++;
        else BlueScore++;

        GiveRoundRewards(winnerTeam);

        Phase = GamePhase.RoundEnd;
        PhaseTimer = TickTimer.CreateFromSeconds(Runner, roundEndDuration);

        string teamName = winnerTeam == 0 ? "ĐỎ" : "XANH";
        Debug.Log($"<color=yellow><b>=== ĐỘI {teamName} THẮNG ROUND {CurrentRound}! Tỉ số {RedScore} - {BlueScore} ===</b></color>");
    }

    // Phát tiền cuối round. Đội thắng và đội thua nhận mức khác nhau,
    // con số cụ thể đặt ở Inspector của PlayerEconomy trên prefab nhân vật.
    //
    // Truyền winnerTeam = -1 khi hoà: cả hai đội cùng nhận mức thưởng của bên thua.
    private void GiveRoundRewards(int winnerTeam)
    {
        foreach (PlayerHealth p in PlayerHealth.AllPlayers)
        {
            if (p == null) continue;

            PlayerEconomy economy = p.GetComponent<PlayerEconomy>();
            if (economy == null) continue;

            bool won = winnerTeam != -1 && p.Team == winnerTeam;
            economy.GiveRoundReward(won);
        }
    }

    private void OnRoundEndFinished()
    {
        int winner = GetMatchWinner();

        if (winner != -1)
        {
            MatchWinner = winner;
            Phase = GamePhase.MatchEnd;
            PhaseTimer = TickTimer.CreateFromSeconds(Runner, matchEndDuration);

            string teamName = winner == 0 ? "ĐỎ" : "XANH";
            Debug.Log($"<color=magenta><b>########## ĐỘI {teamName} VÔ ĐỊCH! {RedScore} - {BlueScore} ##########</b></color>");
            return;
        }

        StartNewRound();
    }

    // --- KIỂM TRA ĐIỀU KIỆN ---

    // Round kết thúc khi một đội không còn ai sống sót
    private void CheckRoundOver()
    {
        int redAlive = 0;
        int blueAlive = 0;
        int totalPlayers = 0;

        foreach (PlayerHealth p in PlayerHealth.AllPlayers)
        {
            if (p == null) continue;
            totalPlayers++;

            if (!p.IsAlive) continue;

            if (p.Team == 0) redAlive++;
            else blueAlive++;
        }

        // Chưa có ai trong trận thì chưa xét, tránh kết thúc round oan lúc đang load
        if (totalPlayers == 0) return;

        // Hoà: cả hai đội cùng bị xoá sổ (ví dụ TNT nổ chết cả hai).
        // Xử lý: không ai được điểm, đá sang round mới luôn.
        if (redAlive == 0 && blueAlive == 0)
        {
            Debug.Log("<color=grey><b>=== HOÀ! Cả hai đội cùng bị hạ gục, không ai được điểm ===</b></color>");
            LastRoundWinner = -1;

            // Hoà thì cả hai đội cùng nhận mức thưởng của bên thua
            GiveRoundRewards(-1);

            Phase = GamePhase.RoundEnd;
            PhaseTimer = TickTimer.CreateFromSeconds(Runner, roundEndDuration);
            return;
        }

        if (redAlive == 0) EndRound(1);       // Đỏ hết người -> Xanh thắng
        else if (blueAlive == 0) EndRound(0); // Xanh hết người -> Đỏ thắng
    }

    /// <summary>
    /// Hết giờ pha chiến đấu mà chưa đội nào bị xoá sổ: đội nào TỔNG ĐIỆN TÍCH THẤP HƠN
    /// thì thắng round.
    ///
    /// Vì sao chọn cách này chứ không phải hoà: nó thưởng cho đội chơi hay hơn.
    /// Nhiễm ít điện nghĩa là né giỏi và đánh trúng nhiều - xứng đáng thắng.
    /// Nếu để hoà thì đội đang bị dồn ép sẽ có động cơ chạy vòng quanh câu giờ,
    /// đúng thứ làm hỏng trải nghiệm.
    ///
    /// Người đã bị loại (rơi khỏi đảo) tính là đã nạp ĐẦY điện, nên đội mất người
    /// gần như chắc chắn thua nếu để hết giờ.
    /// </summary>
    private void EndRoundByCharge()
    {
        float redCharge = 0f;
        float blueCharge = 0f;
        int totalPlayers = 0;

        foreach (PlayerHealth p in PlayerHealth.AllPlayers)
        {
            if (p == null) continue;
            totalPlayers++;

            // Đã rơi khỏi đảo thì coi như quá tải hoàn toàn
            float charge = p.IsAlive ? p.CurrentCharge : p.maxCharge;

            if (p.Team == 0) redCharge += charge;
            else blueCharge += charge;
        }

        if (totalPlayers == 0) return;

        Debug.Log($"<color=orange><b>=== HẾT GIỜ! Tổng điện tích - Đỏ: {redCharge:F0} | Xanh: {blueCharge:F0} ===</b></color>");

        // Bằng nhau tuyệt đối thì mới xử hoà. Hiếm, nhưng phải có nhánh này.
        if (Mathf.Approximately(redCharge, blueCharge))
        {
            Debug.Log("<color=grey><b>=== HOÀ! Hai đội cùng mức nhiễm điện ===</b></color>");
            LastRoundWinner = -1;
            GiveRoundRewards(-1);

            Phase = GamePhase.RoundEnd;
            PhaseTimer = TickTimer.CreateFromSeconds(Runner, roundEndDuration);
            return;
        }

        EndRound(redCharge < blueCharge ? 0 : 1);
    }

    // Trả về đội vô địch, hoặc -1 nếu chưa ai đủ điều kiện.
    //
    // Luật: phải đạt đủ số điểm quy định VÀ hơn đối thủ ít nhất 2 round.
    // Ví dụ 5-4 thì CHƯA thắng, phải đấu tiếp tới 6-4 hoặc 7-5 (Overtime).
    private int GetMatchWinner()
    {
        int lead = Mathf.Abs(RedScore - BlueScore);
        if (lead < requiredLead) return -1;

        if (RedScore >= pointsToWin && RedScore > BlueScore) return 0;
        if (BlueScore >= pointsToWin && BlueScore > RedScore) return 1;

        return -1;
    }

    // Map là đảo lơ lửng nên rơi khỏi rìa phải chết, không thì kẹt dưới vực mãi
    private void CheckKillZone()
    {
        foreach (PlayerHealth p in PlayerHealth.AllPlayers)
        {
            if (p == null || !p.IsAlive) continue;

            if (p.transform.position.y < killZoneY)
            {
                Debug.Log($"<color=red>[KILLZONE] Player {p.Object.InputAuthority} rơi khỏi đảo</color>");
                p.Die();
            }
        }
    }

    // --- HỒI SINH ---

    private void RespawnAllPlayers()
    {
        NetworkRunnerHandler handler = NetworkRunnerHandler.Instance;
        if (handler == null)
        {
            Debug.LogError("Không tìm thấy NetworkRunnerHandler để lấy điểm hồi sinh!");
            return;
        }

        // Đếm riêng từng đội để giãn vị trí ra, tránh hai người chồng lên nhau
        int redIndex = 0;
        int blueIndex = 0;

        foreach (PlayerHealth p in PlayerHealth.AllPlayers)
        {
            if (p == null) continue;

            int indexInTeam = p.Team == 0 ? redIndex++ : blueIndex++;

            Vector3 position = handler.GetSpawnPosition(p.Team, indexInTeam);
            float yaw = handler.GetSpawnYaw(p.Team);

            p.Respawn(position, yaw);
        }
    }

    /// <summary>
    /// Trả toàn bộ bản đồ về nguyên trạng đầu trận.
    ///
    /// THỨ TỰ QUAN TRỌNG: phải gỡ vật khỏi tay và khỏi túi TRƯỚC, rồi mới trả vật về chỗ cũ.
    /// Làm ngược lại thì vật vừa về chỗ cũ đã bị bàn tay kéo giật lại ngay khung hình sau,
    /// vì LateUpdate() vẫn thấy GrabbedObjectId còn trỏ vào nó.
    /// </summary>
    private void ResetWorldObjects()
    {
        // 1. Buông vật đang cầm + xoá phần vật thể map trong túi
        foreach (PlayerHealth p in PlayerHealth.AllPlayers)
        {
            if (p == null) continue;

            PlayerMagnetController magnet = p.GetComponent<PlayerMagnetController>();
            if (magnet != null) magnet.ReleaseGrabbedObject();

            InventorySystem inventory = p.GetComponent<InventorySystem>();
            if (inventory != null) inventory.ClearStoredObjects();
        }

        // 2. Giờ mới trả mọi vật thể về đúng chỗ đứng ban đầu
        int count = 0;
        foreach (MagneticObject obj in MagneticObject.AllObjects)
        {
            if (obj == null) continue;

            obj.ResetForNewRound();
            count++;
        }

        Debug.Log($"<color=cyan>[RESET MAP] Đã trả {count} vật thể về chỗ cũ</color>");
    }

    // --- PHẢN ỨNG TRÊN MỌI MÁY ---

    private void OnPhaseChanged()
    {
        Debug.Log($"<color=white>[PHA] Chuyển sang: {Phase}</color>");

        switch (Phase)
        {
            case GamePhase.BuyPhase:
                AudioManager.BuyPhase();
                break;

            case GamePhase.Combat:
                // Tiếng báo rào hạ, xông lên được rồi
                AudioManager.RoundStart();
                break;

            case GamePhase.RoundEnd:
                PlayRoundResultSound();
                break;
        }
    }

    // Thắng hay thua thì kêu khác nhau, xét theo đội của chính người ngồi trước máy này
    private void PlayRoundResultSound()
    {
        if (LastRoundWinner == -1) return; // hoà thì không kêu gì

        PlayerHealth myHealth = FPSMovement.Local != null
            ? FPSMovement.Local.GetComponent<PlayerHealth>()
            : null;

        if (myHealth == null) return;

        if (LastRoundWinner == myHealth.Team) AudioManager.RoundWin();
        else AudioManager.RoundLose();
    }

    // --- HÀM CHO SCRIPT KHÁC HỎI ---

    /// <summary>
    /// Người chơi có được phép đánh nhau lúc này không.
    /// Trong Buy Phase và lúc kết thúc round thì bị khoá.
    /// </summary>
    public bool IsCombatAllowed()
    {
        return Phase == GamePhase.Combat;
    }
}
