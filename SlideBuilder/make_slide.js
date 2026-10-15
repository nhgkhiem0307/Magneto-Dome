// Dựng bộ slide bảo vệ đồ án Magneto-Dome theo nhận diện VTC Academy.
// Nền trắng + thanh tiêu đề xanh lấy thẳng từ slide master của trường (image1.png).

const pptxgen = require("pptxgenjs");
const pres = new pptxgen();

pres.layout = "LAYOUT_16x9"; // 10 x 5.625 inch — đúng cỡ file mẫu của trường
pres.author = "Nguyen Hoang Gia Khiem";
pres.title = "Magneto-Dome - Bao cao do an tot nghiep";

// ---------- BẢNG MÀU ----------
const NAVY = "26407D"; // xanh thanh tiêu đề
const DARK = "002060"; // xanh đậm cho chữ tiêu đề mục
const WHITE = "FFFFFF";
const INK = "1A1A1A";
const MUTED = "5A6478";
const TINT = "EFF3FA"; // nền thẻ
const LINE = "C7D2E6";
const RED = "E8453C"; // cực dương
const BLUE = "2E86DE"; // cực âm
const F = "Arial";

// ---------- HÌNH HỌC (đo từ nền của trường) ----------
const BAR_Y = 0.376,
  BAR_H = 0.368;
const TOP = 0.95; // mép trên vùng nội dung
const BOT = 5.1; // mép dưới vùng nội dung
const L = 0.45,
  R = 9.55;
const W = R - L;

// ---------- LAYOUT ----------
pres.defineSlideMaster({
  title: "VTC_TITLE",
  background: { path: "bg_navy.png" },
  objects: [{ image: { path: "image21.png", x: 0.45, y: 0.3, w: 2.3, h: 0.42 } }],
});

pres.defineSlideMaster({
  title: "VTC_CONTENT",
  background: { path: "image1.png" },
  slideNumber: { x: 9.25, y: 5.28, w: 0.5, h: 0.25, fontFace: F, fontSize: 9, color: MUTED, align: "right" },
});

pres.defineSlideMaster({
  title: "VTC_CLOSING",
  background: { path: "bg_navy.png" },
  objects: [{ image: { path: "image21.png", x: 0.45, y: 0.3, w: 2.3, h: 0.42 } }],
});

// ---------- TIỆN ÍCH ----------
let secNo = 0;

// Tiêu đề nằm trong thanh xanh
function head(slide, text) {
  slide.addText(text.toUpperCase(), {
    x: 0.3, y: BAR_Y, w: 7.0, h: BAR_H,
    fontFace: F, fontSize: 17, bold: true, color: WHITE,
    valign: "middle", align: "left", margin: 0, isTextBox: true,
  });
}

// Dòng dẫn đề ngay dưới thanh xanh
function lead(slide, text, y) {
  slide.addText(text, {
    x: L, y: y === undefined ? TOP : y, w: W, h: 0.42,
    fontFace: F, fontSize: 13.5, bold: true, color: DARK,
    valign: "middle", margin: 0, isTextBox: true,
  });
}

// Thẻ nền nhạt
function card(slide, o) {
  slide.addShape(pres.ShapeType.roundRect, {
    x: o.x, y: o.y, w: o.w, h: o.h, rectRadius: 0.06,
    fill: { color: o.fill || TINT },
    line: { color: o.line || LINE, width: 1 },
    objectName: o.name || "card",
  });
}

// Khung chờ ảnh: viền đứt + chú thích bên trong
function picBox(slide, o) {
  slide.addShape(pres.ShapeType.roundRect, {
    x: o.x, y: o.y, w: o.w, h: o.h, rectRadius: 0.06,
    fill: { color: "F7F9FD" },
    line: { color: NAVY, width: 1.25, dashType: "dash" },
    objectName: "khung-anh",
  });
  slide.addText(
    [
      { text: "KHUNG ẢNH", options: { fontSize: 11, bold: true, color: NAVY, breakLine: true } },
      { text: o.caption, options: { fontSize: 11, color: MUTED } },
    ],
    {
      x: o.x + 0.12, y: o.y, w: o.w - 0.24, h: o.h,
      fontFace: F, align: "center", valign: "middle", margin: 0, isTextBox: true,
    }
  );
}

// Đặt ẢNH THẬT, GIỮ ĐÚNG TỈ LỆ GỐC.
//
// ⚠️ KHÔNG dùng tuỳ chọn "sizing" của pptxgenjs — đã thử và nó KHÔNG có tác dụng:
// khung ảnh vẫn đúng bằng w/h truyền vào, nên ảnh bị kéo méo. Phải tự tính lấy.
//
// Cách tính: thu ảnh sao cho vừa HẲN trong ô (contain), rồi căn giữa ô.
// o.ar là tỉ lệ ngang chia dọc của ảnh gốc.
//
// ⚠️ Ảnh phải để trong thư mục img/ và nhúng đường dẫn ở đây, KHÔNG chèn tay vào
// file .pptx — chạy lại script là ảnh chèn tay mất sạch. Xem README.
function picImg(slide, o) {
  let w = o.w, h = w / o.ar;
  if (h > o.h) { h = o.h; w = h * o.ar; }
  slide.addImage({
    path: o.path,
    x: o.x + (o.w - w) / 2,
    y: o.y + (o.h - h) / 2,
    w: w, h: h,
  });
}

// Chấm tròn màu + chữ, dùng làm mô-típ lặp lại
function dot(slide, x, y, color, label) {
  slide.addShape(pres.ShapeType.ellipse, {
    x: x, y: y, w: 0.22, h: 0.22, fill: { color: color }, line: { color: color, width: 0 },
  });
  if (label) {
    slide.addText(label, {
      x: x + 0.3, y: y - 0.04, w: 3.4, h: 0.3,
      fontFace: F, fontSize: 12, bold: true, color: INK, valign: "middle", margin: 0, isTextBox: true,
    });
  }
}

// Ô số liệu lớn
function stat(slide, o) {
  card(slide, { x: o.x, y: o.y, w: o.w, h: o.h, fill: o.fill || TINT, line: o.line });
  slide.addText(o.value, {
    x: o.x, y: o.y + 0.1, w: o.w, h: 0.62,
    fontFace: F, fontSize: o.size || 30, bold: true, color: o.color || DARK,
    align: "center", valign: "middle", margin: 0, isTextBox: true,
  });
  slide.addText(o.label, {
    x: o.x + 0.08, y: o.y + 0.72, w: o.w - 0.16, h: o.h - 0.8,
    fontFace: F, fontSize: 11, color: MUTED, align: "center", valign: "top", margin: 0, isTextBox: true,
  });
}

function bullets(slide, items, o) {
  const runs = items.map(function (t, i) {
    return { text: t, options: { bullet: true, breakLine: i < items.length - 1 } };
  });
  slide.addText(runs, {
    x: o.x, y: o.y, w: o.w, h: o.h,
    fontFace: F, fontSize: o.size || 13.5, color: o.color || INK,
    paraSpaceAfter: 7, valign: "top", margin: 0, isTextBox: true, lineSpacingMultiple: 1.05,
  });
}

function tbl(slide, rows, o) {
  slide.addTable(rows, {
    x: o.x, y: o.y, w: o.w, colW: o.colW,
    fontFace: F, fontSize: o.size || 12, color: INK,
    border: { type: "solid", color: LINE, pt: 1 },
    align: "left", valign: "middle",
    rowH: o.rowH || 0.33,
    margin: [3, 7, 3, 7],
  });
}

