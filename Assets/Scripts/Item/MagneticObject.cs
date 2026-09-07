using Fusion;
using Fusion.Addons.Physics; // NetworkRigidbody3D.Teleport() khi reset map mỗi round
using UnityEngine;
using System.Collections.Generic; // Cần thiết để lọc trùng danh sách khi nổ TNT

public class MagneticObject : NetworkBehaviour
{
    // Danh sách mọi vật thể từ tính đang có trong trận, để GameManager trả chúng về
    // chỗ cũ mỗi khi sang round mới. Làm giống PlayerHealth.AllPlayers cho nhất quán.
    public static readonly List<MagneticObject> AllObjects = new List<MagneticObject>();

    public enum Polarity { None, Positive, Negative }
    public enum ObjectType { Normal, Heavy, Spike, TNT }

    [Header("Base Settings")]
    [Tooltip("Loại GỐC của prefab này. Chỉ dùng làm giá trị khởi đầu - lúc chạy hãy đọc CurrentType.")]
    public ObjectType objectType = ObjectType.Normal;

    [Tooltip("Sát thương gốc của loại vật thể này: Normal 10, Heavy 20, Spike 25.")]
    public float baseDamage = 10f;

    // Ghi chú thiết kế: Spike đã BỎ tính chất cắm dính vào người trúng.
    // Giờ nó va chạm y hệt vật thường, chỉ khác ở con số sát thương cao hơn.

    [Header("Bullet Settings")]
    [Tooltip("Lực hất văng khi đạn TRÚNG NGƯỜI, tính cho đạn Normal (10 sát thương). " +
             "Heavy và Spike tự động mạnh hơn theo tỉ lệ sát thương. " +
             "So sánh: cú đấm cận chiến đang để 200.")]
    public float hitKnockbackForce = 120f;

    [Tooltip("Bay chậm hơn tốc độ này thì thôi không tính là đạn nữa, không gây sát thương.")]
    public float minBulletSpeed = 3f;

    [Tooltip("Khoảng thời gian ngay sau khi phóng, chưa xét tốc độ vội. Cần vì lực đẩy phải sang bước vật lý kế tiếp mới thành vận tốc.")]
    public float bulletArmTime = 0.25f;

    [Tooltip("Khi đạn NẢY LÊN từ sàn/tường, giữ lại bao nhiêu phần tốc độ đi lên. " +
             "0.2 = nảy còn 20% độ cao, chỉ tưng nhẹ một cái rồi trượt tiếp. " +
             "1 = không can thiệp gì, để vật lý tự nhiên hoàn toàn. " +
             "CHỈ áp dụng đúng khoảnh khắc chạm, giữa đường bay không đụng tới.")]
    [Range(0f, 1f)]
    public float bounceRiseDamp = 0.2f;

    [Tooltip("Nhân trọng lực RIÊNG cho lúc đang bay là đạn. Trọng lực project chỉ -3 nên đạn " +
             "lơ lửng rất lâu; để 2-3 sẽ làm đường đạn phẳng và dứt khoát hơn mà không phải " +
             "đụng vào trọng lực chung của cả game. 1 = không đổi gì.")]
    public float bulletGravityMultiplier = 2f;

    [Tooltip("Chạm vào mặt có pháp tuyến hướng lên hơn mức này thì coi là ĐẤT/GỜ (lướt qua, " +
             "giữ nguyên hướng bay). Thấp hơn thì coi là TƯỜNG (dội lại bình thường). " +
             "0.5 tương đương dốc 60 độ. Hạ xuống nếu đạn vẫn quẹo ở sườn dốc.")]
    [Range(0f, 1f)]
    public float groundNormalThreshold = 0.5f;

    [Tooltip("Lướt qua gờ đất thì giữ lại bao nhiêu phần tốc độ ngang. " +
             "1 = không mất tốc chút nào, 0.85 = mất 15% mỗi lần chạm.")]
    [Range(0f, 1f)]
    public float groundHitSpeedRetain = 0.85f;

    [Header("Ngủ đông - nằm bất động khi không ai đụng tới")]
    [Tooltip("Bật thì vật tự khoá cứng tại chỗ khi đã đứng yên, và tự tỉnh khi bị tác động. " +
             "Tắt nếu muốn vật lăn tự do như vật lý bình thường.")]
    public bool enableAutoSleep = true;

    [Tooltip("Chậm hơn mức này (m/s) thì coi như đã đứng yên. Để quá cao thì vật đang lăn chậm cũng bị khoá đứng lại giữa chừng.")]
    public float sleepSpeedThreshold = 0.4f;

    [Tooltip("Phải đứng yên liên tục bấy nhiêu giây mới ngủ. Có độ trễ này để vật không ngủ ngay ở đỉnh đường bay, lúc nó chậm nhất.")]
    public float sleepDelay = 0.6f;

    [Tooltip("Bị vật khác đâm trúng thì nhận lại bao nhiêu phần động lượng. 1 = giống hệt va chạm thường, 0.5 = nặng nề hơn.")]
    public float wakeImpulseTransfer = 1f;

    [Header("Explosion Settings (chỉ dùng cho TNT)")]
    public float explosionRadius = 6f;
    public float explosionForce = 15f;

    [Tooltip("Nhân riêng lực thổi VẬT THỂ xung quanh, không đụng tới lực hất người chơi.\n\n" +
             "Phải tách ra vì hai bên đi qua hai đường vật lý khác hẳn nhau: người chơi dùng " +
             "AddImpact (đặt thẳng vận tốc), còn vật thể dùng AddExplosionForce với " +
             "ForceMode.Impulse - mà Impulse thì CHIA CHO KHỐI LƯỢNG và còn giảm dần theo " +
             "khoảng cách. Với explosionForce 15 và mass 3 thì vật ở tâm vụ nổ chỉ nhận " +
             "được 5 m/s, chưa đủ để nhìn ra là có nổ.")]
    public float debrisForceMultiplier = 4f;

    [Tooltip("Mảnh văng do vụ nổ cũng tính là ĐẠN, đâm trúng người là trừ điện.\n\n" +
             "Chủ nhân của mảnh văng kế thừa từ chính thùng TNT, nên người ném TNT không bị " +
             "chính mảnh văng của mình đánh. Sóng nổ thì vẫn trúng họ như thường.\n\n" +
             "⚠️ SINH RA PHẢN ỨNG DÂY CHUYỀN. Một thùng TNT khác nằm trong bán kính cũng " +
             "thành đạn, mà TNT đang là đạn thì NỔ khi va vào bất cứ thứ gì. Hai thùng đặt " +
             "gần nhau sẽ nổ dây chuyền. Đây là hệ quả thật chứ không phải lỗi - nhưng nếu " +
             "thấy mất kiểm soát thì tắt ô này.")]
    public bool debrisBecomesBullet = true;
    public float tntDamage = 35f;

    [Header("Inventory Settings")]
    [Tooltip("Kéo file ItemData tương ứng vào đây (Ví dụ: bàn/ghế kéo file NormalAmmoData)")]
    public ItemData itemData;

    // --- TRẠNG THÁI ĐỒNG BỘ QUA MẠNG ---

    // Giữ nguyên tên viết thường để mọi chỗ đang gọi tới không phải sửa.
    // OnChangedRender: mọi máy tự đổi màu và hào quang khi điện tích thay đổi.
    [Networked, OnChangedRender(nameof(OnPolarityChanged))]
    public Polarity currentPolarity { get; set; }

