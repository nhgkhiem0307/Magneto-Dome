using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Kho hiệu ứng chiến đấu dùng chung: tia laze, vòng sóng nổ, vệt đạn.
///
/// ⚠️ MỌI THỨ Ở ĐÂY SINH RA LÚC CHẠY, KHÔNG CÓ PREFAB, KHÔNG CÓ ẢNH TRONG DỰ ÁN.
///
/// Ba lý do, theo thứ tự quan trọng:
///   1. Assets/Resources đã 528MB. Thêm ảnh là thêm vào dung lượng bản build.
///   2. Prefab hiệu ứng phải kéo thả vào Inspector, mà đó là chỗ hay quên nhất - quên
///      một ô là hiệu ứng im lặng biến mất, không báo lỗi gì.
///   3. Sinh bằng code thì mọi máy chắc chắn có, không phụ thuộc ai đã gán gì.
///
/// ⚠️ BẪY URP ĐÃ DÍNH BA LẦN Ở CONTROLZONE - ĐỪNG LẶP LẠI:
/// shader "Sprites/Default" là của pipeline CŨ. URP bỏ qua nó IM LẶNG: không lỗi, không
/// cảnh báo, chỉ đơn giản là không thấy gì. Phải dùng Particles/Unlit và tự đặt blend.
/// Công thức bên dưới chép nguyên từ ControlZone vì nó đã được kiểm chứng là chạy.
/// </summary>
public static class CombatVFX
{
    private static Material _additive;
    private static Texture2D _softDot;

    /// <summary>
    /// Bật để in ra Console mỗi lần có hiệu ứng được gọi.
    ///
    /// Có nó thì phân biệt được HAI trường hợp rất khác nhau nhưng nhìn giống hệt: "móc
    /// sự kiện không chạy" và "chạy rồi nhưng vẽ ra không thấy". Không có log thì chỉ
    /// biết là 'không thấy gì' và phải đoán mò.
    ///
    /// Đã TẮT cho bản nộp: mỗi cú đấm, phát bắn đều in một dòng, bản build ghi hết vào
    /// file Player.log. Hiệu ứng lại không hiện thì bật lên true để dò.
    /// </summary>
    public static bool logVFX = false;

    /// <summary>
    /// Vật liệu cộng sáng dùng chung cho MỌI hiệu ứng ở đây.
    ///
    /// Chỉ tạo một lần rồi dùng lại mãi. Mỗi vật liệu riêng là một batch vẽ riêng, mà chủ
    /// project đã chốt giữ batch dưới 300 - không thể mỗi tia laze một vật liệu được.
    /// </summary>
    public static Material Additive
    {
        get
        {
            if (_additive != null) return _additive;

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");

            if (shader == null)
            {
                Debug.LogError("[CombatVFX] Không tìm thấy shader để vẽ hiệu ứng.");
                return null;
            }

            _additive = new Material(shader) { name = "CombatVFX Additive (runtime)" };

            // CỘNG SÁNG: hiệu ứng là ÁNH SÁNG, nó cộng vào cảnh chứ không che cảnh.
            // Và cộng sáng thì không bao giờ vô hình trên nền tối - an toàn hơn hẳn.
            _additive.SetFloat("_Surface", 1f);   // Transparent
            _additive.SetFloat("_Blend", 2f);     // Additive
            _additive.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            _additive.SetFloat("_DstBlend", (float)BlendMode.One);
            _additive.SetFloat("_ZWrite", 0f);
            _additive.SetFloat("_Cull", 0f);      // hai mặt

            _additive.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _additive.DisableKeyword("_ALPHATEST_ON");

            _additive.renderQueue = (int)RenderQueue.Transparent;

            // ⚠️ PHẢI GÁN CẢ BA, KHÔNG CHỈ mainTexture.
            //
            // mainTexture ghi vào ô "_MainTex" - tên của pipeline CŨ. Shader URP đọc ô
            // "_BaseMap", nên gán mỗi mainTexture là shader không nhận được gì. Đây đúng
            // cách ControlZone làm và đó là lý do nó hiện được.
            _additive.mainTexture = SoftLine;
            if (_additive.HasProperty("_BaseMap")) _additive.SetTexture("_BaseMap", SoftLine);
            if (_additive.HasProperty("_BaseColor")) _additive.SetColor("_BaseColor", Color.white);

            return _additive;
        }
    }

