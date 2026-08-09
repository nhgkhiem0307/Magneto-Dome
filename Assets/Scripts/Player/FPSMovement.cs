using Fusion;
using UnityEngine;

public class FPSMovement : NetworkBehaviour, IBeforeAllTicks
{
    // Nhân vật của CHÍNH người chơi đang ngồi trước máy này.
    // NetworkRunnerHandler cần nó để biết đọc độ nhạy chuột và phím Dash từ đâu.
    public static FPSMovement Local { get; private set; }

    [Header("Keybinds")]
    public KeyCode dashKey = KeyCode.Q; // Dễ dàng đổi phím Dash trên Inspector (Q, LeftShift, E, Mouse0, v.v.)

    [Header("Movement Settings")]
    public float moveSpeed = 13f;

    [Tooltip("Chỉ là giá trị MẶC ĐỊNH cho lần chơi đầu. Sau đó người chơi tự chỉnh trong Settings, " +
             "và giá trị thật được đọc từ GameSettings.MouseSensitivity.")]
    public float mouseSensitivity = 1f;
    public Transform cameraTransform;

    [Header("Dash Settings")]
    public float dashForce = 100f;      // Độ mạnh cú lướt
    public float dashCooldown = 1f;    // Thời gian hồi chiêu Dash (giây)

    [Header("Nước Tăng Lực")]
    [Tooltip("Hiệu lực kéo dài bao nhiêu giây sau khi uống.")]
    public float energyDrinkDuration = 15f;

    [Tooltip("Giảm bao nhiêu phần thời gian hồi chiêu Dash. 0.15 = giảm 15%.")]
    public float energyDrinkCooldownReduction = 0.15f;

    [Header("Gravity & Physics")]
    public float gravity = -19.62f;    // Trọng lực
    public float mass = 3f;
    public float drag = 5f;             // Ma sát giảm tốc khi Dash

    // --- TRẠNG THÁI ĐƯỢC FUSION ĐỒNG BỘ ---
    // Những biến này trước đây là biến thường. Giờ phải đánh dấu [Networked] để Host
    // và các máy Client cùng thấy một giá trị giống nhau, và để Fusion tua lại
    // (resimulation) chính xác khi mạng bị trễ.
    [Networked] private Vector3 NetVelocity { get; set; }
    [Networked] private Vector3 NetImpact { get; set; }
    [Networked] private float NetDashTimer { get; set; }

    // Góc ngẩng/cúi. Cần đồng bộ để người chơi khác nhìn thấy mình đang ngắm lên hay xuống.
    [Networked] private float NetPitch { get; set; }

    // Trạng thái nút bấm ở tick TRƯỚC. Dùng để phát hiện khoảnh khắc "vừa bấm xuống".
    // Bắt buộc phải [Networked], nếu để biến thường thì khi Fusion tua lại sẽ tính sai.
    [Networked] private NetworkButtons PreviousButtons { get; set; }

    // Đếm số lần đã dash. Bản thân con số vô nghĩa - nó chỉ tồn tại để mỗi cú dash
    // là giá trị đổi một lần, kích hoạt OnChangedRender trên mọi máy.
    //
    // Vì sao phải vòng vo vậy: không được phát tiếng thẳng trong FixedUpdateNetwork,
    // vì Fusion tua lại nhiều tick mỗi khung hình -> một cú dash sẽ kêu 5-6 lần.
    // OnChangedRender thì chỉ chạy đúng một lần cho mỗi lần giá trị thật sự đổi.
    [Networked, OnChangedRender(nameof(OnDashPerformed))]
    private int DashCount { get; set; }

    // Đồng hồ đếm ngược hiệu lực Nước Tăng Lực.
    //
    // Không cần lưu riêng "hệ số hồi chiêu" làm gì: còn hạn thì giảm, hết hạn thì thôi,
    // suy ra từ chính cái đồng hồ này là đủ. Ít trạng thái đồng bộ hơn, cũng ít chỗ sai hơn.
    [Networked] private TickTimer EnergyDrinkTimer { get; set; }

