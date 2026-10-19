using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rào chắn giữ người chơi trong khu vực xuất phát suốt pha chuẩn bị,
/// và tự hạ xuống khi bắt đầu chiến đấu. Giống rào spawn của Valorant.
///
/// CỐ Ý là MonoBehaviour thường, KHÔNG cần NetworkObject.
///
/// Lý do: trạng thái đóng/mở của rào suy ra được hoàn toàn từ pha hiện tại của
/// GameManager, mà pha đó đã là [Networked] rồi. Mỗi máy tự đọc pha rồi tự bật/tắt
/// là khớp nhau tuyệt đối, không cần truyền thêm gì qua mạng.
///
/// Đây là cùng nguyên tắc đã dùng cho vật cầm trên tay: chỉ đồng bộ thứ BẮT BUỘC,
/// thứ nào suy ra được tại chỗ thì tính tại chỗ.
///
/// HÌNH (làm lại 17/10): màn năng lượng theo MÀU ĐỘI, shader "SkyArena/Barrier Curtain".
///   - KÍNH MỜ (19/10, trước đó đặc ~99%): thấy dáng phía sau nhưng nhoè + ám màu đội. Viền địch, bảng
///     tên, hiệu ứng phía sau bị che nhờ tường ghi chiều sâu — không lộ vị trí rõ ràng.
///   - Chỉ tan dần BÊN NGOÀI MÉP ĐẢO và khi lên cao → không bao giờ thấy mép / góc của khối
///     tường 200m, mà mọi đường nhìn sang căn cứ địch vẫn cắt tường ở chỗ đặc.
///   - Gợn sóng ở chỗ người chơi hay vật thể chạm vào.
///   - Vào trận: va chạm tắt NGAY (luật chơi phải chính xác), còn hình thì tan từ dưới lên
///     trong <see cref="dissolveDuration"/> giây rồi mới tắt hẳn. Round mới thì mọc lại.
/// Mọi thứ trên đây chỉ là HÌNH, mỗi máy tự vẽ — không ảnh hưởng mạng.
/// </summary>
public class RoundBarrier : MonoBehaviour
{
    [Tooltip("Các collider chặn đường. Để trống thì tự tìm trên object này và các object con.")]
    public Collider[] barrierColliders;

    [Tooltip("Phần nhìn thấy được của rào. Để trống thì tự tìm.")]
    public Renderer[] barrierRenderers;

    [Header("Hình (17/10)")]
    [Tooltip("Màu màn chắn. Để màu ĐEN (mặc định) thì tự chọn theo tên object: có chữ \"Red\" → đỏ, \"Blue\" → xanh.")]
    public Color barrierColor = Color.black;

    [Tooltip("Màn đặc trong bán kính này tính từ TÂM ĐẢO (mét). Phải lớn hơn bán kính đảo (~37m), " +
             "không thì nhìn xiên qua phần đã tan sẽ thấy được căn cứ địch.")]
    public float fadeStart = 40f;

    [Tooltip("...và tan hết ở bán kính này — phía trên khoảng trống ngoài đảo. Hai đầu tường cách " +
             "tâm đảo ~100m nên không bao giờ lộ ra.")]
    public float fadeEnd = 85f;

    [Tooltip("Tâm đảo (x, z). Để (0, 0) thì tự lấy vị trí của CloudBank (vốn đặt ở tâm đảo), không có thì (40, 40).")]
    public Vector2 islandCenter = Vector2.zero;

    [Tooltip("Thời gian tan rã khi vào trận (giây). Va chạm thì tắt ngay từ giây đầu.")]
    public float dissolveDuration = 0.8f;

    [Tooltip("Thời gian mọc lại khi sang round mới (giây).")]
    public float riseDuration = 0.5f;

    private static readonly Color RedTeam = new Color(1.0f, 0.17f, 0.12f);
    private static readonly Color BlueTeam = new Color(0.12f, 0.45f, 1.0f);

    // Trạng thái đang áp dụng. Dùng để chỉ đụng vào component khi thật sự có thay đổi,
    // thay vì ghi đè mỗi khung hình cho phí.
    private bool _isBlocking;
    private bool _hasAppliedOnce;

    private Material _material;          // bản riêng của từng bức tường (màu đội khác nhau)
    private float _dissolve;             // 0 = nguyên vẹn, 1 = biến mất
    private float _dissolveTarget;
    private readonly Vector4[] _hits = new Vector4[4];
    private int _nextHit;
    private readonly Dictionary<Transform, float> _lastTouch = new Dictionary<Transform, float>();