    /// <summary>
    /// Ảnh cho SỢI SÁNG: đặc suốt chiều dài, mềm dần ra hai mép bên.
    ///
    /// ⚠️ BẢN ĐẦU DÙNG ẢNH CHẤM TRÒN VÀ ĐÓ LÀ LÝ DO KHÔNG THẤY GÌ.
    ///
    /// LineRenderer trải ảnh theo kiểu: trục U chạy DỌC sợi, trục V chạy NGANG bề dày.
    /// Đặt một chấm tròn vào đó thì hai ĐẦU sợi (U = 0 và U = 1) rơi đúng vào rìa trong
    /// suốt của chấm - cả sợi tắt ngóm, chỉ còn một đốm mờ ở khoảng giữa.
    ///
    /// Ảnh đúng cho sợi phải KHÔNG đổi theo U, chỉ mềm dần theo V. Nhờ vậy sợi sáng đều
    /// từ đầu tới cuối mà hai mép bên vẫn nhoè.
    /// </summary>
    private static Texture2D SoftLine
    {
        get
        {
            if (_softDot != null) return _softDot;

            const int size = 32;
            _softDot = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "CombatVFX SoftLine (runtime)",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            for (int y = 0; y < size; y++)
            {
                // Khoảng cách tới đường tâm ngang, quy về 0..1.
                float v = (y + 0.5f) / size;
                float d = Mathf.Abs(v * 2f - 1f);

                // Lõi đặc hẳn ở giữa rồi mới tắt: sợi có RUỘT SÁNG chứ không nhoè đều,
                // đó mới là cảm giác của một tia năng lượng.
                float a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a);

                for (int x = 0; x < size; x++)
                {
                    _softDot.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            _softDot.Apply();
            return _softDot;
        }
    }

    /// <summary>Màu ứng với điện tích. Dương đỏ, âm xanh dương, trung tính trắng.</summary>
    public static Color PolarityColor(MagneticObject.Polarity p)
    {
        switch (p)
        {
            case MagneticObject.Polarity.Positive: return new Color(1f, 0.25f, 0.25f);
            case MagneticObject.Polarity.Negative: return new Color(0.3f, 0.5f, 1f);
            default: return new Color(0.9f, 0.9f, 1f);
        }
    }

    /// <summary>Bắn một tia sáng từ A tới B, tự tắt dần rồi tự huỷ.</summary>
    public static void Beam(Vector3 from, Vector3 to, Color color,
                            float duration = 0.22f, float width = 0.2f)
    {
        if (Additive == null) return;

        if (logVFX) Debug.Log($"<color=#66CCFF>[CombatVFX] Tia laze {from} -> {to}, " +
                              $"dài {Vector3.Distance(from, to):F1}m</color>");

        GameObject go = new GameObject("VFX Beam");
        go.transform.position = from;

        BeamFlash beam = go.AddComponent<BeamFlash>();
        beam.Play(from, to, color, duration, width);
    }

    /// <summary>
    /// Chớp sáng đầu nòng: cục sáng bung ra ngay tại chỗ phóng tia, tắt rất nhanh.
    ///
    /// ⚠️ VÌ SAO BẮT BUỘC PHẢI CÓ: không có nó thì tia laze TỰ NHIÊN XUẤT HIỆN giữa không
    /// khí, không rõ từ đâu ra. Mắt người cần thấy NƠI năng lượng thoát ra thì mới tin
    /// rằng bàn tay là thứ đã bắn nó.
    ///
    /// Mọi khẩu súng trong game đều có chi tiết này, kể cả súng bắn tia - chớp sáng đầu
    /// nòng không phải để đẹp, nó là thứ nối hành động với kết quả.
    /// </summary>
    public static void Flash(Vector3 pos, Vector3 dir, Color color,
                             float size = 0.5f, float duration = 0.22f, int layer = 0)
    {
        if (Additive == null) return;

        Vector3 d = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;

        // Nhích ra trước một chút khỏi đầu ngón tay.
        //
        // ⚠️ ĐÂY LÀ LÝ DO BẢN TRƯỚC KHÔNG THẤY GÌ. Đặt cục sáng đúng ngay tại đầu ngón
        // thì nó nằm LỌT TRONG khối thịt bàn tay, và bàn tay được vẽ SAU (viewmodel là
        // camera Overlay, vẽ chồng lên sau cùng) nên phủ kín cục sáng.
        Vector3 at = pos + d * (size * 0.35f);

        GameObject go = new GameObject("VFX Flash");
        go.transform.position = at;
        go.layer = layer;

        MuzzleFlash flash = go.AddComponent<MuzzleFlash>();
        flash.Play(at, d, color, size, duration);

        // VÒNG BUNG quanh đầu ngón, mặt phẳng vuông góc với hướng bắn.
        //
        // Cục sáng không thôi chỉ là một đốm phình to. Thêm một vòng loang ra là mắt đọc
        // ngay thành "có cái gì vừa NỔ ở đây" - vòng loang là dấu hiệu của áp suất bung
        // ra, thứ mà một đốm sáng đứng yên không diễn tả được.
        GameObject ringObj = new GameObject("VFX FlashRing");
        ringObj.transform.position = at;
        ringObj.transform.rotation = Quaternion.FromToRotation(Vector3.up, d);
        ringObj.layer = layer;

        ShockwaveRing ring = ringObj.AddComponent<ShockwaveRing>();
        ring.Play(size * 0.95f, color, duration * 0.8f, size * 0.17f, 3.5f);

        if (logVFX) Debug.Log($"<color=#66CCFF>[CombatVFX] Chớp đầu nòng tại {at}, " +
                              $"layer {layer}, cỡ {size:F2}</color>");
    }

    /// <summary>
    /// QUYỀN KHÍ: một luồng lực lao từ nắm đấm tới người trúng, rồi nổ ra ở đó.
    ///
    /// ⚠️ VÌ SAO CẦN: cú đấm có hỗ trợ ngắm cự ly gần - đối thủ lệch tới 70 độ vẫn trúng.
    /// Nhưng nắm đấm thì luôn thọc THẲNG ra trước. Kết quả là tay đấm vào khoảng không mà
    /// người đứng bên cạnh vẫn bay đi: hình ảnh và kết quả không khớp nhau.
    ///
    /// Luồng khí bay TỚI ĐÚNG NGƯỜI TRÚNG là thứ nối hai cái đó lại. Hỗ trợ ngắm vẫn y
    /// nguyên, người chơi chỉ thấy "luồng khí đánh trúng".
    /// </summary>
    /// <param name="power01">Điện tích của người trúng (0..1) — càng nhiễm điện nổ càng to, khớp với lực văng.</param>
    public static void QiStrike(Vector3 from, Vector3 to, Color color, float power01 = 0f)
    {
        if (Additive == null) return;
        if ((to - from).sqrMagnitude < 0.0001f) return;

        if (logVFX) Debug.Log($"<color=#66CCFF>[CombatVFX] Quyền khí {from} -> {to}</color>");

        GameObject go = new GameObject("VFX QiBolt");
        go.transform.position = from;
        go.AddComponent<QiBolt>().Play(from, to, color, power01);
    }

    /// <summary>
    /// CÚ ĐẤM TRÚNG (17/10) — mạnh tay hơn Impact thường, và TO DẦN theo điện tích của người trúng.
    ///
    /// Game này lực văng tăng theo điện tích (x1 → x4), nên hiệu ứng cũng phải nói được điều đó:
    /// đấm người sạch điện thì nổ vừa, đấm người 90% điện thì nổ to gấp rưỡi — người chơi đọc
    /// được "cú này sẽ hất xa" ngay tại khoảnh khắc trúng.
    ///
    /// Bốn lớp: cục sáng chớp tại chỗ · vòng sóng màu · vòng sóng trắng thứ hai TRỄ NHỊP 0,06 giây
    /// (hai nhịp đọc thành "bùm-bùm", một vụ va chạm có độ nặng) · nhiều tia lửa hơn.
    /// </summary>
    public static void PunchImpact(Vector3 at, Vector3 dir, Color color, float power01)
    {
        if (Additive == null) return;

        float p = Mathf.Clamp01(power01);
        float size = 1.5f * (1f + 0.8f * p);
        Vector3 d = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;
        Quaternion face = Quaternion.FromToRotation(Vector3.up, d);

        GameObject flash = new GameObject("VFX PunchFlash");
        flash.transform.position = at;
        flash.AddComponent<MuzzleFlash>().Play(at, d, color, 0.9f * (1f + 0.6f * p), 0.14f);

        GameObject ring1 = new GameObject("VFX PunchRing");
        ring1.transform.SetPositionAndRotation(at, face);
        ring1.AddComponent<ShockwaveRing>().Play(size, color, 0.34f, size * 0.24f, 12f);

        // Vòng thứ hai: trắng hơn, rộng hơn, mảnh hơn, KHÔNG có đèn (đèn đã có ở hai lớp trên)
        GameObject ring2 = new GameObject("VFX PunchRing2");
        ring2.transform.SetPositionAndRotation(at, face);
        ring2.AddComponent<ShockwaveRing>().Play(size * 1.5f, Color.Lerp(color, Color.white, 0.6f), 0.4f,
                                                 size * 0.12f, 0f, 0.06f);

        GameObject sparks = new GameObject("VFX PunchSparks");
        sparks.transform.position = at;
        sparks.AddComponent<SparkBurst>().Play(at, d, color, size, 9 + Mathf.RoundToInt(10f * p), 0.3f);
    }

    /// <summary>
    /// TIA NẠP ĐIỆN (17/10): tia laze thường + tia sét lách tách quấn quanh + chỗ vật trúng loé sáng.
    ///
    /// Tia thẳng trơn thì đọc thành "laze" của game bắn súng viễn tưởng. Thêm sợi điện ngoằn
    /// ngoèo quấn quanh là đọc ra ngay "điện từ" — đúng chủ đề găng tay từ tính. Còn vật trúng
    /// phải PHẢN ỨNG (tia lửa + vòng sóng màu điện tích vừa nạp): trước đây vật bị bắn đứng im
    /// như không có gì xảy ra, mắt không nối được tia với kết quả.
    /// </summary>
    public static void ChargeBeam(Vector3 from, Vector3 to, Color color)
    {
        if (Additive == null) return;

        Beam(from, to, color);

        float len = Vector3.Distance(from, to);
        GameObject arcs = new GameObject("VFX BeamArcs");
        arcs.transform.position = from;
        arcs.AddComponent<ArcCrackle>().Play(from, to, color, 0.26f, 2, Mathf.Clamp(0.06f + len * 0.012f, 0.08f, 0.35f));

        // Vật trúng: vòng sóng vuông góc với tia + tia lửa bắn ngược về phía người bắn
        Vector3 back = (from - to).sqrMagnitude > 0.0001f ? (from - to).normalized : Vector3.up;
        GameObject ring = new GameObject("VFX BeamHitRing");
        ring.transform.SetPositionAndRotation(to, Quaternion.FromToRotation(Vector3.up, back));
        ring.AddComponent<ShockwaveRing>().Play(0.9f, color, 0.3f, 0.16f, 5f);

        GameObject sparks = new GameObject("VFX BeamHitSparks");
        sparks.transform.position = to;
        sparks.AddComponent<SparkBurst>().Play(to, back, color, 0.6f, 7, 0.22f);
    }

    /// <summary>Nổ ra tại chỗ trúng: vòng sóng vuông góc hướng đánh + tia văng toả ra.</summary>
    public static void Impact(Vector3 at, Vector3 dir, Color color, float size = 1.5f)
    {
        if (Additive == null) return;

        Vector3 d = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;

        // Mặt vòng VUÔNG GÓC với hướng đánh, như sóng áp suất bị đẩy ra từ điểm va chạm.
        // Vòng nằm ngang như vụ nổ TNT thì mắt đọc thành "có gì đó nổ dưới đất", không
        // đọc thành "vừa bị đấm từ hướng kia".
        GameObject ringObj = new GameObject("VFX QiRing");
        ringObj.transform.position = at;
        ringObj.transform.rotation = Quaternion.FromToRotation(Vector3.up, d);
        ringObj.AddComponent<ShockwaveRing>().Play(size, color, 0.34f, size * 0.24f, 12f);

        GameObject sparks = new GameObject("VFX QiSparks");
        sparks.transform.position = at;
        sparks.AddComponent<SparkBurst>().Play(at, d, color, size);
    }

    // ==================== NỔ TNT + CAN XĂNG (17/10) ====================
    //
    // Lửa, khói, tàn lửa cần hàng chục đốm MỀM bay theo quỹ đạo riêng - LineRenderer không làm nổi,
    // nên phần này dùng Particle System dựng bằng code. Không lặp lại lỗi tụt FPS của biển mây cũ:
    // mỗi vụ nổ chỉ ~60 hạt, sống tối đa 2 giây rồi tự huỷ (stopAction = Destroy).

    private static Material _blobAdd, _blobAlpha;
    private static Texture2D _blobTex;

    /// <summary>Ảnh đốm tròn mềm cho hạt (khác SoftLine — cái đó chỉ mềm theo bề ngang sợi).</summary>
    private static Texture2D BlobTexture
    {
        get
        {
            if (_blobTex != null) return _blobTex;
            const int size = 64;
            _blobTex = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = "CombatVFX Blob (runtime)",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
            };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    a = a * a * (3f - 2f * a);
                    _blobTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            _blobTex.Apply();
            return _blobTex;
        }
    }

