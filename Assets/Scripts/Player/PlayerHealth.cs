using Fusion;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    public float maxHealth = 100f;

    // Máu phải là [Networked] để mọi máy cùng thấy một con số.
    // Trước đây nó là biến private thường, nên mỗi máy tự tính một kiểu
    // và người chơi có thể sửa máu của mình mà Host không biết.
    //
    // OnChangedRender: Fusion tự gọi hàm OnHealthChanged mỗi khi con số này thay đổi,
    // và gọi trên MỌI máy - cả người bắn lẫn người trúng. Sau này thanh máu trên HUD
    // cũng sẽ móc vào đây thay vì phải kiểm tra mỗi khung hình.
    [Networked, OnChangedRender(nameof(OnHealthChanged))]
    public float CurrentHealth { get; set; }

    public override void Spawned()
    {
        // Chỉ Host được đặt giá trị ban đầu. Client sẽ tự nhận về qua mạng.
        if (HasStateAuthority)
        {
            CurrentHealth = maxHealth;
        }
    }

    // Chạy trên MỌI máy mỗi khi CurrentHealth đổi giá trị.
    // Nhờ vậy mở Console ở cả 2 cửa sổ ParrelSync là đối chiếu được ngay:
    // nếu 2 bên ra cùng một con số thì máu đã đồng bộ đúng.
    private void OnHealthChanged()
    {
        // HasInputAuthority = true nghĩa là nhân vật này của người đang ngồi trước máy.
        string who = HasInputAuthority
            ? "<color=lime>MÁU CỦA BẠN</color>"
            : $"<color=orange>MÁU ĐỐI PHƯƠNG (Player {Object.InputAuthority})</color>";

        Debug.Log($"[MÁU] {who}: {CurrentHealth} / {maxHealth}");
    }

    public void TakeDamage(float amount)
    {
        // Chỉ Host mới được trừ máu (Host Mode - server authoritative).
        //
        // Hàm này đang được MagneticObject gọi vào, mà MagneticObject vẫn là MonoBehaviour
        // chạy trên MỌI máy. Nếu không chặn ở đây thì mỗi máy sẽ trừ máu một lần,
        // dẫn tới mất máu gấp nhiều lần thực tế.
        if (!HasStateAuthority) return;

        CurrentHealth -= amount;

        // Chỉ in lượng sát thương ở đây. Con số máu còn lại do OnHealthChanged in ra,
        // vì hàm đó chạy trên mọi máy còn hàm này chỉ chạy trên Host.
        Debug.Log($"<color=yellow>[SÁT THƯƠNG] Player {Object.InputAuthority} trúng đòn -{amount}</color>");

        if (CurrentHealth <= 0f)
        {
            Debug.Log("<color=red><b>PLAYER DIED!</b></color>");

            // TẠM THỜI: hồi đầy máu như bản cũ để còn test tiếp được.
            // Theo thiết kế, chết là bị LOẠI khỏi round và chỉ hồi sinh khi round sau bắt đầu.
            // Phần đó sẽ do GameManager xử lý khi làm hệ thống vòng đấu.
            CurrentHealth = maxHealth;
        }
    }
}