function hdrRow(cells) {
  return cells.map(function (c) {
    return { text: c, options: { bold: true, color: WHITE, fill: { color: NAVY } } };
  });
}

// =====================================================================
// 1. BÌA
// =====================================================================
pres.addSection({ title: "Mo dau" });
let s = pres.addSlide({ masterName: "VTC_TITLE", sectionTitle: "Mo dau" });

s.addText("BÁO CÁO ĐỒ ÁN TỐT NGHIỆP", {
  x: 0.45, y: 1.15, w: 6.6, h: 0.3,
  fontFace: F, fontSize: 14, bold: true, color: "BFD0EA", charSpacing: 2, margin: 0, isTextBox: true,
});
s.addText("MAGNETO-DOME", {
  x: 0.45, y: 1.45, w: 6.6, h: 0.78,
  fontFace: F, fontSize: 44, bold: true, color: WHITE, margin: 0, isTextBox: true,
});
s.addText("GAME FPS ĐỐI KHÁNG 2V2 TRÊN NỀN UNITY VÀ PHOTON FUSION 2", {
  x: 0.45, y: 2.26, w: 5.85, h: 0.62,
  fontFace: F, fontSize: 15, bold: true, color: "DCE6F7", margin: 0, isTextBox: true,
});
// hai chấm cực từ — mô-típ của cả bộ slide
s.addShape(pres.ShapeType.ellipse, { x: 0.47, y: 2.98, w: 0.16, h: 0.16, fill: { color: RED }, line: { width: 0 } });
s.addShape(pres.ShapeType.ellipse, { x: 0.71, y: 2.98, w: 0.16, h: 0.16, fill: { color: BLUE }, line: { width: 0 } });

s.addText(
  [
    { text: "Học viên thực hiện: ", options: { color: "BFD0EA" } },
    { text: "Nguyễn Hoàng Gia Khiêm", options: { bold: true, breakLine: true } },
    { text: "Giảng viên hướng dẫn: ", options: { color: "BFD0EA" } },
    { text: "Phạm Thị Hằng", options: { bold: true, breakLine: true } },
    { text: "Lớp: ", options: { color: "BFD0EA" } },
    { text: "K24M-GAME1", options: { bold: true, breakLine: true } },
    { text: "Khóa: ", options: { color: "BFD0EA" } },
    { text: "2024 - 2026", options: { bold: true } },
  ],
  { x: 0.45, y: 3.45, w: 5.2, h: 1.5, fontFace: F, fontSize: 13, color: WHITE, paraSpaceAfter: 4, margin: 0, isTextBox: true }
);

picImg(s, { x: 6.45, y: 1.3, w: 3.1, h: 3.4, path: "img/icon.png", ar: 367 / 355 });
s.addNotes("0:00-0:15 — Chào hội đồng, giới thiệu tên đề tài và bản thân. Không đọc lại slide.");

// =====================================================================
// 2. NỘI DUNG
// =====================================================================
s = pres.addSlide({ masterName: "VTC_CONTENT", sectionTitle: "Mo dau" });
head(s, "Nội dung");

const agenda = [
  "GIỚI THIỆU ĐỀ TÀI", "MỤC TIÊU", "TECH STACK",
  "PHÂN TÍCH & THIẾT KẾ", "QUY TRÌNH PHÁT TRIỂN", "DEMO TRỰC TIẾP",
  "KẾT QUẢ ĐẠT ĐƯỢC", "HƯỚNG PHÁT TRIỂN", "KẾT LUẬN",
];
const aw = 2.95, ah = 1.12, gx = 0.26, gy = 0.2;
for (let i = 0; i < 9; i++) {
  const cx = L + (i % 3) * (aw + gx);
  const cy = 1.18 + Math.floor(i / 3) * (ah + gy);
  card(s, { x: cx, y: cy, w: aw, h: ah, name: "muc" + (i + 1) });
  s.addText(String(i + 1).padStart(2, "0"), {
    x: cx + 0.14, y: cy + 0.08, w: 0.6, h: 0.32,
    fontFace: F, fontSize: 15, bold: true, color: i % 2 === 0 ? RED : BLUE, margin: 0, isTextBox: true,
  });
  s.addText(agenda[i], {
    x: cx + 0.14, y: cy + 0.42, w: aw - 0.28, h: 0.58,
    fontFace: F, fontSize: 13, bold: true, color: DARK, valign: "top", margin: 0, isTextBox: true,
  });
}
s.addNotes("0:15-0:35 — Đọc lướt 9 mục, dừng lại ở mục 4 và 6 vì đó là hai phần chính.");

// =====================================================================
// 3. §1 GIỚI THIỆU ĐỀ TÀI
// =====================================================================
secNo = 1;
pres.addSection({ title: "1. Gioi thieu de tai" });
s = pres.addSlide({ masterName: "VTC_CONTENT", sectionTitle: "1. Gioi thieu de tai" });
head(s, "1. Giới thiệu đề tài");

s.addText(
  [
    { text: "Magneto-Dome", options: { bold: true, color: DARK } },
    { text: " là game bắn súng góc nhìn thứ nhất 2 đấu 2 theo vòng, diễn ra trên một hòn đảo lơ lửng. Người chơi " },
    { text: "không có súng", options: { bold: true, color: RED } },
    { text: " — họ dùng găng tay từ tính nạp điện cho đồ vật trong đấu trường rồi bắn chính những đồ vật đó." },
  ],
  { x: L, y: TOP, w: W, h: 0.72, fontFace: F, fontSize: 14, color: INK, valign: "top", margin: 0, isTextBox: true }
);

const usp = [
  ["KHÔNG SÚNG ĐẠN", "Vũ khí là bàn, ghế, cây, đá có sẵn trong map. Hết đồ quanh mình là hết đạn.", RED],
  ["KHÔNG THANH MÁU", "Trúng đòn không mất máu mà bị nạp điện. Càng nhiễm điện càng bị hất xa.", BLUE],
  ["CHẾT DO VỊ TRÍ", "Cách chết duy nhất là rơi khỏi đảo. Đứng ở đâu quan trọng hơn còn bao nhiêu máu.", DARK],
];
// BỐ CỤC HAI CỘT: ba thẻ xếp DỌC bên trái, ảnh chiếm hẳn cột phải.
//
// Trước đó ba thẻ nằm ngang và ảnh bị nhét vào một băng rộng 9.1 cao 1.5 — tỉ lệ 6:1,
// trong khi ảnh chụp màn hình là 1.54:1. Giữ đúng tỉ lệ thì ảnh teo còn 2.5 inch nằm
// lọt thỏm giữa khoảng trắng. Xếp dọc thì ảnh được hẳn 4.3 inch bề ngang.
for (let i = 0; i < 3; i++) {
  const cy = 1.80 + i * 1.06;
  card(s, { x: L, y: cy, w: 4.55, h: 0.96 });
  s.addShape(pres.ShapeType.ellipse, { x: L + 0.16, y: cy + 0.15, w: 0.2, h: 0.2, fill: { color: usp[i][2] }, line: { width: 0 } });
  s.addText(usp[i][0], {
    x: L + 0.45, y: cy + 0.08, w: 3.9, h: 0.3,
    fontFace: F, fontSize: 12.5, bold: true, color: DARK, valign: "middle", margin: 0, isTextBox: true,
  });
  s.addText(usp[i][1], {
    x: L + 0.16, y: cy + 0.40, w: 4.25, h: 0.5,
    fontFace: F, fontSize: 11, color: MUTED, valign: "top", margin: 0, isTextBox: true,
  });
}
picImg(s, { x: 5.25, y: 1.80, w: 4.3, h: 3.02, path: "img/shot_dao.png", ar: 1843 / 1195 });
s.addNotes("0:35-1:25 — Nói một câu duy nhất về cái mới: không súng, không thanh máu. Đừng kể cốt truyện.");