    private static Material MakeBlobMaterial(bool additive)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) return null;
        Material m = new Material(shader) { name = additive ? "CombatVFX BlobAdd" : "CombatVFX BlobAlpha", hideFlags = HideFlags.DontSave };
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", additive ? 2f : 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        m.mainTexture = BlobTexture;
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", BlobTexture);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
        return m;
    }

    /// <summary>Đốm cộng sáng (lửa, tàn lửa, chớp).</summary>
    private static Material BlobAdd => _blobAdd != null ? _blobAdd : (_blobAdd = MakeBlobMaterial(true));
    /// <summary>Đốm che sáng (khói, xăng) — khói phải TỐI hơn nền, cộng sáng không làm được.</summary>
    private static Material BlobAlpha => _blobAlpha != null ? _blobAlpha : (_blobAlpha = MakeBlobMaterial(false));

    /// <summary>Dựng một hệ hạt phát MỘT LOẠT rồi tự huỷ. Trả về để chỉnh thêm.</summary>
    private static ParticleSystem Burst(string name, Vector3 at, Material mat, int count, float delay,
                                        Vector2 life, Vector2 speed, Vector2 size, float gravity,
                                        Gradient colors, AnimationCurve sizeOverLife, float shapeRadius)
    {
        GameObject go = new GameObject(name);
        go.transform.position = at;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.playOnAwake = false;
        main.startDelay = delay;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life.x, life.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
        main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = count;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Sphere;
        sh.radius = Mathf.Max(0.01f, shapeRadius);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new ParticleSystem.MinMaxGradient(colors);

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, sizeOverLife);

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        return ps;
    }

    private static Gradient Grad(params (float t, Color c)[] keys)
    {
        var g = new Gradient();
        var ck = new GradientColorKey[keys.Length];
        var ak = new GradientAlphaKey[keys.Length];
        for (int i = 0; i < keys.Length; i++)
        {
            ck[i] = new GradientColorKey(keys[i].c, keys[i].t);
            ak[i] = new GradientAlphaKey(keys[i].c.a, keys[i].t);
        }
        g.SetKeys(ck, ak);
        return g;
    }

    /// <summary>
    /// VỤ NỔ TNT (17/10). Năm lớp: quả cầu lửa · khói bốc lên · tàn lửa văng · vòng sóng ĐÚNG bán kính
    /// sát thương (giữ nguyên — người chơi học tầm nổ bằng mắt) · vòng trắng loang nhanh.
    /// Mọi cỡ tính theo bán kính nổ, nên đổi explosionRadius thì vụ nổ to nhỏ theo.
    /// </summary>
    public static void Explosion(Vector3 center, float radius)
    {
        if (Additive == null || BlobAdd == null) return;
        float k = radius / 6f;   // 6m là bán kính chuẩn đã cân hình
        Color orange = new Color(1f, 0.55f, 0.12f);

        // 0. Chớp sáng lớn ở tâm đúng khoảnh khắc nổ - một đốm to cỡ cả vùng nổ, tắt trong 0,2 giây
        Burst("VFX TNT Flash", center + Vector3.up * 0.8f * k, BlobAdd, 1, 0f,
            new Vector2(0.2f, 0.2f), Vector2.zero, new Vector2(radius * 3f, radius * 3f), 0f,
            Grad((0f, new Color(4f, 3.6f, 2.8f, 1f)), (1f, new Color(2f, 1f, 0.3f, 0f))),
            new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 1f)), 0.01f).Play();

        // 1. Quả cầu lửa: trắng chói → vàng → cam → đỏ sẫm rồi tắt. To cỡ ~2/3 vùng nổ.
        ParticleSystem fire = Burst("VFX TNT Fire", center + Vector3.up * 0.5f * k, BlobAdd, 24, 0f,
            new Vector2(0.55f, 0.9f), new Vector2(3f * k, 9f * k), new Vector2(5.5f * k, 8.5f * k), -0.08f,
            // Đốm hạt mềm dần ra rìa nên phần THẤY RÕ chỉ cỡ một nửa kích thước - số to là cố ý
            Grad((0f, new Color(3f, 2.6f, 2f, 1f)), (0.12f, new Color(2.2f, 1.2f, 0.35f, 1f)),
                 (0.45f, new Color(1.6f, 0.45f, 0.08f, 0.85f)), (1f, new Color(0.4f, 0.06f, 0.02f, 0f))),
            new AnimationCurve(new Keyframe(0f, 0.35f), new Keyframe(0.25f, 1f), new Keyframe(1f, 1.25f)),
            radius * 0.12f);
        var fireLimit = fire.limitVelocityOverLifetime;   // bung nhanh rồi khựng lại như lửa thật
        fireLimit.enabled = true;
        fireLimit.limit = 1.0f;
        fireLimit.dampen = 0.2f;
        // Lửa LUÔN vẽ sau cùng (đè lên khói). Không thì khói che sáng vẽ đè lên trước lửa,
        // thành một cái lỗ tối giữa quả cầu lửa. Số càng nhỏ càng được vẽ sau (nằm trên).
        fire.GetComponent<ParticleSystemRenderer>().sortingFudge = -20f;
        fire.Play();

        // 2. Khói: trễ một nhịp sau lửa, bốc lên chậm, nở ra rồi tan
        ParticleSystem smoke = Burst("VFX TNT Smoke", center + Vector3.up * 0.8f * k, BlobAlpha, 16, 0.15f,
            new Vector2(1.7f, 2.6f), new Vector2(1.5f * k, 4f * k), new Vector2(6f * k, 9.5f * k), -0.15f,
            Grad((0f, new Color(0.30f, 0.27f, 0.25f, 0f)), (0.2f, new Color(0.28f, 0.26f, 0.24f, 0.75f)),
                 (0.55f, new Color(0.40f, 0.38f, 0.37f, 0.5f)), (1f, new Color(0.55f, 0.55f, 0.55f, 0f))),
            new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(1f, 2.0f)),
            radius * 0.18f);
        smoke.GetComponent<ParticleSystemRenderer>().sortingFudge = 20f;   // nằm dưới lửa
        var smokeLimit = smoke.limitVelocityOverLifetime;
        smokeLimit.enabled = true;
        smokeLimit.limit = 0.6f;
        smokeLimit.dampen = 0.12f;
        smoke.Play();

        // 3. Tàn lửa: vệt sáng kéo dài theo hướng bay, rơi theo trọng lực
        ParticleSystem embers = Burst("VFX TNT Embers", center, BlobAdd, 36, 0f,
            new Vector2(0.6f, 1.3f), new Vector2(7f * k, 16f * k), new Vector2(0.14f, 0.28f), 1.2f,
            Grad((0f, new Color(3f, 2.4f, 1.2f, 1f)), (0.5f, new Color(2.2f, 0.9f, 0.2f, 1f)), (1f, new Color(1f, 0.2f, 0.05f, 0f))),
            new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.4f)),
            radius * 0.08f);
        ParticleSystemRenderer er = embers.GetComponent<ParticleSystemRenderer>();
        er.renderMode = ParticleSystemRenderMode.Stretch;
        er.velocityScale = 0.06f;
        er.lengthScale = 1.5f;
        embers.Play();

        // 4. Vòng sóng ĐÚNG bán kính sát thương (như cũ) — đèn chớp mạnh hơn cho vụ nổ có sức nặng
        GameObject ring = new GameObject("VFX Shockwave");
        ring.transform.position = center;
        ring.AddComponent<ShockwaveRing>().Play(radius, orange, 0.45f, 0.6f, 30f);

        // 5. Vòng trắng mảnh loang nhanh hơn, rộng hơn: "áp suất" thoát ra trước cả lửa
        GameObject ring2 = new GameObject("VFX Shockwave2");
        ring2.transform.position = center + Vector3.up * 0.3f;
        ring2.AddComponent<ShockwaveRing>().Play(radius * 1.35f, new Color(1f, 0.95f, 0.85f), 0.28f, 0.25f, 0f);
    }

    /// <summary>
    /// ĐỔ CAN XĂNG lên một vật (17/10): xăng màu hổ phách bắn tung + chớp lửa nhỏ quanh vật + vòng cam
    /// dưới chân. Nói rõ "vật này vừa bị biến thành thùng nổ" cho MỌI người thấy, không chỉ người đổ.
    /// </summary>
    public static void GasolineSplash(Vector3 center, float objRadius)
    {
        if (Additive == null || BlobAdd == null) return;
        float r = Mathf.Max(0.3f, objRadius);
        Color amber = new Color(1f, 0.62f, 0.12f);

        ParticleSystem fuel = Burst("VFX Fuel", center + Vector3.up * r * 0.5f, BlobAlpha, 20, 0f,
            new Vector2(0.55f, 0.9f), new Vector2(2.5f, 5f), new Vector2(0.09f, 0.18f), 1.6f,
            Grad((0f, new Color(1f, 0.7f, 0.2f, 0.95f)), (0.7f, new Color(0.95f, 0.55f, 0.1f, 0.85f)), (1f, new Color(0.8f, 0.4f, 0.05f, 0f))),
            new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.7f)),
            r * 0.4f);
        var fs = fuel.shape;
        fs.shapeType = ParticleSystemShapeType.Hemisphere;   // bắn tung LÊN, không xuống đất
        fs.rotation = new Vector3(-90f, 0f, 0f);
        ParticleSystemRenderer fr = fuel.GetComponent<ParticleSystemRenderer>();
        fr.renderMode = ParticleSystemRenderMode.Stretch;
        fr.velocityScale = 0.05f;
        fr.lengthScale = 1.2f;
        fuel.Play();

        ParticleSystem flare = Burst("VFX FuelFlare", center, BlobAdd, 10, 0.05f,
            new Vector2(0.35f, 0.6f), new Vector2(0.6f, 1.6f), new Vector2(r * 0.6f, r * 1.0f), -0.3f,
            Grad((0f, new Color(2.6f, 1.6f, 0.4f, 0f)), (0.2f, new Color(2.4f, 1.2f, 0.25f, 0.9f)), (1f, new Color(0.8f, 0.15f, 0.03f, 0f))),
            new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 1.1f)),
            r * 0.7f);
        flare.Play();

        GameObject ring = new GameObject("VFX FuelRing");
        ring.transform.position = center - Vector3.up * r * 0.6f;
        ring.AddComponent<ShockwaveRing>().Play(r * 2.2f, amber, 0.45f, 0.22f, 8f);
    }

    /// <summary>
    /// Tàn lửa lách tách bốc lên LIÊN TỤC từ vật đã bị đổ xăng (17/10). Gắn làm con của vật; bay theo vật
    /// khi bị ném, và vì hạt sống trong toạ độ THẾ GIỚI nên để lại một vệt tàn lửa sau lưng nó.
    /// Trả về hệ hạt để MagneticObject bật / tắt (vật bị cất vào túi, đã nổ, sang round mới).
    /// </summary>
    public static ParticleSystem CreateHazardEmbers(Transform parent, float objRadius)
    {
        if (BlobAdd == null) return null;
        float r = Mathf.Max(0.3f, objRadius);

        GameObject go = new GameObject("VFX HazardEmbers");
        go.transform.SetParent(parent, false);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.4f);
        main.gravityModifier = -0.25f;   // bốc lên như tàn lửa nóng
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 32;

        var em = ps.emission;
        em.rateOverTime = 16f;

        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Sphere;
        sh.radius = r * 0.7f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new ParticleSystem.MinMaxGradient(Grad(
            (0f, new Color(3f, 2.2f, 0.8f, 0f)), (0.15f, new Color(2.8f, 1.5f, 0.3f, 1f)), (1f, new Color(1f, 0.15f, 0.03f, 0f))));

        var noise = ps.noise;   // lắc lư như bị gió thổi, không bay thẳng tắp
        noise.enabled = true;
        noise.strength = 0.6f;
        noise.frequency = 1.2f;

        ParticleSystemRenderer rend = go.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = BlobAdd;
        rend.shadowCastingMode = ShadowCastingMode.Off;
        rend.receiveShadows = false;

        // Quầng nóng màu cam phập phồng quanh vật - mấy chấm tàn lửa thôi thì đứng xa không thấy.
        // Là hệ hạt CON nên Play / Stop của cha điều khiển luôn nó.
        GameObject glowGo = new GameObject("Glow");
        glowGo.transform.SetParent(go.transform, false);
        ParticleSystem glow = glowGo.AddComponent<ParticleSystem>();
        glow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var gm = glow.main;
        gm.loop = true;
        gm.playOnAwake = false;
        gm.startLifetime = 0.9f;
        gm.startSpeed = 0f;
        gm.startSize = new ParticleSystem.MinMaxCurve(r * 2.2f, r * 2.8f);
        gm.simulationSpace = ParticleSystemSimulationSpace.Local;   // dính theo vật
        gm.maxParticles = 6;
        var ge = glow.emission;
        ge.rateOverTime = 4f;
        var gs = glow.shape;
        gs.enabled = false;
        var gc = glow.colorOverLifetime;
        gc.enabled = true;
        gc.color = new ParticleSystem.MinMaxGradient(Grad(
            (0f, new Color(1.6f, 0.5f, 0.08f, 0f)), (0.5f, new Color(1.6f, 0.5f, 0.08f, 0.22f)), (1f, new Color(1.4f, 0.3f, 0.05f, 0f))));
        ParticleSystemRenderer gr = glowGo.GetComponent<ParticleSystemRenderer>();
        gr.sharedMaterial = BlobAdd;
        gr.shadowCastingMode = ShadowCastingMode.Off;
        gr.receiveShadows = false;
        return ps;
    }

    /// <summary>Vòng sóng xung kích loang ra từ tâm vụ nổ, kèm một nháy sáng.</summary>
    public static void Shockwave(Vector3 center, float radius, Color color, float duration = 0.45f)
    {
        if (Additive == null) return;

        if (logVFX) Debug.Log($"<color=#FF9933>[CombatVFX] Sóng nổ tại {center}, " +
                              $"bán kính {radius:F1}m</color>");

        GameObject go = new GameObject("VFX Shockwave");
        go.transform.position = center;

        ShockwaveRing ring = go.AddComponent<ShockwaveRing>();
        ring.Play(radius, color, duration);
    }
}

