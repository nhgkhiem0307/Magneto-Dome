using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Tia điện chạy trên găng tay, cho biết găng đang mang điện ÂM hay DƯƠNG.
///
/// ⚠️ ĐÂY KHÔNG PHẢI TRANG TRÍ. Bấm nhầm phím 1/2 là cú bắn HÚT thay vì ĐẨY - sai hoàn
/// toàn ý định. Trước đây muốn kiểm tra thì phải RỜI MẮT khỏi tâm ngắm liếc xuống HUD ở
/// góc màn hình, ngay giữa lúc giao tranh. Quầng sáng ở tay trả lời đúng câu hỏi đó mà
/// mắt không phải rời mục tiêu.
///
/// CHỈ CHẠY CHO NGƯỜI NGỒI TRƯỚC MÁY: nó được gắn vào bản sao viewmodel, mà bản sao đó
/// chỉ dựng cho nhân vật của chính mình. Đối thủ đã có hiệu ứng riêng ở PlayerVisuals
/// (quả cầu sáng trong lòng bàn tay) - hai thứ độc lập, đừng lẫn.
///
/// ⚠️ BẪY LAYER: viewmodel nằm trên layer riêng và được vẽ bằng camera Overlay riêng, vẽ
/// SAU cùng. Tia điện để ở layer mặc định thì camera chính vẽ nó trước, rồi bàn tay đè
/// lên - mất sạch. Đã dính đúng lỗi này ở chớp sáng đầu nòng. Init() nhận layer để tránh.
/// </summary>
public class GloveArcs : MonoBehaviour
{
    [Tooltip("Mấy tia cùng lúc trên mỗi bàn tay.")]
    public int arcsPerHand = 6;

    [Tooltip("Mỗi tia gồm mấy đoạn gấp khúc. Ít quá thì ra sợi dây, nhiều quá thì ra sợi bông.")]
    public int segments = 7;

    [Tooltip("Biên độ gấp khúc, mét. Tia điện phải GÃY GÓC - đường cong mượt đọc thành " +
             "sợi chỉ, không thành phóng điện.")]
    public float jitter = 0.016f;

    [Tooltip("Bề dày QUẦNG MÀU của tia, mét. Ruột trắng bên trong tự lấy 35% số này.")]
    public float width = 0.017f;

    [Tooltip("Màu dạ quang khi găng mang điện DƯƠNG.\n\n" +
             "Không dùng chung màu với vật thể (CombatVFX.PolarityColor). Màu vật thể cố ý " +
             "dịu để không chói khi cả bãi đầy đạn; còn găng tay chỉ có hai bàn tay, nằm sát " +
             "mắt, nên chịu được màu gắt hẳn - và cần gắt thì mới liếc là thấy.")]
    public Color positiveNeon = new Color(1f, 0.12f, 0.22f, 1f);

    [Tooltip("Màu dạ quang khi găng mang điện ÂM.")]
    public Color negativeNeon = new Color(0.1f, 0.65f, 1f, 1f);

    [Tooltip("Bao nhiêu giây thì tia nhảy sang cặp ngón khác.\n\n" +
             "Rất ngắn. Tia điện thật không bao giờ đứng yên - nó tắt rồi đánh lại ở chỗ " +
             "khác. Giữ nguyên một đường quá lâu là mắt đọc thành dây thép.")]
    public float rearcInterval = 0.06f;

    [Tooltip("Độ sáng lúc bình thường, không làm gì.\n\n" +
             "Thấp thôi. Nó phải đủ để LIẾC là biết, nhưng không được chói tới mức chiếm " +
             "mất sự chú ý suốt trận - thứ cần nhìn là đối thủ, không phải tay mình.")]
    [Range(0f, 1f)]
    public float idleIntensity = 0.32f;

    [Tooltip("Loé sáng bao nhiêu giây sau khi ĐỔI điện tích.\n\n" +
             "Đây là phần quan trọng hơn cả. Người chơi cần XÁC NHẬN NGAY rằng phím vừa " +
             "bấm đã ăn - im lìm thì họ phải liếc HUD kiểm tra, đúng cái việc mà hiệu ứng " +
             "này sinh ra để tránh.")]
    public float flashDuration = 0.45f;