// =====================================================================
// 4. §2 MỤC TIÊU
// =====================================================================
pres.addSection({ title: "2. Muc tieu" });
s = pres.addSlide({ masterName: "VTC_CONTENT", sectionTitle: "2. Muc tieu" });
head(s, "2. Mục tiêu");

card(s, { x: L, y: TOP, w: 4.4, h: 2.3 });
s.addText("MỤC TIÊU ĐỀ TÀI", { x: L + 0.2, y: TOP + 0.12, w: 4.0, h: 0.3, fontFace: F, fontSize: 12.5, bold: true, color: DARK, margin: 0, isTextBox: true });
bullets(s, [
  "Xây dựng một game FPS đối kháng 2v2 chơi qua mạng hoàn chỉnh, có thắng thua rõ ràng",
  "Nghiên cứu và áp dụng Photon Fusion 2 ở chế độ Host Mode",
  "Thiết kế một cơ chế chiến đấu thay thế hoàn toàn thanh máu truyền thống",
  "Đồng bộ được vật thể có vật lý qua mạng — phần khó nhất của đề tài",
], { x: L + 0.2, y: TOP + 0.48, w: 4.0, h: 1.7, size: 12 });

card(s, { x: 5.15, y: TOP, w: 4.4, h: 2.3 });
s.addText("PHẠM VI", { x: 5.35, y: TOP + 0.12, w: 4.0, h: 0.3, fontFace: F, fontSize: 12.5, bold: true, color: DARK, margin: 0, isTextBox: true });
bullets(s, [
  "Nền tảng: PC chạy Windows",
  "Một chế độ chơi, một bản đồ, đúng 4 người mỗi trận",
  "Không làm bot máy — mọi người chơi đều là người thật",
  "Không làm tài khoản, không lưu tiến trình giữa các trận",
], { x: 5.35, y: TOP + 0.48, w: 4.0, h: 1.7, size: 12 });

s.addText("CHỈ TIÊU KỸ THUẬT", { x: L, y: 3.45, w: 4.0, h: 0.3, fontFace: F, fontSize: 12.5, bold: true, color: DARK, margin: 0, isTextBox: true });
stat(s, { x: L, y: 3.8, w: 2.2, h: 1.1, value: "60+", label: "khung hình/giây ở 1920x1080", color: DARK });
stat(s, { x: 2.85, y: 3.8, w: 2.2, h: 1.1, value: "<100ms", label: "độ trễ mạng, khu vực asia", size: 22, color: DARK });
stat(s, { x: 5.25, y: 3.8, w: 2.05, h: 1.1, value: "4", label: "người chơi mỗi trận", color: RED });
stat(s, { x: 7.5, y: 3.8, w: 2.05, h: 1.1, value: "2v2", label: "chia hai đội Đỏ - Xanh", color: BLUE });
s.addNotes("1:25-2:00 — CÓ THỂ BỎ SLIDE NÀY nếu đang chậm giờ. Nói nhanh phạm vi để hội đồng không hỏi về bot.");

// =====================================================================
// 5. §3 CÔNG NGHỆ & CÔNG CỤ
// =====================================================================
pres.addSection({ title: "3. Cong nghe va cong cu" });
s = pres.addSlide({ masterName: "VTC_CONTENT", sectionTitle: "3. Cong nghe va cong cu" });
head(s, "3. Tech stack");

const tech = [
  ["Unity 6000.3.17f1", "Engine chính. Bản Unity 6 mới nhất."],
  ["URP 17.3.0", "Luồng dựng hình. Nhẹ, chạy tốt trên máy cấu hình vừa."],
  ["Photon Fusion 2", "Khung mạng. Chạy Host Mode, khu vực máy chủ asia."],
  ["New Input System 1.19", "Đọc phím và chuột, ghép với gói dữ liệu gửi qua mạng."],
  ["ParrelSync", "Nhân bản project để mở 2 client cùng lúc khi thử nghiệm."],
  ["Visual Studio + Git", "Viết mã và quản lý phiên bản."],
];
for (let i = 0; i < 6; i++) {
  const cx = L + (i % 3) * (2.95 + 0.26);
  const cy = TOP + Math.floor(i / 3) * (1.18 + 0.18);
  card(s, { x: cx, y: cy, w: 2.95, h: 1.18 });
  s.addText(tech[i][0], {
    x: cx + 0.16, y: cy + 0.12, w: 2.63, h: 0.34,
    fontFace: F, fontSize: 12.5, bold: true, color: DARK, valign: "middle", margin: 0, isTextBox: true,
  });
  s.addText(tech[i][1], {
    x: cx + 0.16, y: cy + 0.46, w: 2.63, h: 0.62,
    fontFace: F, fontSize: 11, color: MUTED, valign: "top", margin: 0, isTextBox: true,
  });
}
stat(s, { x: L, y: 3.75, w: 3.1, h: 1.05, value: "49", label: "tệp mã nguồn tự viết", color: DARK });
stat(s, { x: 3.7, y: 3.75, w: 3.1, h: 1.05, value: "~20.000", label: "dòng mã tự viết (không tính thư viện)", size: 26, color: DARK });
stat(s, { x: 6.9, y: 3.75, w: 2.65, h: 1.05, value: "1", label: "người thực hiện toàn bộ", color: RED });
s.addNotes("2:00-2:35 — Nhấn mạnh Fusion 2 Host Mode vì phần sau sẽ quay lại. Đọc nhanh phần còn lại.");

// =====================================================================
// 6. §4 CƠ CHẾ CỐT LÕI
// =====================================================================
pres.addSection({ title: "4. Phan tich va thiet ke" });
s = pres.addSlide({ masterName: "VTC_CONTENT", sectionTitle: "4. Phan tich va thiet ke" });
head(s, "4. Phân tích & thiết kế — Magnetic Gloves");

lead(s, "Toàn bộ chiến đấu dựa trên một luật duy nhất: cùng dấu đẩy nhau, trái dấu hút nhau");

s.addShape(pres.ShapeType.ellipse, { x: L, y: 1.52, w: 0.22, h: 0.22, fill: { color: RED }, line: { width: 0 } });
s.addText("CỰC DƯƠNG — phím 1", { x: L + 0.3, y: 1.48, w: 2.4, h: 0.3, fontFace: F, fontSize: 12, bold: true, color: INK, valign: "middle", margin: 0, isTextBox: true });
s.addShape(pres.ShapeType.ellipse, { x: 2.95, y: 1.52, w: 0.22, h: 0.22, fill: { color: BLUE }, line: { width: 0 } });
s.addText("CỰC ÂM — phím 2", { x: 3.25, y: 1.48, w: 2.4, h: 0.3, fontFace: F, fontSize: 12, bold: true, color: INK, valign: "middle", margin: 0, isTextBox: true });