/// <summary>
/// Một tia sáng nối hai điểm, mờ dần rồi tự huỷ.
///
/// Dùng LineRenderer chứ không dựng lưới riêng: LineRenderer tự quay mặt về camera mỗi
/// khung hình, nên nhìn từ góc nào tia cũng dày như nhau. Tự dựng lưới thì nhìn đúng cạnh
/// sẽ thấy nó mỏng dính rồi biến mất.
/// </summary>
public class BeamFlash : MonoBehaviour
{
    private LineRenderer _glow;   // quầng rộng, mang màu điện tích
    private LineRenderer _core;   // ruột mảnh, trắng nóng
    private float _life;
    private float _duration;
    private float _width;
    private Color _color;

    public void Play(Vector3 from, Vector3 to, Color color, float duration, float width)
    {
        _duration = Mathf.Max(0.02f, duration);
        _life = _duration;
        _width = width;
        _color = color;

        // HAI LỚP CHỒNG LÊN NHAU. Đây là chỗ tạo ra độ ĐẬM.
        //
        // Một sợi đơn sắc dù to tới đâu cũng chỉ là một vệt màu. Tia năng lượng thật có
        // RUỘT TRẮNG NÓNG bọc trong QUẦNG MÀU - vì phần lõi sáng tới mức mọi màu đều bão
        // hoà thành trắng, chỉ phần rìa yếu hơn mới còn giữ được màu.
        //
        // Chồng hai sợi cộng sáng lên nhau cho ra đúng hiệu ứng đó.
        _glow = MakeLine(gameObject, from, to);

        GameObject coreObj = new GameObject("Core");
        coreObj.transform.SetParent(transform, false);
        _core = MakeLine(coreObj, from, to);

        Apply(1f);
    }