    [Tooltip("Đẩy cả tia về phía CAMERA bấy nhiêu mét, để nó luôn nằm TRƯỚC bề mặt bàn tay.\n\n" +
             "⚠️ Neo tia lên mu tay thôi là CHƯA ĐỦ. Mặt nào của bàn tay quay về phía mắt còn " +
             "tuỳ tư thế: tay phải nắm đấm thì mu tay hướng về camera, nhưng tay trái có thể " +
             "cầm nghiêng, mu tay chúc xuống hoặc quay ra ngoài. Lúc đó tia nằm đúng trên mu " +
             "tay mà vẫn bị chính bàn tay che, vì bộ đệm chiều sâu thấy da ở gần mắt hơn.\n\n" +
             "Đẩy về phía camera thì bất kể tay xoay kiểu gì, tia luôn phủ lên mặt đang nhìn " +
             "thấy. Hai bàn tay khác tư thế mà không cần xử lý riêng.")]
    public float cameraBias = 0.035f;

    // ==================== TRẠNG THÁI ====================

    private PlayerMagnetController _magnet;
    private PlayerHealth _health;
    private bool _shown = true;
    private Transform[] _anchorsL, _anchorsR;
    private LineRenderer[] _arcs;    // quầng màu, dày
    private LineRenderer[] _cores;   // ruột trắng nóng, mảnh

    // Mỗi tia nhớ hai đầu mút của nó: (mốc đầu, mốc cuối) trong mảng mốc tương ứng.
    private int[] _fromIdx, _toIdx;
    private bool[] _onRight;

    private float _rearcTimer;
    private float _flash;
    private MagneticObject.Polarity _lastPolarity;
    private bool _ready;

    /// <summary>
    /// Dựng tia. FirstPersonViewmodel gọi vào đây sau khi đã bắt được xương ngón tay.
    /// </summary>
    /// <param name="magnet">Nguồn đọc điện tích găng.</param>
    /// <param name="anchorsLeft">Cổ tay + năm đầu ngón tay TRÁI.</param>
    /// <param name="anchorsRight">Cổ tay + năm đầu ngón tay PHẢI.</param>
    /// <param name="layer">Layer của viewmodel - xem ghi chú ở đầu file.</param>
    public void Init(PlayerMagnetController magnet,
                     Transform[] anchorsLeft, Transform[] anchorsRight, int layer)
    {
        _magnet = magnet;
        _health = magnet != null ? magnet.GetComponent<PlayerHealth>() : null;
        _anchorsL = anchorsLeft;
        _anchorsR = anchorsRight;

        if (CombatVFX.Additive == null) return;
        if (!HasUsableAnchors(_anchorsL) && !HasUsableAnchors(_anchorsR)) return;

        int total = Mathf.Max(1, arcsPerHand) * 2;
        _arcs = new LineRenderer[total];
        _cores = new LineRenderer[total];
        _fromIdx = new int[total];
        _toIdx = new int[total];
        _onRight = new bool[total];

        for (int i = 0; i < total; i++)
        {
            // HAI LỚP CHO MỖI TIA - đây là thứ tạo ra chất DẠ QUANG.
            //
            // Một sợi đơn sắc dù dày cỡ nào cũng chỉ là một vệt màu. Đèn neon và tia điện
            // thật có RUỘT TRẮNG NÓNG bọc trong QUẦNG MÀU: lõi sáng tới mức mọi màu đều bão
            // hoà thành trắng, chỉ phần rìa yếu hơn mới còn giữ được màu. Cùng cách đã làm
            // cho tia laze bắn ra từ ngón tay.
            _arcs[i] = MakeLine("GloveArc", transform, layer);
            _cores[i] = MakeLine("GloveArcCore", _arcs[i].transform, layer);

            _onRight[i] = i >= total / 2;
        }

        if (_magnet != null) _lastPolarity = _magnet.currentGlovePolarity;

        _ready = true;
        PickAnchors();
    }

    private LineRenderer MakeLine(string name, Transform parent, int layer)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.layer = layer;

