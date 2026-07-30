using Fusion;
using UnityEngine;
using System.Collections.Generic; // Cần thiết để lọc trùng danh sách khi nổ TNT

public class MagneticObject : NetworkBehaviour
{
    public enum Polarity { None, Positive, Negative }
    public enum ObjectType { Normal, Heavy, Spike, TNT }

    [Header("Base Settings")]
    [Tooltip("Loại vật thể. Đặt cố định cho từng prefab, không đổi lúc chạy nên không cần đồng bộ.")]
    public ObjectType objectType = ObjectType.Normal;

    [Tooltip("Sát thương gốc của loại vật thể này: Normal 10, Heavy 20, Spike 25.")]
    public float baseDamage = 10f;

    // Ghi chú thiết kế: Spike đã BỎ tính chất cắm dính vào người trúng.
    // Giờ nó va chạm y hệt vật thường, chỉ khác ở con số sát thương cao hơn.

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

    // Người đã bắn vật này ra. CHỈ có ý nghĩa trên máy Host, vì chỉ Host xử lý va chạm.
    [HideInInspector] public PlayerMagnetController shooterOwner;

    private Rigidbody rb;
    private Material mat;
    private MagneticAura auraScript;

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody>();

        Renderer rend = GetComponent<Renderer>();
        if (rend != null) mat = rend.material;

        auraScript = GetComponent<MagneticAura>();

        // Chỉ Host đặt giá trị khởi đầu, Client nhận về qua mạng
        if (HasStateAuthority)
        {
            CurrentDamage = baseDamage;
        }

        // Áp màu và hào quang theo điện tích hiện tại.
        // Gọi ở đây để vật thể vào trận muộn vẫn hiển thị đúng trạng thái.
        OnPolarityChanged();
    }

    // Fusion tự gọi trên MỌI máy mỗi khi currentPolarity đổi giá trị.
    // Nhờ vậy màu sắc và hào quang tự khớp nhau mà không cần gửi RPC riêng.
    private void OnPolarityChanged()
    {
        RefreshAura();
        SetColorBasedOnPolarity();
    }

    public void SetPolarity(Polarity newPolarity)
    {
        // Chỉ Host được đổi điện tích. Đã thử cho Client tự đoán trước rồi bỏ (30/07),
        // vì phép thử dự đoán bên PlayerMagnetController làm mọi thứ giật hơn.
        if (!HasStateAuthority) return;

        currentPolarity = newPolarity;

        // Không cần gọi RefreshAura ở đây: OnChangedRender sẽ tự kích hoạt trên mọi máy.
    }

    void SetColorBasedOnPolarity()
    {
        if (mat == null) return;
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

    void OnCollisionEnter(Collision collision)
    {
        // Chỉ Host tính va chạm và sát thương.
        // Nếu để mọi máy cùng tính thì nạn nhân sẽ bị trừ máu nhiều lần.
        if (Object == null || !Object.IsValid || !HasStateAuthority) return;

        if (!isMovingAsBullet) return;

        // Bỏ qua va chạm với chính người bắn ra nó
        if (shooterOwner != null && collision.collider.gameObject == shooterOwner.gameObject) return;

        // Trúng đối thủ
        if (collision.collider.CompareTag("Player"))
        {
            PlayerHealth health = collision.collider.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(CurrentDamage);
            }
        }

        // Thùng TNT thì kích nổ khi va chạm
        if (objectType == ObjectType.TNT)
        {
            Explode();
            return;
        }

        ResetBulletState();
    }

    public void ResetBulletState()
    {
        if (!HasStateAuthority) return;

        isMovingAsBullet = false;
        shooterOwner = null;
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

        // Dùng Runner.Despawn thay cho Destroy. Destroy chỉ xoá ở máy này,
        // còn Despawn báo cho mọi máy cùng xoá nên không ai bị sót lại thùng TNT ma.
        Runner.Despawn(Object);
    }
}