    private static LineRenderer MakeLine(GameObject host, Vector3 from, Vector3 to)
    {
        LineRenderer line = host.AddComponent<LineRenderer>();
        line.sharedMaterial = CombatVFX.Additive;
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        line.numCapVertices = 4;

        // Không đổ bóng và không nhận bóng: đây là ánh sáng, không phải vật thể.
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;

        // Kéo giãn ảnh theo cả chiều dài tia thay vì lặp lại.
        line.textureMode = LineTextureMode.Stretch;

        return line;
    }

    private void Update()
    {
        _life -= Time.deltaTime;

        if (_life <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        Apply(_life / _duration);
    }

    private void Apply(float t01)
    {
        if (_glow == null || _core == null) return;

        // Tắt theo BÌNH PHƯƠNG: sáng rực gần như suốt rồi tắt phụt ở cuối.
        float fade = t01 * t01;

        // Nhân màu lên quá 1. Với blend cộng sáng thì phần dư tràn sang hậu kỳ Bloom
        // và toả hào quang - đó là thứ làm tia trông ĐẬM chứ không chỉ to.
        Color glowC = _color * 1.8f;
        glowC.a = fade;
        _glow.startColor = glowC;
        _glow.endColor = glowC;

        // Đầu tia (phía tay) dày hơn đầu kia, để mắt đọc được HƯỚNG BẮN.
        _glow.startWidth = _width * Mathf.Lerp(0.4f, 1f, fade);
        _glow.endWidth = _width * Mathf.Lerp(0.2f, 0.55f, fade);

        // Ruột trắng: pha màu về phía trắng chứ không trắng hẳn, để vẫn nhận ra
        // được âm hay dương khi nhìn thoáng qua.
        Color coreC = Color.Lerp(_color, Color.white, 0.75f) * 2.2f;
        coreC.a = fade;
        _core.startColor = coreC;
        _core.endColor = coreC;

        _core.startWidth = _glow.startWidth * 0.34f;
        _core.endWidth = _glow.endWidth * 0.34f;
    }
}

/// <summary>
/// Cục sáng bung ra ở đầu nòng, phình rồi tắt trong một nhịp rất ngắn.
///
/// Dựng bằng LineRenderer với hai điểm rất gần nhau, bề dày lớn và hai đầu bo tròn. Nghe
/// lạ nhưng đó là cách rẻ nhất để có một đốm sáng LUÔN QUAY MẶT VỀ CAMERA mà không phải
/// dựng lưới billboard riêng - LineRenderer vốn đã tự làm việc đó mỗi khung hình.
///
/// Kéo dài một đoạn ngắn theo HƯỚNG BẮN chứ không tròn đều: chớp sáng đầu nòng thật cũng
/// phụt về phía trước, và hình thuôn đó cho mắt biết ngay tia sẽ đi đâu.
/// </summary>
public class MuzzleFlash : MonoBehaviour
{
    private LineRenderer _line;
    private Light _light;
    private float _life;
    private float _duration;
    private float _size;
    private Color _color;
    private Vector3 _from;
    private Vector3 _dir;