tbl(s, [
  hdrRow(["Tình huống", "Thao tác", "Kết quả"]),
  ["Vật chưa có điện", "Chuột trái", "Nạp điện cùng cực với găng tay"],
  ["Vật CÙNG dấu", "Chuột trái", "ĐẨY — vật bay đi, thành đạn"],
  ["Vật TRÁI dấu", "Chuột trái", "HÚT — vật bay về tay, cầm được"],
  ["Đang cầm vật", "Chuột phải", "Bắn vật đang cầm đi"],
], { x: L, y: 1.92, w: 5.45, colW: [1.6, 1.2, 2.65], rowH: 0.32, size: 11 });

card(s, { x: L, y: 3.85, w: 5.45, h: 1.15 });
s.addText("CẬN CHIẾN — chuột phải khi tay trống", { x: L + 0.16, y: 3.95, w: 5.1, h: 0.26, fontFace: F, fontSize: 11.5, bold: true, color: DARK, margin: 0, isTextBox: true });
bullets(s, [
  "Dưới 3m, cùng dấu: đấm hất văng đối thủ",
  "Dưới 3m, trái dấu: cả hai nảy bật nhẹ ra",
  "Từ 3 đến 8m, trái dấu: kéo áp sát, lướt nhanh về phía đối thủ",
], { x: L + 0.16, y: 4.22, w: 5.1, h: 0.72, size: 10.5, color: MUTED });

picImg(s, { x: 6.2, y: 1.52, w: 3.35, h: 3.48, path: "img/shot_hut.png", ar: 1916 / 1057 });
s.addNotes("2:35-3:35 — Slide nặng nhất về luật chơi. Chỉ vào bảng, nói ba dòng giữa. Không đọc hết bảng.");

// =====================================================================
// 7. §4 HỆ SINH THÁI ĐẠN
// =====================================================================
s = pres.addSlide({ masterName: "VTC_CONTENT", sectionTitle: "4. Phan tich va thiet ke" });
head(s, "4. Phân tích & thiết kế — Ammo Types");

lead(s, "Mỗi loại vật thể có một luật riêng, buộc người chơi phải nhìn màu vật trước khi bấm chuột");

tbl(s, [
  hdrRow(["Loại", "Điện tích nạp", "Luật riêng"]),
  ["Normal", "10", "Hút đẩy linh hoạt, không có ràng buộc gì thêm"],
  ["Heavy", "20", "Đang bay thì KHÔNG hút về tay được, chỉ cản lại: tốc độ x0,5 và sát thương x0,5"],
  ["Spike", "25", "Hút nhầm lúc nó đang bay tới thì chịu gấp đôi điện tích"],
  ["TNT", "35", "Nổ trong bán kính 6m, lực giảm dần theo khoảng cách"],
], { x: L, y: 1.45, w: 9.1, colW: [1.3, 1.5, 6.3], rowH: 0.42, size: 12 });

card(s, { x: L, y: 3.6, w: 9.1, h: 1.35, fill: "FDF2F1", line: "F0C2BE" });
s.addShape(pres.ShapeType.ellipse, { x: L + 0.2, y: 3.78, w: 0.2, h: 0.2, fill: { color: RED }, line: { width: 0 } });
s.addText("HEAVY VÀ SPIKE LÀ MỘT CẶP ĐỐI XỨNG", {
  x: L + 0.5, y: 3.72, w: 6.0, h: 0.3, fontFace: F, fontSize: 12.5, bold: true, color: DARK, valign: "middle", margin: 0, isTextBox: true,
});
s.addText(
  "Cả hai dùng chung một đoạn mã xử lý, nhưng ý nghĩa ngược nhau: Heavy thưởng cho phản xạ hút, Spike phạt đúng phản xạ đó. Người chơi không thể giữ chuột bừa — phải phân biệt màu vật trong khoảng một phần tư giây.",
  { x: L + 0.5, y: 4.04, w: 8.3, h: 0.82, fontFace: F, fontSize: 11.5, color: INK, valign: "top", margin: 0, isTextBox: true }
);
s.addNotes("3:35-4:15 — Nhấn vào cặp Heavy/Spike: đây là chỗ thể hiện tư duy thiết kế, không phải chỉ lập trình.");

// =====================================================================
// 8. §4 CHẾ ĐỘ QUÁ TẢI — slide quan trọng nhất
// =====================================================================
s = pres.addSlide({ masterName: "VTC_CONTENT", sectionTitle: "4. Phan tich va thiet ke" });
head(s, "4. Phân tích & thiết kế — Overload Mode");

s.addText(
  [
    { text: "Trúng đòn không làm mất máu — nó nạp điện vào người. ", options: { bold: true, color: DARK } },
    { text: "Càng nhiễm điện, từ trường tác động lên bạn càng mạnh, nên cùng một cú đấm sẽ hất bạn đi càng xa." },
  ],
  { x: L, y: TOP, w: W, h: 0.45, fontFace: F, fontSize: 14, color: INK, valign: "middle", margin: 0, isTextBox: true }
);

stat(s, { x: L, y: 1.5, w: 2.9, h: 1.25, value: "0 → 100", label: "thang điện tích thay cho thanh máu", size: 26, color: DARK });
stat(s, { x: 3.55, y: 1.5, w: 2.9, h: 1.25, value: "x1,0 → x4,0", label: "lực văng tăng tuyến tính theo lượng điện", size: 22, color: RED });
stat(s, { x: 6.65, y: 1.5, w: 2.9, h: 1.25, value: "1", label: "cách chết duy nhất: rơi khỏi đảo", size: 30, color: BLUE });

card(s, { x: L, y: 2.95, w: 5.4, h: 2.05 });
s.addText("VÌ SAO ĐẦY ĐIỆN LẠI KHÔNG CHẾT", {
  x: L + 0.18, y: 3.06, w: 5.05, h: 0.3, fontFace: F, fontSize: 12.5, bold: true, color: DARK, margin: 0, isTextBox: true,
});
s.addText(
  "Nếu đầy thanh là chết thì đây chỉ là thanh máu chạy ngược, không có gì mới. Để cái chết đến từ VỊ TRÍ mới tạo được căng thẳng thật: đứng giữa sân với 90% điện vẫn an toàn, còn đứng sát rìa với 30% đã là mạo hiểm.",
  { x: L + 0.18, y: 3.38, w: 5.05, h: 0.95, fontFace: F, fontSize: 12, color: INK, valign: "top", margin: 0, isTextBox: true }
);
s.addText(
  "Hệ quả bắt buộc: pha chiến đấu phải có đồng hồ 5 phút, vì không còn ai chết vì hết máu.",
  { x: L + 0.18, y: 4.38, w: 5.05, h: 0.5, fontFace: F, fontSize: 11, italic: true, color: MUTED, valign: "top", margin: 0, isTextBox: true }
);

picImg(s, { x: 6.1, y: 2.95, w: 3.45, h: 2.05, path: "img/shot_hud.png", ar: 1914 / 1059 });
s.addNotes("4:15-5:35 — SLIDE QUAN TRỌNG NHẤT. Nói chậm. Nếu hội đồng chỉ nhớ một điều, phải là điều này.");

// =====================================================================
// 9. §4 THỂ THỨC THI ĐẤU
// =====================================================================
s = pres.addSlide({ masterName: "VTC_CONTENT", sectionTitle: "4. Phan tich va thiet ke" });
head(s, "4. Phân tích & thiết kế — Match Format");

