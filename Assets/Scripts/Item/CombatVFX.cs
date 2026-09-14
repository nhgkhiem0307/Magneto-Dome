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
    /// </summary>
    public static bool logVFX = true;

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
    public static void QiStrike(Vector3 from, Vector3 to, Color color)
    {
        if (Additive == null) return;
        if ((to - from).sqrMagnitude < 0.0001f) return;

        if (logVFX) Debug.Log($"<color=#66CCFF>[CombatVFX] Quyền khí {from} -> {to}</color>");

        GameObject go = new GameObject("VFX QiBolt");
        go.transform.position = from;
        go.AddComponent<QiBolt>().Play(from, to, color);
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

    public void Play(float radius, Color color, float duration,
                     float width = 0.6f, float lightPower = 14f)
    {
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
        _flash = gameObject.AddComponent<Light>();
        _flash.type = LightType.Point;
        _flash.color = color;
        _flash.range = radius * 2.5f;
        _flash.shadows = LightShadows.None;   // đèn chớp mà đổ bóng thì rất tốn

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

    public void Play(Vector3 from, Vector3 to, Color color)
    {
        _from = from;
        _to = to;
        _color = color;

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
            CombatVFX.Impact(_to, _to - _from, _color);
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
    private const int Count = 9;
    private const float Duration = 0.26f;

    private LineRenderer[] _lines;
    private Vector3[] _dirs;
    private float[] _speed;
    private Vector3 _at;
    private Color _color;
    private float _size;
    private float _life;

    public void Play(Vector3 at, Vector3 hitDir, Color color, float size)
    {
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
