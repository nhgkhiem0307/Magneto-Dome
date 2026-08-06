using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Một ô hàng trong cửa hàng: icon, tên, giá, và nút bấm mua.
/// Gắn lên mỗi nút trong panel Shop.
/// </summary>
[RequireComponent(typeof(Button))]
public class ShopItemButton : MonoBehaviour
{
    [Header("Tham chiếu con")]
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text priceText;

    private int _itemIndex = -1;
    private ShopUI _shopUI;
    private Button _button;

    void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);
    }

    /// <summary>ShopUI gọi vào lúc mở cửa hàng để điền nội dung cho ô này.</summary>
    public void Setup(int itemIndex, ItemData item, ShopUI shopUI)
    {
        _itemIndex = itemIndex;
        _shopUI = shopUI;

        if (nameText != null) nameText.text = item.itemName;
        if (priceText != null) priceText.text = $"${item.price}";

        if (iconImage != null)
        {
            iconImage.sprite = item.itemIcon;

            // Chưa gán icon thì ẩn ô ảnh đi, đỡ hiện ô trắng xấu
            iconImage.enabled = item.itemIcon != null;
        }
    }

    private void OnClick()
    {
        if (_shopUI == null || _itemIndex < 0) return;

        _shopUI.OnClickBuy(_itemIndex);
    }
}