    // Đang bay với tư cách "đạn" hay không. Quyết định va chạm có gây sát thương không.
    [Networked] public NetworkBool isMovingAsBullet { get; set; }

    // Sát thương THỰC TẾ ở thời điểm hiện tại. Tách riêng khỏi baseDamage vì con số này
    // bị nhân lên / giảm đi lúc đang bay (Heavy giảm 50%, Spike hút sai lầm thì x2),
    // còn baseDamage là hằng số gốc của prefab đặt sẵn ở Inspector.
    [Networked] public float CurrentDamage { get; set; }

    // Đang nằm trong túi đồ của ai đó hay không.
    //
    // "Cất vào túi" KHÔNG despawn vật, cũng không SetActive(false) - cả hai đều làm hỏng
    // vòng đời mô phỏng của Fusion. Thay vào đó chỉ tắt hình ảnh và va chạm đi,
    // đúng cách đang dùng cho người chơi bị loại (PlayerHealth.IsAlive).
    //
    // Nhờ vậy vật giữ nguyên MỌI THỨ khi rút ra: đúng prefab, đúng điện tích, đúng sát thương.
    [Networked, OnChangedRender(nameof(OnStoredChanged))]
    public NetworkBool IsStored { get; set; }

    // Đã nổ tan trong round này hay chưa (chỉ TNT).
    //
    // CỐ Ý KHÔNG dùng Runner.Despawn khi nổ nữa. Despawn xoá hẳn vật khỏi mạng, nên
    // sang round mới không có cách nào dựng lại đúng vật đó - mà spawn lại từ prefab thì
    // sai, vì có 5 prefab khác nhau cùng loại Normal (xem ghi chú ở CLAUDE.md).
    // Thay vào đó chỉ ẩn đi, y hệt cách IsStored làm, rồi ResetForNewRound() bật lại.
    [Networked, OnChangedRender(nameof(OnStoredChanged))]
    public NetworkBool IsDestroyed { get; set; }

    // Đếm số lần nổ, để mọi máy phát tiếng.
    // Trước đây tiếng nổ đặt trong Despawned() - giờ không despawn nữa nên phải
    // chuyển sang bộ đếm, đúng khuôn với LaunchCount / DashCount.
    [Networked, OnChangedRender(nameof(OnExploded))]
    private int ExplodeCount { get; set; }

    // Đang ngủ đông (khoá cứng tại chỗ) hay không.
    //
    // Vì sao phải [Networked] chứ không để mỗi máy tự quyết: Client cũng phải biết vật
    // đang ngủ để đặt isKinematic giống Host. Nếu Host khoá mà Client vẫn mô phỏng vật lý,
    // vật sẽ trượt trên máy Client rồi bị Fusion giật về mỗi khi nhận dữ liệu từ Host.
    //
    // Cùng khuôn với IsStored / IsDestroyed: một cờ [Networked] + OnChangedRender áp dụng.
    [Networked, OnChangedRender(nameof(ApplySleepState))]
    public NetworkBool IsSleeping { get; set; }

    // Đếm thời gian đứng yên liên tục trước khi ngủ. Đặt lại về None mỗi khi vật động lại.
    [Networked] private TickTimer SleepCheckTimer { get; set; }

    // Khoảng khoá ngay sau khi phóng, chưa xét tốc độ để tắt tư cách đạn.
    [Networked] private TickTimer BulletArmTimer { get; set; }

    // Đếm số lần vật được phóng đi, để mọi máy phát tiếng.
    // Không phát tiếng thẳng trong LaunchAsBullet được vì hàm đó chỉ chạy trên Host.
    [Networked, OnChangedRender(nameof(OnLaunched))]
    private int LaunchCount { get; set; }

    // Trong LẦN BAY này đã bị đối phương can thiệp chưa (cản Heavy / hút nhầm Spike).
    //
    // Cần cờ này vì giữ chuột trái thì hàm hút chạy liên tục mỗi tick. Không chặn thì
    // Heavy bị cản vô hạn lần cho tới đứng yên, còn Spike thì x2 -> x4 -> x8.
    // Mỗi cú bay chỉ cho can thiệp đúng một lần, biến nó thành pha đọc tình huống
    // chứ không phải cuộc thi bấm chuột nhanh.
    [Networked] private NetworkBool WasCounteredInFlight { get; set; }

    // Loại vật thể THỰC TẾ ở thời điểm hiện tại.
    //
    // Tách khỏi objectType vì Chai Xăng Tẩy Chế biến một vật Normal thành thùng TNT
    // ngay giữa trận. objectType là hằng số của prefab, còn con số này thay đổi được
    // và phải đồng bộ, nếu không thì Host thấy TNT mà Client vẫn thấy khúc gỗ.
    //
    // Cùng khuôn với cặp baseDamage / CurrentDamage.
    [Networked, OnChangedRender(nameof(OnTypeChanged))]
    public ObjectType CurrentType { get; set; }

    // Người đã bắn vật này ra. CHỈ có ý nghĩa trên máy Host, vì chỉ Host xử lý va chạm.
    [HideInInspector] public PlayerMagnetController shooterOwner;

    private Rigidbody rb;
    private Material mat;
    private MagneticAura auraScript;
    private Renderer[] cachedRenderers;
    private Collider[] cachedColliders;

    // Kích thước gốc của prefab, ghi lại lúc vừa sinh ra.
    // Cầm lên tay thì vật bị thu nhỏ cho vừa màn hình, buông ra phải trả lại đúng cỡ này.
    private Vector3 _originalScale = Vector3.one;

    // Chỗ đứng ban đầu trên map, ghi lại lúc vừa sinh ra để sang round mới trả về đúng đây.
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;

    // Vừa đập vào môi trường ở khung vật lý trước -> tick tới giảm bớt cú nảy lên.
    // Biến thường, không cần [Networked]: chỉ Host mô phỏng vật lý, và kết quả (vận tốc)
    // đã được NetworkRigidbody3D truyền đi rồi.
    private bool _dampRiseNextTick;

    private bool _keepHeadingNextTick;

    // Hướng bay NGANG gần nhất khi đạn còn đang bay tự do, đã chuẩn hoá.
    // Dùng để dựng lại đường bay sau khi lướt qua gờ đất.
    private Vector3 _flightHeading;

    public override void Spawned()
    {
        AllObjects.Add(this);

        rb = GetComponent<Rigidbody>();

        Renderer rend = GetComponent<Renderer>();
        if (rend != null) mat = rend.material;

        auraScript = GetComponent<MagneticAura>();

        // Lấy sẵn một lần, khỏi phải tìm lại mỗi lần cất vào rút ra
        cachedRenderers = GetComponentsInChildren<Renderer>();
        cachedColliders = GetComponentsInChildren<Collider>();

        // Ghi lại cỡ gốc TRƯỚC khi có ai kịp thu nhỏ nó
        _originalScale = transform.localScale;

        // Ghi lại chỗ đứng ban đầu, để reset map mỗi round trả vật về đúng đây
        _originalPosition = transform.position;
        _originalRotation = transform.rotation;

        // Chỉ Host đặt giá trị khởi đầu, Client nhận về qua mạng
        if (HasStateAuthority)
        {
            CurrentDamage = baseDamage;
            CurrentType = objectType;

            // NGỦ NGAY TỪ ĐẦU, đừng để rơi tự do lấy một giây nào.
            //
            // Không có dòng này thì từ lúc spawn tới khi round đầu tiên bắt đầu
            // (warmupDuration, 2 giây) vật vẫn chạy vật lý tự do - đủ để mấy cái cây
            // cao mảnh đổ rạp trước cả khi trận đấu kịp bắt đầu.
            //
            // Cùng lý do với ResetForNewRound(): chỗ đặt trong Editor là chỗ ĐÚNG,
            // không cần vật lý "ổn định" lại giúp.
            IsSleeping = enableAutoSleep;
        }

        // Áp màu, hào quang và trạng thái ẩn/hiện theo dữ liệu hiện tại.
        // Gọi ở đây để vật thể vào trận muộn vẫn hiển thị đúng.
        OnPolarityChanged();
        ApplyStoredState();
        ApplySleepState();
    }