const loop = [
  ["CHUẨN BỊ", "15 giây", "Mua đồ, bị rào chắn giữ lại"],
  ["CHIẾN ĐẤU", "5 phút", "Rào mở, hai đội giao tranh"],
  ["KẾT ROUND", "8 giây", "Cộng điểm, cộng tiền thưởng"],
  ["ROUND MỚI", "—", "Hồi sinh, xả sạch điện về 0"],
];
for (let i = 0; i < 4; i++) {
  const cx = L + i * 2.32;
  card(s, { x: cx, y: TOP, w: 2.05, h: 1.15 });
  s.addText(loop[i][0], { x: cx + 0.14, y: TOP + 0.1, w: 1.8, h: 0.28, fontFace: F, fontSize: 11.5, bold: true, color: DARK, margin: 0, isTextBox: true });
  s.addText(loop[i][1], { x: cx + 0.14, y: TOP + 0.38, w: 1.8, h: 0.3, fontFace: F, fontSize: 15, bold: true, color: i % 2 === 0 ? RED : BLUE, margin: 0, isTextBox: true });
  s.addText(loop[i][2], { x: cx + 0.14, y: TOP + 0.7, w: 1.8, h: 0.4, fontFace: F, fontSize: 10, color: MUTED, valign: "top", margin: 0, isTextBox: true });
  if (i < 3) {
    s.addText("›", { x: cx + 2.03, y: TOP + 0.3, w: 0.3, h: 0.5, fontFace: F, fontSize: 24, bold: true, color: LINE, align: "center", valign: "middle", margin: 0, isTextBox: true });
  }
}

card(s, { x: L, y: 2.42, w: 4.45, h: 1.5 });
s.addText("THẮNG MỘT ROUND — hai đường", { x: L + 0.18, y: 2.52, w: 4.1, h: 0.28, fontFace: F, fontSize: 12, bold: true, color: DARK, margin: 0, isTextBox: true });
bullets(s, [
  "Chiếm giữ khu trung tâm đủ 60 giây — thắng ngay",
  "Hết giờ 5 phút: đội có tiến độ chiếm cao hơn thắng",
  "Bằng tiến độ thì hoà, không ai được điểm",
], { x: L + 0.18, y: 2.84, w: 4.1, h: 1.0, size: 11.5 });

card(s, { x: 5.1, y: 2.42, w: 4.45, h: 1.5 });
s.addText("THẮNG CẢ TRẬN", { x: 5.28, y: 2.52, w: 4.1, h: 0.28, fontFace: F, fontSize: 12, bold: true, color: DARK, margin: 0, isTextBox: true });
bullets(s, [
  "Đạt 5 round trước VÀ hơn đối thủ ít nhất 2 round",
  "Tỉ số 5-4 không kết thúc trận — vào Overtime",
  "Đấu tiếp tới khi một đội hơn 2 round: 6-4, 7-5...",
], { x: 5.28, y: 2.84, w: 4.1, h: 1.0, size: 11.5 });

card(s, { x: L, y: 4.05, w: 9.1, h: 0.95, fill: "F3F7FD" });
s.addText("KINH TẾ", { x: L + 0.18, y: 4.14, w: 1.2, h: 0.26, fontFace: F, fontSize: 11.5, bold: true, color: DARK, margin: 0, isTextBox: true });
s.addText(
  "Thắng round +$150  ·  Thua round +$100  ·  Trần ví $750  ·  Cửa hàng 5 vật phẩm: Giáp Cách Điện, Energy Drink, Bộ Xả Điện, Can Xăng, Lõi Tường Chắn",
  { x: L + 0.18, y: 4.42, w: 8.75, h: 0.48, fontFace: F, fontSize: 11.5, color: INK, valign: "top", margin: 0, isTextBox: true }
);
s.addNotes("5:35-6:10 — Lướt nhanh. Hội đồng chỉ cần thấy trận đấu có cấu trúc và có điểm dừng rõ ràng.");

// =====================================================================
// 10. §4 KIẾN TRÚC MẠNG
// =====================================================================
s = pres.addSlide({ masterName: "VTC_CONTENT", sectionTitle: "4. Phan tich va thiet ke" });
head(s, "4. Phân tích & thiết kế — Netcode");

lead(s, "Photon Fusion 2 — Host Mode: một máy giữ quyền quyết định, ba máy kia chỉ gửi phím bấm");

// sơ đồ: host ở giữa, 3 client quanh
const hx = 1.55, hy = 2.52;
card(s, { x: hx, y: hy, w: 1.75, h: 0.75, fill: NAVY, line: NAVY });
s.addText("HOST", { x: hx, y: hy, w: 1.75, h: 0.4, fontFace: F, fontSize: 13, bold: true, color: WHITE, align: "center", valign: "middle", margin: 0, isTextBox: true });
s.addText("StateAuthority", { x: hx, y: hy + 0.36, w: 1.75, h: 0.3, fontFace: F, fontSize: 9.5, color: "C8D6EF", align: "center", valign: "middle", margin: 0, isTextBox: true });

const cls = [[0.5, 1.52], [0.5, 3.82], [2.6, 4.4]];
for (let i = 0; i < 3; i++) {
  card(s, { x: cls[i][0], y: cls[i][1], w: 1.5, h: 0.5 });
  s.addText("CLIENT " + (i + 1), {
    x: cls[i][0], y: cls[i][1], w: 1.5, h: 0.5,
    fontFace: F, fontSize: 10.5, bold: true, color: DARK, align: "center", valign: "middle", margin: 0, isTextBox: true,
  });
}
s.addShape(pres.ShapeType.line, { x: 1.25, y: 2.02, w: 0.75, h: 0.5, line: { color: LINE, width: 1.5, dashType: "dash" } });
s.addShape(pres.ShapeType.line, { x: 1.25, y: 3.27, w: 0.75, h: 0.55, line: { color: LINE, width: 1.5, dashType: "dash" } });
s.addShape(pres.ShapeType.line, { x: 2.6, y: 3.27, w: 0.75, h: 1.13, line: { color: LINE, width: 1.5, dashType: "dash" } });
s.addText("phím bấm ↑   ·   trạng thái ↓", {
  x: 0.5, y: 4.9, w: 3.6, h: 0.25, fontFace: F, fontSize: 9.5, italic: true, color: MUTED, align: "center", margin: 0, isTextBox: true,
});

card(s, { x: 4.4, y: 1.5, w: 5.15, h: 1.8 });
s.addText("VÌ SAO CHỌN HOST MODE", { x: 4.58, y: 1.6, w: 4.8, h: 0.28, fontFace: F, fontSize: 12, bold: true, color: DARK, margin: 0, isTextBox: true });
bullets(s, [
  "Chỉ một máy tính vật lý nên hai bên không bao giờ thấy hai kết quả khác nhau",
  "Máy khách không tự quyết định sát thương, nên không gian lận được",
  "Ổn định hơn Shared Mode khi vật thể va chạm liên tục",
], { x: 4.58, y: 1.94, w: 4.8, h: 1.28, size: 11.5 });

