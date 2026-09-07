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

    [Tooltip("Giới hạn giờ pha chiến đấu. Hết giờ thì đội có TIẾN ĐỘ CHIẾM cao hơn thắng round. " +
             "Vẫn cần dù đã có khu chiếm đóng: hai bên cùng né khu thì round sẽ kéo dài vô tận.")]
    public float combatDuration = 90f;

    [Header("Điều kiện thắng")]
    [Tooltip("Số round cần thắng để vô địch.")]
    public int pointsToWin = 5;
    [Tooltip("Khoảng cách tối thiểu với đối thủ. Không đủ thì vào Overtime, đấu tiếp tới khi đủ.")]
    public int requiredLead = 2;

    [Header("Khu chiếm đóng - điều kiện thắng round")]
    [Tooltip("Tiến độ cần đạt để thắng round. Để 100 cho dễ hiểu là phần trăm.")]
    public float zoneProgressToWin = 100f;

    [Tooltip("Một người đứng trong khu thì tiến độ tăng bao nhiêu mỗi giây. " +
             "10 nghĩa là đứng một mình 10 giây liên tục là thắng round.")]
    public float zoneCaptureRate = 10f;

    [Tooltip("Có thêm người thứ hai của cùng đội thì nhân tốc độ lên bấy nhiêu. " +
             "1.5 chứ không phải 2 - thưởng cho phối hợp nhưng không biến trận đấu thành " +
             "cuộc thi xem ai dồn đủ hai người vào ô trước.")]
    public float zoneTwoPlayerMultiplier = 1.5f;

    [Tooltip("Đội địch chết một người thì đội mình được cộng bao nhiêu tiến độ. " +
             "BẮT BUỘC phải lớn hơn 0: hồi sinh xả sạch điện tích, nên nếu chết mà không " +
             "mất gì thì tự nhảy xuống vực lúc nhiễm nặng sẽ thành nước đi tối ưu.")]
    public float deathZoneBonus = 8f;

    [Tooltip("Chết rồi bao lâu thì sống lại, tính bằng giây.")]
    public float respawnDelay = 5f;

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

    // Tiến độ chiếm khu của từng đội trong round hiện tại, 0 -> zoneProgressToWin.
    //
    // Đặt ở đây chứ không đặt trên ControlZone, vì GameManager vốn đã là NetworkObject.
    // Thêm một NetworkObject đặt sẵn trong scene là thêm một chỗ có thể hỏng.
    [Networked] public float RedZoneProgress { get; set; }
    [Networked] public float BlueZoneProgress { get; set; }
    [Networked] public int CurrentRound { get; set; }

    // Đội thắng round vừa rồi: 0 = Đỏ, 1 = Xanh, -1 = chưa có
    [Networked] public int LastRoundWinner { get; set; }

    // Đội vô địch: 0 = Đỏ, 1 = Xanh, -1 = chưa xong
    [Networked] public int MatchWinner { get; set; }

    /// <summary>
    /// Trận bị HUỶ giữa chừng vì có người thoát, chứ không phải kết thúc bình thường.
    ///
    /// Cần một cờ riêng chứ không mượn MatchWinner = -1, vì HUD phải hiện hai câu khác
    /// hẳn nhau: "RED TEAM WINS!" và "MATCH CANCELLED". Mượn chung một ô thì không phân
    /// biệt được "chưa ai thắng" với "trận hỏng".
    /// </summary>
    [Networked] public NetworkBool MatchAbandoned { get; set; }

    // Số người chơi lúc trận bắt đầu. Tụt xuống dưới mức này nghĩa là có người thoát.
    //
    // Vì sao đếm mốc đầu thay vì so quân số hai đội mỗi tick: lúc mới vào trận, các nhân
    // vật spawn lần lượt chứ không cùng một khung hình, nên có vài tick đội này đông hơn
    // đội kia. So trực tiếp sẽ huỷ trận ngay khi vừa bắt đầu.
    [Networked] private int ExpectedPlayerCount { get; set; }

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
            MatchAbandoned = false;
            ExpectedPlayerCount = 0; // chốt lại ở StartNewRound() của round đầu tiên

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

        // CÓ NGƯỜI THOÁT GIỮA TRẬN -> huỷ trận. Thêm 01/09.
        //
        // Xét trước mọi thứ khác: không có lý do gì chạy tiếp một round mà quân số đã lệch.
        if (CheckForAbandonedMatch()) return;

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
                // Người chết được sống lại sau respawnDelay giây. Chạy TRƯỚC mọi thứ khác
                // để người vừa hết giờ chờ được tính là đang sống ngay trong tick này.
                CheckRespawns();

                // Hết giờ -> đội nào chiếm được nhiều hơn thì thắng.
                // Xét TRƯỚC phần tích tiến độ, để hết giờ là chốt ngay không chờ tick sau.
                if (PhaseTimer.Expired(Runner))
                {
                    EndRoundByProgress();
                    break;
                }

                UpdateZoneCapture();
                CheckZoneVictory();
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

        // CHỐT MỐC QUÂN SỐ ở round ĐẦU TIÊN.
        //
        // Chốt ở đây chứ không phải trong Spawned(): lúc GameManager sinh ra thì nhân vật
        // chưa spawn xong (xem thứ tự trong NetworkRunnerHandler.OnSceneLoadDone), đếm lúc
        // đó sẽ ra 0. Tới round đầu tiên thì warmupDuration đã trôi qua và ai cũng có mặt.
        //
        // Chỉ chốt MỘT LẦN. Chốt lại mỗi round thì người thoát ở round 3 sẽ thành mốc mới
        // của round 4, và trận cứ thế đánh tiếp với quân số lệch - đúng thứ đang muốn chặn.
        if (ExpectedPlayerCount <= 0)
        {
            ExpectedPlayerCount = PlayerHealth.AllPlayers.Count;
            Debug.Log($"<color=cyan>[TRẬN] Chốt quân số: {ExpectedPlayerCount} người.</color>");
        }

        // Xoá tiến độ chiếm của round trước. Không có dòng này thì round 2 bắt đầu
        // với thanh đã gần đầy sẵn và kết thúc trong vài giây.
        RedZoneProgress = 0f;
        BlueZoneProgress = 0f;

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

    /// <summary>
    /// Hồi sinh những người đã hết giờ chờ.
    ///
    /// Từ 16/08 chết KHÔNG còn là bị loại hết round. Cái chết giờ chỉ lấy đi hai thứ:
    /// thời gian (respawnDelay giây) và vị trí (bị đẩy về điểm xuất phát). Nhờ vậy
    /// lực văng mạnh tay không còn gây ức chế - bị hất khỏi đảo là mất nhịp, không phải mất round.
    /// </summary>
    private void CheckRespawns()
    {
        NetworkRunnerHandler handler = NetworkRunnerHandler.Instance;
        if (handler == null) return;

        foreach (PlayerHealth p in PlayerHealth.AllPlayers)
        {
            if (p == null || p.IsAlive) continue;
            if (!p.RespawnTimer.Expired(Runner)) continue;

            p.RespawnTimer = TickTimer.None;

            // Dùng index 0: ở 2v2 hai đồng đội hiếm khi chết cùng lúc nên không lo chồng nhau.
            p.Respawn(handler.GetSpawnPosition(p.Team, 0), handler.GetSpawnYaw(p.Team));

            Debug.Log($"<color=lime>[HỒI SINH] Player {p.Object.InputAuthority} đã trở lại</color>");
        }
    }

    /// <summary>
    /// Cộng tiến độ cho đội đang giữ khu chiếm đóng.
    ///
    /// LUẬT: chỉ cộng khi MỘT ĐỘI DUY NHẤT có người trong khu. Hai đội cùng đứng thì
    /// đóng băng - không ai tiến. Tranh chấp phải giải quyết bằng cách đẩy đối phương ra,
    /// mà đẩy chính là cơ chế cốt lõi của game này. Mục tiêu và cách chơi khớp làm một.
    ///
    /// Tiến độ KHÔNG tự tụt khi mất khu. Bỏ cơ chế tụt vì nó kéo dài round và gây ức chế,
    /// mà đồng hồ combatDuration đã đủ để chặn round lê thê rồi.
    /// </summary>
    private void UpdateZoneCapture()
    {
        ControlZone zone = ControlZone.Instance;
        if (zone == null) return;

        zone.CountPlayersInside(out int redInside, out int blueInside);

        // Không ai, hoặc cả hai đội cùng có mặt -> đóng băng
        if (redInside == 0 && blueInside == 0) return;
        if (redInside > 0 && blueInside > 0) return;

        int holders = redInside > 0 ? redInside : blueInside;

        // Người thứ hai cộng thêm tốc độ nhưng không gấp đôi - thưởng phối hợp,
        // không biến trận đấu thành cuộc thi dồn đủ hai người vào ô.
        float rate = zoneCaptureRate * (holders >= 2 ? zoneTwoPlayerMultiplier : 1f);
        float gain = rate * Runner.DeltaTime;

        if (redInside > 0) RedZoneProgress = Mathf.Min(RedZoneProgress + gain, zoneProgressToWin);
        else BlueZoneProgress = Mathf.Min(BlueZoneProgress + gain, zoneProgressToWin);
    }

    /// <summary>Đội nào chạm mốc trước thì thắng round ngay lập tức.</summary>
    private void CheckZoneVictory()
    {
        if (RedZoneProgress >= zoneProgressToWin) EndRound(0);
        else if (BlueZoneProgress >= zoneProgressToWin) EndRound(1);
    }

    /// <summary>
    /// Hết giờ pha chiến đấu: đội nào tiến độ chiếm cao hơn thì thắng round.
    ///
    /// Thay cho EndRoundByCharge() cũ (so tổng điện tích). Cách cũ vốn là giải pháp
    /// tình thế cho việc "không ai chết thì round không bao giờ kết thúc"; giờ đã có
    /// khu chiếm đóng làm đường kết thúc tự nhiên nên bỏ đi được.
    /// </summary>
    private void EndRoundByProgress()
    {
        Debug.Log($"<color=orange><b>=== HẾT GIỜ! Tiến độ chiếm - Đỏ: {RedZoneProgress:F0} | Xanh: {BlueZoneProgress:F0} ===</b></color>");

        if (Mathf.Approximately(RedZoneProgress, BlueZoneProgress))
        {
            Debug.Log("<color=grey><b>=== HOÀ! Hai đội cùng tiến độ ===</b></color>");
            LastRoundWinner = -1;
            GiveRoundRewards(-1);

            Phase = GamePhase.RoundEnd;
            PhaseTimer = TickTimer.CreateFromSeconds(Runner, roundEndDuration);
            return;
        }

        EndRound(RedZoneProgress > BlueZoneProgress ? 0 : 1);
    }

    /// <summary>
    /// PlayerHealth.Die() gọi vào đây mỗi khi có người bị hạ.
    ///
    /// Hai việc: hẹn giờ hồi sinh, và cộng tiến độ chiếm cho ĐỘI ĐỊCH.
    ///
    /// Phần cộng tiến độ là bắt buộc, không phải trang trí. Respawn() xả sạch điện tích,
    /// nên nếu chết mà không mất gì thì người đang nhiễm 95% sẽ tự nhảy xuống vực để
    /// được "hồi máu" miễn phí. Cho địch điểm mỗi lần mình chết là chặn đứng lối chơi đó.
    ///
    /// Nó còn tạo ra sự cộng hưởng: hất địch khỏi đảo TRỰC TIẾP đẩy mình tới chiến thắng,
    /// nên knockback và mục tiêu round trở thành cùng một thứ.
    /// </summary>
    public void OnPlayerDied(PlayerHealth victim)
    {
        if (!HasStateAuthority) return;
        if (victim == null) return;

        victim.RespawnTimer = TickTimer.CreateFromSeconds(Runner, respawnDelay);

        // Chỉ cộng tiến độ trong pha chiến đấu. Chết lúc đang mua đồ hoặc lúc hết round
        // (ví dụ do phím debug) thì không được tính.
        if (Phase != GamePhase.Combat) return;
        if (deathZoneBonus <= 0f) return;

        if (victim.Team == 0)
        {
            BlueZoneProgress = Mathf.Min(BlueZoneProgress + deathZoneBonus, zoneProgressToWin);
        }
        else
        {
            RedZoneProgress = Mathf.Min(RedZoneProgress + deathZoneBonus, zoneProgressToWin);
        }

        Debug.Log($"<color=#FFAA00>[TIẾN ĐỘ] Địch hạ được 1 người -> +{deathZoneBonus}. " +
                  $"Đỏ {RedZoneProgress:F0} | Xanh {BlueZoneProgress:F0}</color>");
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

    /// <summary>
    /// Phát hiện có người thoát giữa trận và huỷ trận. Trả về true nếu vừa huỷ.
    ///
    /// VÌ SAO HUỶ CHỨ KHÔNG ĐÁNH TIẾP 2v1:
    ///
    /// Điều kiện thắng round của game này là TIẾN ĐỘ CHIẾM KHU, mà khu được quyết định
    /// bằng số người đứng trong đó. Đội 2 người chỉ cần một người giữ khu, một người quấy
    /// rối là thắng chắc - người còn lại bên kia không có cách nào xoay chuyển.
    ///
    /// Đền bù bằng tiền KHÔNG cứu được: tiền mua vật phẩm tiêu hao, không mua được một
    /// người thứ hai để tranh khu. Đền bù kinh tế chỉ hợp với game mà tiền đổi thẳng ra
    /// sức mạnh (như CS: tiền -> súng tốt hơn).
    ///
    /// Quyết định này nhất quán với việc CHẶN người vào giữa trận: cả hai cùng nói
    /// "2v2 là thể thức cố định, không đủ người thì không đấu".
    /// </summary>
    private bool CheckForAbandonedMatch()
    {
        // Trận đã kết thúc rồi thì thôi, không huỷ chồng lên nữa.
        if (Phase == GamePhase.MatchEnd || Phase == GamePhase.WaitingToStart) return false;

        // Chưa chốt mốc quân số thì chưa có gì để so.
        if (ExpectedPlayerCount <= 0) return false;

        if (PlayerHealth.AllPlayers.Count >= ExpectedPlayerCount) return false;

        Debug.LogWarning($"<color=orange>[TRẬN] Có người thoát " +
                         $"({PlayerHealth.AllPlayers.Count}/{ExpectedPlayerCount}) -> huỷ trận.</color>");

        MatchAbandoned = true;
        MatchWinner = -1;   // không ai thắng
        Phase = GamePhase.MatchEnd;

        // Dùng đúng đồng hồ của MatchEnd: người chơi có vài giây đọc thông báo rồi mới
        // bị đưa về menu. Không cắt thẳng về menu - đang đánh nhau mà màn hình nhảy phắt
        // sang menu thì không ai hiểu chuyện gì vừa xảy ra.
        PhaseTimer = TickTimer.CreateFromSeconds(Runner, matchEndDuration);

        return true;
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
