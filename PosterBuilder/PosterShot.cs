using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Dàn cảnh chiến đấu và render ảnh chủ đạo cho poster (19/10). Không lưu scene.
/// File này CỐ Ý nằm ngoài Assets/ để không vào game. Muốn render lại: chép vào Assets/Editor/,
/// tắt Unity, chạy -executeMethod PosterShotTemp.Final, chép ảnh ra PosterBuilder/keyart.png, rồi XOÁ khỏi Assets/Editor.
/// </summary>
public static class PosterShotTemp
{
    public static void Preview() { Run(944, 1336, "C:/mdtmp/poster/keyart_preview.png"); }
    public static void Final() { Run(4677, 6622, "C:/mdtmp/poster/keyart_final.png"); }

    private static readonly List<PlayableGraph> Graphs = new List<PlayableGraph>();

    private static void Run(int w, int h, string outPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outPath));
        EditorSceneManager.OpenScene("Assets/TestScene.unity", OpenSceneMode.Single);
        Physics.SyncTransforms();

        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) c.enabled = false;
        foreach (RoundBarrier rb in Object.FindObjectsByType<RoundBarrier>(FindObjectsSortMode.None))
            foreach (Renderer rend in rb.GetComponentsInChildren<Renderer>()) rend.enabled = false;
        foreach (Camera c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) { c.enabled = false; c.tag = "Untagged"; }

        // --- Camera: thấp, ngước nhẹ lên cho bầu trời chiếm phần trên (chỗ đặt logo) ---
        Camera cam = new GameObject("PosterCam").AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.fieldOfView = 58f;
        cam.farClipPlane = 1500f;
        cam.allowHDR = true;
        cam.allowMSAA = true;
        var cd = cam.GetUniversalAdditionalCameraData();
        cd.renderPostProcessing = true;
        cd.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        cd.antialiasingQuality = AntialiasingQuality.High;
        cd.requiresColorOption = CameraOverrideOption.On;
        cd.requiresDepthOption = CameraOverrideOption.On;
        // Mọi vị trí tính theo trục nhìn: L = tâm trận đánh, f = hướng nhìn (ngang), r = sang phải màn hình
        Vector3 L = Ground(36.5f, 45.5f);
        Vector3 f = L - new Vector3(43.0f, L.y, 38.6f); f.y = 0f; f.Normalize();
        Vector3 r = new Vector3(f.z, 0f, -f.x);
        Vector3 C0 = L - f * 6.4f;
        Vector3 C = Ground(C0.x, C0.z) + Vector3.up * 1.0f;              // camera thấp ~1m: góc anh hùng
        cam.transform.position = C;
        cam.transform.LookAt(L + Vector3.up * 2.0f);

        // --- Nhân vật ---
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Player.prefab");
        Color red = new Color(1f, 0.28f, 0.2f), blue = new Color(0.25f, 0.6f, 1f);
        string fbx = "Assets/Resources/Character/Vanguard By T. Choonyung@";

        Vector3 redPos = Ground(L.x - r.x * 1.5f, L.z - r.z * 1.5f);                      // trái màn hình
        Vector3 bluePos = Ground(L.x + r.x * 1.4f + f.x * 0.4f, L.z + r.z * 1.4f + f.z * 0.4f) + Vector3.up * 0.8f;
        Vector3 rock = L + f * 9.0f - r * 1.2f + Vector3.up * 5.0f;                        // đá lơ lửng bị nạp điện
        Vector3 red2Pos = Ground(L.x + f.x * 13.0f + r.x * 6.4f, L.z + f.z * 13.0f + r.z * 6.4f);

        GameObject pRed = Spawn(prefab, redPos, Face(redPos, bluePos), red);
        GameObject pBlue = Spawn(prefab, bluePos, Face(bluePos, redPos) * Quaternion.Euler(-26f, 0f, -10f), blue);
        GameObject pRed2 = Spawn(prefab, red2Pos, Face(red2Pos, rock), red);

        // Đặt tư thế cả ba trong CÙNG MỘT LƯỢT lấy mẫu - AnimationMode trả tư thế cũ cho mọi thứ
        // không được lấy mẫu trong lượt hiện tại
        AnimationMode.StartAnimationMode();
        AnimationMode.BeginSampling();
        Pose(pRed, fbx + "Hook Punch.fbx", 0.42f);
        Pose(pBlue, fbx + "Falling Idle.fbx", 0.3f);
        Pose(pRed2, fbx + "Hook Punch.fbx", 0.5f);
        AnimationMode.EndSampling();

        // CHÂN CHẠM ĐẤT: gốc prefab Player nằm ở GIỮA THÂN (tâm CharacterController), đặt gốc ở mặt đất
        // là lún nửa người. Sau khi có tư thế, tìm điểm thấp nhất của bàn chân rồi nâng cả người lên.
        PlantFeet(pRed, redPos.y);
        PlantFeet(pRed2, red2Pos.y);

        // --- Vật thể lơ lửng: nhấc vài tảng đá lên không cho ra chất "từ tính" ---
        var lift = new (string name, float x, float z, Vector3 to, Vector3 rot)[]
        {
            ("Rock6C (1)", 30.5f, 41.0f, rock, new Vector3(18f, 40f, 25f)),
            ("Rock6B", 49.5f, 39.0f, L + f * 11.0f - r * 5.6f + Vector3.up * 6.4f, new Vector3(-20f, 70f, 10f)),
            ("Rock1D", 56.9f, 30.05f, L + f * 13f + r * 5.2f + Vector3.up * 6.6f, new Vector3(35f, 10f, -15f)),
        };
        foreach (MagneticObject m in Object.FindObjectsByType<MagneticObject>(FindObjectsSortMode.None))
            foreach (var l in lift)
                if (m.name == l.name && Mathf.Abs(m.transform.position.x - l.x) < 1.5f && Mathf.Abs(m.transform.position.z - l.z) < 1.5f)
                {
                    m.transform.position = l.to;
                    m.transform.rotation = Quaternion.Euler(l.rot);
                    if (l.name == "Rock6C (1)") m.transform.localScale *= 0.75f;   // tảng giữa đỡ che vụ nổ
                }

        // --- Hiệu ứng ---
        Transform red2Hand = pRed2.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.RightHand);
        Transform redHand = pRed.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.RightHand);
        Vector3 blueChest = pBlue.transform.position + Vector3.up * 1.2f;

        CombatVFX.ChargeBeam(red2Hand.position, rock, red);
        CombatVFX.Flash(red2Hand.position, rock - red2Hand.position, red, 0.6f, 0.22f);
        CombatVFX.PunchImpact(Vector3.Lerp(redHand.position, blueChest, 0.6f), blueChest - redHand.position, new Color(0.55f, 0.85f, 1f), 0.35f);
        CombatVFX.Explosion(Ground(L.x + f.x * 16f + r.x * 1.5f, L.z + f.z * 16f + r.z * 1.5f) + Vector3.up * 0.6f, 6f);

        // Đặt mọi hiệu ứng vào một khoảnh khắc đẹp (Update không chạy khi render trong Editor)
        FreezeEffects();

        // Biển mây quay về camera
        CloudBank cloud = Object.FindFirstObjectByType<CloudBank>();
        if (cloud != null) typeof(CloudBank).GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(cloud, null);

        // --- Render ---
        RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        File.WriteAllBytes(outPath, tex.EncodeToPNG());
        RenderTexture.active = null;
        cam.targetTexture = null;
        foreach (var g in Graphs) if (g.IsValid()) g.Destroy();
        EditorApplication.Exit(0);
    }

    private static void PlantFeet(GameObject g, float groundY)
    {
        Animator a = g.GetComponentInChildren<Animator>();
        float low = float.MaxValue;
        foreach (HumanBodyBones b in new[] { HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot, HumanBodyBones.LeftToes, HumanBodyBones.RightToes })
        {
            Transform t = a.GetBoneTransform(b);
            if (t != null) low = Mathf.Min(low, t.position.y);
        }
        if (low == float.MaxValue) return;
        g.transform.position += Vector3.up * (groundY + 0.06f - low);   // 6cm: xương cổ chân nằm trên đế giày
    }

    private static Quaternion Face(Vector3 from, Vector3 to)
    {
        Vector3 d = to - from; d.y = 0f;
        return Quaternion.LookRotation(d.normalized, Vector3.up);
    }

    private static Vector3 Ground(float x, float z)
    {
        foreach (RaycastHit hit in Physics.RaycastAll(new Vector3(x, 60f, z), Vector3.down, 120f, ~0, QueryTriggerInteraction.Ignore))
            if (hit.collider.attachedRigidbody == null && !hit.collider.name.StartsWith("Barrier")) return hit.point;
        return new Vector3(x, 4.5f, z);
    }

    private static GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Color team)
    {
        GameObject g = Object.Instantiate(prefab, pos, rot);
        foreach (Camera c in g.GetComponentsInChildren<Camera>(true)) c.enabled = false;
        foreach (AudioListener a in g.GetComponentsInChildren<AudioListener>(true)) a.enabled = false;
        foreach (Canvas c in g.GetComponentsInChildren<Canvas>(true)) c.enabled = false;
        foreach (TMPro.TMP_Text t in g.GetComponentsInChildren<TMPro.TMP_Text>(true)) t.enabled = false;

        // Viền màu đội như trong game (đồng đội / địch đều đọc ra phe ngay)
        Material mask = Object.Instantiate(Resources.Load<Material>("Materials/OutlineMask"));
        Material fill = Object.Instantiate(Resources.Load<Material>("Materials/OutlineFill"));
        mask.SetFloat("_ZTest", (float)CompareFunction.Always);
        fill.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
        fill.SetColor("_OutlineColor", team);
        fill.SetFloat("_OutlineWidth", 4f);
        foreach (Renderer r in g.GetComponentsInChildren<Renderer>(true))
        {
            if (!(r is SkinnedMeshRenderer) && !(r is MeshRenderer)) continue;
            if (!r.enabled) continue;
            var mats = new List<Material>(r.sharedMaterials) { mask, fill };
            r.sharedMaterials = mats.ToArray();
        }
        return g;
    }

    // Đặt tư thế bằng PlayableGraph (chạy được trong Editor, với nhân vật humanoid)
    private static void Pose(GameObject g, string fbxPath, float normalizedTime)
    {
        Animator anim = g.GetComponentInChildren<Animator>();
        AnimationClip clip = null;
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            if (o is AnimationClip c && !c.name.StartsWith("__preview__")) { clip = c; break; }
        if (anim == null || clip == null) { Debug.LogWarning("[POSTER] Không đặt được tư thế " + fbxPath); return; }

        anim.applyRootMotion = false;
        Transform hand = anim.GetBoneTransform(HumanBodyBones.RightHand);
        Vector3 before = hand != null ? hand.position : Vector3.zero;
        AnimationMode.SampleAnimationClip(anim.gameObject, clip, clip.length * normalizedTime);
        Debug.Log("[POSTER] " + clip.name + " len " + clip.length + " humanoid " + clip.isHumanMotion + " avatar " + (anim.avatar != null) +
                  " hand moved " + (hand != null ? (hand.position - before).magnitude : -1f));
    }

    private static void FreezeEffects()
    {
        // Hệ hạt: tua tới khoảnh khắc lửa + khói cùng hiện
        foreach (ParticleSystem ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
            if (ps.gameObject.name.StartsWith("VFX")) ps.Simulate(ps.gameObject.name.Contains("Smoke") ? 0.55f : 0.28f, true, true);

        // Các hiệu ứng vẽ bằng đường: gọi Apply ở giữa nhịp
        foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (!mb.gameObject.name.StartsWith("VFX")) continue;
            System.Type t = mb.GetType();
            FieldInfo delay = t.GetField("_delay", BindingFlags.NonPublic | BindingFlags.Instance);
            if (delay != null) { delay.SetValue(mb, 0f); LineRenderer lr = mb.GetComponent<LineRenderer>(); if (lr) lr.enabled = true; }
            MethodInfo apply = t.GetMethod("Apply", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(float) }, null);
            if (apply == null) continue;
            // Vòng sóng của cú ĐẤM chụp sớm (còn nhỏ, sáng) - loang rộng thì vạch trắng chằng chịt giữa ảnh
            bool punch = mb.gameObject.name.StartsWith("VFX Punch");
            float v = t.Name == "SparkBurst" ? 0.3f : t.Name == "ShockwaveRing" ? (punch ? 0.82f : 0.55f) : 0.75f;
            apply.Invoke(mb, new object[] { v });
        }
    }
}
