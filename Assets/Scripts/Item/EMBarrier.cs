using Fusion;
using UnityEngine;

/// <summary>
/// Bức tường tạm do Lõi Tường Điện Từ (EM Barrier Core) sinh ra.
/// Tự biến mất sau một khoảng thời gian.
///
/// Khác với RoundBarrier (rào chắn đầu round): rào đó đặt sẵn trong scene và chỉ bật/tắt,
/// còn cái này được Host sinh ra giữa trận nên bắt buộc phải là NetworkObject.
///
/// HÌNH ẢNH (14/10): tự dựng bằng mã lúc chạy, không cần model hay texture nào —
/// một tấm khiên ghép từ ô lục giác lơ lửng trên một viên lõi kim loại nằm dưới đất.
/// Toàn bộ vẻ ngoài nằm ở shader "SkyArena/Hex Shield". Cố ý KHÁC rào chắn đầu round
/// (lưới ô vuông) để người chơi không nhầm hai thứ với nhau.
///
/// Gốc toạ độ của object này nằm ở MẶT ĐẤT, chính giữa bức tường.
/// </summary>
public class EMBarrier : NetworkBehaviour
{
    [Tooltip("Tồn tại bao nhiêu giây rồi tự biến mất.")]
    public float lifetime = 10f;

    [Header("Hình dáng")]
    [Tooltip("Rộng × cao của tấm khiên (mét). Va chạm tự khớp theo số này.")]
    public Vector2 panelSize = new Vector2(4f, 3f);

    [Tooltip("Tấm khiên lơ lửng cách mặt đất bao nhiêu mét. Phần va chạm thì vẫn chạm sát đất, "
           + "để không vật nào lọt qua khe dưới.")]
    public float hoverHeight = 0.3f;

    [Tooltip("Độ dày phần va chạm (mét). Hình thì mỏng như tờ giấy, nhưng va chạm quá mỏng "
           + "thì vật bay nhanh có thể xuyên qua giữa hai tick vật lý.")]
    public float colliderThickness = 0.35f;

    [Networked] private TickTimer LifeTimer { get; set; }

    // Chỗ bị vật thể đập vào, tính bằng mét trên mặt khiên. Host ghi, mọi máy vẽ gợn sóng.
    // Dùng bộ đếm + OnChangedRender: cùng khuôn với LaunchCount / ExplodeCount.
    [Networked] private Vector2 HitPoint { get; set; }
    [Networked, OnChangedRender(nameof(OnHitChanged))] private int HitCount { get; set; }

