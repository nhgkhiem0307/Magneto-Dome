using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerMagnetController : MonoBehaviour
{
    [Header("References")]
    public Camera cam;
    public Transform holdPoint; 

    [Header("Magnet Settings")]
    public float shootRange = 100f;
    public float pullForce = 40f;
    public float pushForce = 300f;

    [Header("Juggling / Toss Settings (NEW)")]
    public KeyCode tossKey = KeyCode.V;
    public float tossUpForce = 0.5f;
    public float tossForwardForce = 0.2f;

    [Header("Melee & Ability Settings")]
    public float meleeRange = 3f;
    public float dashLockRange = 8f; 
    public float meleePushForce = 40f;
    public float meleeCooldown = 1f;

    [Header("Current State")]
    public MagneticObject.Polarity currentGlovePolarity = MagneticObject.Polarity.Positive; 

    private MagneticObject grabbedObject;
    private Rigidbody grabbedRb;
    private float originalBaseDamage;
    private float nextMeleeTime = 0f;
    private bool isDashingToEnemy = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            currentGlovePolarity = MagneticObject.Polarity.Positive;
            Debug.Log("<color=red>Găng tay chuyển sang trạng thái DƯƠNG (+)</color>");
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            currentGlovePolarity = MagneticObject.Polarity.Negative;
            Debug.Log("<color=blue>Găng tay chuyển sang trạng thái ÂM (-)</color>");
        }

        if (isDashingToEnemy) return;

        // TRẠNG THÁI 1: NẾU ĐANG CÓ ĐỒ TRÊN TAY
        if (grabbedObject != null)
        {
            KeepObjectInHand();

            // Nhấn V -> Tung hứng
            if (Input.GetKeyDown(tossKey))
            {
                TossObjectUp();
                return;
            }

            // Click Chuột Phải -> Bắn
            if (Input.GetMouseButtonDown(1))
            {
                FireGrabbedObject();
                return;
            }

            return;
        }

        // TRẠNG THÁI 2: NẾU TAY ĐANG TRỐNG -> Cho phép Hút hoặc Cận chiến
        if (Input.GetMouseButton(0))
        {
            HandleLeftClickMagnet();
        }

        if (Input.GetMouseButtonDown(1) && Time.time >= nextMeleeTime)
        {
            HandleRightClickMelee();
        }
    }

    void HandleLeftClickMagnet()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, shootRange))
        {
            if (hit.collider.CompareTag("Magnetic"))
            {
                MagneticObject magObj = hit.collider.GetComponent<MagneticObject>();
                if (magObj == null) return;

                Rigidbody targetRb = hit.collider.GetComponent<Rigidbody>();

                if (magObj.currentPolarity == MagneticObject.Polarity.None && Input.GetMouseButtonDown(0))
                {
                    magObj.SetPolarity(currentGlovePolarity);
                }
                else if (magObj.currentPolarity == currentGlovePolarity && Input.GetMouseButtonDown(0))
                {
                    magObj.isMovingAsBullet = true;
                    magObj.shooterOwner = this; 

                    Vector3 pushDirection = cam.transform.forward;
                    pushDirection.y = 0f; 
                    pushDirection.Normalize();

                    targetRb.linearVelocity = Vector3.zero;
                    targetRb.AddForce(pushDirection * pushForce, ForceMode.Impulse);

                    if (magObj.objectType == MagneticObject.ObjectType.Heavy)
                    {
                        magObj.baseDamage *= 0.5f; 
                    }
                }
                else if (magObj.currentPolarity != currentGlovePolarity)
                {
                    if (magObj.objectType == MagneticObject.ObjectType.Spike && magObj.isMovingAsBullet)
                    {
                        targetRb.AddForce(-cam.transform.forward * pullForce * 1.5f, ForceMode.Force);
                        magObj.baseDamage *= 2f; 
                        return;
                    }

                    if (magObj.objectType == MagneticObject.ObjectType.Heavy && magObj.isMovingAsBullet)
                    {
                        targetRb.AddForce(-cam.transform.forward * pullForce, ForceMode.Force);
                        magObj.baseDamage *= 0.5f; 
                        return;
                    }

                    // KÉO VẬT THỂ MƯỢT MÀ VỀ TAY (Không Teleport)
                    targetRb.isKinematic = false;
                    targetRb.useGravity = false;
                    
                    Vector3 pullDirection = (holdPoint.position - magObj.transform.position).normalized;
                    targetRb.linearVelocity = pullDirection * pullForce;

                    // Chỉ khi nào bay đến sát tay (< 0.5 mét) mới khóa cứng lại
                    if (Vector3.Distance(magObj.transform.position, holdPoint.position) < 0.7f)
                    {
                        ForceGrabObject(magObj);
                    }
                }
            }
        }
    }

    // Các hàm HandleRightClickMelee, DashToEnemyRoutine, ApplyKnockback giữ nguyên như cũ
    void HandleRightClickMelee()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, dashLockRange))
        {
            if (hit.collider.CompareTag("Player"))
            {
                MagneticObject.Polarity targetPolarity = MagneticObject.Polarity.None;
                
                PlayerMagnetController enemyGlove = hit.collider.GetComponent<PlayerMagnetController>();
                DummyMagnetTarget dummyTarget = hit.collider.GetComponent<DummyMagnetTarget>();

                if (enemyGlove != null) targetPolarity = enemyGlove.currentGlovePolarity;
                else if (dummyTarget != null) targetPolarity = dummyTarget.currentGlovePolarity;
                else return;

                float distance = Vector3.Distance(transform.position, hit.transform.position);

                if (distance > meleeRange && currentGlovePolarity != targetPolarity)
                {
                    StartCoroutine(DashToEnemyRoutine(hit.transform.position));
                    nextMeleeTime = Time.time + meleeCooldown; 
                }
                else if (distance <= meleeRange)
                {
                    Vector3 pushDirection = (hit.transform.position - transform.position).normalized;
                    pushDirection.y = 0.3f; // Hất tung nhẹ

                    if (currentGlovePolarity == targetPolarity)
                    {
                        Debug.Log("<color=red>CÙNG DẤU! Đấm văng đối thủ!</color>");
                        
                        // GỌI HÀM ADDIMPACT TRỰC TIẾP (KHÔNG DÙNG COROUTINE NỮA)
                        FPSMovement enemyMove = hit.collider.GetComponent<FPSMovement>();
                        DummyGravity dummyGrav = hit.collider.GetComponent<DummyGravity>();

                        if (enemyMove != null) enemyMove.AddImpact(pushDirection, meleePushForce);
                        if (dummyGrav != null) dummyGrav.AddImpact(pushDirection, meleePushForce);
                    }
                    else
                    {
                        Debug.Log("<color=yellow>KHÁC DẤU! Nảy bật nhẹ!</color>");
                        
                        FPSMovement myMove = GetComponent<FPSMovement>();
                        FPSMovement enemyMove = hit.collider.GetComponent<FPSMovement>();
                        DummyGravity dummyGrav = hit.collider.GetComponent<DummyGravity>();

                        if (enemyMove != null) enemyMove.AddImpact(pushDirection, 15f);
                        if (dummyGrav != null) dummyGrav.AddImpact(pushDirection, 15f);
                        if (myMove != null) myMove.AddImpact(-pushDirection, 15f);
                    }

                    nextMeleeTime = Time.time + meleeCooldown; 
                }
            }
        }
    }

    IEnumerator DashToEnemyRoutine(Vector3 targetPos)
    {
        isDashingToEnemy = true;
        CharacterController controller = GetComponent<CharacterController>();
        float duration = 0.4f; 
        float elapsed = 0f;

        Vector3 startPos = transform.position;
        Vector3 finalPos = targetPos - (targetPos - startPos).normalized * 2f; 

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Vector3 currentPos = Vector3.Lerp(startPos, finalPos, elapsed / duration);
            
            if (controller != null)
            {
                controller.Move(currentPos - transform.position);
            }
            yield return null;
        }

        isDashingToEnemy = false;
    }

    IEnumerator ApplyKnockback(CharacterController target, Vector3 force)
    {
        float duration = 0.3f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            target.Move(force * (1f - (elapsed / duration)) * Time.deltaTime);
            yield return null;
        }
    }

    void KeepObjectInHand()
    {
        grabbedRb.isKinematic = true;
        // grabbedRb.linearVelocity = Vector3.zero;
        // grabbedRb.angularVelocity = Vector3.zero;

        grabbedObject.transform.position = holdPoint.position;
        grabbedObject.transform.rotation = cam.transform.rotation;
    }

    void TossObjectUp()
    {
        MagneticObject objToToss = grabbedObject;
        Rigidbody rbToToss = grabbedRb;

        // Bật lại va chạm trước khi rời tay
        Collider playerCol = GetComponent<Collider>();
        Collider objCol = objToToss.GetComponent<Collider>();
        if (playerCol != null && objCol != null) Physics.IgnoreCollision(playerCol, objCol, false);

        // Giải phóng găng tay
        grabbedObject = null;
        grabbedRb = null;

        // Trả lại vật lý
        rbToToss.isKinematic = false;
        rbToToss.useGravity = true;
        rbToToss.linearDamping = 0.05f;

        // Hất lên và xoay nhẹ
        Vector3 tossDirection = Vector3.up * tossUpForce + cam.transform.forward * tossForwardForce;
        rbToToss.AddForce(tossDirection, ForceMode.Impulse);
        rbToToss.AddTorque(Random.insideUnitSphere * 1f, ForceMode.Impulse);
    }

    void FireGrabbedObject()
    {
        grabbedObject.isMovingAsBullet = true;
        grabbedObject.shooterOwner = this; 
        grabbedObject.baseDamage = originalBaseDamage;
        
        // Bật lại va chạm trước khi bắn
        Collider playerCol = GetComponent<Collider>();
        Collider objCol = grabbedObject.GetComponent<Collider>();
        if (playerCol != null && objCol != null) Physics.IgnoreCollision(playerCol, objCol, false);

        grabbedRb.isKinematic = false;
        grabbedRb.useGravity = true;
        grabbedRb.linearDamping = 0f;

        Vector3 fireDirection = cam.transform.forward;
        fireDirection.y = 0.05f; 
        fireDirection.Normalize();

        grabbedRb.AddForce(fireDirection * pushForce, ForceMode.Impulse);
        
        grabbedObject = null;
        grabbedRb = null;
    }

    void ReleaseGrabbedObject()
    {
        if (grabbedObject != null)
        {
            Collider playerCol = GetComponent<Collider>();
            Collider objCol = grabbedObject.GetComponent<Collider>();
            if (playerCol != null && objCol != null) Physics.IgnoreCollision(playerCol, objCol, false);

            grabbedObject.ResetBulletState();
        }
        
        if (grabbedRb != null)
        {
            grabbedRb.isKinematic = false;
            grabbedRb.useGravity = true;
            grabbedRb.linearDamping = 0f;
        }
        grabbedObject = null;
        grabbedRb = null;
    }

    public void ForceGrabObject(MagneticObject targetObj)
    {
        if (grabbedObject != null)
        {
            ReleaseGrabbedObject();
        }

        Rigidbody targetRb = targetObj.GetComponent<Rigidbody>();
        if (targetRb == null) return;

        grabbedObject = targetObj;
        grabbedRb = targetRb;
        originalBaseDamage = targetObj.baseDamage;

        grabbedObject.transform.position = holdPoint.position;
        grabbedObject.transform.rotation = cam.transform.rotation;

        grabbedRb.isKinematic = true; 
        grabbedRb.useGravity = false;
        grabbedRb.linearDamping = 10f; 

        // TẮT VA CHẠM: Để vật thể không đè lên người chơi
        Collider playerCol = GetComponent<Collider>();
        Collider objCol = targetObj.GetComponent<Collider>();
        if (playerCol != null && objCol != null) Physics.IgnoreCollision(playerCol, objCol, true);
    }

    // --- CÁC HÀM GETTER ĐỂ INVENTORY GỌI ---
    public MagneticObject GetGrabbedObject()
    {
        return grabbedObject;
    }

    public void ClearGrabbedObjectWithoutReset()
    {
        grabbedObject = null;
        grabbedRb = null;
    }
}