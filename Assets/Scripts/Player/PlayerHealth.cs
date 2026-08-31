using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    // Danh sách mọi nhân vật đang trong trận, để GameManager đếm quân số còn sống của từng đội.
    // Làm giống kiểu RoomPlayer.AllPlayers cho nhất quán với codebase.
    public static readonly List<PlayerHealth> AllPlayers = new List<PlayerHealth>();

    // ==================== CHẾ ĐỘ QUÁ TẢI ====================
    //
    // Game này KHÔNG CÓ THANH MÁU. Trúng đòn không làm bạn mất máu - nó làm bạn
    // NHIỄM ĐIỆN. Càng nhiễm nhiều, từ trường tác động lên bạn càng mạnh, nên cùng
    // một cú đấm sẽ hất bạn đi càng xa.
    //
    // Cái chết DUY NHẤT trong game là rơi khỏi đảo (GameManager.CheckKillZone).
    // Điện tích đầy 100% KHÔNG giết bạn - nó chỉ khiến bạn nhẹ như tờ giấy.
    //
    // Vì sao thiết kế vậy: nếu đầy điện là chết thì đây chỉ là thanh máu chạy ngược,
    // không có gì mới. Để cái chết đến từ VỊ TRÍ mới tạo ra được sự căng thẳng thật:
    // đứng giữa sân với 90% điện vẫn an toàn, đứng sát rìa với 30% đã là mạo hiểm.

    [Header("Quá Tải - thay cho thanh máu")]
    [Tooltip("Trần điện tích. Chạm trần KHÔNG chết, đây chỉ là mức bị văng xa nhất.")]
    public float maxCharge = 100f;

    [Tooltip("Hệ số lực văng khi điện tích ĐẦY. 5 = bị hất xa gấp 5 lần lúc sạch điện.")]
    public float knockbackAtMaxCharge = 5f;

    [Tooltip("Nhân TOÀN BỘ lượng điện nhận vào. Đặt ở đây - một chỗ duy nhất - thay vì sửa " +
             "baseDamage của từng prefab, nên chỉnh một con số là cả game đổi theo: đạn, " +
             "nổ TNT, và mọi nguồn sát thương thêm vào sau này.")]
    public float chargeGainMultiplier = 1.5f;

    [Header("Cảm giác khi ăn đòn")]
    [Tooltip("Ăn một đòn nặng bấy nhiêu điểm điện thì choáng ở mức tối đa.\n\n" +
             "Đòn nhẹ hơn thì choáng ít hơn theo tỉ lệ. Con số này tính SAU khi đã nhân " +
             "Charge Gain Multiplier, nên đối chiếu: đạn thường ~15, Heavy ~30, TNT ~52. " +
             "Để 35 nghĩa là trúng TNT choáng kịch khung, trúng đạn thường choáng nhẹ.")]
    public float hitChargeForFullEffect = 35f;

    [Tooltip("Mức choáng tối thiểu của MỘT đòn bất kỳ, thang 0..1.\n\n" +
             "Cần sàn này vì đòn sượt qua chỉ nạp vài điểm điện, tính theo tỉ lệ thì ra " +
             "gần như bằng 0 và người chơi không nhận được phản hồi gì cả — trúng đòn mà " +
             "màn hình im lìm thì tưởng là lỗi.")]
    public float hitMinStrength = 0.3f;

    [Tooltip("Phần XÓC sắc nét đi kèm lúc vừa trúng, thang 0..1. Nhân với độ nặng của đòn.\n\n" +
             "Váng đầu một mình thì khởi đầu quá êm, không ra được khoảnh khắc 'bị nện'. " +
             "Xóc lo phần đầu, váng đầu lo phần dư âm.")]
    public float hitShakeTrauma = 0.55f;

    [Header("Giáp Cách Điện")]
    [Tooltip("Lượng giáp cộng thêm mỗi lần mua Shield Armor.")]
    public float armorPerPurchase = 10f;

    [Tooltip("Trần giáp. Mua thêm khi đã đầy thì không có tác dụng gì.")]
    public float maxArmor = 30f;

    [Header("Băng gạc Nano - nay là Bộ Xả Điện")]
    [Tooltip("Lượng điện tích tối đa xả được trong một lần dùng.")]
    public float bandageMaxDischarge = 20f;

    [Tooltip("Chỉ xả được tối đa bấy nhiêu phần điện tích ĐANG CÓ. 0.5 = 50%.")]
    public float bandageChargeRatio = 0.5f;

    // Giáp chịu sát thương THAY cho máu, và bị xoá sạch mỗi khi sang round mới
    // (theo GDD: reset bất kể còn nguyên hay đã vỡ).
    [Networked, OnChangedRender(nameof(OnArmorChanged))]
    public float CurrentArmor { get; set; }

    // Điện tích tích luỹ. Phải là [Networked] để mọi máy cùng thấy một con số -
    // nếu không thì Host tính lực văng một kiểu, Client thấy một kiểu.
    //
    // OnChangedRender: Fusion tự gọi OnChargeChanged mỗi khi con số này đổi,
    // và gọi trên MỌI máy - cả người bắn lẫn người trúng.
    [Networked, OnChangedRender(nameof(OnChargeChanged))]
    public float CurrentCharge { get; set; }

    /// <summary>Mức nhiễm điện quy về 0..1. HUD dùng cái này cho thanh đo.</summary>
    public float ChargeRatio => maxCharge > 0f ? Mathf.Clamp01(CurrentCharge / maxCharge) : 0f;

    /// <summary>
    /// Hệ số nhân lực văng theo mức nhiễm điện: sạch điện = 1.0, đầy điện = knockbackAtMaxCharge.
    ///
    /// Đây là TOÀN BỘ cơ chế của chế độ Quá Tải, gói trong một dòng.
    /// FPSMovement.AddImpact() đọc giá trị này mỗi lần bạn hứng một cú đẩy.
    /// </summary>
    public float KnockbackMultiplier => Mathf.Lerp(1f, knockbackAtMaxCharge, ChargeRatio);

    // 0 = Đỏ, 1 = Xanh. Host gán lúc spawn, dựa theo đội đã chọn trong phòng chờ.
    [Networked] public int Team { get; set; }

    // Còn sống trong round này hay đã bị loại.
    // Chết KHÔNG despawn nhân vật, chỉ "tắt" nó đi - xem OnAliveChanged.
    [Networked, OnChangedRender(nameof(OnAliveChanged))]
    public NetworkBool IsAlive { get; set; }

    // Đếm số lần hồi sinh. Bản thân con số không có ý nghĩa gì,
    // nó chỉ tồn tại để mỗi lần hồi sinh là giá trị đổi -> OnChangedRender được kích hoạt
    // trên mọi máy. Nếu móc vào góc xoay thì cùng một đội sẽ luôn cùng một góc,
    // giá trị không đổi nên Fusion sẽ không gọi hàm.
    [Networked, OnChangedRender(nameof(OnRespawned))]
    public int RespawnCount { get; set; }

    // Đếm ngược tới lúc được sống lại. Chỉ chạy khi đã bị loại giữa pha chiến đấu.
    //
    // Từ 16/08 chết KHÔNG còn là bị loại hết round nữa - chỉ mất vị trí và mất thời gian.
    // GameManager đặt đồng hồ này và cũng là nơi kiểm nó hết hạn để hồi sinh.
    [Networked] public TickTimer RespawnTimer { get; set; }

    /// <summary>Đang nằm chờ hồi sinh hay không. HUD dùng để hiện đồng hồ đếm ngược.</summary>
    public bool IsWaitingToRespawn => !IsAlive && RespawnTimer.IsRunning;

    /// <summary>Số giây còn lại trước khi sống lại, 0 nếu không trong trạng thái chờ.</summary>
    public float RespawnSecondsLeft => RespawnTimer.RemainingTime(Runner) ?? 0f;

    private CharacterController controller;
    private Renderer[] cachedRenderers;

    // Lấy sẵn để khỏi GetComponent mỗi lần trúng đòn. Dùng để rung camera khi ăn đòn.
    private FPSMovement movement;

    // Giá trị ở lần đổi trước, để biết điện tích/giáp vừa TĂNG hay GIẢM.
    // OnChangedRender chỉ báo "có thay đổi", không cho biết đổi theo chiều nào,
    // mà tiếng trúng đòn thì chỉ được kêu khi NHIỄM THÊM điện, không phải lúc xả điện.
    private float _lastKnownCharge;
    private float _lastKnownArmor;

    public override void Spawned()
    {
        AllPlayers.Add(this);

        controller = GetComponent<CharacterController>();
        movement = GetComponent<FPSMovement>();

        // Lấy sẵn danh sách renderer một lần, khỏi phải đi tìm mỗi lần chết đi sống lại
        cachedRenderers = GetComponentsInChildren<Renderer>();

        if (HasStateAuthority)
        {
            CurrentCharge = 0f; // vào trận là sạch điện, nặng và khó bị đẩy nhất
            IsAlive = true;
        }

        // Ghi nhận giá trị khởi đầu, nếu không lần đổi đầu tiên sẽ bị hiểu nhầm
        // là "vừa nhiễm điện" và kêu tiếng trúng đòn oan.
        _lastKnownCharge = CurrentCharge;
        _lastKnownArmor = CurrentArmor;

        ApplyAliveState();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        AllPlayers.Remove(this);
    }

    // --- SÁT THƯƠNG & CHẾT ---

    /// <summary>
    /// Hứng một đòn. Trong chế độ Quá Tải, đòn đánh KHÔNG trừ máu mà CỘNG ĐIỆN TÍCH.
    ///
    /// Tên hàm giữ nguyên là TakeDamage để MagneticObject, vụ nổ TNT và mọi chỗ khác
    /// không phải sửa gì cả - chúng vẫn nói "gây 20 sát thương", chỉ có ý nghĩa của
    /// con số 20 là đổi: giờ nó là "nạp thêm 20 điểm điện".
    /// </summary>
    public void TakeDamage(float amount)
    {
        // Chỉ Host mới được đổi điện tích (Host Mode - server authoritative).
        //
        // Hàm này được MagneticObject gọi vào, mà MagneticObject chạy trên MỌI máy.
        // Nếu không chặn ở đây thì mỗi máy nạp một lần, điện tăng gấp nhiều lần thực tế.
        if (!HasStateAuthority) return;

        // Đã bị loại rồi thì không nhiễm thêm nữa
        if (!IsAlive) return;

        // Nhân hệ số NGAY TỪ ĐẦU, trước cả giáp. Nghĩa là đòn mạnh hơn thì giáp cũng
        // phải gánh nhiều hơn, thay vì giáp chặn được y như cũ rồi mới nhân phần thừa.
        amount *= chargeGainMultiplier;

        // GIÁP CÁCH ĐIỆN CHỊU TRƯỚC. Nó hấp thụ điện thay cho cơ thể,
        // hỏng dần cho tới khi hết. Phần thừa mới ngấm vào người.
        if (CurrentArmor > 0f)
        {
            float absorbed = Mathf.Min(CurrentArmor, amount);
            CurrentArmor -= absorbed;
            amount -= absorbed;

            Debug.Log($"<color=#88CCFF>[GIÁP] Cách điện được {absorbed}, giáp còn {CurrentArmor}</color>");

            // Giáp nuốt trọn cú này, người không nhiễm thêm tí nào
            if (amount <= 0f) return;
        }

        // Chạm trần thì DỪNG LẠI, không chết.
        //
        // Đây là điểm khác biệt cốt lõi so với thanh máu: đầy điện không phải là
        // thua cuộc, nó chỉ có nghĩa "một cú chạm nữa là bạn bay khỏi bản đồ".
        CurrentCharge = Mathf.Min(CurrentCharge + amount, maxCharge);

        Debug.Log($"<color=yellow>[NHIỄM ĐIỆN] Player {Object.InputAuthority} +{amount} " +
                  $"-> lực văng hiện tại x{KnockbackMultiplier:F1}</color>");
    }

    // Bị loại khỏi round hiện tại. GameManager sẽ tự phát hiện và kết thúc round
    // khi một đội không còn ai sống.
    //
    // Trong chế độ Quá Tải, hàm này gần như CHỈ được gọi từ GameManager.CheckKillZone()
    // - tức là khi rơi khỏi đảo. Không còn đường chết nào khác.
    public void Die()
    {
        if (!HasStateAuthority) return;
        if (!IsAlive) return;

        IsAlive = false;

        string teamName = Team == 0 ? "Đỏ" : "Xanh";
        Debug.Log($"<color=red><b>[LOẠI]</b> Player {Object.InputAuthority} (đội {teamName}) đã bị hạ gục</color>");

        // Báo cho GameManager để nó hẹn giờ hồi sinh VÀ cộng tiến độ chiếm cho đội địch.
        //
        // Gọi từ đây chứ không gọi ở chỗ CheckKillZone, vì cái chết còn đến từ đường khác
        // (phím tự sát lúc test, và sau này có thể thêm nguồn mới). Đặt ở Die() thì mọi
        // đường chết đều đi qua đúng một chỗ xử lý.
        if (GameManager.Instance != null) GameManager.Instance.OnPlayerDied(this);
    }

    /// <summary>
    /// Bộ Xả Điện (trước là Băng gạc Nano): xả tối đa 20 điểm điện, nhưng KHÔNG quá
    /// 50% lượng điện đang mang. Ví dụ đang nhiễm 20 thì chỉ xả được 50% của 20 = 10.
    ///
    /// Giữ nguyên công thức "giảm dần hiệu quả" của băng gạc cũ: càng nguy kịch thì
    /// một món đồ càng cứu được ít, nên không thể dựa vào nó để lì đòn vô hạn.
    ///
    /// Trả về false khi đang sạch điện, để không nuốt mất món đồ của người chơi.
    /// </summary>
    public bool ApplyBandage()
    {
        if (!HasStateAuthority) return false;
        if (!IsAlive) return false;

        if (CurrentCharge <= 0f) return false; // sạch điện rồi, dùng vô nghĩa

        float dischargeAmount = Mathf.Min(bandageMaxDischarge, CurrentCharge * bandageChargeRatio);
        if (dischargeAmount <= 0f) return false;

        CurrentCharge = Mathf.Max(CurrentCharge - dischargeAmount, 0f);
        Debug.Log($"<color=lime>[XẢ ĐIỆN] Xả {dischargeAmount}, điện tích còn {CurrentCharge}</color>");
        return true;
    }

    /// <summary>
    /// Cộng giáp khi mua Shield Armor. Trả về false nếu giáp đã đầy,
    /// để Shop không trừ tiền oan.
    /// </summary>
    public bool AddArmor()
    {
        if (!HasStateAuthority) return false;
        if (CurrentArmor >= maxArmor) return false;

        CurrentArmor = Mathf.Min(CurrentArmor + armorPerPurchase, maxArmor);
        return true;
    }

    // Hồi sinh đầu round mới. Chỉ GameManager gọi vào.
    public void Respawn(Vector3 position, float yaw)
    {
        if (!HasStateAuthority) return;

        // Sang round mới là xả sạch điện, ai cũng về mức nặng nhất, khó đẩy nhất
        CurrentCharge = 0f;
        IsAlive = true;

        // Theo GDD: hết round là giáp bị xoá sạch, bất kể còn nguyên hay đã vỡ.
        // Muốn có giáp ở round sau thì phải mua lại.
        CurrentArmor = 0f;

        // Tắt CharacterController trước khi dịch chuyển, nếu không nó sẽ kéo nhân vật
        // về chỗ cũ. Đúng cái bẫy đã gặp hồi spawn lần đầu.
        if (controller != null) controller.enabled = false;
        transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        if (controller != null) controller.enabled = true;

        FPSMovement movement = GetComponent<FPSMovement>();
        if (movement != null)
        {
            // Báo hướng nhìn mới, để máy của chính người chơi đặt lại góc camera cho khớp.
            movement.SpawnYaw = yaw;

            // Hiệu lực Nước Tăng Lực bị xoá khi sang round mới, giống như giáp.
            movement.ClearEnergyDrink();
        }

        // Chai xăng đang cầm trên tay cũng mất theo round.
        // Số chai còn trong TÚI thì vẫn giữ - chỉ cái đang cầm sẵn mới bị bỏ.
        PlayerMagnetController magnet = GetComponent<PlayerMagnetController>();
        if (magnet != null) magnet.HasGasolineEquipped = false;

        // Đổi giá trị này để OnRespawned được gọi trên mọi máy
        RespawnCount++;
    }

    // --- CÁC HÀM PHẢN ỨNG, CHẠY TRÊN MỌI MÁY ---

    // Chạy mỗi khi CurrentCharge đổi giá trị.
    // Mở Console ở cả 2 cửa sổ ParrelSync là đối chiếu được ngay: hai bên cùng một
    // con số thì điện tích đã đồng bộ đúng.
    private void OnChargeChanged()
    {
        string who = HasInputAuthority
            ? "<color=lime>ĐIỆN TÍCH CỦA BẠN</color>"
            : $"<color=orange>ĐIỆN TÍCH ĐỐI PHƯƠNG (Player {Object.InputAuthority})</color>";

        Debug.Log($"[QUÁ TẢI] {who}: {CurrentCharge:F0} / {maxCharge} (x{KnockbackMultiplier:F1})");

        // Chỉ kêu khi NHIỄM THÊM điện. Xả điện bằng vật phẩm thì không dùng tiếng này.
        if (CurrentCharge > _lastKnownCharge)
        {
            AudioManager.Hit(transform.position);

            // Choáng theo đúng lượng điện vừa nạp vào, không phải một mức cố định.
            PlayHitFeedback(CurrentCharge - _lastKnownCharge);
        }
        _lastKnownCharge = CurrentCharge;
    }

    /// <summary>
    /// Làm người chơi này choáng: xóc một cái rồi lảo đảo, kèm mờ màn hình.
    ///
    /// Gọi từ HAI chỗ — điện tích tăng, và giáp vơi đi. Phải có cả hai vì khi còn giáp
    /// thì đòn đánh KHÔNG làm điện tích tăng chút nào; chỉ móc vào điện tích thì mặc giáp
    /// vào là mọi cú trúng đòn trở nên im lìm, người chơi tưởng mình chưa bị bắn trúng.
    ///
    /// Hàm này chạy trên MỌI máy (vì OnChangedRender là vậy), nhưng FPSMovement.HitCamera
    /// và ShakeCamera đều lọc bằng HasInputAuthority, nên chỉ màn hình của chính người
    /// trúng đòn mới choáng. Không cần thêm điều kiện ở đây.
    /// </summary>
    /// <param name="amount">Lượng điện vừa nạp, hoặc lượng giáp vừa mất.</param>
    private void PlayHitFeedback(float amount)
    {
        if (movement == null || amount <= 0f) return;

        // Quy độ nặng của đòn về thang 0..1, có sàn để đòn sượt vẫn có phản hồi.
        float strength = hitChargeForFullEffect > 0f
            ? Mathf.Clamp01(amount / hitChargeForFullEffect)
            : 1f;
        strength = Mathf.Max(strength, hitMinStrength);

        // Xóc trước (khoảnh khắc va chạm), váng đầu sau (dư âm). Hai lời gọi này cố ý
        // tách rời chứ không gộp - xem phần đầu CameraShake.cs về lý do ba hệ thống rung
        // phải độc lập với nhau.
        movement.ShakeCamera(hitShakeTrauma * strength);
        movement.HitCamera(strength);
    }

    private void OnArmorChanged()
    {
        if (HasInputAuthority)
        {
            Debug.Log($"<color=#88CCFF>[GIÁP] Giáp của bạn: {CurrentArmor} / {maxArmor}</color>");
        }

        // Giáp VƠI ĐI nghĩa là vừa chặn được một đòn -> tiếng kim loại.
        // Giáp tăng lên là do mua, không dùng tiếng này.
        if (CurrentArmor < _lastKnownArmor)
        {
            AudioManager.ArmorHit(transform.position);

            // Giáp chặn được điện tích, KHÔNG chặn được cú va đập. Vẫn phải choáng.
            PlayHitFeedback(_lastKnownArmor - CurrentArmor);
        }
        _lastKnownArmor = CurrentArmor;
    }

    private void OnAliveChanged()
    {
        ApplyAliveState();

        if (!IsAlive)
        {
            AudioManager.Death(transform.position);
        }
    }

    private void OnRespawned()
    {
        // Chỉ máy của chính người chơi mới cần đặt lại góc nhìn.
        // Người khác hồi sinh thì máy mình không phải làm gì cả.
        if (!HasInputAuthority) return;

        FPSMovement movement = GetComponent<FPSMovement>();
        if (movement == null) return;

        NetworkRunnerHandler.SetLookAngles(movement.SpawnYaw, 0f);
    }

    // "Tắt" hoặc "bật" nhân vật.
    //
    // CỐ Ý KHÔNG dùng gameObject.SetActive(false) khi chết. Tắt hẳn một NetworkObject
    // sẽ làm hỏng vòng đời mô phỏng của Fusion. Thay vào đó chỉ tắt phần nhìn thấy
    // và phần va chạm, còn object vẫn sống bình thường - nhờ vậy camera của người chết
    // vẫn hoạt động để họ xem tiếp trận đấu.
    private void ApplyAliveState()
    {
        bool alive = IsAlive;

        if (cachedRenderers != null)
        {
            foreach (Renderer r in cachedRenderers)
            {
                if (r != null) r.enabled = alive;
            }
        }

        // Chết rồi thì không cản đường ai, cũng không hứng đạn nữa
        if (controller != null) controller.detectCollisions = alive;
    }
}
