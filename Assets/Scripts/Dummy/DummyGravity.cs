using UnityEngine;

public class DummyGravity : MonoBehaviour
{
    private CharacterController controller;
    private Vector3 velocity;
    public float gravity = -9.81f; // Trọng lực đủ đầm để rơi mượt
    private Vector3 impact = Vector3.zero;
    public float mass = 3f; 

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (controller != null)
        {
            // Ép sát đất nếu đang đứng để không bị nảy tưng tưng
            if (controller.isGrounded && velocity.y < 0)
                velocity.y = -2f;

            // Rơi xuống
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }
        if (impact.magnitude > 0.2f)
        {
            controller.Move(impact * Time.deltaTime);
        }
        // Giảm tốc theo đường cong quán tính (5f là hệ số ma sát, số càng cao dừng càng nhanh)
        impact = Vector3.Lerp(impact, Vector3.zero, 5f * Time.deltaTime);
    }

    public void AddImpact(Vector3 dir, float force)
    {
        dir.Normalize();
        if (dir.y < 0) dir.y = -dir.y; // Đảm bảo lực luôn hất nhẹ lên trên chứ không cắm xuống đất
        impact += dir * (force / mass);
    }
}