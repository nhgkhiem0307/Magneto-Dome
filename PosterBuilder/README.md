# PosterBuilder — bộ dựng poster *(19/10)*

Thư mục này **nằm ngoài `Assets/`** nên Unity không đụng tới, không vào bản build.

## Kết quả

`NOP_BAI/Poster_MagnetoDome_A1.pdf` (+ bản `.png`) — **A1 dọc 594 × 841 mm**, 4677 × 6622 điểm ảnh
ở 200 dpi. In được cả A0 (phóng lên vẫn đủ nét khi đứng xem cách 1–2 m).

## Gồm những gì

| File | Vai trò |
|---|---|
| `keyart.png` | Ảnh cảnh chiến đấu **render thẳng từ game** (TestScene, model nhân vật thật, hiệu ứng `CombatVFX` thật) — không vẽ tay, không AI tạo ảnh |
| `make_poster.py` | Ghép chữ lên ảnh: logo, khẩu hiệu, 3 điểm nhấn, thông tin học viên. **Sửa chữ thì sửa ở đây** rồi chạy lại |
| `PosterShot.cs` | Script Unity đã dùng để dàn cảnh và render `keyart.png`. **Cố ý để ngoài `Assets/`** |
| `fonts/` | Poppins (logo chữ Anh) + Be Vietnam Pro (mọi chữ có dấu tiếng Việt). Cả hai đều Google Fonts, giấy phép **OFL** |
| `poster_preview.png` | Bản nhỏ để xem nhanh, tự sinh mỗi lần chạy |

## Sửa chữ trên poster

```
py -3 -m pip install pillow        (chỉ lần đầu)
py -3 PosterBuilder/make_poster.py
```

Chạy xong file trong `NOP_BAI/` bị ghi đè bằng bản mới.

⚠️ **Đừng dùng Poppins cho chữ tiếng Việt** — Poppins không có dấu tiếng Việt, chữ "ạ", "ể", "ữ"
hiện thành ô vuông. Mọi dòng có dấu đi qua hàm `font()` (Be Vietnam Pro); chỉ logo
`MAGNETO` / `DOME` dùng `fontP()` (Poppins).

## Render lại ảnh cảnh chiến đấu *(hiếm khi cần)*

1. Chép `PosterShot.cs` vào `Assets/Editor/`.
2. **Tắt Unity**, chạy:
   `Unity.exe -batchmode -projectPath "<thư mục project>" -executeMethod PosterShotTemp.Final -logFile log.txt`
   (bản xem nhanh: `PosterShotTemp.Preview`). Script không lưu scene.
3. Ảnh ra ở `C:/mdtmp/poster/keyart_final.png` → chép đè `PosterBuilder/keyart.png`.
4. **Xoá `PosterShot.cs` khỏi `Assets/Editor/`** (kèm file `.meta`) — để lại thì nó nằm trong project nộp.