    private static readonly int IdColor = Shader.PropertyToID("_Color");
    private static readonly int IdEdgeColor = Shader.PropertyToID("_EdgeColor");
    private static readonly int IdBaseY = Shader.PropertyToID("_BaseY");
    private static readonly int IdFadeStart = Shader.PropertyToID("_FadeStart");
    private static readonly int IdFadeEnd = Shader.PropertyToID("_FadeEnd");
    private static readonly int IdFadeCenter = Shader.PropertyToID("_FadeCenter");
    private static readonly int IdDissolve = Shader.PropertyToID("_Dissolve");
    private static readonly int[] IdHits =
    {
        Shader.PropertyToID("_Hit0"), Shader.PropertyToID("_Hit1"),
        Shader.PropertyToID("_Hit2"), Shader.PropertyToID("_Hit3"),
    };

    void Awake()
    {
        if (barrierColliders == null || barrierColliders.Length == 0)
        {
            barrierColliders = GetComponentsInChildren<Collider>();
        }

        if (barrierRenderers == null || barrierRenderers.Length == 0)
        {
            barrierRenderers = GetComponentsInChildren<Renderer>();
        }

        BuildMaterial();
    }

    void OnDestroy()
    {
        if (_material != null) Destroy(_material);
    }

    // Dựng vật liệu từ shader trong Resources (chắc chắn vào bản build) — không phải kéo thả gì.
    // Không tìm thấy shader thì giữ vật liệu cũ trong scene, game vẫn chạy bình thường.
    private void BuildMaterial()
    {
        Shader shader = Shader.Find("SkyArena/Barrier Curtain");
        if (shader == null)
        {
            Debug.LogWarning("[RÀO] Không tìm thấy shader SkyArena/Barrier Curtain — dùng hình cũ.", this);
            return;
        }

        Color c = barrierColor;
        if (c.maxColorComponent < 0.01f)
            c = name.Contains("Blue") ? BlueTeam : RedTeam;

        _material = new Material(shader) { name = name + " (runtime)" };

        // (19/10) Vẽ TRƯỚC mọi vật trong suốt khác (2995 < 3000) và ghi chiều sâu ở phần tường đủ đậm
        // (lượt "BarrierDepth" trong shader). Nhờ vậy mọi thứ trong suốt NẰM SAU tường — mây, viền địch,
        // bảng tên trên đầu, hiệu ứng — bị chiều sâu của tường che, kể cả khi Unity xếp chúng vẽ sau.
        // (Bản 17/10 dùng 3050 = vẽ SAU mây để mây không đè lên tường; nhưng như vậy bảng tên địch, vốn vẽ
        // ở 3000, vẫn đọc được xuyên rào.) Ảnh kính mờ lấy từ Opaque Texture nên vẽ sớm không mất gì.
        _material.renderQueue = 2995;
        _material.SetColor(IdColor, c * 1.15f);   // nhỉnh hơn 1 một chút cho Bloom bắt nhẹ
        _material.SetColor(IdEdgeColor, Color.Lerp(c, Color.white, 0.6f) * 1.2f);
        _material.SetFloat(IdBaseY, FindGroundY());
        _material.SetFloat(IdFadeStart, fadeStart);
        _material.SetFloat(IdFadeEnd, fadeEnd);
        _material.SetVector(IdFadeCenter, FindIslandCenter());
        for (int i = 0; i < _hits.Length; i++) _hits[i] = new Vector4(0f, -999f, 0f, -99f);

        foreach (Renderer r in barrierRenderers)
            if (r != null) r.sharedMaterial = _material;
    }

    private Vector4 FindIslandCenter()
    {
        if (islandCenter != Vector2.zero) return new Vector4(islandCenter.x, islandCenter.y, 0f, 0f);
        CloudBank cloud = FindFirstObjectByType<CloudBank>();
        if (cloud != null) return new Vector4(cloud.transform.position.x, cloud.transform.position.z, 0f, 0f);
        return new Vector4(40f, 40f, 0f, 0f);
    }

    // Độ cao mặt đất ngay dưới tâm bức tường — để vạch sáng chân tường nằm đúng mặt đảo.
    private float FindGroundY()
    {
        Vector3 c = transform.position;
        foreach (RaycastHit h in Physics.RaycastAll(new Vector3(c.x, c.y + 150f, c.z), Vector3.down, 400f,
                                                    ~0, QueryTriggerInteraction.Ignore))
        {
            if (h.collider.transform.IsChildOf(transform)) continue;   // bỏ qua chính bức tường
            if (h.collider.attachedRigidbody != null) continue;        // bỏ qua đá, cây (đạn)
            return h.point.y;
        }
        return 4.4f;   // mặt đảo TestScene, phòng khi không bắn trúng gì
    }

    void Update()
    {
        bool shouldBlock = ShouldBlock();

        if (!_hasAppliedOnce || shouldBlock != _isBlocking)
        {
            bool first = !_hasAppliedOnce;
            _isBlocking = shouldBlock;
            _hasAppliedOnce = true;
            ApplyState(shouldBlock, first);
        }

        AnimateDissolve();
        if (_isBlocking) DetectPlayerTouch();
    }