        LineRenderer line = go.AddComponent<LineRenderer>();
        line.sharedMaterial = CombatVFX.Additive;
        line.useWorldSpace = true;
        line.positionCount = Mathf.Max(2, segments);
        // KHÔNG BO ĐẦU, VÀ THON VỀ 0 Ở HAI ĐẦU.
        //
        // Bản trước để numCapVertices = 3 nên hai đầu tia là hai nửa hình tròn. Tệ hơn,
        // nhiều tia dùng CHUNG điểm neo (cùng một khớp ngón), mà vật liệu lại cộng sáng -
        // mấy cái đầu tròn chồng đúng một chỗ cộng dồn lại thành một CHẤM SÁNG CHÓI.
        //
        // Thon bề dày về 0 ở hai đầu thì không còn gì để chồng: tia mọc ra từ hư không ở
        // đầu này và tan vào hư không ở đầu kia, đúng dáng một tia phóng điện.
        line.numCapVertices = 0;
        line.numCornerVertices = 2;   // bo góc gấp khúc, tia dày mà góc nhọn thì lộ răng cưa
        line.widthCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.18f, 1f),
            new Keyframe(0.82f, 1f),
            new Keyframe(1f, 0f));
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.textureMode = LineTextureMode.Stretch;
        return line;
    }

    private static bool HasUsableAnchors(Transform[] a)
    {
        if (a == null) return false;
        int n = 0;
        foreach (Transform t in a) if (t != null) n++;
        return n >= 2;
    }

    /// <summary>Bốc lại hai đầu mút cho mọi tia.</summary>
    private void PickAnchors()
    {
        for (int i = 0; i < _arcs.Length; i++)
        {
            Transform[] set = _onRight[i] ? _anchorsR : _anchorsL;
            if (!HasUsableAnchors(set)) continue;

            int a = RandomValidIndex(set);
            int b = RandomValidIndex(set);

            // Hai đầu trùng nhau thì tia thành một điểm. Đẩy sang mốc kế tiếp còn dùng được.
            int guard = 0;
            while (b == a && guard++ < 8) b = RandomValidIndex(set);

            _fromIdx[i] = a;
            _toIdx[i] = b;
        }
    }

    private static int RandomValidIndex(Transform[] set)
    {
        for (int attempt = 0; attempt < 12; attempt++)
        {
            int i = Random.Range(0, set.Length);
            if (set[i] != null) return i;
        }
        for (int i = 0; i < set.Length; i++) if (set[i] != null) return i;
        return 0;
    }

    /// <summary>
    /// Chạy ở LateUpdate chứ không phải Update.
    ///
    /// Tia bám vào xương ngón tay, mà xương đó do FirstPersonViewmodel đặt trong
    /// LateUpdate của chính nó. Vẽ ở Update là vẽ theo vị trí xương của khung hình TRƯỚC,
    /// và tia sẽ trễ một nhịp so với bàn tay - nhìn như nó trôi lệch khỏi tay.
    /// </summary>
    private void LateUpdate()
    {
        if (!_ready || _magnet == null) return;

        // --- Chết thì tắt hết tia ---
        //
        // ⚠️ PlayerHealth.ApplyAliveState() tắt renderer khi chết, nhưng nó chỉ tắt những
        // renderer có sẵn LÚC NHÂN VẬT SINH RA. Tia điện được dựng SAU đó và gắn vào camera,
        // nên không nằm trong danh sách - bàn tay biến mất mà tia vẫn lơ lửng trên màn hình.
        // Tự hỏi chủ nhân còn sống không thì khỏi phụ thuộc vào danh sách của ai khác.
        bool alive = _health == null || _health.IsAlive;
        if (alive != _shown)
        {
            _shown = alive;
            SetAllVisible(alive);

            // Hồi sinh thì đừng loé sáng: điện tích có thể đã đổi trong lúc chết, nhưng
            // đó không phải lúc người chơi vừa bấm phím nên không cần xác nhận gì.
            if (alive) _lastPolarity = _magnet.currentGlovePolarity;
        }
        if (!alive) return;

        // --- Đổi điện tích thì loé lên ---
        MagneticObject.Polarity now = _magnet.currentGlovePolarity;
        if (now != _lastPolarity)
        {
            _lastPolarity = now;
            _flash = 1f;
        }

        if (_flash > 0f && flashDuration > 0.001f)
        {
            _flash -= Time.deltaTime / flashDuration;
            if (_flash < 0f) _flash = 0f;
        }

        // --- Nhảy sang cặp ngón khác ---
        _rearcTimer -= Time.deltaTime;
        if (_rearcTimer <= 0f)
        {
            _rearcTimer = Mathf.Max(0.01f, rearcInterval);
            PickAnchors();
        }

        // Bình phương độ loé: sáng rực gần như suốt rồi tắt phụt, thay vì nhạt dần đều.
        float power = idleIntensity + (1f - idleIntensity) * (_flash * _flash);

        Color neon = now == MagneticObject.Polarity.Positive ? positiveNeon : negativeNeon;

        // Nhân màu lên xa quá 1. Với blend cộng sáng, phần dư tràn sang Bloom và toả hào
        // quang ra quanh ngón tay - đó là thứ làm tia trông PHÁT SÁNG chứ không chỉ có màu.
        Color c = neon * (2.6f + power * 3.4f);
        c.a = Mathf.Clamp01(0.55f + power * 0.45f);

        for (int i = 0; i < _arcs.Length; i++)
        {
            DrawArc(i, c, power);
        }
    }

    private void SetAllVisible(bool visible)
    {
        // Chỉ cần TẮT. Lúc bật lại thì DrawArc() tự bật từng tia - kể cả tia nào đang tạm
        // tắt vì mất điểm neo cũng được nó xử lý đúng.
        if (visible) return;

        for (int i = 0; i < _arcs.Length; i++)
        {
            if (_arcs[i] != null) _arcs[i].enabled = false;
            if (_cores[i] != null) _cores[i].enabled = false;
        }
    }

    private void DrawArc(int i, Color color, float power)
    {
        LineRenderer line = _arcs[i];
        if (line == null) return;

        Transform[] set = _onRight[i] ? _anchorsR : _anchorsL;
        if (set == null) { line.enabled = false; return; }

        Transform a = set[_fromIdx[i]];
        Transform b = set[_toIdx[i]];

        if (a == null || b == null)
        {
            line.enabled = false;
            if (_cores[i] != null) _cores[i].enabled = false;
            return;
        }

        line.enabled = true;

        Vector3 p0 = a.position;
        Vector3 p1 = b.position;
        Vector3 dir = p1 - p0;

        // Hai trục vuông góc với tia, để lệch ra mọi phía chứ không chỉ trong một mặt phẳng.
        Vector3 side = Vector3.Cross(dir, Vector3.up);
        if (side.sqrMagnitude < 0.000001f) side = Vector3.Cross(dir, Vector3.right);
        side.Normalize();
        Vector3 up = Vector3.Cross(dir.normalized, side);

        int n = line.positionCount;
        float amp = jitter * (0.6f + power);

        for (int k = 0; k < n; k++)
        {
            float t = (float)k / (n - 1);
            Vector3 p = Vector3.Lerp(p0, p1, t);

            // Hai đầu mút phải DÍNH vào ngón tay, chỉ khúc giữa mới lệch. Không ghì lại
            // thì tia lơ lửng cách đầu ngón mấy phân, trông như rời khỏi tay.
            float hold = Mathf.Sin(t * Mathf.PI);

            p += side * (Random.value - 0.5f) * amp * hold * 2f;
            p += up * (Random.value - 0.5f) * amp * hold * 2f;

            // Đẩy về phía mắt. Object chứa script này gắn thẳng vào camera ở toạ độ gốc,
            // nên vị trí của nó CHÍNH LÀ vị trí mắt - không cần đi tìm camera.
            Vector3 toEye = transform.position - p;
            if (toEye.sqrMagnitude > 0.000001f) p += toEye.normalized * cameraBias;

            line.SetPosition(k, p);
        }

        line.startColor = color;
        line.endColor = color;
        // Chỉ đổi widthMultiplier. Gán startWidth/endWidth là Unity XOÁ đường cong thon
        // ở trên và thay bằng đường thẳng hai điểm - chấm tròn lại quay về.
        line.widthMultiplier = width * (0.8f + power * 0.7f);

        // RUỘT: cùng đường gấp khúc, mảnh hơn, pha mạnh về trắng.
        LineRenderer core = _cores[i];
        if (core == null) return;

        core.enabled = true;
        for (int k = 0; k < n; k++) core.SetPosition(k, line.GetPosition(k));

        // Pha 70% về trắng chứ không trắng hẳn, để liếc qua vẫn nhận ra âm hay dương.
        Color cc = Color.Lerp(color, Color.white * color.maxColorComponent, 0.7f);
        cc.a = color.a;
        core.startColor = cc;
        core.endColor = cc;
        core.widthMultiplier = line.widthMultiplier * 0.35f;
    }
}
