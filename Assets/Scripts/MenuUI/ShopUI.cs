using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Giao diện cửa hàng. Đặt trên Canvas trong scene gameplay, KHÔNG đặt trên nhân vật.
///
/// Đây là MonoBehaviour thường, không cần NetworkObject: mọi thứ ở đây đều thuần cục bộ.
/// Việc mua thật sự do ShopManager trên nhân vật lo, qua RPC gửi lên Host.
///
/// Cùng nguyên tắc đã dùng cho RoundBarrier: chỉ đồng bộ thứ bắt buộc.
/// </summary>
public class ShopUI : MonoBehaviour
{
    [Header("Tham chiếu UI")]
    [Tooltip("Panel chứa toàn bộ cửa hàng. Sẽ tự bật/tắt.")]
    public GameObject shopPanel;

    [Tooltip("Dòng chữ hiện số tiền đang có.")]
    public TMP_Text moneyText;

    [Tooltip("Dòng chữ báo còn bao nhiêu giây được mua.")]
    public TMP_Text timerText;

    [Header("Nút mua hàng")]
    [Tooltip("Kéo 5 nút vào đây theo ĐÚNG thứ tự trong Catalogue của ShopManager.")]
    public ShopItemButton[] itemButtons;

    [Header("Phím tắt")]
    public KeyCode shopKey = KeyCode.B;

    private bool _isOpen;

    void Start()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
    }

    // Settings mở ra thì Shop tự đóng - chỉ một bảng trên màn hình tại một thời điểm.
    // Đăng ký ở OnEnable/OnDisable chứ không ở Start, để object bị huỷ lúc đổi scene
    // thì tự huỷ đăng ký theo, không để lại lời gọi tới một object đã chết.
    void OnEnable() { SettingsUI.Opened += OnSettingsOpened; }
    void OnDisable() { SettingsUI.Opened -= OnSettingsOpened; }

    private void OnSettingsOpened()
    {
        if (_isOpen) CloseShop();
    }

    void Update()
    {
        ShopManager shop = GetLocalShop();

        // Chưa vào trận thì không làm gì
        if (shop == null)
        {
            if (_isOpen) CloseShop();
            return;
        }

        bool inBuyPhase = GameManager.Instance != null
                          && GameManager.Instance.Phase == GameManager.GamePhase.BuyPhase;

        // Hết pha chuẩn bị là tự đóng, không cho mua nữa
        if (_isOpen && !inBuyPhase)
        {
            CloseShop();
            return;
        }

        if (Input.GetKeyDown(shopKey) && inBuyPhase)
        {
            if (_isOpen) CloseShop();

            // Đang có bảng khác mở (Settings, Radial Menu) thì không mở đè lên.
            // Chuột đang được thả tự do nghĩa là có bảng nào đó đang chiếm nó - cùng
            // cách Radial Menu dùng, nên thêm bảng mới sau này cũng tự được tính vào.
            else if (!SettingsUI.IsOpen && !NetworkRunnerHandler.IsCursorFree()) OpenShop(shop);
        }

        if (_isOpen) RefreshTexts(shop);
    }

    private void OpenShop(ShopManager shop)
    {
        _isOpen = true;
        if (shopPanel != null) shopPanel.SetActive(true);

        // Xin thả chuột ra mới bấm nút được. Đăng ký qua CursorLock thay vì tự ghi,
        // để đóng bảng khác không cướp mất chuột của mình.
        CursorLock.Request(this);

        SetupButtons(shop);
    }

    private void CloseShop()
    {
        _isOpen = false;
        if (shopPanel != null) shopPanel.SetActive(false);

        CursorLock.Release(this);
    }

    // Điền tên, giá, icon lên từng nút. Chỉ chạy lúc mở cửa hàng, không phải mỗi khung hình.
    private void SetupButtons(ShopManager shop)
    {
        if (itemButtons == null) return;

        for (int i = 0; i < itemButtons.Length; i++)
        {
            ShopItemButton button = itemButtons[i];
            if (button == null) continue;

            ItemData item = shop.GetItem(i);

            if (item == null)
            {
                button.gameObject.SetActive(false);
                continue;
            }

            button.gameObject.SetActive(true);
            button.Setup(i, item, this);
        }
    }

    /// <summary>Nút mua gọi vào đây.</summary>
    public void OnClickBuy(int itemIndex)
    {
        ShopManager shop = GetLocalShop();
        if (shop == null) return;

        shop.RequestBuy(itemIndex);
    }

    private void RefreshTexts(ShopManager shop)
    {
        if (moneyText != null)
        {
            PlayerEconomy economy = shop.GetComponent<PlayerEconomy>();
            if (economy != null) moneyText.text = $"${economy.Money}";
        }

        if (timerText != null && GameManager.Instance != null)
        {
            float? remaining = GameManager.Instance.PhaseTimer.RemainingTime(GameManager.Instance.Runner);
            timerText.text = remaining.HasValue ? $"{Mathf.CeilToInt(remaining.Value)}s" : "";
        }
    }

    // Lấy ShopManager của chính nhân vật mình. FPSMovement.Local đã giữ sẵn tham chiếu đó.
    private ShopManager GetLocalShop()
    {
        if (FPSMovement.Local == null) return null;
        return FPSMovement.Local.GetComponent<ShopManager>();
    }
}
