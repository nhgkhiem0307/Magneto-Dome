using UnityEngine;
using System.Collections.Generic; // Cần thiết để lọc trùng danh sách khi nổ TNT

public class MagneticObject : MonoBehaviour
{
    public enum Polarity { None, Positive, Negative }
    public enum ObjectType { Normal, Heavy, Spike, TNT }

    [Header("Base Settings")]
    public Polarity currentPolarity = Polarity.None;
    public ObjectType objectType = ObjectType.Normal;
    public float baseDamage = 10f;

    [Header("Movement State")]
    public bool isMovingAsBullet = false;
    
    [HideInInspector] public PlayerMagnetController shooterOwner; 

    [Header("Inventory Settings")]
    [Tooltip("Kéo file ItemData tương ứng vào đây (Ví dụ: bàn/ghế kéo file NormalAmmoData)")]
    public ItemData itemData; 

    private Rigidbody rb;
    private Material mat;
    private MagneticAura auraScript;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        Renderer rend = GetComponent<Renderer>();
        if (rend != null) mat = rend.material;

        // Đã sửa lỗi CS0411: Thêm Generic Type <MagneticAura>
        auraScript = GetComponent<MagneticAura>();
        
        RefreshAura();
        SetColorBasedOnPolarity();
    }

    public void SetPolarity(Polarity newPolarity)
    {
        // Đã sửa lỗi CS0103: Đồng bộ dùng biến currentPolarity
        currentPolarity = newPolarity;
        RefreshAura();
        SetColorBasedOnPolarity();
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
            // Đã sửa lỗi CS0103: Truyền đúng biến currentPolarity vào Aura
            auraScript.UpdateAura(currentPolarity);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isMovingAsBullet) return;

        // Bỏ qua va chạm với chính người bắn ra nó
        if (shooterOwner != null && collision.collider.gameObject == shooterOwner.gameObject) return;

        // Trúng đối thủ (Player khác hoặc Kẻ địch)
        if (collision.collider.CompareTag("Player"))
        {
            PlayerHealth health = collision.collider.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(baseDamage);
            }
            
            if (objectType == ObjectType.Spike)
            {
                // Đạn Spike dính vào người đối thủ
                transform.SetParent(collision.transform);
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
                isMovingAsBullet = false;
                return;
            }
        }

        // Nếu là thùng TNT thì kích nổ khi va chạm
        if (objectType == ObjectType.TNT)
        {
            Explode();
            return;
        }

        ResetBulletState();
    }

    public void ResetBulletState()
    {
        isMovingAsBullet = false;
        shooterOwner = null;
        if (rb != null)
        {
            rb.useGravity = true;
            rb.linearDamping = 0.05f; // Giá trị mặc định của Unity 6
        }
    }

    void Explode()
    {
        float explosionRadius = 6f;
        float explosionForce = 15f;
        float tntDamage = 35f;

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

        Destroy(gameObject);
    }
}