    public void Play(Vector3 pos, Vector3 dir, Color color, float size, float duration)
    {
        _duration = Mathf.Max(0.02f, duration);
        _life = _duration;
        _size = size;
        _color = color;
        _from = pos;
        _dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;

        _line = gameObject.AddComponent<LineRenderer>();
        _line.sharedMaterial = CombatVFX.Additive;
        _line.useWorldSpace = true;
        _line.positionCount = 2;
        _line.numCapVertices = 8;   // bo tron de ra cuc sang chu khong ra que
        _line.shadowCastingMode = ShadowCastingMode.Off;
        _line.receiveShadows = false;
        _line.textureMode = LineTextureMode.Stretch;

        // Den nhay: lam ban tay va canh quanh do sang len dung khoanh khac ban. Thieu no
        // thi cuc sang trong nhu dan len man hinh chu khong o trong the gioi.
        _light = gameObject.AddComponent<Light>();
        _light.type = LightType.Point;
        _light.color = color;
        _light.range = size * 9f;
        _light.shadows = LightShadows.None;

        Apply(1f);
    }

    private void Update()
    {
        _life -= Time.deltaTime;

        if (_life <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        Apply(_life / _duration);
    }

    private void Apply(float t01)
    {
        // PHINH RA ROI TAT, khong phai nho dan deu. Dinh sang roi vao khoang giua nhip,
        // giong mot tieng no nho: bung ra truoc, tan sau.
        float burst = Mathf.Sin(Mathf.Clamp01(1f - t01) * Mathf.PI * 0.9f + 0.1f);

        _line.SetPosition(0, _from);
        _line.SetPosition(1, _from + _dir * (_size * 0.7f * burst));

        Color c = Color.Lerp(_color, Color.white, 0.5f) * 3.2f;
        c.a = t01;
        _line.startColor = c;
        _line.endColor = c;

        _line.startWidth = _size * burst;
        _line.endWidth = _size * burst * 0.45f;

        if (_light != null) _light.intensity = 16f * t01 * t01;
    }
}

/// <summary>
/// Vòng sóng xung kích: một vòng tròn loang rộng ra rồi mỏng dần và tắt.
///
/// Cũng dùng LineRenderer, bật chế độ vòng kín. Đỡ được toàn bộ phần dựng lưới, mà vẫn
/// tự quay mặt về camera nên nhìn từ trên hay từ ngang đều thấy.
/// </summary>
public class ShockwaveRing : MonoBehaviour
{
    private const int Segments = 48;

    private LineRenderer _line;
    private Light _flash;
    private float _life;
    private float _duration;
    private float _radius;
    private Color _color;
    private float _width = 0.6f;
    private float _lightPower = 14f;
    private float _delay;   // (17/10) chờ bấy nhiêu giây rồi mới loang — để làm nhịp "bùm-bùm"