    // --- KÍCH THƯỚC KHI CẦM TRÊN TAY ---

    /// <summary>
    /// Thu vật về một cỡ chuẩn để cầm trên tay không che hết màn hình.
    ///
    /// Phải ĐO kích thước thật rồi mới tính được hệ số, vì mỗi prefab một cỡ.
    /// Nếu chỉ nhân cứng một con số (ví dụ 0.5) cho mọi vật thì khúc gỗ to vẫn to
    /// gấp mấy lần gốc cây nhỏ - không giải quyết được gì.
    ///
    /// Đây là thay đổi THUẦN HÌNH ẢNH và được từng máy tự tính lấy, không cần đồng bộ:
    /// máy nào cũng biết ai đang cầm vật nào (qua GrabbedObjectId) nên tự thu nhỏ được.
    /// </summary>
    /// <param name="targetSize">Cạnh dài nhất sau khi thu, tính bằng mét.</param>
    /// <param name="onlyShrink">Bật thì vật vốn đã nhỏ hơn cỡ chuẩn sẽ được giữ nguyên, không phóng to lên.</param>
    public void ApplyHeldScale(float targetSize, bool onlyShrink)
    {
        if (targetSize <= 0f) return;

        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend == null) return;

        // DÙNG localBounds, KHÔNG DÙNG bounds.
        //
        // rend.bounds là hộp bao theo TRỤC THẾ GIỚI, nên nó phình to ra khi vật xoay
        // nghiêng: một tấm ván dài xoay 45 độ đo ra to hơn hẳn lúc nằm thẳng.
        // Mà KeepObjectInHand() xoay vật theo hướng nhìn mỗi khung hình, nên đo bằng
        // rend.bounds sẽ ra cỡ khác nhau tuỳ lúc nhặt bạn đang nhìn về đâu - cùng một
        // cái bàn mà lần thì thu còn 0.6m, lần thì còn 0.45m.
        //
        // localBounds là hộp bao theo trục của CHÍNH VẬT, không đổi khi xoay.
        // Nhân với lossyScale để quy về kích thước thật trong thế giới.
        Vector3 worldSize = Vector3.Scale(rend.localBounds.size, rend.transform.lossyScale);
        float largestSide = Mathf.Max(worldSize.x, Mathf.Max(worldSize.y, worldSize.z));

        if (largestSide <= 0.0001f) return; // vật không có hình, bỏ qua

        float ratio = targetSize / largestSide;

        // Vốn đã nhỏ hơn cỡ chuẩn thì để yên, đừng phóng to hòn sỏi thành tảng đá
        if (onlyShrink && ratio >= 1f) return;