    private Visual _visual;
    private float _hitTime = -99f;

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            LifeTimer = TickTimer.CreateFromSeconds(Runner, lifetime);
        }

        // Va chạm lấy theo đúng kích thước khiên, để hình và va chạm không bao giờ lệch nhau.
        if (TryGetComponent(out BoxCollider box))
        {
            float height = panelSize.y + hoverHeight;
            box.size = new Vector3(panelSize.x, height, colliderThickness);
            box.center = new Vector3(0f, height * 0.5f, 0f);
        }

        _visual = Visual.Build(transform, panelSize, hoverHeight);

        // Khối hộp cũ trên prefab chỉ còn làm phương án dự phòng khi dựng hình thất bại.
        if (_visual != null && TryGetComponent(out MeshRenderer oldCube))
        {
            oldCube.enabled = false;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _visual?.Dispose();
        _visual = null;
    }

    public override void FixedUpdateNetwork()
    {
        // Chỉ Host được quyền xoá. Dùng Runner.Despawn chứ không phải Destroy,
        // để mọi máy cùng xoá, không ai bị sót lại bức tường ma.
        if (!HasStateAuthority) return;

        if (LifeTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
        }
    }

    public override void Render()
    {
        if (_visual == null) return;

        // Mọi máy tự tính tuổi bức tường từ bộ đếm giờ đã đồng bộ — không cần gửi thêm gì.
        float remaining = LifeTimer.RemainingTime(Runner) ?? lifetime;
        float elapsed = lifetime - remaining;

        _visual.Tick(elapsed, remaining, Time.time - _hitTime, HitPoint);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Vật lý chỉ tính thật trên Host, nên chỉ Host ghi chỗ trúng.
        if (Object == null || !Object.IsValid || !HasStateAuthority) return;
        if (collision.contactCount == 0) return;

        Vector3 local = transform.InverseTransformPoint(collision.GetContact(0).point);
        HitPoint = new Vector2(local.x + panelSize.x * 0.5f, local.y - hoverHeight);
        HitCount++;
    }

    private void OnHitChanged()
    {
        _hitTime = Time.time;
    }

    // =====================================================================
    //  PHẦN HÌNH ẢNH — tách thành lớp riêng, không dính gì tới mạng,
    //  nên dựng thử được ngoài trận (vd. để chụp ảnh xem trước trong Editor).
    // =====================================================================
    public sealed class Visual
    {
        public const float RevealTime = 0.5f;     // các ô lan ra từ lõi trong bao lâu
        public const float DissolveTime = 0.4f;   // tan về lõi trong bao lâu trước khi mất
        public const float WarnTime = 2f;         // nhấp nháy báo sắp hết từ giây thứ mấy

        private const string ShaderName = "SkyArena/Hex Shield";
        private const float CoreRadius = 0.32f;
        private const float CoreHeight = 0.14f;
        private const float GroundSize = 2.4f;

        private readonly Transform _root;
        private readonly Transform _panel;
        private readonly Transform _core;
        private readonly float _hover;
        private readonly Material _shieldMat, _coreMat, _groundMat;
        private readonly Mesh _shieldMesh, _coreMesh, _groundMesh;

        private static readonly int RevealId = Shader.PropertyToID("_Reveal");
        private static readonly int FlickerId = Shader.PropertyToID("_Flicker");
        private static readonly int HitPosId = Shader.PropertyToID("_HitPos");
        private static readonly int HitAgeId = Shader.PropertyToID("_HitAge");

        public static Visual Build(Transform parent, Vector2 size, float hover)
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError("[TƯỜNG ĐIỆN TỪ] Không tìm thấy shader '" + ShaderName
                             + "' — kiểm tra file Assets/Resources/Shaders/HexShield.shader");
                return null;
            }
            return new Visual(parent, shader, size, hover);
        }

        private Visual(Transform parent, Shader shader, Vector2 size, float hover)
        {
            _hover = hover;

            _root = new GameObject("EMBarrierVisual").transform;
            _root.SetParent(parent, false);

            // --- Tấm khiên ---
            _shieldMat = NewMaterial(shader, 0f, transparent: true, 3000);
            _shieldMat.SetVector("_Size", new Vector4(size.x, size.y, 0f, 0f));
            _shieldMat.SetVector("_CorePos", new Vector4(size.x * 0.5f, -hover + CoreHeight, 0f, 0f));
            _shieldMesh = BuildShieldMesh(size, _shieldMat.GetFloat("_HexSize"));
            _panel = NewPart("Shield", _shieldMesh, _shieldMat, new Vector3(0f, hover, 0f));

            // --- Vệt sáng dưới đất (vẽ trước tấm khiên) ---
            _groundMat = NewMaterial(shader, 2f, transparent: true, 2990);
            _groundMesh = BuildGroundMesh(GroundSize);
            NewPart("GroundGlow", _groundMesh, _groundMat, new Vector3(0f, 0.015f, 0f));

            // --- Lõi kim loại ---
            _coreMat = NewMaterial(shader, 1f, transparent: false, 2000);
            _coreMat.SetVector("_Size", new Vector4(0f, 0f, CoreRadius, CoreHeight));
            _coreMesh = BuildCoreMesh(CoreRadius, CoreHeight);
            _core = NewPart("Core", _coreMesh, _coreMat, Vector3.zero);
        }

        /// <param name="elapsed">Giây kể từ lúc thả.</param>
        /// <param name="remaining">Giây còn lại trước khi biến mất.</param>
        /// <param name="hitAge">Giây kể từ lần cuối bị vật đập vào.</param>
        /// <param name="hitPoint">Chỗ bị đập, mét trên mặt khiên.</param>
        public void Tick(float elapsed, float remaining, float hitAge, Vector2 hitPoint)
        {
            // Lan ra lúc thả, thu về lúc sắp mất.
            float reveal;
            if (remaining < DissolveTime) reveal = Mathf.Clamp01(remaining / DissolveTime);
            else reveal = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / RevealTime), 3f);

            // 2 giây cuối: chớp tắt, càng về cuối càng dồn dập.
            float flicker = 1f;
            if (remaining < WarnTime)
            {
                float urgency = 1f - remaining / WarnTime;
                float freq = Mathf.Lerp(3f, 11f, urgency);
                flicker = Mathf.Sin(Time.time * freq * Mathf.PI * 2f) > 0f ? 1f : 0.3f;
            }

            _shieldMat.SetFloat(RevealId, reveal);
            _shieldMat.SetFloat(FlickerId, flicker);
            _shieldMat.SetVector(HitPosId, hitPoint);
            _shieldMat.SetFloat(HitAgeId, hitAge);
            _groundMat.SetFloat(RevealId, reveal);
            _groundMat.SetFloat(FlickerId, flicker);
            _coreMat.SetFloat(FlickerId, flicker);

            // Khiên nhấp nhô rất nhẹ — đủ để thấy nó "lơ lửng", không đủ để rối mắt.
            _panel.localPosition = new Vector3(0f, _hover + Mathf.Sin(Time.time * 1.4f) * 0.03f, 0f);

            // Lõi bật lên lúc thả (vọt quá một chút rồi về), co lại lúc biến mất.
            float pop = Mathf.Clamp01(elapsed / 0.25f);
            float popScale = 1f + 2.7f * Mathf.Pow(pop - 1f, 3f) + 1.7f * Mathf.Pow(pop - 1f, 2f);
            _core.localScale = Vector3.one * Mathf.Max(0.001f, popScale * Mathf.Clamp01(remaining / DissolveTime + 0.15f));
        }

        public void Dispose()
        {
            if (_root != null) UnityEngine.Object.Destroy(_root.gameObject);
            UnityEngine.Object.Destroy(_shieldMat);
            UnityEngine.Object.Destroy(_coreMat);
            UnityEngine.Object.Destroy(_groundMat);
            UnityEngine.Object.Destroy(_shieldMesh);
            UnityEngine.Object.Destroy(_coreMesh);
            UnityEngine.Object.Destroy(_groundMesh);
        }

        // ---------------------------------------------------------------

        private Transform NewPart(string name, Mesh mesh, Material mat, Vector3 localPos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(_root, false);
            go.transform.localPosition = localPos;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;

            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go.transform;
        }

        // transparent = true: "premultiplied alpha" — vừa nhuộm màu cảnh phía sau vừa phát sáng
        // (xem giải thích trong shader). false: khối đặc bình thường, dùng cho lõi kim loại.
        private static Material NewMaterial(Shader shader, float mode, bool transparent, int queue)
        {
            Material m = new Material(shader) { name = "EMBarrier (runtime)", renderQueue = queue };
            m.SetFloat("_Mode", mode);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_DstBlend", (float)(transparent ? UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha : UnityEngine.Rendering.BlendMode.Zero));
            m.SetFloat("_ZWrite", transparent ? 0f : 1f);
            return m;
        }

        // Một tấm phẳng hơi to hơn khiên một ô mỗi phía: shader tự bỏ các ô có tâm nằm ngoài,
        // nên mép thật là một chuỗi ô lục giác nguyên vẹn. UV = mét trên mặt khiên.
        private static Mesh BuildShieldMesh(Vector2 size, float margin)
        {
            float x0 = -size.x * 0.5f - margin, x1 = size.x * 0.5f + margin;
            float y0 = -margin, y1 = size.y + margin;

            Mesh mesh = new Mesh { name = "EMBarrierShield (runtime)" };
            mesh.vertices = new[] { new Vector3(x0, y0, 0f), new Vector3(x1, y0, 0f), new Vector3(x0, y1, 0f), new Vector3(x1, y1, 0f) };
            mesh.uv = new[]
            {
                new Vector2(-margin, -margin), new Vector2(size.x + margin, -margin),
                new Vector2(-margin, size.y + margin), new Vector2(size.x + margin, size.y + margin),
            };
            mesh.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildGroundMesh(float size)
        {
            float h = size * 0.5f;
            Mesh mesh = new Mesh { name = "EMBarrierGround (runtime)" };
            mesh.vertices = new[] { new Vector3(-h, 0f, -h), new Vector3(h, 0f, -h), new Vector3(-h, 0f, h), new Vector3(h, 0f, h) };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            mesh.RecalculateBounds();
            return mesh;
        }

        // Lăng trụ lục giác: mặt trên + 6 mặt bên, mỗi mặt pháp tuyến riêng cho cạnh sắc.
        // Không có mặt đáy vì nó nằm úp xuống đất, không ai nhìn thấy.
        private static Mesh BuildCoreMesh(float radius, float height)
        {
            var verts = new System.Collections.Generic.List<Vector3>();
            var norms = new System.Collections.Generic.List<Vector3>();
            var tris = new System.Collections.Generic.List<int>();

            Vector3 Corner(int i, float y)
            {
                float a = Mathf.Deg2Rad * (60f * i + 30f);
                return new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius);
            }

            // Mặt trên
            verts.Add(new Vector3(0f, height, 0f)); norms.Add(Vector3.up);
            for (int i = 0; i < 6; i++) { verts.Add(Corner(i, height)); norms.Add(Vector3.up); }
            for (int i = 0; i < 6; i++) { tris.Add(0); tris.Add(1 + (i + 1) % 6); tris.Add(1 + i); }

            // Mặt bên
            for (int i = 0; i < 6; i++)
            {
                Vector3 a = Corner(i, 0f), b = Corner((i + 1) % 6, 0f);
                Vector3 n = ((a + b) * 0.5f).normalized;
                int s = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(Corner(i, height)); verts.Add(Corner((i + 1) % 6, height));
                for (int k = 0; k < 4; k++) norms.Add(n);
                tris.Add(s); tris.Add(s + 2); tris.Add(s + 1);
                tris.Add(s + 1); tris.Add(s + 2); tris.Add(s + 3);
            }

            Mesh mesh = new Mesh { name = "EMBarrierCore (runtime)" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