    /// <summary>Nước Tăng Lực có đang còn hiệu lực không. HUD dùng để hiện biểu tượng buff.</summary>
    public bool IsEnergyDrinkActive => Runner != null && !EnergyDrinkTimer.ExpiredOrNotRunning(Runner);

    /// <summary>Số giây hiệu lực Nước Tăng Lực còn lại, bằng 0 nếu đã hết.</summary>
    public float EnergyDrinkRemaining
    {
        get
        {
            if (Runner == null) return 0f;
            float? remaining = EnergyDrinkTimer.RemainingTime(Runner);
            return remaining ?? 0f;
        }
    }

    // Hướng nhìn ban đầu (độ), do Host quyết định theo đội lúc spawn.
    // Phải [Networked] vì Host gán giá trị này, còn máy Client mới là nơi cần đọc nó
    // để đặt góc nhìn cho đúng.
    [Networked] public float SpawnYaw { get; set; }

    private CharacterController controller;
    private PlayerHealth health;

    public override void Spawned()
    {
        controller = GetComponent<CharacterController>();
        health = GetComponent<PlayerHealth>();

        // Tắt rồi bật lại CharacterController ngay khi vừa sinh ra.
        //
        // Lý do: CharacterController giữ một bản toạ độ riêng bên trong nó. Khi vừa được tạo,
        // bản toạ độ đó vẫn là (0,0,0) của prefab, nên lệnh Move() đầu tiên sẽ kéo nhân vật
        // về gốc toạ độ thay vì giữ ở chỗ được spawn. Tắt/bật buộc nó đọc lại transform.
        //
        // Đây chính là cách Photon tự xử lý trong NetworkCharacterController.cs của họ.
        if (controller != null)
        {
            controller.enabled = false;
            controller.enabled = true;
        }

        // HasInputAuthority = true nghĩa là nhân vật này thuộc về người ngồi trước máy này.
        bool isMine = HasInputAuthority;

        if (isMine)
        {
            Local = this;

            // Đưa con số đã cân bằng tay ở Inspector vào làm mặc định cho Settings,
            // nhưng CHỈ khi người chơi chưa từng tự chỉnh. Đã chỉnh rồi thì giữ ý họ.
            GameSettings.SeedDefaultSensitivity(mouseSensitivity);

            // Đặt góc nhìn khớp với hướng Host đã xoay lúc spawn.
            // Bỏ bước này thì ngay tick đầu tiên FixedUpdateNetwork sẽ bẻ nhân vật
            // về góc 0 độ, và công xoay hướng lúc spawn thành vô nghĩa.
            NetworkRunnerHandler.SetLookAngles(SpawnYaw, 0f);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Chỉ bật Camera và AudioListener của nhân vật mình.
        // Nếu để cả 4 nhân vật cùng bật camera thì màn hình sẽ loạn,
        // còn nhiều AudioListener cùng lúc sẽ khiến Unity báo lỗi âm thanh.
        if (cameraTransform != null)
        {
            Camera cam = cameraTransform.GetComponent<Camera>();
            if (cam != null) cam.enabled = isMine;

            AudioListener listener = cameraTransform.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = isMine;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Local == this) Local = null;
    }

    // Fusion gọi hàm này trước mỗi đợt tick, đặc biệt là trước khi "tua lại"
    // (resimulation - chạy lại các tick cũ khi có dữ liệu mới từ Host về).
    //
    // Khi tua lại, NetworkTransform đã kéo transform về đúng vị trí Host xác nhận,
    // NHƯNG CharacterController vẫn nhớ vị trí cũ trong bộ nhớ riêng của nó.
    // Hậu quả: mỗi lần tua, quãng đường di chuyển bị cộng dồn thêm một lần nữa
    // -> nhân vật ở máy Client chạy nhanh gấp nhiều lần bình thường.
    //
    // Tắt/bật CharacterController ép nó đọc lại vị trí đã được tua, xoá bộ nhớ cũ đi.
    void IBeforeAllTicks.BeforeAllTicks(bool resimulation, int tickCount)
    {
        if (controller == null) return;

        // CHỈ làm với nhân vật mà máy này thật sự có tính toán di chuyển:
        //   - Trên Host: mọi nhân vật (Host tính hết)
        //   - Trên Client: chỉ nhân vật của chính mình (dự đoán trước)
        //
        // TUYỆT ĐỐI không đụng tới nhân vật người khác. Nhân vật của họ đang được
        // NetworkTransform nội suy cho mượt giữa các tick. Tắt/bật CharacterController
        // sẽ ép transform nhảy thẳng về vị trí thô của từng tick, phá nát nội suy
        // -> nhìn đối phương thấy giật liên tục dù vị trí vẫn đúng.
        if (!HasStateAuthority && !HasInputAuthority) return;

        controller.enabled = false;
        controller.enabled = true;
    }

    // Toàn bộ logic di chuyển chuyển từ Update() sang đây.
    // FixedUpdateNetwork chạy theo nhịp tick của mạng chứ không theo khung hình,
    // nhờ vậy Host và Client tính ra cùng một kết quả.
    public override void FixedUpdateNetwork()
    {
        // GetInput chỉ trả về true trên Host, và trên máy của chính chủ nhân vật.
        // Nhân vật của người khác trên máy mình sẽ không vào được đây,
        // vị trí của họ do Fusion tự nội suy cho mượt.
        if (!GetInput(out NetworkInputData input)) return;

        // 1. XOAY NGƯỜI THEO GÓC NHÌN NGANG
        // Góc đã được máy người chơi tính sẵn và gửi kèm input, nên ở đây chỉ việc áp vào.
        transform.rotation = Quaternion.Euler(0f, input.Yaw, 0f);
        NetPitch = input.Pitch;

        // ĐÃ BỊ LOẠI KHỎI ROUND -> đứng yên tại chỗ, không đi lại được nữa.
        // Cố ý đặt SAU phần xoay ở trên, để người chết vẫn ngó nghiêng xem trận đấu tiếp diễn.
        if (health != null && !health.IsAlive) return;

        // 2. DI CHUYỂN WASD
        Vector3 inputDir = new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);
        inputDir = Vector3.ClampMagnitude(inputDir, 1f);

        Vector3 moveDirection = transform.right * inputDir.x + transform.forward * inputDir.z;

        // 3. COOLDOWN DASH & LỆNH DASH
        float dashTimer = NetDashTimer;
        if (dashTimer > 0f) dashTimer -= Runner.DeltaTime;

        // WasPressed so sánh với tick trước để chỉ bắt đúng lúc phím vừa được nhấn xuống,
        // tương đương Input.GetKeyDown của bản singleplayer cũ.
        bool dashPressed = input.Buttons.WasPressed(PreviousButtons, (int)InputButton.Dash);

        if (dashPressed && dashTimer <= 0f)
        {
            Vector3 dashDirection = moveDirection.normalized;

            if (dashDirection == Vector3.zero)
            {
                dashDirection = transform.forward;
            }

            // scaleByCharge = false: Dash là sức của chính mình, không phải bị đẩy.
            // Nhiễm điện nặng cũng chỉ lướt được đúng bấy nhiêu mét.
            AddImpact(dashDirection, dashForce, false);
            DashCount++; // để mọi máy phát tiếng dash, xem OnDashPerformed

            // Còn Nước Tăng Lực trong người thì chờ ít hơn
            float multiplier = IsEnergyDrinkActive ? (1f - energyDrinkCooldownReduction) : 1f;
            dashTimer = dashCooldown * multiplier;
        }

        NetDashTimer = dashTimer;
        PreviousButtons = input.Buttons;

        // 4. TRỌNG LỰC
        Vector3 velocity = NetVelocity;
        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }
        velocity.y += gravity * Runner.DeltaTime;
        NetVelocity = velocity;

