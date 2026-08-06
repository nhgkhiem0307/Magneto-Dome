using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    // Danh sách mọi nhân vật đang trong trận, để GameManager đếm quân số còn sống của từng đội.
    // Làm giống kiểu RoomPlayer.AllPlayers cho nhất quán với codebase.
    public static readonly List<PlayerHealth> AllPlayers = new List<PlayerHealth>();

    public float maxHealth = 100f;

    [Tooltip("Lượng giáp cộng thêm mỗi lần mua Shield Armor.")]
    public float armorPerPurchase = 10f;

    [Tooltip("Trần giáp. Mua thêm khi đã đầy thì không có tác dụng gì.")]
    public float maxArmor = 30f;

    [Header("Băng gạc Nano")]
    [Tooltip("Lượng máu tối đa hồi được trong một lần dùng.")]
    public float bandageMaxHeal = 20f;

    [Tooltip("Chỉ hồi được tối đa bấy nhiêu phần lượng máu ĐÃ MẤT. 0.5 = 50%.")]
    public float bandageLostRatio = 0.5f;

    // Giáp chịu sát thương THAY cho máu, và bị xoá sạch mỗi khi sang round mới
    // (theo GDD: reset bất kể còn nguyên hay đã vỡ).
    [Networked, OnChangedRender(nameof(OnArmorChanged))]
    public float CurrentArmor { get; set; }

    // Máu phải là [Networked] để mọi máy cùng thấy một con số.
    //
    // OnChangedRender: Fusion tự gọi hàm OnHealthChanged mỗi khi con số này thay đổi,
    // và gọi trên MỌI máy - cả người bắn lẫn người trúng. Sau này thanh máu trên HUD
    // cũng sẽ móc vào đây thay vì phải kiểm tra mỗi khung hình.
    [Networked, OnChangedRender(nameof(OnHealthChanged))]
    public float CurrentHealth { get; set; }

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

    private CharacterController controller;
    private Renderer[] cachedRenderers;

    // Giá trị ở lần đổi trước, để biết máu/giáp vừa TĂNG hay GIẢM.
    // OnChangedRender chỉ báo "có thay đổi", không cho biết đổi theo chiều nào,
    // mà tiếng trúng đòn thì chỉ được kêu khi mất máu chứ không phải lúc hồi máu.
    private float _lastKnownHealth;
    private float _lastKnownArmor;

    public override void Spawned()
    {
        AllPlayers.Add(this);

        controller = GetComponent<CharacterController>();

        // Lấy sẵn danh sách renderer một lần, khỏi phải đi tìm mỗi lần chết đi sống lại
        cachedRenderers = GetComponentsInChildren<Renderer>();

        if (HasStateAuthority)
        {
            CurrentHealth = maxHealth;
            IsAlive = true;
        }

        // Ghi nhận giá trị khởi đầu, nếu không lần đổi đầu tiên sẽ bị hiểu nhầm
        // là "vừa mất máu" và kêu tiếng trúng đòn oan.
        _lastKnownHealth = CurrentHealth;
        _lastKnownArmor = CurrentArmor;

        ApplyAliveState();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        AllPlayers.Remove(this);
    }

    // --- SÁT THƯƠNG & CHẾT ---

    public void TakeDamage(float amount)
    {
        // Chỉ Host mới được trừ máu (Host Mode - server authoritative).
        //
        // Hàm này được MagneticObject gọi vào, mà MagneticObject chạy trên MỌI máy.
        // Nếu không chặn ở đây thì mỗi máy sẽ trừ máu một lần, mất máu gấp nhiều lần thực tế.
        if (!HasStateAuthority) return;

        // Đã bị loại rồi thì không ăn thêm sát thương nữa
        if (!IsAlive) return;

        // GIÁP CHỊU ĐÒN TRƯỚC. Vỡ hết giáp thì phần thừa mới ăn vào máu.
        if (CurrentArmor > 0f)
        {
            float absorbed = Mathf.Min(CurrentArmor, amount);
            CurrentArmor -= absorbed;
            amount -= absorbed;

            Debug.Log($"<color=#88CCFF>[GIÁP] Chặn được {absorbed}, giáp còn {CurrentArmor}</color>");

            // Giáp đỡ trọn cú này, máu không suy suyển
            if (amount <= 0f) return;
        }

        CurrentHealth -= amount;

        // Chỉ in lượng sát thương ở đây. Con số máu còn lại do OnHealthChanged in ra,
        // vì hàm đó chạy trên mọi máy còn hàm này chỉ chạy trên Host.
        Debug.Log($"<color=yellow>[SÁT THƯƠNG] Player {Object.InputAuthority} trúng đòn -{amount}</color>");

        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }

    // Bị loại khỏi round hiện tại. GameManager sẽ tự phát hiện và kết thúc round
    // khi một đội không còn ai sống.
    public void Die()
    {
        if (!HasStateAuthority) return;
        if (!IsAlive) return;

        CurrentHealth = 0f;
        IsAlive = false;

        string teamName = Team == 0 ? "Đỏ" : "Xanh";
        Debug.Log($"<color=red><b>[LOẠI]</b> Player {Object.InputAuthority} (đội {teamName}) đã bị hạ gục</color>");
    }

    /// <summary>
    /// Băng gạc Nano: hồi tối đa 20 HP, nhưng KHÔNG quá 50% lượng máu đã mất.
    /// Ví dụ mất 20 HP (còn 80) thì chỉ hồi 50% của 20 = 10 HP, thành 90 HP.
    ///
    /// Trả về false khi máu đang đầy, để không nuốt mất món đồ của người chơi.
    /// </summary>
    public bool ApplyBandage()
    {
        if (!HasStateAuthority) return false;
        if (!IsAlive) return false;

        float lostHealth = maxHealth - CurrentHealth;
        if (lostHealth <= 0f) return false; // máu đầy rồi, dùng vô nghĩa

        float healAmount = Mathf.Min(bandageMaxHeal, lostHealth * bandageLostRatio);
        if (healAmount <= 0f) return false;

        CurrentHealth = Mathf.Min(CurrentHealth + healAmount, maxHealth);
        Debug.Log($"<color=lime>[BĂNG GẠC] Hồi {healAmount} HP, máu còn {CurrentHealth}</color>");
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

        CurrentHealth = maxHealth;
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

    // Chạy mỗi khi CurrentHealth đổi giá trị.
    // Mở Console ở cả 2 cửa sổ ParrelSync là đối chiếu được ngay: hai bên cùng một
    // con số thì máu đã đồng bộ đúng.
    private void OnHealthChanged()
    {
        string who = HasInputAuthority
            ? "<color=lime>MÁU CỦA BẠN</color>"
            : $"<color=orange>MÁU ĐỐI PHƯƠNG (Player {Object.InputAuthority})</color>";

        Debug.Log($"[MÁU] {who}: {CurrentHealth} / {maxHealth}");

        // Chỉ kêu khi MẤT máu. Hồi máu bằng băng gạc thì không dùng tiếng này.
        if (CurrentHealth < _lastKnownHealth)
        {
            AudioManager.Hit(transform.position);
        }
        _lastKnownHealth = CurrentHealth;
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