    public void Play(float radius, Color color, float duration,
                     float width = 0.6f, float lightPower = 14f, float delay = 0f)
    {
        _delay = Mathf.Max(0f, delay);
        _duration = Mathf.Max(0.05f, duration);
        _life = _duration;
        _radius = radius;
        _color = color;

        _width = width;
        _lightPower = lightPower;

        _line = gameObject.AddComponent<LineRenderer>();
        _line.sharedMaterial = CombatVFX.Additive;
        _line.useWorldSpace = false;
        _line.loop = true;
        _line.positionCount = Segments;
        _line.shadowCastingMode = ShadowCastingMode.Off;
        _line.receiveShadows = false;
        _line.textureMode = LineTextureMode.Stretch;

        // NHÁY SÁNG. Vụ nổ mà không làm sáng cảnh xung quanh thì trông như một hình vẽ
        // dán lên màn hình, không phải một sự kiện xảy ra trong thế giới.
        // Không cần đèn thì không tạo — mỗi đèn chớp là thêm một nguồn sáng phải tính
        if (lightPower > 0f)
        {
            _flash = gameObject.AddComponent<Light>();
            _flash.type = LightType.Point;
            _flash.color = color;
            _flash.range = radius * 2.5f;
            _flash.shadows = LightShadows.None;   // đèn chớp mà đổ bóng thì rất tốn
        }

        Apply(1f);
        if (_delay > 0f) _line.enabled = false;
    }

    private void Update()
    {
        if (_delay > 0f)
        {
            _delay -= Time.deltaTime;
            if (_delay > 0f) return;
            _line.enabled = true;
        }

        _life -= Time.deltaTime;

        if (_life <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        Apply(_life / _duration);
    }

    private void Apply(float t01)
    {
        float grow = 1f - t01;   // 0 -> 1 theo thời gian

        // Loang NHANH LÚC ĐẦU rồi chậm dần, đúng cách sóng xung kích thật mất đà.
        // Loang đều tốc độ thì trông như một hoạt hình rẻ tiền.
        float eased = 1f - (1f - grow) * (1f - grow);
        float r = _radius * eased;

        Vector3[] pts = new Vector3[Segments];
        for (int i = 0; i < Segments; i++)
        {
            float a = (float)i / Segments * Mathf.PI * 2f;
            pts[i] = new Vector3(Mathf.Cos(a) * r, 0.15f, Mathf.Sin(a) * r);
        }
        _line.SetPositions(pts);

        Color c = _color;
        c.a = t01 * t01;
        _line.startColor = c;
        _line.endColor = c;

        // Càng loang rộng càng mỏng - tổng năng lượng trải ra một vòng dài hơn.
        _line.startWidth = Mathf.Lerp(_width, _width * 0.13f, eased);
        _line.endWidth = _line.startWidth;

        if (_flash != null) _flash.intensity = _lightPower * t01 * t01;
    }
}

/// <summary>
/// Luồng quyền khí: một vệt sáng đầu dày đuôi mảnh, lao từ nắm đấm tới đích rất nhanh.
///
/// Hai lớp như tia laze (quầng màu + ruột trắng), vì một lớp đơn sắc đọc thành vệt màu
/// chứ không đọc thành một luồng năng lượng.
///
/// ĐẦU DÀY ĐUÔI MẢNH là có chủ ý: mắt đọc hướng bay theo phía nào to hơn. Hai đầu bằng
/// nhau thì trông như một que sáng đứng yên chứ không phải thứ đang lao đi.
/// </summary>
public class QiBolt : MonoBehaviour
{
    // Rất nhanh. Cú đấm là tức thì; luồng khí mà bay chậm thì nạn nhân đã văng đi từ
    // trước khi nó tới, và nó đâm vào khoảng không.
    private const float Travel = 0.07f;
    private const float Linger = 0.09f;

    private LineRenderer _glow, _core;
    private Vector3 _from, _to;
    private Color _color;
    private float _t;
    private bool _impacted;
    private float _power;

    public void Play(Vector3 from, Vector3 to, Color color, float power01 = 0f)
    {
        _from = from;
        _to = to;
        _color = color;
        _power = power01;

        _glow = Make(gameObject);

        GameObject coreObj = new GameObject("Core");
        coreObj.transform.SetParent(transform, false);
        _core = Make(coreObj);

        Apply();
    }

    private static LineRenderer Make(GameObject host)
    {
        LineRenderer l = host.AddComponent<LineRenderer>();
        l.sharedMaterial = CombatVFX.Additive;
        l.useWorldSpace = true;
        l.positionCount = 2;
        l.numCapVertices = 0;
        l.shadowCastingMode = ShadowCastingMode.Off;
        l.receiveShadows = false;
        l.textureMode = LineTextureMode.Stretch;

        // Điểm 0 là ĐUÔI, điểm 1 là ĐẦU. Đuôi thon về 0 nên không có chấm tròn ở đó.
        l.widthCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.7f, 0.75f),
            new Keyframe(1f, 1f));
        return l;
    }

    private void Update()
    {
        _t += Time.deltaTime;

        if (!_impacted && _t >= Travel)
        {
            _impacted = true;
            CombatVFX.PunchImpact(_to, _to - _from, _color, _power);
        }

        if (_t >= Travel + Linger)
        {
            Destroy(gameObject);
            return;
        }

        Apply();
    }

    private void Apply()
    {
        // Đầu: phóng nhanh rồi chậm lại khi sắp tới, như bị hãm bởi va chạm.
        float hp = Mathf.Clamp01(_t / Travel);
        hp = 1f - (1f - hp) * (1f - hp);

        // Đuôi: xuất phát muộn hơn đầu và tiếp tục đuổi theo sau khi đầu đã chạm đích, nên
        // luồng khí "rút vào" người trúng thay vì tắt phụt tại chỗ.
        float tp = Mathf.Clamp01((_t - Travel * 0.25f) / (Travel * 0.75f + Linger));
        tp = tp * tp;

        Vector3 head = Vector3.Lerp(_from, _to, hp);
        Vector3 tail = Vector3.Lerp(_from, _to, tp);

        float fade = _t < Travel ? 1f : 1f - (_t - Travel) / Linger;

        Paint(_glow, tail, head, _color * 2.4f, fade, 0.6f);
        Paint(_core, tail, head, Color.Lerp(_color, Color.white, 0.7f) * 3f, fade, 0.2f);
    }