card(s, { x: 4.4, y: 3.42, w: 5.15, h: 1.6 });
s.addText("BA NGUYÊN TẮC ĐÃ ÁP DỤNG", { x: 4.58, y: 3.5, w: 4.8, h: 0.28, fontFace: F, fontSize: 12, bold: true, color: DARK, margin: 0, isTextBox: true });
bullets(s, [
  "Chỉ đồng bộ thứ bắt buộc, phần còn lại thì suy ra tại chỗ",
  "Nhưng chỉ suy khi nguồn đáng tin — trên Host, vị trí nhân vật khách thì không",
  "Hiệu ứng và âm thanh phát theo bộ đếm, tránh phát lại khi Fusion tua lại",
], { x: 4.58, y: 3.82, w: 4.8, h: 1.14, size: 10.5 });
s.addNotes("6:10-7:10 — Phần kỹ thuật nặng nhất. Nói nguyên tắc 1 và 2 — đó là bài học thật, không phải lý thuyết sách.");

// =====================================================================
// 11. §5 QUY TRÌNH PHÁT TRIỂN
// =====================================================================
pres.addSection({ title: "5. Quy trinh phat trien" });
s = pres.addSlide({ masterName: "VTC_CONTENT", sectionTitle: "5. Quy trinh phat trien" });
head(s, "5. Quy trình phát triển — Workflow");

const phases = [
  ["GIAI ĐOẠN 1", "Gameplay chạy một máy", ["Di chuyển, nhảy, lướt", "Găng tay từ tính, bốn loại vật thể", "Cận chiến, túi đồ"]],
  ["GIAI ĐOẠN 2", "Đưa lên mạng", ["Chuyển sang Fusion Host Mode", "Đồng bộ vật thể có vật lý", "Gỡ các lỗi do tua lại"]],
  ["GIAI ĐOẠN 3", "Hoàn thiện vòng trận", ["Vòng round, kinh tế, cửa hàng", "HUD, menu, cài đặt", "Âm thanh và lồng tiếng"]],
  ["GIAI ĐOẠN 4", "Cân bằng và đánh bóng", ["Thử nghiệm hai máy", "Chỉnh số liệu cân bằng", "Sửa lỗi, viết tài liệu"]],
];
for (let i = 0; i < 4; i++) {
  const cx = L + i * 2.32;
  card(s, { x: cx, y: TOP, w: 2.1, h: 2.55 });
  s.addShape(pres.ShapeType.rect, { x: cx, y: TOP, w: 2.1, h: 0.34, fill: { color: i % 2 === 0 ? NAVY : "3D5C9E" }, line: { width: 0 } });
  s.addText(phases[i][0], { x: cx, y: TOP, w: 2.1, h: 0.34, fontFace: F, fontSize: 10.5, bold: true, color: WHITE, align: "center", valign: "middle", margin: 0, isTextBox: true });
  s.addText(phases[i][1], { x: cx + 0.12, y: TOP + 0.42, w: 1.86, h: 0.46, fontFace: F, fontSize: 11.5, bold: true, color: DARK, valign: "top", margin: 0, isTextBox: true });
  bullets(s, phases[i][2], { x: cx + 0.12, y: TOP + 0.92, w: 1.86, h: 1.5, size: 10, color: MUTED });
}

card(s, { x: L, y: 3.68, w: 9.1, h: 1.3, fill: "F3F7FD" });
s.addText("CÁCH KIỂM THỬ", { x: L + 0.18, y: 3.78, w: 3.0, h: 0.26, fontFace: F, fontSize: 11.5, bold: true, color: DARK, margin: 0, isTextBox: true });
s.addText(
  "Dùng ParrelSync nhân bản project để mở hai client trên cùng một máy — nhờ vậy thử được mọi tình huống mạng mà không cần hai máy tính. Những lỗi chỉ xuất hiện khi có độ trễ thật thì kiểm bằng hai máy riêng, kết nối qua khu vực máy chủ asia.",
  { x: L + 0.18, y: 4.06, w: 8.75, h: 0.82, fontFace: F, fontSize: 11.5, color: INK, valign: "top", margin: 0, isTextBox: true }
);
s.addNotes("7:10-7:40 — CÓ THỂ BỎ SLIDE NÀY nếu chậm giờ. Nhưng đừng bỏ phần ParrelSync, hội đồng hay hỏi cách test.");

// =====================================================================
// 12. §5 BA BẪY KỸ THUẬT
// =====================================================================
s = pres.addSlide({ masterName: "VTC_CONTENT", sectionTitle: "5. Quy trinh phat trien" });
head(s, "5. Quy trình phát triển — Top 3 Bugs");

const traps = [
  ["Nhân vật máy khách chạy nhanh gấp nhiều lần", "Fusion tua lại nhiều lượt mỗi khung hình. Mỗi lượt tua, CharacterController vẫn nhớ vị trí cũ nên quãng đường bị cộng dồn.", "Tắt rồi bật lại CharacterController ở đầu mỗi chuỗi tua, qua IBeforeAllTicks."],
  ["Vật tung khỏi tay bay lệch ngẫu nhiên", "Vật cầm trên tay bị thu nhỏ. Trả lại cỡ thật ngay trong người làm hai khối chồng nhau, PhysX bắn ra lực gỡ kẹt mạnh hơn cả lực tung.", "Dời vật ra khỏi người theo phương ngang trước, rồi mới bật va chạm và trả vật lý."],
  ["Game tắt đột ngột khi kết thúc trận", "Lệnh tắt kết nối được gọi từ bên trong hàm mô phỏng của Fusion — tức là tự huỷ chính thứ đang chạy mình.", "Hoãn lệnh tắt sang khung hình sau bằng coroutine, để thoát khỏi ngăn xếp mô phỏng."],
];
for (let i = 0; i < 3; i++) {
  const cy = TOP + i * 1.38;
  card(s, { x: L, y: cy, w: 9.1, h: 1.24 });
  s.addShape(pres.ShapeType.ellipse, { x: L + 0.18, y: cy + 0.17, w: 0.34, h: 0.34, fill: { color: i === 2 ? DARK : i === 0 ? RED : BLUE }, line: { width: 0 } });
  s.addText(String(i + 1), { x: L + 0.18, y: cy + 0.17, w: 0.34, h: 0.34, fontFace: F, fontSize: 13, bold: true, color: WHITE, align: "center", valign: "middle", margin: 0, isTextBox: true });
  s.addText(traps[i][0], { x: L + 0.64, y: cy + 0.14, w: 8.3, h: 0.3, fontFace: F, fontSize: 12.5, bold: true, color: DARK, valign: "middle", margin: 0, isTextBox: true });
  s.addText(
    [
      { text: "Nguyên nhân: ", options: { bold: true, color: MUTED } },
      { text: traps[i][1], options: { color: INK, breakLine: true } },
      { text: "Cách chữa: ", options: { bold: true, color: MUTED } },
      { text: traps[i][2], options: { color: INK } },
    ],
    { x: L + 0.64, y: cy + 0.45, w: 8.3, h: 0.72, fontFace: F, fontSize: 10.5, valign: "top", margin: 0, isTextBox: true, lineSpacingMultiple: 1.0 }
  );
}
s.addNotes("7:40-8:40 — Slide ghi điểm với hội đồng kỹ thuật. Kể lỗi số 2 như một câu chuyện, nó dễ hình dung nhất.\n\n"
  + "8:40-9:20 — MỤC 6: DEMO TRỰC TIẾP (không có slide riêng, Alt+Tab sang game). Mở game sẵn trước buổi bảo vệ. "
  + "Bấm F1 tắt bảng hướng dẫn kẻo nó tự hiện ở pha chuẩn bị. Thứ tự gợi ý: toàn cảnh đảo -> nạp điện và bắn vật thể "
  + "-> thanh điện tăng, lực văng tăng -> bị hất khỏi rìa đảo. Demo xong Alt+Tab về slide Kết quả.");