        // 5. GIẢM TỐC QUÁN TÍNH (IMPACT)
        Vector3 impact = NetImpact;
        if (impact.sqrMagnitude > 0.04f)
        {
            impact = Vector3.Lerp(impact, Vector3.zero, drag * Runner.DeltaTime);
        }
        else
        {
            impact = Vector3.zero;
        }
        NetImpact = impact;

        // 6. GỘP LỰC VÀ DI CHUYỂN (1 LẦN MOVE/TICK)
        Vector3 finalMovement = (moveDirection * moveSpeed) + velocity + impact;
        controller.Move(finalMovement * Runner.DeltaTime);
    }

    // Render chạy mỗi khung hình (giống Update cũ), dùng cho phần hình ảnh thuần tuý.
    public override void Render()
    {
        if (cameraTransform == null) return;

        // Nhân vật của mình: lấy thẳng góc chuột cục bộ để camera không bị trễ 1 tick,
        // nếu dùng giá trị mạng thì rê chuột sẽ có cảm giác nặng và giật.
        // Nhân vật người khác: dùng giá trị đã đồng bộ qua mạng.
        float pitch = HasInputAuthority ? NetworkRunnerHandler.LookPitch : NetPitch;

        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    // Hàm này được PlayerMagnetController, MagneticObject (nổ TNT), DummyGravity... gọi vào
    // để hất văng nhân vật. Giữ nguyên tên và tham số như cũ nên các script đó không phải sửa gì.
    //
    // LƯU Ý: hiện tại các script gọi vào đây vẫn là MonoBehaviour thuần (chưa lên mạng),
    // nên lực hất văng mới chỉ đúng khi chạy trên máy Host. Sẽ xử lý triệt để
    // khi chuyển PlayerMagnetController sang NetworkBehaviour ở bước sau.
    /// <summary>
    /// Uống Nước Tăng Lực: giảm cooldown Dash trong một khoảng thời gian.
    ///
    /// KHÔNG cộng dồn. Đang còn hiệu lực mà uống tiếp thì trả về false,
    /// món đồ không bị tiêu tốn. Hết hiệu lực rồi mới uống bình mới được.
    /// </summary>
    public bool ApplyEnergyDrink()
    {
        if (!HasStateAuthority) return false;

        // Đang còn hiệu lực -> không cho uống chồng
        if (IsEnergyDrinkActive)
        {
            Debug.Log("<color=orange>[NƯỚC TĂNG LỰC] Đang còn hiệu lực, chưa uống bình mới được</color>");
            return false;
        }

        EnergyDrinkTimer = TickTimer.CreateFromSeconds(Runner, energyDrinkDuration);

        Debug.Log($"<color=cyan>[NƯỚC TĂNG LỰC] Hồi chiêu Dash giảm {energyDrinkCooldownReduction:P0} trong {energyDrinkDuration} giây</color>");
        return true;
    }

    // Chạy trên MỌI máy, đúng một lần cho mỗi cú dash
    private void OnDashPerformed()
    {
        AudioManager.Dash(transform.position);
    }

    /// <summary>Xoá hiệu lực Nước Tăng Lực. GameManager gọi khi hồi sinh đầu round.</summary>
    public void ClearEnergyDrink()
    {
        if (!HasStateAuthority) return;

        EnergyDrinkTimer = TickTimer.None;
    }

    /// <summary>
    /// Đẩy nhân vật này đi theo một hướng.
    /// </summary>
    /// <param name="scaleByCharge">
    /// Bật (mặc định) = đây là cú đẩy TỪ BÊN NGOÀI (bị đấm, dính nổ, trúng vật thể),
    /// nên phải nhân theo mức nhiễm điện - càng nhiễm nhiều càng bay xa. Đây chính là
    /// cơ chế cốt lõi của chế độ Quá Tải.
    ///
    /// Tắt = đây là chuyển động DO CHÍNH MÌNH tạo ra (Dash, Grapple kéo áp sát).
    /// Những cái đó phải giữ nguyên cự ly bất kể nhiễm điện bao nhiêu, nếu không thì
    /// người sắp thua lại dash xa gấp 5 lần - vừa vô lý vừa vỡ cân bằng.
    /// </param>
    public void AddImpact(Vector3 dir, float force, bool scaleByCharge = true)
    {
        dir.Normalize();
        if (dir.y < 0f) dir.y = -dir.y;

        if (scaleByCharge && health != null)
        {
            force *= health.KnockbackMultiplier;
        }

        NetImpact += dir * (force / mass);
    }
}
