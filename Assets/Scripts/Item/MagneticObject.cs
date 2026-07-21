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

    [Header("Inventory Settings (New)")]
    [Tooltip("Kéo file ItemData tương ứng vào đây (Ví dụ: bàn/ghế kéo file NormalAmmoData)")]
    public ItemData itemData; 

    private Rigidbody rb;
    private Material mat;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        Renderer rend = GetComponent<Renderer>();
        if (rend != null) mat = rend.material;
        
        SetColorBasedOnPolarity();
    }

    public void SetPolarity(Polarity newPolarity)
    {
        currentPolarity = newPolarity;
        SetColorBasedOnPolarity();
    }

    void SetColorBasedOnPolarity()
    {
        if (mat == null) return;
        if (currentPolarity == Polarity.Positive) mat.color = Color.red;
        else if (currentPolarity == Polarity.Negative) mat.color = Color.blue;
        else mat.color = Color.white;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isMovingAsBullet) return;

        // Bỏ qua va chạm với chính người bắn ra nó
        if (collision.collider.gameObject == shooterOwner.gameObject) return;

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
                rb.isKinematic = true;
                rb.useGravity = false;
                isMovingAsBullet = false;
                return;
            }
        }

        // Nếu là thùng TNT thì kích nổ khi va chạm mạnh
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
            rb.linearDamping = 0.05f; // Khôi phục lại giá trị mặc định của Unity 6
        }
    }

    void Explode()
    {
        float explosionRadius = 6f;
        float explosionForce = 10f;
        float tntDamage = 35f;

        Debug.Log("<color=red><b>TNT BARREL EXPLODED!</b></color>");

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        
        // Dùng HashSet để lưu danh sách Player đã dính sát thương, tránh bị trừ máu 2 lần do trùng Collider
        HashSet<PlayerHealth> playersDamaged = new HashSet<PlayerHealth>();

        foreach (Collider hit in colliders)
        {
            Rigidbody targetRb = hit.GetComponent<Rigidbody>();
            if (targetRb != null && targetRb != rb)
            {
                targetRb.AddExplosionForce(explosionForce, transform.position, explosionRadius, 1f, ForceMode.Impulse);
            }

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
            }
        }

        Destroy(gameObject);
    }
}