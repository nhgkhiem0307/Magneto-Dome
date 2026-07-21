using UnityEngine;

public class FPSMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;
    public Transform cameraTransform;

    [Header("Gravity & Physics")]
    public float gravity = -9.81f; // Trọng lực (Thường game FPS để gấp đôi thực tế rơi cho đầm người)
    private Vector3 velocity;       // Vận tốc rơi (Cộng dồn theo thời gian)

    private Vector3 impact = Vector3.zero;
    public float mass = 3f;
    private CharacterController controller;
    private float verticalRotation = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        
        // Ẩn con trỏ chuột và khóa nó ở giữa màn hình giống game FPS thực tế
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 1. XOAY CAMERA THEO CHUỘT
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Xoay nhân vật theo trục ngang (Trái/Phải)
        transform.Rotate(Vector3.up * mouseX);

        // Tính toán xoay camera theo trục dọc (Lên/Xuống) và giới hạn góc nhìn
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        // 2. DI CHUYỂN BẰNG PHÍM W, A, S, D
        float moveX = Input.GetAxis("Horizontal"); 
        float moveZ = Input.GetAxis("Vertical");   

        // Tính hướng di chuyển ngang
        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        
        // Thực hiện lệnh di chuyển NGANG riêng biệt
        controller.Move(move * moveSpeed * Time.deltaTime);

        // 3. HỆ THỐNG TRỌNG LỰC (GRAVITY) CHUẨN
        // Kiểm tra nếu nhân vật đang chạm đất và đang có xu hướng rơi
        if (controller.isGrounded && velocity.y < 0)
        {
            // Ép nhẹ nhân vật xuống sàn để đi cầu thang/dốc không bị nảy tưng tưng
            velocity.y = -2f; 
        }

        // Cộng dồn gia tốc rơi theo thời gian
        velocity.y += gravity * Time.deltaTime; 
        // Thực hiện lệnh rơi DỌC riêng biệt
        controller.Move(velocity * Time.deltaTime);

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