        transform.localScale = transform.localScale * ratio;
    }

    /// <summary>Trả vật về đúng kích thước gốc của prefab. Gọi khi buông tay.</summary>
    public void RestoreScale()
    {
        transform.localScale = _originalScale;
    }

    /// <summary>
    /// Bán kính hình cầu bao quanh vật, tính theo kích thước THẬT hiện tại.
    ///
    /// Dùng localBounds nên KHÔNG đổi khi vật xoay - cùng lý do đã giải thích ở
    /// ApplyHeldScale(). Đo bằng rend.bounds sẽ cho ra số nhảy loạn khi vật lăn.
    ///
    /// Hai chỗ cần tới: ngưỡng bắt vật vào tay (vật to phải bắt từ xa hơn),
    /// và tính chỗ đặt vật khi buông tay sao cho không lồng vào người.
    /// </summary>
    public float GetBoundingRadius()
    {
        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend == null) return 0.5f;

        Vector3 worldSize = Vector3.Scale(rend.localBounds.size, rend.transform.lossyScale);
        return worldSize.magnitude * 0.5f;
    }

    // --- CẤT VÀO TÚI / RÚT RA ---

    /// <summary>Cất vào túi: tắt hình ảnh và va chạm, vật vẫn tồn tại nhưng vô hình.</summary>
    public void StoreInBag()
    {
        if (!HasStateAuthority) return;

        // Đang bay dở thì huỷ trạng thái đạn trước, tránh cất xong rút ra vẫn còn tính sát thương
        ResetBulletState();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        IsStored = true;
    }

    /// <summary>Rút khỏi túi: dịch chuyển tới vị trí chỉ định rồi bật lại.</summary>
    public void TakeOutOfBag(Vector3 position)
    {
        if (!HasStateAuthority) return;

        transform.position = position;

        if (rb != null)
        {
            rb.isKinematic = true;   // vẫn nằm trên tay nên chưa trả vật lý về
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        IsStored = false;
    }

    private void OnStoredChanged()
    {
        ApplyStoredState();
    }

    // Chạy trên MỌI máy, đúng một lần cho mỗi cú phóng
    private void OnLaunched()
    {
        AudioManager.Launch(transform.position);
    }

    // Chạy trên MỌI máy, đúng một lần cho mỗi lần nổ.
    // Trước đây tiếng nổ nằm trong Despawned(); giờ vật không bị xoá nữa nên chuyển vào đây.
    private void OnExploded()
    {
        AudioManager.Explosion(transform.position);
    }

    // Bật/tắt phần nhìn thấy được và phần va chạm. Chạy trên mọi máy.
    //
    // Hai lý do khiến vật bị ẩn: đang nằm trong túi ai đó, hoặc đã nổ tan trong round này.
    // Cả hai đều dẫn tới cùng một kết quả nên gộp chung một chỗ.
    private void ApplyStoredState()
    {
        bool visible = !IsStored && !IsDestroyed;

        if (cachedRenderers != null)
        {
            foreach (Renderer r in cachedRenderers)
            {
                if (r != null) r.enabled = visible;
            }
        }

        if (cachedColliders != null)
        {
            foreach (Collider c in cachedColliders)
            {
                if (c != null) c.enabled = visible;
            }
        }
    }

    // Fusion tự gọi trên MỌI máy mỗi khi currentPolarity đổi giá trị.
    // Nhờ vậy màu sắc và hào quang tự khớp nhau mà không cần gửi RPC riêng.
    private void OnPolarityChanged()
    {
        RefreshAura();
        SetColorBasedOnPolarity();

        // Chỉ kêu khi vật được NẠP điện, không kêu lúc bị xả về trung tính
        if (currentPolarity != Polarity.None)
        {
            AudioManager.Charge(transform.position);
        }
    }

    public void SetPolarity(Polarity newPolarity)
    {
        // Chỉ Host được đổi điện tích. Đã thử cho Client tự đoán trước rồi bỏ (30/07),
        // vì phép thử dự đoán bên PlayerMagnetController làm mọi thứ giật hơn.
        if (!HasStateAuthority) return;

        currentPolarity = newPolarity;

        // Không cần gọi RefreshAura ở đây: OnChangedRender sẽ tự kích hoạt trên mọi máy.
    }

    /// <summary>
    /// Chai Xăng Tẩy Chế: biến một vật thường thành thùng TNT phát nổ.
    /// Trả về false nếu vật này không phải loại Normal, để không tiêu tốn chai xăng vô ích.
    /// </summary>
    public bool ConvertToTNT()
    {
        if (!HasStateAuthority) return false;

        // Chỉ vật thường mới chế được. Đã là TNT rồi thì thôi.
        if (CurrentType != ObjectType.Normal) return false;

        CurrentType = ObjectType.TNT;

        Debug.Log($"<color=#FF6600><b>[CHAI XĂNG] {name} đã bị chế thành thùng TNT!</b></color>");
        return true;
    }

    private void OnTypeChanged()
    {
        // Đổi loại thì màu cũng phải đổi theo, để mọi người nhìn là biết vật này giờ sẽ nổ
        SetColorBasedOnPolarity();

        if (CurrentType == ObjectType.TNT && objectType != ObjectType.TNT)
        {
            AudioManager.ConvertTNT(transform.position);
        }
    }

    // Fusion gọi trên MỌI máy khi vật bị xoá khỏi mạng.
    //
    // Tiếng nổ ĐÃ CHUYỂN sang OnExploded(), vì TNT nổ giờ chỉ bị ẩn đi chứ không
    // despawn nữa (để round sau còn dựng lại được). Hàm này giờ chỉ lo dọn danh sách.
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        AllObjects.Remove(this);
    }

    void SetColorBasedOnPolarity()
    {
        if (mat == null) return;

        // Vật đã bị chế thành TNG thì luôn hiện màu cam cảnh báo, đè lên màu điện tích.
        // Không có dấu hiệu này thì thùng thuốc nổ nhìn y hệt khúc gỗ, rất nguy hiểm cho
        // người chơi mà cũng khó hiểu.
        if (CurrentType == ObjectType.TNT && objectType != ObjectType.TNT)
        {
            mat.color = new Color(1f, 0.4f, 0f);
            return;
        }

        if (currentPolarity == Polarity.Positive) mat.color = Color.red;
        else if (currentPolarity == Polarity.Negative) mat.color = Color.blue;
        else mat.color = Color.white;
    }

    public void RefreshAura()
    {
        if (auraScript != null)
        {
            auraScript.UpdateAura(currentPolarity);
        }
    }

    /// <summary>
    /// Phóng vật đi với tư cách "đạn". Cả đẩy từ môi trường lẫn bắn từ tay đều gọi hàm này,
    /// để hai đường không bao giờ lệch nhau nữa.
    /// </summary>
    public void LaunchAsBullet(PlayerMagnetController owner)
    {
        if (!HasStateAuthority) return;

        isMovingAsBullet = true;
        shooterOwner = owner;
        WasCounteredInFlight = false; // cú bay mới, cho phép can thiệp lại
        LaunchCount++; // để mọi máy phát tiếng, xem OnLaunched

        // Bật quét va chạm liên tục, nếu không đạn bay 125 m/s sẽ xuyên qua người chơi.
        // Xem giải thích đầy đủ ở EnableHighSpeedCollision().
        EnableHighSpeedCollision();

        // Khoá một khoảng ngắn không cho tự tắt tư cách đạn.
        // Cần vì lực đẩy chỉ thật sự biến thành vận tốc ở bước vật lý kế tiếp,
        // nên ngay lúc vừa phóng thì tốc độ vẫn đang bằng 0.
        BulletArmTimer = TickTimer.CreateFromSeconds(Runner, bulletArmTime);
    }

    /// <summary>
    /// Đánh dấu vật là đạn mà KHÔNG phát tiếng bắn và KHÔNG động tới cơ chế can thiệp.
    ///
    /// Dùng cho những đường không phải "phóng đi" theo nghĩa thông thường:
    ///   - Vật đang bị HÚT về tay (PlayerMagnetController)
    ///   - Mảnh văng do TNT nổ (Explode)
    ///
    /// ⚠️ KHÔNG DÙNG LaunchAsBullet() CHO NHỮNG ĐƯỜNG ĐÓ. Nhánh hút chạy LẠI MỖI TICK
    /// suốt thời gian giữ chuột, còn vụ nổ thì quét hàng chục vật cùng lúc. Gọi
    /// LaunchAsBullet ở đó sẽ hỏng ba thứ:
    ///
    ///   1. LaunchCount++ mỗi lần  -> tiếng bắn kêu liên tục như súng máy
    ///   2. WasCounteredInFlight về false -> mất cơ chế "chỉ can thiệp 1 lần mỗi cú bay",
    ///      tức là hỏng luôn cả luật Heavy lẫn luật Spike
    ///   3. BulletArmTimer nạp lại liên tục -> vật không bao giờ tự hết tư cách đạn
    ///
    /// Hàm này làm đúng phần tối thiểu và TỰ BỎ QUA nếu đã đánh dấu cho cùng chủ nhân,
    /// nên gọi bao nhiêu lần cũng như một lần.
    /// </summary>
    /// <summary>
    /// Bật chế độ phát hiện va chạm LIÊN TỤC cho vật đang bay nhanh.
    ///
    /// ⚠️ ĐÂY LÀ LỖI "BẮN XUYÊN QUA NGƯỜI" — sửa 01/09. Đọc kỹ trước khi gỡ.
    ///
    /// Mọi prefab đều để Collision Detection = Discrete (mặc định Unity). Ở chế độ đó
    /// PhysX chỉ kiểm tra chồng lấn TẠI CÁC VỊ TRÍ RỜI RẠC, mỗi bước vật lý một lần.
    ///
    /// Làm phép tính với thông số thật của game:
    ///     bước vật lý  = 0.02 giây (50Hz)
    ///     pushSpeed    = 125 m/s  ->  2.5 MÉT mỗi bước
    ///     fireSpeed    = 45 m/s   ->  0.9 mét mỗi bước
    ///     bề ngang người chơi     =  1 mét
    ///
    /// Vật nhảy 2.5m mỗi bước qua một mục tiêu rộng 1m thì nó XUYÊN THẲNG QUA mà không
    /// hề sinh ra va chạm nào. Đó là lý do đạn "lúc trúng lúc không" và vật càng MẢNH
    /// càng hay trượt - vật to còn có cơ hội chồng lấn tại điểm lấy mẫu, thân cây thì không.
    ///
    /// ContinuousDynamic quét cả quãng đường giữa hai bước thay vì chỉ kiểm hai đầu mút.
    /// Đắt hơn Discrete, nhưng chỉ bật cho vật ĐANG BAY nên số lượng luôn nhỏ.
    ///
    /// Đặt bằng code chứ không sửa từng prefab: có hàng chục prefab vật thể, sửa tay thì
    /// chắc chắn sót, mà sót cái nào là cái đó bắn xuyên người mãi mãi.
    /// </summary>
    private void EnableHighSpeedCollision()
    {
        if (rb == null) return;

        // Không đặt được cho vật kinematic - Unity sẽ cảnh báo và bỏ qua.
        // Vật đang là đạn thì luôn không kinematic, nhưng kiểm cho chắc.
        if (rb.isKinematic) return;

        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public void MarkAsBullet(PlayerMagnetController owner)
    {
        if (!HasStateAuthority) return;

        // Đã đánh dấu cho đúng người này rồi thì thôi, đây là lần gọi thứ hai trở đi
        if (isMovingAsBullet && shooterOwner == owner) return;

        EnableHighSpeedCollision();

        isMovingAsBullet = true;
        shooterOwner = owner;

        // Cố ý KHÔNG đụng tới WasCounteredInFlight và KHÔNG tăng LaunchCount.
        BulletArmTimer = TickTimer.CreateFromSeconds(Runner, bulletArmTime);
    }

    /// <summary>
    /// Giảm biên độ cú NẢY LÊN khi đạn đập vào sàn hoặc tường.
    ///
    /// VẤN ĐỀ ĐANG CHỮA: collider của cây/đá là mấy khối hộp ghép thô, mặt đất cũng gồ ghề.
    /// Vật bay ngang đập vào MÉP collider hoặc khe nối giữa hai khối thì pháp tuyến va chạm
    /// chếch lên vài độ, PhysX phản xạ vận tốc theo pháp tuyến đó và sinh ra thành phần
    /// thẳng đứng. Cộng thêm lực gỡ kẹt khi vật lún vào địa hình.
    ///
    /// ĐÂY KHÔNG PHẢI "bounciness". Physic Material mặc định đã để độ nảy bằng 0 rồi,
    /// đi chỉnh chỗ đó không giải quyết được gì.
    ///
    /// Trọng lực project chỉ -3 (bằng 1/3 mặc định) nên cùng một vận tốc bay lên sẽ lơ lửng
    /// lâu gấp 3 lần bình thường - một cú nảy nhỏ cũng thành cú bay vòng cung rất lộ.
    ///
    /// VÌ SAO LÀM Ở KHOẢNH KHẮC VA CHẠM CHỨ KHÔNG CẮT MỖI TICK:
    /// Bản đầu cắt trần vận tốc mỗi tick, nhưng như vậy đạn đang bay giữa không trung
    /// cũng bị ghìm - nhìn như có tấm khiên vô hình chặn ngang đường bay, rất phi lý.
    /// Chỉ nhân nhỏ đúng một lần ngay sau cú chạm thì cú nảy VẪN CÒN (vẫn thấy đạn tưng lên,
    /// đúng chất vật lý), chỉ là thấp hơn. Giữa đường bay tuyệt đối không đụng vào.
    ///
    /// Phải làm ở tick SAU chứ không làm thẳng trong OnCollisionEnter, vì vận tốc đọc được
    /// bên trong callback đó có thể là giá trị TRƯỚC khi PhysX giải va chạm - lúc đó
    /// thành phần y còn đang âm (đang lao xuống) nên nhân vào chẳng có tác dụng gì.
    /// </summary>
    private void DampBounceRise()
    {
        if (!_dampRiseNextTick) return;

        bool keepHeading = _keepHeadingNextTick;
        _dampRiseNextTick = false;
        _keepHeadingNextTick = false;

        if (rb.isKinematic) return;

        Vector3 v = rb.linearVelocity;

        // GIỮ NGUYÊN HƯỚNG BAY khi chỉ lướt qua gờ đất.
        //
        // Đây là phần chữa việc "đạn quẹo trái quẹo phải dù mặt đất gần như phẳng".
        // Mesh collider của model địa hình có hàng chục nghìn tam giác với pháp tuyến
        // lệch nhau vài độ. PhysX phản xạ vận tốc theo pháp tuyến của đúng tam giác vừa
        // chạm, nên chỉ cần một gờ cao vài centimet là đường đạn đổi hướng hẳn.
        //
        // Thay vì để nó phản xạ, ép vận tốc ngang quay về ĐÚNG hướng đang bay trước đó,
        // chỉ giảm tốc một chút. Đạn sẽ cày thẳng qua chỗ gồ ghề thay vì nảy tứ tung.
        if (keepHeading && _flightHeading.sqrMagnitude > 0.0001f)
        {
            float horizontalSpeed = new Vector2(v.x, v.z).magnitude * groundHitSpeedRetain;

            Vector3 restored = _flightHeading * horizontalSpeed;
            restored.y = v.y; // phần dọc để đoạn dưới xử lý

            v = restored;
        }

        // Ghìm cú nảy lên. Chỉ đụng khi đang đi LÊN, đang rơi xuống thì để yên.
        if (bounceRiseDamp < 1f && v.y > 0f)
        {
            v.y *= bounceRiseDamp;
        }

        rb.linearVelocity = v;
    }

    /// <summary>
    /// Trọng lực phụ chỉ dành riêng cho đạn đang bay.
    ///
    /// Trọng lực chung của project là -3 để đồ đạc trên map lơ lửng nhẹ nhàng, nhưng
    /// con số đó làm đường đạn bay vòng cung quá lâu. Nhân riêng ở đây thì đường đạn
    /// nặng và dứt khoát mà không phải đụng vào trọng lực chung của cả game.
    /// </summary>
    /// <summary>
    /// Ghi nhớ hướng bay NGANG hiện tại, để dựng lại được sau khi lướt qua gờ đất.
    ///
    /// Cập nhật mỗi tick trong lúc đạn bay tự do. Ngay sau va chạm thì KHÔNG cập nhật
    /// (DampBounceRise() đã tiêu thụ cờ trước rồi), nếu không nó sẽ ghi nhớ đúng cái
    /// hướng vừa bị lệch - thành ra chẳng chữa được gì.
    /// </summary>
    private void RememberFlightHeading()
    {
        if (!isMovingAsBullet) return;
        if (rb.isKinematic) return;

        Vector3 flat = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // Bay quá chậm thì hướng không còn đáng tin, giữ nguyên hướng cũ
        if (flat.sqrMagnitude < 1f) return;

        _flightHeading = flat.normalized;
    }

    private void ApplyBulletGravity()
    {
        if (!isMovingAsBullet) return;
        if (rb.isKinematic) return;
        if (Mathf.Approximately(bulletGravityMultiplier, 1f)) return;

        // Chỉ cộng PHẦN DƯ, vì Physics.gravity đã được áp một lần rồi.
        //
        // Công thức này chạy đúng cho MỌI giá trị, không riêng gì lớn hơn 1:
        //   2   -> cộng thêm 1 lần  = nặng gấp đôi, rơi dứt khoát
        //   1   -> không cộng gì    = trọng lực bình thường
        //   0   -> cộng thêm -1 lần = TRIỆT TIÊU hoàn toàn, đạn bay THẲNG TUYỆT ĐỐI
        //
        // Mốc 0 là thứ đáng chú ý nhất: kết hợp với lockFireToHorizontal bên
        // PlayerMagnetController thì đường đạn thành một đường thẳng nằm ngang hoàn hảo,
        // đi đúng nơi người chơi ngắm và không bao giờ tự cắm xuống đất.
        //
        // ForceMode.Acceleration để không phụ thuộc khối lượng - đúng bản chất trọng lực.
        rb.AddForce(Physics.gravity * (bulletGravityMultiplier - 1f), ForceMode.Acceleration);
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        if (rb == null) return;

        // 0. GHÌM BỚT CÚ NẢY VỪA XẢY RA + trọng lực riêng của đạn.
        // Phải chạy TRƯỚC bước xét tốc độ bên dưới, vì cả hai đều làm đổi vận tốc.
        DampBounceRise();
        ApplyBulletGravity();

        // Ghi nhớ hướng SAU khi đã xử lý va chạm, để lần chạm kế tiếp dựng lại đúng
        // hướng đã được sửa chứ không phải hướng bị lệch.
        RememberFlightHeading();

        // 1. HẾT TƯ CÁCH ĐẠN KHI BAY CHẬM LẠI
        // Bay chậm lại rồi thì thôi không còn là đạn nữa, dù chưa va vào đâu cả.
        // Không có bước này thì một vật lăn tới khi dừng hẳn vẫn gây sát thương.
        //
        // BulletArmTimer: chưa hết thời gian khoá thì chưa xét tốc độ vội.
        if (isMovingAsBullet && BulletArmTimer.ExpiredOrNotRunning(Runner))
        {
            if (rb.linearVelocity.magnitude < minBulletSpeed)
            {
                ResetBulletState();
            }
        }

        // 2. NGỦ ĐÔNG
        UpdateSleepState();
    }

    /// <summary>
    /// Quyết định lúc nào vật nên khoá cứng tại chỗ.
    ///
    /// VÌ SAO CẦN: địa hình gồ ghề khiến vật nằm trên dốc trượt và rung mãi không dứt.
    /// Unity có cơ chế tự ngủ sẵn (Rigidbody.sleepThreshold) nhưng nó chỉ ngủ khi vật
    /// thật sự đứng yên - mà trên dốc thì trọng lực kéo liên tục nên không bao giờ đạt.
    /// Ở đây khoá thẳng bằng isKinematic nên dốc cỡ nào cũng nằm im.
    ///
    /// Kèm theo hai cái lợi: Host thôi phải mô phỏng vật lý cho nó, và NetworkRigidbody3D
    /// thôi phải gửi vị trí qua mạng mỗi tick. Với hai chục vật thể trong map thì đáng kể.
    /// </summary>
    private void UpdateSleepState()
    {
        if (!enableAutoSleep) return;

        // Đang nằm trong túi hoặc đã nổ tan: ApplyStoredState() lo phần vật lý rồi, đừng tranh
        if (IsStored || IsDestroyed) return;

        if (IsSleeping)
        {
            // TỰ CHỮA LỆCH TRẠNG THÁI.
            //
            // Các đường hút / đẩy / bắn / tung bên PlayerMagnetController đặt thẳng
            // isKinematic = false chứ không gọi WakeUp(). Nếu không có nhánh này thì cờ
            // IsSleeping kẹt ở true trong khi vật đã chạy vật lý bình thường -> vật sẽ
            // KHÔNG BAO GIỜ ngủ lại được nữa, vì nhánh dưới bị chặn ngay ở đây.
            //
            // Cố ý bắt lỗi tại chỗ thay vì đi sửa 6 chỗ bên kia: bớt rủi ro đụng vào
            // đường bắn vốn đã cân bằng tay xong, và sau này thêm đường tương tác mới
            // cũng không phải nhớ gọi WakeUp().
            if (!rb.isKinematic)
            {
                IsSleeping = false;
                SleepCheckTimer = TickTimer.None;
            }
            return;
        }

        // Đang bay với tư cách đạn thì tuyệt đối không ngủ - ngủ là mất khả năng
        // va chạm gây sát thương giữa chừng.
        if (isMovingAsBullet) return;

        // Đang bị giữ kinematic bởi người khác (cầm trên tay) thì không đụng vào
        if (rb.isKinematic) return;

        bool isStill = rb.linearVelocity.magnitude < sleepSpeedThreshold
                    && rb.angularVelocity.magnitude < sleepSpeedThreshold;

        if (!isStill)
        {
            // Động lại thì xoá đồng hồ, lần sau phải đếm lại từ đầu
            SleepCheckTimer = TickTimer.None;
            return;
        }

        if (!SleepCheckTimer.IsRunning)
        {
            SleepCheckTimer = TickTimer.CreateFromSeconds(Runner, sleepDelay);
            return;
        }

        if (SleepCheckTimer.Expired(Runner))
        {
            SleepCheckTimer = TickTimer.None;
            IsSleeping = true;
            ApplySleepState();
        }
    }

    /// <summary>
    /// Áp cờ ngủ lên Rigidbody. Chạy trên MỌI máy nhờ OnChangedRender của IsSleeping.
    /// </summary>
    private void ApplySleepState()
    {
        if (rb == null) return;

        // Vật đang ẩn trong túi / đã nổ: ApplyStoredState() mới là chủ của isKinematic
        // lúc này, ghi đè ở đây sẽ làm vật sống lại giữa lúc đang nằm trong túi.
        if (IsStored || IsDestroyed) return;

        // TRẢ VỀ DISCRETE TRƯỚC KHI ĐÓNG BĂNG.
        //
        // Hai lý do, cả hai đều bắt buộc:
        //   1. Unity cảnh báo nếu đặt ContinuousDynamic cho vật kinematic
        //   2. Map có hơn 4000 vật nằm im - để chúng quét va chạm liên tục là phí sạch,
        //      mà đó đúng là thứ ta vừa mất cả buổi để tối ưu
        //
        // Vật tỉnh dậy sẽ được bật lại ở LaunchAsBullet / MarkAsBullet, tức là đúng lúc
        // nó thật sự bay nhanh. Vật lăn lều bều không cần quét liên tục.
        if (IsSleeping) rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

        rb.isKinematic = IsSleeping;

        // CỐ Ý KHÔNG đụng tới useGravity. Lúc bị hút về tay, PlayerMagnetController
        // tắt trọng lực đi để vật bay thẳng - bật lại ở đây sẽ làm nó rơi giữa đường.
    }

    /// <summary>
    /// Đánh thức vật khỏi ngủ đông. Gọi trước khi định tác động lực lên nó.
    /// </summary>
    public void WakeUp()
    {
        if (!HasStateAuthority) return;

        SleepCheckTimer = TickTimer.None;

        if (!IsSleeping) return;

        IsSleeping = false;
        ApplySleepState();
    }

    /// <summary>
    /// Bị vật khác đâm trúng lúc đang ngủ: tỉnh dậy VÀ nhận lại động lượng của cú đâm.
    ///
    /// Phải trả động lượng bằng tay vì vật ngủ là kinematic - PhysX coi nó như bức tường
    /// khối lượng vô hạn, nên toàn bộ lực của cú đâm bị nuốt mất. Không có bước này thì
    /// bắn cái ghế vào cái bàn, bàn chỉ tỉnh dậy rồi đứng nguyên tại chỗ, nhìn như
    /// cú va chạm không có tác dụng gì.
    /// </summary>
    private void WakeUpFromImpact(Collision collision)
    {
        Rigidbody otherRb = collision.rigidbody;

        // Vật đâm vào cũng đang đứng yên / cũng đang ngủ thì kệ, không có động lượng nào để truyền
        if (otherRb == null || otherRb.isKinematic) return;

        float impactSpeed = collision.relativeVelocity.magnitude;

        // Cú chạm quá nhẹ (vật lăn lều bều tới sát bên) thì không đáng để tỉnh
        if (impactSpeed < sleepSpeedThreshold) return;

        WakeUp();

        // relativeVelocity là vận tốc của vật kia SO VỚI mình. Mình đứng yên nên nó
        // chính là vận tốc vật kia, mang dấu ngược - nên đảo dấu ra hướng nó đang lao tới.
        Vector3 impactDir = -collision.relativeVelocity.normalized;

        // Bảo toàn động lượng thô: vật nặng đâm vật nhẹ thì vật nhẹ bay mạnh, và ngược lại.
        //
        // Đặt THẲNG linearVelocity thay vì AddForce, cùng lý do với cú tung V ở
        // PlayerMagnetController: phép gán xoá sạch mọi vận tốc rác, và nó ăn ngay
        // trong khung hình này chứ không phải đợi bước vật lý sau - quan trọng vì
        // isKinematic vừa mới được gỡ xong.
        float massRatio = otherRb.mass / Mathf.Max(rb.mass, 0.01f);
        rb.linearVelocity = impactDir * (impactSpeed * massRatio * wakeImpulseTransfer);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Chỉ Host tính va chạm và sát thương.
        // Nếu để mọi máy cùng tính thì nạn nhân sẽ bị trừ máu nhiều lần.
        if (Object == null || !Object.IsValid || !HasStateAuthority) return;

        // ĐANG NGỦ MÀ BỊ ĐÂM -> tỉnh dậy và văng đi theo cú đâm.
        //
        // Phải xét TRƯỚC dòng chặn bên dưới. Vật đang ngủ thì isMovingAsBullet = false,
        // nên nếu để sau thì hàm thoát mất và vật ngủ trở thành bất tử: bắn gì vào cũng
        // trơ ra như đá tảng.
        if (IsSleeping)
        {
            WakeUpFromImpact(collision);
        }

        if (!isMovingAsBullet) return;

        // Bỏ qua va chạm với chính người bắn ra nó
        if (shooterOwner != null && collision.collider.gameObject == shooterOwner.gameObject) return;

        // THÙNG TNT: nổ khi va vào BẤT CỨ THỨ GÌ, kể cả người chơi.
        //
        // Phải xét TRƯỚC nhánh "trúng đối thủ" bên dưới. Nếu để sau, TNT đâm vào người
        // sẽ rơi vào nhánh đó rồi return mất, thành ra chỉ gây sát thương va chạm thường
        // và không bao giờ nổ. Sát thương nổ đã bao gồm cả người bị đâm trúng,
        // vì Explode() quét toàn bộ bán kính.
        if (CurrentType == ObjectType.TNT)
        {
            Explode();
            return;
        }

        // Trúng đối thủ
        if (collision.collider.CompareTag("Player"))
        {
            PlayerHealth health = collision.collider.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(CurrentDamage);
            }

            // HẤT VĂNG NGƯỜI TRÚNG ĐẠN.
            //
            // Trước 09/08 phần này KHÔNG TỒN TẠI - trúng đạn chỉ mất máu chứ không hề
            // bị đẩy. Với chế độ Quá Tải thì đó là thiếu sót lớn, vì lực văng chính là
            // toàn bộ trò chơi: nạn nhân nhiễm điện càng nặng phải bay càng xa.
            //
            // Hướng đẩy tính từ vật tới người, ép về phương ngang rồi hất nhẹ lên -
            // cùng công thức với cú đấm cận chiến, để hai đòn cho cảm giác nhất quán.
            // Nhấc chân khỏi mặt đất mới bay xa được, dính đất thì ma sát hãm lại ngay.
            Vector3 hitDir = collision.collider.transform.position - transform.position;
            hitDir.y = 0f;
            if (hitDir.sqrMagnitude < 0.0001f) hitDir = transform.forward;
            hitDir.Normalize();
            hitDir.y = 0.25f;

            // Đạn mạnh đẩy mạnh: Normal(10) x1, Heavy(20) x2, Spike(25) x2.5.
            // Tự động đúng tỉ lệ, không phải chỉnh tay từng prefab.
            float knockback = hitKnockbackForce * (CurrentDamage / 10f);

            // KHÔNG truyền scaleByCharge - để mặc định true, vì đây là cú đẩy từ bên ngoài.
            // Chính chỗ này làm nạn nhân nhiễm điện nặng bay xa gấp nhiều lần.
            FPSMovement playerMove = collision.collider.GetComponent<FPSMovement>();
            if (playerMove != null) playerMove.AddImpact(hitDir, knockback);

            DummyGravity dummyGrav = collision.collider.GetComponent<DummyGravity>();
            if (dummyGrav != null) dummyGrav.AddImpact(hitDir, knockback);

            // Trúng người rồi thì hết là đạn, tránh trừ máu nhiều lần trong một cú bắn
            ResetBulletState();
            return;
        }

        // VA VÀO MÔI TRƯỜNG (đất, tường, cây...)
        //
        // Đang bay nhanh thì VẪN tính là đạn, cứ bay tiếp.
        //
        // Vì sao cần: vật nằm sẵn dưới đất thì đang chạm mặt đất. Vừa đẩy đi là dính ngay
        // một lần va chạm với mặt đất -> code cũ tước tư cách đạn ngay lập tức, nên đẩy vật
        // từ môi trường không bao giờ gây được sát thương. Bắn từ tay thì không dính lỗi này
        // vì lúc đó vật đang lơ lửng giữa không trung.
        // ĐẤT/GỜ hay TƯỜNG? Phân biệt bằng PHÁP TUYẾN của điểm chạm.
        //
        // Pháp tuyến hướng lên  -> mặt đất, mô đất, gờ nhỏ -> chỉ lướt qua, giữ nguyên hướng bay
        // Pháp tuyến nằm ngang  -> tường, thân cây, vách đá -> để vật lý dội lại bình thường
        //
        // Nhờ vậy đạn CÀY THẲNG qua chỗ đất lồi lõm mà vẫn còn phản ứng đúng khi đâm tường.
        // Không phân biệt thì hoặc là đạn xuyên tường, hoặc là vẫn lệch hướng ở mọi gờ đất.
        if (collision.contactCount > 0)
        {
            float normalUp = collision.GetContact(0).normal.y;
            _keepHeadingNextTick = normalUp > groundNormalThreshold;
        }

        // Vừa đập vào môi trường -> hẹn giảm bớt cú nảy ở tick sau.
        // Xem DampBounceRise() để biết vì sao không xử lý thẳng tại đây.
        _dampRiseNextTick = true;

        if (rb != null && rb.linearVelocity.magnitude >= minBulletSpeed) return;

        ResetBulletState();
    }

    /// <summary>
    /// Xin quyền can thiệp vào vật đang bay (cản Heavy, hoặc hút nhầm Spike).
    /// Trả về false nếu vật không bay, hoặc đã bị can thiệp trong lần bay này rồi.
    /// </summary>
    public bool TryCounterInFlight()
    {
        if (!HasStateAuthority) return false;
        if (!isMovingAsBullet) return false;
        if (WasCounteredInFlight) return false;

        WasCounteredInFlight = true;
        return true;
    }

    public void ResetBulletState()
    {
        if (!HasStateAuthority) return;

        isMovingAsBullet = false;
        shooterOwner = null;
        WasCounteredInFlight = false;
        CurrentDamage = baseDamage; // trả sát thương về mức gốc của prefab

        if (rb != null)
        {
            rb.useGravity = true;
            rb.linearDamping = 0.05f; // Giá trị mặc định của Unity 6
        }
    }

    void Explode()
    {
        Debug.Log("<color=red><b>TNT BARREL EXPLODED!</b></color>");

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

        // Dùng HashSet để lưu danh sách Player đã dính sát thương, tránh bị trừ máu 2 lần do trùng Collider
        HashSet<PlayerHealth> playersDamaged = new HashSet<PlayerHealth>();

        foreach (Collider hit in colliders)
        {
            // 1. Tác động lực đẩy lên các vật thể vật lý môi trường (Rigidbody)
            Rigidbody targetRb = hit.GetComponent<Rigidbody>();
            if (targetRb != null && targetRb != rb)
            {
                // ĐÁNH THỨC TRƯỚC KHI CỘNG LỰC.
                //
                // AddExplosionForce hoàn toàn vô tác dụng lên Rigidbody kinematic - PhysX
                // bỏ qua mọi lực tác động lên vật kinematic. Thiếu hai dòng này thì bom nổ
                // giữa đống bàn ghế đang ngủ mà không cái nào nhúc nhích.
                MagneticObject targetMag = hit.GetComponent<MagneticObject>();
                if (targetMag != null) targetMag.WakeUp();

                targetRb.AddExplosionForce(explosionForce * debrisForceMultiplier,
                                           transform.position, explosionRadius, 1f, ForceMode.Impulse);

                // MẢNH VĂNG CŨNG LÀ ĐẠN.
                //
                // Chủ nhân kế thừa từ chính thùng TNT này. Nhờ vậy người ném TNT không bị
                // mảnh văng của chính mình đánh - dòng "bỏ qua va chạm với người bắn" trong
                // OnCollisionEnter lo phần đó. Sóng nổ thì vẫn trúng họ như thường, vì vòng
                // lặp tính sát thương người chơi bên dưới không xét chủ nhân.
                //
                // Vì sao miễn nhiễm với mảnh văng của mình: một vụ nổ thổi cả chục vật cùng
                // lúc, mà người ném thường đứng gần. Không miễn thì ném TNT gần như luôn
                // đồng nghĩa tự sát - không phải mạo hiểm nữa mà là vô dụng.
                //
                // TNT nằm trong bán kính cũng bị đánh dấu -> nó thành đạn -> va vào bất cứ
                // thứ gì là nổ tiếp. Đó chính là phản ứng dây chuyền, cố ý để vậy.
                if (debrisBecomesBullet && targetMag != null)
                {
                    targetMag.MarkAsBullet(shooterOwner);
                }
            }

            // 2. Sát thương và Lực văng cho Player / Dummy (CharacterController)
            if (hit.CompareTag("Player"))
            {
                PlayerHealth health = hit.GetComponent<PlayerHealth>();
                if (health != null && !playersDamaged.Contains(health))
                {
                    playersDamaged.Add(health); // Đánh dấu Player này đã tính toán xong

                    float distance = Vector3.Distance(transform.position, hit.transform.position);
                    float damageMultiplier = 1f - (distance / explosionRadius);
                    health.TakeDamage(tntDamage * Mathf.Clamp01(damageMultiplier));
                }

                // Đẩy văng nhân vật dùng CharacterController theo hướng vụ nổ
                Vector3 explodeDir = (hit.transform.position - transform.position).normalized;
                explodeDir.y = 0.4f; // Hất nhẹ lên không trung

                FPSMovement playerMove = hit.GetComponent<FPSMovement>();
                DummyGravity dummyGrav = hit.GetComponent<DummyGravity>();

                if (playerMove != null) playerMove.AddImpact(explodeDir, explosionForce * 2f);
                if (dummyGrav != null) dummyGrav.AddImpact(explodeDir, explosionForce * 2f);
            }
        }

        // ẨN ĐI, KHÔNG XOÁ.
        //
        // Trước đây dùng Runner.Despawn(Object). Nhưng despawn là xoá vĩnh viễn khỏi mạng,
        // nên sang round mới không dựng lại được đúng vật đó nữa - mà spawn lại từ prefab
        // thì sai, vì nhiều prefab khác nhau cùng thuộc một loại.
        //
        // Ẩn đi thì giữ nguyên đúng vật, đúng chỗ đứng gốc, đúng mọi thông số;
        // ResetForNewRound() chỉ việc bật lại.
        IsDestroyed = true;
        ExplodeCount++; // để mọi máy phát tiếng nổ, xem OnExploded

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    /// <summary>
    /// Trả vật về nguyên trạng đầu trận: đúng chỗ, đúng hướng, đúng cỡ, sạch điện,
    /// hết là đạn, hết bị chế thành TNT, và sống lại nếu đã nổ.
    ///
    /// GameManager gọi cho TOÀN BỘ vật thể mỗi khi bắt đầu round mới.
    /// Bắt buộc gọi từ FixedUpdateNetwork vì Teleport() của Fusion yêu cầu vậy.
    /// </summary>
    public void ResetForNewRound()
    {
        if (!HasStateAuthority) return;

        // Xoá mọi trạng thái tạm của round cũ
        IsDestroyed = false;
        IsStored = false;
        isMovingAsBullet = false;
        shooterOwner = null;
        WasCounteredInFlight = false;
        BulletArmTimer = TickTimer.None;

        // Về đúng loại và sát thương gốc của prefab (huỷ tác dụng Chai Xăng)
        CurrentType = objectType;
        CurrentDamage = baseDamage;

        // Về trung tính, ai muốn dùng thì phải nạp điện lại từ đầu
        currentPolarity = Polarity.None;

        transform.localScale = _originalScale;

        // Dừng hẳn rồi mới dịch chuyển, nếu không vật về tới chỗ cũ vẫn còn đà bay tiếp.
        // Phải tạm bỏ kinematic ở đây để Teleport() bên dưới chạy đúng đường vật lý.
        SleepCheckTimer = TickTimer.None;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearDamping = 0.05f;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Dùng Teleport() của NetworkRigidbody3D chứ KHÔNG gán thẳng transform.position.
        // Gán thẳng thì máy khác vẫn nội suy mượt từ chỗ cũ sang chỗ mới, nhìn như vật
        // tự bay ngang qua map. Teleport báo cho Fusion "đây là nhảy cóc, đừng nội suy".
        NetworkRigidbody3D netRb = GetComponent<NetworkRigidbody3D>();
        if (netRb != null)
        {
            netRb.Teleport(_originalPosition, _originalRotation);
        }
        else
        {
            transform.SetPositionAndRotation(_originalPosition, _originalRotation);
        }

        // ĐÓNG BĂNG NGAY TẠI CHỖ VỪA ĐẶT - đây là chỗ đã sửa ngày 16/08.
        //
        // Bản cũ cố ý để vật THỨC dậy, với lý do "nó cần rơi xuống và tự ổn định".
        // Lý do đó SAI, và nó chính là nguyên nhân vài cái cây đổ rạp ngay đầu round:
        // cây cao và mảnh, chỉ cần collider chạm đất lệch một chút là trọng lực lật đổ.
        // Mà muốn ngủ lại phải đứng yên liên tục sleepDelay giây, trong khi cây đang đổ
        // thì tốc độ luôn vượt ngưỡng -> nó đổ hẳn xuống mới thôi.
        //
        // _originalPosition CHÍNH LÀ chỗ đã đặt tay trong Editor. Vật lý "ổn định" chỉ có
        // thể đẩy vật RỜI KHỎI chỗ đó, không bao giờ đưa nó về đúng hơn. Nên đóng băng
        // thẳng: mỗi round bắt đầu với bản đồ giống hệt bản đồ bạn đã dựng.
        //
        // Vật đặt lơ lửng sẽ treo giữa không trung - đó là ĐÚNG, không phải lỗi.
        // Nó cho thấy bạn đặt sai chỗ trong Editor, và sửa ở Editor mới là cách chữa đúng.
        if (enableAutoSleep)
        {
            IsSleeping = true;
            ApplySleepState();
        }
    }
}
