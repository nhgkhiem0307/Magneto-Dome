using UnityEngine;

public class FPSMovement : MonoBehaviour
{
    [Header("Keybinds")]
    public KeyCode dashKey = KeyCode.Q; // Dễ dàng đổi phím Dash trên Inspector (Q, LeftShift, E, Mouse0, v.v.)

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;
    public Transform cameraTransform;

    [Header("Dash Settings")]
    public float dashForce = 50f;      // Độ mạnh cú lướt
    public float dashCooldown = 1f;    // Thời gian hồi chiêu Dash (giây)
    private float dashTimer = 0f;

    [Header("Gravity & Physics")]
    public float gravity = -19.62f;    // Trọng lực
    public float mass = 3f;
    public float drag = 5f;             // Ma sát giảm tốc khi Dash

    private CharacterController controller;
    private Vector3 velocity;          
    private Vector3 impact = Vector3.zero; 
    private float verticalRotation = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 1. XOAY CAMERA THEO CHUỘT
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        // 2. DI CHUYỂN WASD
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        Vector3 inputDir = new Vector3(moveX, 0f, moveZ);
        inputDir = Vector3.ClampMagnitude(inputDir, 1f);

        Vector3 moveDirection = transform.right * inputDir.x + transform.forward * inputDir.z;

        // 3. COOLDOWN DASH & LỆNH DASH (DÙNG BIẾN dashKey)
        if (dashTimer > 0) dashTimer -= Time.deltaTime;

        if (Input.GetKeyDown(dashKey) && dashTimer <= 0) // <-- Đã dùng dashKey thay vì hardcode Q
        {
            Vector3 dashDirection = moveDirection.normalized;

            if (dashDirection == Vector3.zero)
            {
                dashDirection = transform.forward;
            }

            AddImpact(dashDirection, dashForce);
            dashTimer = dashCooldown;
        }

        // 4. TRỌNG LỰC
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; 
        }
        velocity.y += gravity * Time.deltaTime;

        // 5. GIẢM TỐC QUÁN TÍNH (IMPACT)
        if (impact.sqrMagnitude > 0.04f)
        {
            impact = Vector3.Lerp(impact, Vector3.zero, drag * Time.deltaTime);
        }
        else
        {
            impact = Vector3.zero;
        }

        // 6. GỘP LỰC VÀ DI CHUYỂN (1 LẦN MOVE/FRAME)
        Vector3 finalMovement = (moveDirection * moveSpeed) + velocity + impact;
        controller.Move(finalMovement * Time.deltaTime);
    }

    public void AddImpact(Vector3 dir, float force)
    {
        dir.Normalize();
        if (dir.y < 0) dir.y = -dir.y; 
        impact += dir * (force / mass);
    }
}