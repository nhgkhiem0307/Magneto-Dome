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
    [Tooltip("Bay chậm hơn tốc độ này thì thôi không tính là đạn nữa, không gây sát thương.")]
    public float minBulletSpeed = 3f;

    [Tooltip("Khoảng thời gian ngay sau khi phóng, chưa xét tốc độ vội. Cần vì lực đẩy phải sang bước vật lý kế tiếp mới thành vận tốc.")]
    public float bulletArmTime = 0.25f;

    [Header("Explosion Settings (chỉ dùng cho TNT)")]
    public float explosionRadius = 6f;
    public float explosionForce = 15f;
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
        }

        // Áp màu, hào quang và trạng thái ẩn/hiện theo dữ liệu hiện tại.
        // Gọi ở đây để vật thể vào trận muộn vẫn hiển thị đúng.
        OnPolarityChanged();
        ApplyStoredState();
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

        // Khoá một khoảng ngắn không cho tự tắt tư cách đạn.
        // Cần vì lực đẩy chỉ thật sự biến thành vận tốc ở bước vật lý kế tiếp,
        // nên ngay lúc vừa phóng thì tốc độ vẫn đang bằng 0.
        BulletArmTimer = TickTimer.CreateFromSeconds(Runner, bulletArmTime);
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        if (!isMovingAsBullet) return;
        if (rb == null) return;

        // Chưa hết thời gian khoá thì cứ để yên
        if (!BulletArmTimer.ExpiredOrNotRunning(Runner)) return;

        // Bay chậm lại rồi thì thôi không còn là đạn nữa, dù chưa va vào đâu cả.
        // Không có bước này thì một vật lăn tới khi dừng hẳn vẫn gây sát thương.
        if (rb.linearVelocity.magnitude < minBulletSpeed)
        {
            ResetBulletState();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Chỉ Host tính va chạm và sát thương.
        // Nếu để mọi máy cùng tính thì nạn nhân sẽ bị trừ máu nhiều lần.
        if (Object == null || !Object.IsValid || !HasStateAuthority) return;

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
                targetRb.AddExplosionForce(explosionForce, transform.position, explosionRadius, 1f, ForceMode.Impulse);
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

        // Dừng hẳn rồi mới dịch chuyển, nếu không vật về tới chỗ cũ vẫn còn đà bay tiếp
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
    }
}