// =====================================================================
// 14. §7 KẾT QUẢ ĐẠT ĐƯỢC
// =====================================================================
pres.addSection({ title: "7. Ket qua dat duoc" });
s = pres.addSlide({ masterName: "VTC_CONTENT", sectionTitle: "7. Ket qua dat duoc" });
head(s, "7. Kết quả đạt được");

stat(s, { x: L, y: TOP, w: 2.2, h: 1.1, value: "85", label: "khung hình/giây, bản build 1920x1080", color: DARK });
stat(s, { x: 2.85, y: TOP, w: 2.2, h: 1.1, value: "16 ms", label: "độ trễ tới máy chủ khu vực asia", size: 26, color: DARK });
stat(s, { x: 5.25, y: TOP, w: 2.05, h: 1.1, value: "49", label: "tệp mã nguồn tự viết", color: DARK });
stat(s, { x: 7.5, y: TOP, w: 2.05, h: 1.1, value: "4", label: "chương báo cáo, 53 trang", color: DARK });

card(s, { x: L, y: 2.22, w: 4.45, h: 2.78 });
s.addText("TÍNH NĂNG ĐÃ HOÀN THÀNH", { x: L + 0.18, y: 2.32, w: 4.1, h: 0.28, fontFace: F, fontSize: 12, bold: true, color: DARK, margin: 0, isTextBox: true });
bullets(s, [
  "Trận 2v2 đầy đủ qua mạng, có thắng thua và Overtime",
  "Bốn loại đạn, cận chiến, kéo áp sát, túi đồ",
  "Chế độ Quá Tải thay thanh máu",
  "Khu chiếm đóng, kinh tế, cửa hàng 5 vật phẩm",
  "HUD, menu, phòng chờ, cài đặt, bảng kết quả",
  "Âm thanh, nhạc nền, lồng tiếng thông báo",
  "Chế độ quan sát khi bị loại khỏi round",
], { x: L + 0.18, y: 2.64, w: 4.1, h: 2.25, size: 11.5 });

card(s, { x: 5.1, y: 2.22, w: 4.45, h: 2.78 });
s.addText("VỀ MẶT KỸ THUẬT", { x: 5.28, y: 2.32, w: 4.1, h: 0.28, fontFace: F, fontSize: 12, bold: true, color: DARK, margin: 0, isTextBox: true });
bullets(s, [
  "Đồng bộ được vật thể có vật lý qua mạng — phần khó nhất của đề tài",
  "Xử lý dứt điểm ba lớp lỗi đặc trưng của mô hình tua lại",
  "Không còn lỗi treo hay tắt đột ngột sau khi sửa",
  "Người chơi thoát giữa trận không làm hỏng trận đấu",
  "Toàn bộ tài nguyên bên thứ ba đều được khai báo và kiểm giấy phép",
], { x: 5.28, y: 2.64, w: 4.1, h: 2.25, size: 11.5 });
s.addNotes("9:20-9:45 — Số đã đo thật ngày 11/10: 85 FPS trên bản build ở 1920x1080 (RX 6600), ping 16 ms tới máy chủ asia. Nếu bị hỏi FPS thì nói kèm độ phân giải.");

// =====================================================================
// 15. §8 + §9 HƯỚNG PHÁT TRIỂN VÀ KẾT LUẬN
// =====================================================================
pres.addSection({ title: "8-9. Huong phat trien va ket luan" });
s = pres.addSlide({ masterName: "VTC_CONTENT", sectionTitle: "8-9. Huong phat trien va ket luan" });
head(s, "8. Hướng phát triển  ·  9. Kết luận");

card(s, { x: L, y: TOP, w: 4.45, h: 1.95 });
s.addText("HẠN CHẾ CÒN LẠI", { x: L + 0.18, y: TOP + 0.1, w: 4.1, h: 0.28, fontFace: F, fontSize: 12, bold: true, color: DARK, margin: 0, isTextBox: true });
bullets(s, [
  "Mới có một bản đồ, nên lối chơi dễ lặp lại",
  "Chưa có bot máy, bắt buộc đủ người thật",
  "Chặn người vào giữa trận, không thay thế được người thoát",
  "Chưa có hệ thống tiến trình giữa các trận",
], { x: L + 0.18, y: TOP + 0.42, w: 4.1, h: 1.45, size: 11.5 });

card(s, { x: 5.1, y: TOP, w: 4.45, h: 1.95 });
s.addText("HƯỚNG PHÁT TRIỂN", { x: 5.28, y: TOP + 0.1, w: 4.1, h: 0.28, fontFace: F, fontSize: 12, bold: true, color: DARK, margin: 0, isTextBox: true });
bullets(s, [
  "Thêm bản đồ và thêm loại vật thể từ tính mới",
  "Mở rộng quy mô lên 3v3 hoặc 4v4",
  "Chuyển sang máy chủ chuyên dụng thay cho Host Mode",
  "Bổ sung hệ thống tiến trình, giao diện tuỳ biến",
], { x: 5.28, y: TOP + 0.42, w: 4.1, h: 1.45, size: 11.5 });

card(s, { x: L, y: 3.1, w: 9.1, h: 1.9, fill: "F3F7FD" });
s.addText("KẾT LUẬN", { x: L + 0.18, y: 3.2, w: 3.0, h: 0.28, fontFace: F, fontSize: 12.5, bold: true, color: DARK, margin: 0, isTextBox: true });
bullets(s, [
  "Đã hoàn thành một game FPS đối kháng 2v2 chơi qua mạng, có đầy đủ vòng trận, điều kiện thắng thua, giao diện, hình ảnh và âm thanh",
  "Đã nắm được cách vận hành của mô hình mạng dự đoán và tua lại — gồm cả những cái bẫy mà tài liệu chính thức không nói tới",
  "Đã chứng minh được rằng một cơ chế thay thế thanh máu là khả thi và tạo ra lối chơi khác biệt, không chỉ là ý tưởng trên giấy",
], { x: L + 0.18, y: 3.52, w: 8.75, h: 1.4, size: 11.5 });
s.addNotes("9:45-10:00 — Kết bằng câu thứ ba: cơ chế mới là khả thi. Đó là đóng góp của đề tài.");

// =====================================================================
// 16. CẢM ƠN
// =====================================================================
pres.addSection({ title: "Cam on" });
s = pres.addSlide({ masterName: "VTC_CLOSING", sectionTitle: "Cam on" });
s.addText("CẢM ƠN HỘI ĐỒNG ĐÃ LẮNG NGHE", {
  x: 0.6, y: 2.05, w: 8.8, h: 0.75,
  fontFace: F, fontSize: 34, bold: true, color: WHITE, align: "center", margin: 0, isTextBox: true,
});
s.addText("Rất mong nhận được câu hỏi và góp ý từ hội đồng", {
  x: 0.6, y: 2.85, w: 8.8, h: 0.4,
  fontFace: F, fontSize: 15, color: "C8D6EF", align: "center", margin: 0, isTextBox: true,
});
s.addShape(pres.ShapeType.ellipse, { x: 4.72, y: 3.5, w: 0.18, h: 0.18, fill: { color: RED }, line: { width: 0 } });
s.addShape(pres.ShapeType.ellipse, { x: 5.1, y: 3.5, w: 0.18, h: 0.18, fill: { color: BLUE }, line: { width: 0 } });
s.addText("MAGNETO-DOME  ·  Nguyễn Hoàng Gia Khiêm  ·  K24M-GAME1", {
  x: 0.6, y: 4.3, w: 8.8, h: 0.3,
  fontFace: F, fontSize: 11, color: "9FB5DA", align: "center", margin: 0, isTextBox: true,
});
s.addNotes("Dừng lại ở đây. Bốn slide sau là slide dự phòng, chỉ mở khi hội đồng hỏi đúng chủ đề đó.");