    private static void Paint(LineRenderer l, Vector3 tail, Vector3 head, Color c, float fade, float w)
    {
        l.SetPosition(0, tail);
        l.SetPosition(1, head);

        Color end = c;
        end.a = fade;
        Color start = c;
        start.a = 0f;           // đuôi tan vào không khí

        l.startColor = start;
        l.endColor = end;
        l.widthMultiplier = w * Mathf.Lerp(0.5f, 1f, fade);
    }
}

/// <summary>
/// Tia văng toả ra từ chỗ trúng đòn, nghiêng về phía hướng đánh.
///
/// Nghiêng về phía trước là có chủ ý: văng đều ra mọi phía thì trông như pháo hoa. Văng
/// dồn theo hướng lực đánh thì mắt đọc ra ngay đòn đi từ đâu tới.
/// </summary>
public class SparkBurst : MonoBehaviour
{
    private int Count = 9;            // (17/10) chỉnh được: cú đấm mạnh văng nhiều tia hơn
    private float Duration = 0.26f;

    private LineRenderer[] _lines;
    private Vector3[] _dirs;
    private float[] _speed;
    private Vector3 _at;
    private Color _color;
    private float _size;
    private float _life;

    public void Play(Vector3 at, Vector3 hitDir, Color color, float size, int count = 9, float duration = 0.26f)
    {
        Count = Mathf.Max(1, count);
        Duration = Mathf.Max(0.05f, duration);
        _at = at;
        _color = color;
        _size = size;

        _lines = new LineRenderer[Count];
        _dirs = new Vector3[Count];
        _speed = new float[Count];

        Vector3 d = hitDir.sqrMagnitude > 0.0001f ? hitDir.normalized : Vector3.forward;

        for (int i = 0; i < Count; i++)
        {
            _dirs[i] = (Random.onUnitSphere + d * 0.9f).normalized;
            _speed[i] = Random.Range(0.7f, 1.3f);

            GameObject go = new GameObject("Spark");
            go.transform.SetParent(transform, false);

            LineRenderer l = go.AddComponent<LineRenderer>();
            l.sharedMaterial = CombatVFX.Additive;
            l.useWorldSpace = true;
            l.positionCount = 2;
            l.numCapVertices = 0;
            l.shadowCastingMode = ShadowCastingMode.Off;
            l.receiveShadows = false;
            l.widthCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));
            _lines[i] = l;
        }

        Apply(0f);
    }

    private void Update()
    {
        _life += Time.deltaTime;

        if (_life >= Duration)
        {
            Destroy(gameObject);
            return;
        }

        Apply(_life / Duration);
    }

    private void Apply(float p)
    {
        float ease = 1f - (1f - p) * (1f - p);
        float fade = 1f - p;

        for (int i = 0; i < Count; i++)
        {
            float reach = _size * 1.4f * _speed[i];
            Vector3 head = _at + _dirs[i] * reach * ease;
            Vector3 tail = _at + _dirs[i] * reach * Mathf.Max(0f, ease - 0.35f);

            LineRenderer l = _lines[i];
            l.SetPosition(0, tail);
            l.SetPosition(1, head);

            Color c = _color * 2.5f;
            c.a = fade;
            Color start = c;
            start.a = 0f;

            l.startColor = start;
            l.endColor = c;
            l.widthMultiplier = 0.02f + 0.1f * fade;
        }
    }
}

/// <summary>
/// Tia sét lách tách quấn quanh tia nạp điện (17/10).
///
/// Mỗi sợi là một LineRenderer ~14 điểm nối từ tay tới vật, lệch ngẫu nhiên khỏi đường thẳng
/// và VẼ LẠI HÌNH mỗi 0,035 giây — chính sự giật cục đổi hình đó là thứ mắt đọc thành "điện".
/// Lệch nhiều ở giữa, bằng 0 ở hai đầu (nhân với sin), nên sợi điện luôn cắm đúng vào ngón tay
/// và vào vật, không lơ lửng.
/// </summary>
public class ArcCrackle : MonoBehaviour
{
    private const int Points = 14;

    private LineRenderer[] _arcs;
    private Vector3 _from, _to;
    private Color _color;
    private float _life, _duration, _amp, _nextJitter;

    public void Play(Vector3 from, Vector3 to, Color color, float duration, int strands, float amplitude)
    {
        _from = from;
        _to = to;
        _color = color;
        _duration = Mathf.Max(0.05f, duration);
        _life = _duration;
        _amp = amplitude;

        _arcs = new LineRenderer[Mathf.Max(1, strands)];
        for (int i = 0; i < _arcs.Length; i++)
        {
            GameObject go = new GameObject("Arc");
            go.transform.SetParent(transform, false);
            LineRenderer l = go.AddComponent<LineRenderer>();
            l.sharedMaterial = CombatVFX.Additive;
            l.useWorldSpace = true;
            l.positionCount = Points;
            l.numCapVertices = 0;
            l.numCornerVertices = 0;
            l.shadowCastingMode = ShadowCastingMode.Off;
            l.receiveShadows = false;
            l.textureMode = LineTextureMode.Stretch;
            _arcs[i] = l;
        }

        Jitter();
        Apply(1f);
    }

    private void Update()
    {
        _life -= Time.deltaTime;
        if (_life <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (Time.time >= _nextJitter) Jitter();
        Apply(_life / _duration);
    }

    private void Jitter()
    {
        _nextJitter = Time.time + 0.035f;

        Vector3 dir = _to - _from;
        Vector3 fwd = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;
        Vector3 right = Vector3.Cross(fwd, Vector3.up);
        if (right.sqrMagnitude < 0.0001f) right = Vector3.Cross(fwd, Vector3.right);
        right.Normalize();
        Vector3 up = Vector3.Cross(right, fwd);

        Vector3[] pts = new Vector3[Points];
        foreach (LineRenderer l in _arcs)
        {
            for (int i = 0; i < Points; i++)
            {
                float t = (float)i / (Points - 1);
                float taper = Mathf.Sin(t * Mathf.PI);
                Vector3 off = (right * Random.Range(-1f, 1f) + up * Random.Range(-1f, 1f)) * (_amp * taper);
                pts[i] = Vector3.Lerp(_from, _to, t) + off;
            }
            l.SetPositions(pts);
        }
    }

    private void Apply(float t01)
    {
        foreach (LineRenderer l in _arcs)
        {
            // Chập chờn: mỗi sợi mỗi khung hình sáng một khác — điện không bao giờ sáng đều
            Color c = Color.Lerp(_color, Color.white, 0.3f) * 2.6f;   // giữ màu điện tích, không trắng bệch
            c.a = t01 * Random.Range(0.55f, 1f);
            l.startColor = c;
            l.endColor = c;
            l.widthMultiplier = 0.045f * (0.5f + 0.5f * t01);
        }
    }
}