    private bool ShouldBlock()
    {
        GameManager gm = GameManager.Instance;

        // Chưa có GameManager - ví dụ khi bạn bấm Play thẳng vào TestScene để test nhanh.
        // Lúc đó MỞ rào, để không bị nhốt trong góc mà không hiểu vì sao.
        if (gm == null) return false;

        return gm.Phase == GameManager.GamePhase.WaitingToStart
            || gm.Phase == GameManager.GamePhase.BuyPhase;
    }

    private void ApplyState(bool blocking, bool instant)
    {
        // VA CHẠM: đổi NGAY. Luật chơi không được chờ hiệu ứng.
        foreach (Collider c in barrierColliders)
        {
            if (c != null) c.enabled = blocking;
        }

        _dissolveTarget = blocking ? 0f : 1f;

        // Lần đầu (vừa vào scene) thì đặt thẳng, không diễn hiệu ứng
        if (instant || _material == null)
        {
            _dissolve = _dissolveTarget;
            SetRenderersVisible(blocking);
            PushDissolve();
        }
        else if (blocking)
        {
            // Round mới: bật hình lên rồi cho mọc lại từ chỗ đang tan
            SetRenderersVisible(true);
        }

        if (!blocking)
        {
            Debug.Log("<color=lime>[RÀO] Rào chắn đã hạ - xông lên!</color>");
        }
    }

    private void AnimateDissolve()
    {
        if (_material == null || Mathf.Approximately(_dissolve, _dissolveTarget)) return;

        float duration = _dissolveTarget > _dissolve ? dissolveDuration : riseDuration;
        _dissolve = Mathf.MoveTowards(_dissolve, _dissolveTarget, Time.deltaTime / Mathf.Max(0.01f, duration));
        PushDissolve();

        // Tan xong hẳn mới tắt renderer — đỡ tốn một lệnh vẽ suốt pha chiến đấu
        if (_dissolve >= 1f) SetRenderersVisible(false);
    }

    private void PushDissolve()
    {
        if (_material != null) _material.SetFloat(IdDissolve, _dissolve);
    }

    private void SetRenderersVisible(bool visible)
    {
        foreach (Renderer r in barrierRenderers)
        {
            if (r != null) r.enabled = visible;
        }
    }

    // ---------- GỢN SÓNG KHI BỊ CHẠM ----------

    /// <summary>Thêm một vòng gợn sóng tại điểm này (toạ độ thế giới). Giữ tối đa 4 vòng cùng lúc.</summary>
    public void AddRipple(Vector3 worldPoint)
    {
        if (_material == null) return;
        _hits[_nextHit] = new Vector4(worldPoint.x, worldPoint.y, worldPoint.z, Time.timeSinceLevelLoad);
        _material.SetVector(IdHits[_nextHit], _hits[_nextHit]);
        _nextHit = (_nextHit + 1) % _hits.Length;
    }

    // Đá, cây (có Rigidbody) đâm vào tường. Người chơi dùng CharacterController nên KHÔNG
    // gọi tới hàm này — người chơi được dò riêng ở DetectPlayerTouch.
    void OnCollisionEnter(Collision collision)
    {
        if (!_isBlocking || collision.contactCount == 0) return;
        AddRipple(collision.GetContact(0).point);
    }

    // Người chơi áp sát tường (dưới 0.7m) thì gợn sóng ở ngang ngực, mỗi người tối đa 2 lần/giây.
    private void DetectPlayerTouch()
    {
        if (barrierColliders.Length == 0 || barrierColliders[0] == null) return;
        Collider wall = barrierColliders[0];

        foreach (PlayerHealth p in PlayerHealth.AllPlayers)
        {
            if (p == null) continue;
            Transform t = p.transform;
            Vector3 chest = t.position + Vector3.up * 1.2f;
            Vector3 closest = wall.ClosestPoint(chest);
            if ((closest - chest).sqrMagnitude > 0.7f * 0.7f) continue;

            if (_lastTouch.TryGetValue(t, out float last) && Time.time - last < 0.5f) continue;
            _lastTouch[t] = Time.time;
            AddRipple(closest);
        }
    }

#if UNITY_EDITOR
    /// <summary>Chỉ để chụp ảnh xem trước trong Editor (không chạy Update).</summary>
    public void PreviewSetup(float dissolve, Vector3? ripplePoint, float rippleAge)
    {
        if (_material == null) { if (barrierRenderers == null || barrierRenderers.Length == 0) barrierRenderers = GetComponentsInChildren<Renderer>(); BuildMaterial(); }
        _dissolve = dissolve; PushDissolve();
        if (ripplePoint.HasValue && _material != null)
            _material.SetVector(IdHits[0], new Vector4(ripplePoint.Value.x, ripplePoint.Value.y, ripplePoint.Value.z, (float)UnityEditor.EditorApplication.timeSinceStartup - rippleAge));
    }

    /// <summary>Chỉ để chụp ảnh: đặt thẳng một ô gợn sóng.</summary>
    public void PreviewSetHit(int slot, Vector4 hit)
    {
        if (_material != null) _material.SetVector(IdHits[slot], hit);
    }
#endif
}