// =====================================================================
// SLIDE DỰ PHÒNG
// =====================================================================
pres.addSection({ title: "Du phong" });

// D1 — tiến độ
s = pres.addSlide({ masterName: "VTC_CONTENT", sectionTitle: "Du phong" });
head(s, "Timeline");
tbl(s, [
  hdrRow(["Mốc", "Nội dung chính", "Kết quả"]),
  ["Giai đoạn 1", "Gameplay chạy một máy: di chuyển, găng tay từ tính, vật thể, cận chiến", "Chơi được offline"],
  ["Giai đoạn 2", "Chuyển toàn bộ sang Photon Fusion 2 Host Mode, đồng bộ vật lý", "Hai máy chơi được"],
  ["Giai đoạn 3", "Vòng round, kinh tế, cửa hàng, HUD, menu, âm thanh, cài đặt", "Trận đấu hoàn chỉnh"],
  ["Giai đoạn 4", "Cân bằng số liệu, kiểm thử hai máy, sửa lỗi, viết báo cáo", "Sẵn sàng bảo vệ"],
], { x: L, y: 1.1, w: 9.1, colW: [1.6, 5.6, 1.9], rowH: 0.52, size: 11.5 });
card(s, { x: L, y: 3.9, w: 9.1, h: 1.0, fill: "F3F7FD" });
s.addText(
  "Toàn bộ đồ án do một người thực hiện: thiết kế lối chơi, lập trình, dựng cảnh, xử lý âm thanh và viết tài liệu. Không có thành viên nào khác.",
  { x: L + 0.18, y: 4.08, w: 8.75, h: 0.7, fontFace: F, fontSize: 12, color: INK, valign: "top", margin: 0, isTextBox: true }
);

// D2 — bảng cân bằng
s = pres.addSlide({ masterName: "VTC_CONTENT", sectionTitle: "Du phong" });
head(s, "Balance Sheet");
tbl(s, [
  hdrRow(["Hạng mục", "Giá trị", "Hạng mục", "Giá trị"]),
  ["Thang điện tích tối đa", "100", "Pha chuẩn bị", "15 giây"],
  ["Hệ số lực văng khi đầy điện", "x4,0", "Pha chiến đấu", "300 giây"],
  ["Hệ số nhân điện tích nhận vào", "x1,5", "Chiếm khu để thắng", "60 giây"],
  ["Điện tích: Normal / Heavy", "10 / 20", "Hồi sinh sau khi rơi", "5 giây"],
  ["Điện tích: Spike / TNT", "25 / 35", "Bất tử sau hồi sinh", "2 giây"],
  ["Bán kính nổ TNT", "6 mét", "Thắng trận", "5 round, cách biệt 2"],
  ["Thưởng thắng / thua round", "$150 / $100", "Trần ví", "$750"],
], { x: L, y: 1.1, w: 9.1, colW: [2.9, 1.65, 2.9, 1.65], rowH: 0.4, size: 11 });

// D3 — kiến trúc mã nguồn
s = pres.addSlide({ masterName: "VTC_CONTENT", sectionTitle: "Du phong" });
head(s, "Code Architecture");
const arch = [
  ["Người chơi", "FPSMovement, PlayerHealth, PlayerMagnetController, InventorySystem, PlayerEconomy, PlayerVisuals", BLUE],
  ["Vật thể", "MagneticObject (gộp cả bốn loại trong một lớp, phân biệt bằng enum), MagneticAura, CombatVFX", RED],
  ["Điều phối trận", "GameManager (vòng round, Overtime, vùng chết), ControlZone, ShopManager", DARK],
  ["Mạng", "NetworkRunnerHandler (khởi tạo Fusion, tạo và vào phòng), RoomPlayer, NetworkInputData", NAVY],
  ["Giao diện", "HUDController, MatchResultPanel, LoadingScreen, ControlsOverlay, SettingsUI, RadialMenuController", MUTED],
];
for (let i = 0; i < 5; i++) {
  const cy = 1.07 + i * 0.7;
  card(s, { x: L, y: cy, w: 9.1, h: 0.63 });
  s.addShape(pres.ShapeType.ellipse, { x: L + 0.16, y: cy + 0.21, w: 0.2, h: 0.2, fill: { color: arch[i][2] }, line: { width: 0 } });
  s.addText(arch[i][0], { x: L + 0.46, y: cy, w: 1.55, h: 0.63, fontFace: F, fontSize: 11.5, bold: true, color: DARK, valign: "middle", margin: 0, isTextBox: true });
  s.addText(arch[i][1], { x: L + 2.0, y: cy, w: 6.95, h: 0.63, fontFace: F, fontSize: 10.5, color: INK, valign: "middle", margin: 0, isTextBox: true });
}
s.addText("49 tệp mã nguồn, khoảng 21.000 dòng do học viên tự viết — chưa tính thư viện Photon và các gói bên thứ ba.", {
  x: L, y: 4.6, w: 8.2, h: 0.24, fontFace: F, fontSize: 9.5, italic: true, color: MUTED, margin: 0, isTextBox: true,
});

// D4 — tài nguyên bên thứ ba
s = pres.addSlide({ masterName: "VTC_CONTENT", sectionTitle: "Du phong" });
head(s, "Credits");
tbl(s, [
  hdrRow(["Hạng mục", "Nguồn", "Giấy phép"]),
  ["Mã nguồn, lối chơi, hiệu ứng, giao diện", "Học viên tự làm", "—"],
  ["Khung mạng Photon Fusion 2", "Photon Engine", "Miễn phí dưới 100 CCU"],
  ["Mô hình hòn đảo", "Sinh bằng Meshy AI", "Có ghi nguồn Meshy"],
  ["Nhân vật và hoạt ảnh", "Mixamo — Adobe", "Miễn phí, dùng thương mại"],
  ["Đá, cây", "Asset Store: Manufactura K4, Pixel Games", "Giấy phép Asset Store"],
  ["Cỏ, hoa, bụi cây", "Quaternius", "CC0, dùng tự do"],
  ["Bầu trời", "Asset Store: Render Knight", "Gói miễn phí"],
  ["Nhạc nền", "Pixabay", "Pixabay Content License"],
  ["Giọng thông báo", "Sinh bằng ElevenLabs", "Có ghi nguồn elevenlabs.io"],
], { x: L, y: 1.05, w: 9.1, colW: [3.0, 3.6, 2.5], rowH: 0.31, size: 10 });
card(s, { x: L, y: 4.25, w: 9.1, h: 0.5, fill: "F3F7FD" });
s.addText("Danh sách đầy đủ nằm ở Phụ lục B của báo cáo và trong tệp CREDITS.md kèm theo mã nguồn.", {
  x: L + 0.18, y: 4.25, w: 8.75, h: 0.5, fontFace: F, fontSize: 11, color: INK, valign: "middle", margin: 0, isTextBox: true,
});

pres.writeFile({ fileName: "MagnetoDome_Slide.pptx" }).then(function () {
  console.log("xong: MagnetoDome_Slide.pptx");
});
