using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        Debug.Log($"<color=yellow>Player bị trúng đạn! Máu còn: {currentHealth}/{maxHealth}</color>");

        if (currentHealth <= 0)
        {
            Debug.Log("<color=red><b>PLAYER DIED!</b></color>");
            // Thêm xử lý hồi sinh hoặc Thua cuộc ở đây nếu muốn
            currentHealth = maxHealth; 
        }
    }
}