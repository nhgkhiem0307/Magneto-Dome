using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Giao diện phòng chờ dạng THẺ NGƯỜI CHƠI (16/10) — thay cho danh sách chữ "- tên" cũ.
///
/// Mỗi đội 2 ô (phòng 1v1 thì 1 ô). Có người: thẻ tên + vạch màu đội, đánh dấu YOU và vương miện
/// chủ phòng. Còn trống: "Waiting for player..." nhấp nháy nhẹ. Đầu bảng: mã phòng to + nút COPY
/// + nút chọn chế độ 1V1 / 2V2.
///
/// CHẾ ĐỘ + KICK (16/10): chủ phòng đổi 1v1 ⇄ 2v2 ở đầu bảng. Phòng đông hơn sức chứa chế độ mới
/// thì không đổi được, hiện dòng báo phải kick bớt. Thẻ người khác có nút KICK (bấm hai lần).
///
/// QUYỀN ĐỔI ĐỘI (16/10): chỉ chủ phòng. Chủ phòng bấm vào thẻ của ai thì người đó sang đội kia
/// (nếu đội kia còn chỗ). Client không có nút Switch Team, chỗ nút Start hiện "Waiting for host…".
/// Chốt chặn thật nằm ở RoomPlayer.RPC_RequestTeamChange (phía Host), giao diện chỉ là lớp ngoài.
///
/// Đọc thẳng RoomPlayer.AllPlayers mỗi 0,15 giây — không phải nối thêm sự kiện nào vào code mạng.
/// Ô chữ cũ teamRedText / teamBlueText / roomTitleText vẫn được NetworkRunnerHandler ghi như trước,
/// chỉ bị ẩn đi.
///
/// Đặt trên RoomLobbyPanel. Các ô tham chiếu bên dưới đã được gán sẵn trong scene.
/// </summary>
public class LobbyView : MonoBehaviour
{
    [Header("Gán sẵn trong scene")]
    public RectTransform redTeamPanel;
    public RectTransform blueTeamPanel;
    public GameObject switchTeamButton;
    public RectTransform startButton;
    public Sprite cardSprite;
    public Sprite crownSprite;
    public TMP_FontAsset fontBlack;
    public TMP_FontAsset fontBold;
    public TMP_FontAsset fontMedium;

    private static readonly Color Red = new Color32(0xE8, 0x45, 0x3C, 0xFF);
    private static readonly Color Blue = new Color32(0x2E, 0x86, 0xDE, 0xFF);
    private static readonly Color RedText = new Color32(0xFF, 0x6B, 0x63, 0xFF);
    private static readonly Color BlueText = new Color32(0x5A, 0xB0, 0xFF, 0xFF);
    private static readonly Color Cyan = new Color32(0x5C, 0xD8, 0xFF, 0xFF);
    private static readonly Color Gold = new Color32(0xFF, 0xC8, 0x3D, 0xFF);
    private static readonly Color Soft = new Color(0.86f, 0.9f, 0.96f, 1f);

    /// <summary>Dữ liệu một thẻ — tách khỏi RoomPlayer để chụp ảnh xem trước được ngoài trận.</summary>
    public struct Entry
    {
        public string name; public int team; public bool host; public bool you; public RoomPlayer player;
    }

    private class Card
    {
        public GameObject root; public Button button; public Image bg, bar, crown;
        public TMP_Text name, tag, hint; public RoomPlayer player;
        public Button kick; public TMP_Text kickLabel; public float kickArmedUntil;
        public float baseY;   // vị trí dọc ở phòng 2v2; phòng 1v1 thì thẻ đầu dời xuống giữa vùng thẻ
    }

    // Cấp sẵn ô cho chế độ lớn nhất (2v2); phòng 1v1 thì ẩn bớt ô thứ hai của mỗi đội
    private readonly Card[,] _cards = new Card[2, RoomPlayer.MaxTeamSize];
    private TMP_Text[] _teamHeader = new TMP_Text[2];
    private TMP_Text _code, _count, _copyLabel, _waiting, _hostHint, _notice, _startHint;
    private readonly Button[] _modeButtons = new Button[RoomPlayer.MaxTeamSize + 1];   // chỉ số = số người mỗi đội
    private readonly TMP_Text[] _modeLabels = new TMP_Text[RoomPlayer.MaxTeamSize + 1];
    private bool _built;
    private float _nextRefresh, _copiedUntil, _noticeUntil;
    private bool _previewMode;

    // --- Theo dõi thay đổi để hiện dòng báo "X joined / left / was kicked", "Host switched to 1v1" ---
    private readonly Dictionary<PlayerRef, string> _known = new Dictionary<PlayerRef, string>();
    private readonly HashSet<PlayerRef> _kickedByMe = new HashSet<PlayerRef>();
    private int _lastTeamSize = -1;
    private float _trackFrom;

    private void OnEnable()
    {
        // Mỗi lần vào phòng mới là một danh sách mới. Trong 1 giây đầu chỉ ghi nhận lặng lẽ:
        // tên những người CÓ SẴN trong phòng về chậm vài nhịp mạng, không được báo nhầm là "vừa vào".
        _known.Clear();
        _kickedByMe.Clear();
        _lastTeamSize = -1;
        _trackFrom = Time.unscaledTime + 1f;

        Build();
        Refresh();
    }

    private void Update()
    {
        // Thẻ trống nhấp nháy nhẹ cho biết "đang chờ"
        float pulse = 0.45f + 0.25f * Mathf.Sin(Time.unscaledTime * 2.4f);
        for (int t = 0; t < 2; t++)
            for (int i = 0; i < RoomPlayer.MaxTeamSize; i++)
            {
                Card c = _cards[t, i];
                if (c == null) continue;
                if (c.player == null && !_previewMode) c.name.alpha = pulse;
                // Nút KICK bấm lần đầu chỉ "lên đạn" (SURE?), quá 2,5 giây không bấm lại thì thôi
                if (c.kickLabel != null) c.kickLabel.text = Time.unscaledTime < c.kickArmedUntil ? "SURE?" : "KICK";
            }

        if (_copyLabel != null) _copyLabel.text = Time.unscaledTime < _copiedUntil ? "COPIED!" : "COPY";

        // Dòng báo (vd. "kick bớt người trước") mờ dần trong nửa giây cuối
        if (_notice != null)
        {
            float left = _noticeUntil - Time.unscaledTime;
            _notice.gameObject.SetActive(left > 0f);
            if (left > 0f) _notice.alpha = Mathf.Clamp01(left / 0.5f);
        }

        if (_previewMode || Time.unscaledTime < _nextRefresh) return;
        _nextRefresh = Time.unscaledTime + 0.15f;
        Refresh();
    }

    // ==================== DỮ LIỆU ====================

    private void Refresh()
    {
        var list = new List<Entry>();
        foreach (RoomPlayer p in RoomPlayer.AllPlayers)
        {
            if (p == null || p.Object == null || !p.Object.IsValid) continue;
            string nick = p.NickName.ToString();
            list.Add(new Entry
            {
                name = string.IsNullOrEmpty(nick) ? "Loading..." : nick,
                team = p.Team, host = p.IsHost, you = p == RoomPlayer.Local, player = p,
            });
        }
        // Thứ tự cố định theo số thứ tự người chơi — thẻ không nhảy lung tung khi ai đó đổi tên
        list.Sort((a, b) => a.player.PlayerRef.RawEncoded.CompareTo(b.player.PlayerRef.RawEncoded));

        NetworkRunnerHandler h = NetworkRunnerHandler.Instance;
        bool iAmHost = h != null && h.IsHost;
        int teamSize = RoomPlayer.TeamSize;
        TrackChanges(list, teamSize, iAmHost);
        Show(list, h != null ? h.CurrentRoomCode : "", iAmHost, teamSize);
    }

    /// <summary>So với lần trước: ai mới vào, ai vừa ra, chế độ có đổi không — rồi báo một dòng.</summary>
    private void TrackChanges(List<Entry> list, int teamSize, bool iAmHost)
    {
        bool announce = Time.unscaledTime >= _trackFrom;
        var present = new HashSet<PlayerRef>();

        foreach (Entry e in list)
        {
            PlayerRef r = e.player.PlayerRef;
            present.Add(r);
            if (e.name == "Loading...") continue;   // chờ có tên rồi mới báo
            if (!_known.ContainsKey(r) && announce && !e.you) ShowNotice($"{e.name} joined", Cyan);
            _known[r] = e.name;
        }

        var gone = new List<PlayerRef>();
        foreach (var kv in _known) if (!present.Contains(kv.Key)) gone.Add(kv.Key);
        foreach (PlayerRef r in gone)
        {
            if (announce) ShowNotice(_kickedByMe.Contains(r) ? $"{_known[r]} was kicked" : $"{_known[r]} left the room", Soft);
            _known.Remove(r);
            _kickedByMe.Remove(r);
        }

        // Client không tự bấm đổi chế độ, nên phải được báo khi chủ phòng đổi — không thì số ô thẻ
        // bỗng dưng thay đổi mà không biết vì sao
        if (_lastTeamSize > 0 && teamSize != _lastTeamSize && !iAmHost)
            ShowNotice($"Host switched the room to {teamSize}v{teamSize}", Cyan);
        _lastTeamSize = teamSize;
    }

    /// <summary>Vẽ lên giao diện. Public để chụp ảnh xem trước được ngoài trận.</summary>
    /// <param name="teamSize">Chế độ phòng: 1 = 1v1, 2 = 2v2.</param>
    public void Show(List<Entry> players, string roomCode, bool iAmHost, int teamSize)
    {
        Build();

        _code.text = string.IsNullOrEmpty(roomCode) ? "-----" : roomCode;
        _count.text = $"{players.Count}/{teamSize * 2} PLAYERS";

        // Nút chế độ: ô đang chọn sáng cyan. Client thấy được chế độ nhưng không bấm được.
        for (int m = 1; m <= RoomPlayer.MaxTeamSize; m++)
        {
            if (_modeButtons[m] == null) continue;
            bool on = m == teamSize;
            _modeButtons[m].interactable = iAmHost;
            _modeButtons[m].targetGraphic.color = on ? new Color(Cyan.r, Cyan.g, Cyan.b, 0.92f) : new Color(0.07f, 0.1f, 0.17f, 0.9f);
            _modeLabels[m].color = on ? new Color(0.03f, 0.06f, 0.12f, 1f) : Soft;
            _modeLabels[m].alpha = on ? 1f : (iAmHost ? 0.6f : 0.3f);
        }

        for (int t = 0; t < 2; t++)
        {
            var team = players.FindAll(p => p.team == t);
            _teamHeader[t].text = (t == 0 ? "RED TEAM" : "BLUE TEAM") + $"  <color=#FFFFFF80>{team.Count}/{teamSize}</color>";
            int otherCount = players.FindAll(p => p.team != t).Count;

            for (int i = 0; i < RoomPlayer.MaxTeamSize; i++)
            {
                Card c = _cards[t, i];

                // Phòng 1v1: mỗi đội chỉ một ô
                bool slotExists = i < teamSize;
                c.root.SetActive(slotExists);
                if (!slotExists) { c.player = null; continue; }

                // Phòng 1v1: thẻ duy nhất nằm giữa vùng hai thẻ, không dính sát mép trên
                ((RectTransform)c.root.transform).anchoredPosition = new Vector2(0f, teamSize == 1 ? c.baseY - 62f : c.baseY);

                bool filled = i < team.Count;
                Color teamCol = t == 0 ? Red : Blue;
                c.player = filled ? team[i].player : null;

                c.bar.gameObject.SetActive(filled);
                c.bar.color = teamCol;
                c.bg.color = filled ? new Color(0.07f, 0.1f, 0.17f, 0.95f) : new Color(0.07f, 0.1f, 0.17f, 0.45f);
                c.crown.gameObject.SetActive(filled && team[i].host);
                c.tag.gameObject.SetActive(filled && team[i].you);

                c.name.text = filled ? team[i].name : "Waiting for player...";
                c.name.fontStyle = filled ? FontStyles.Normal : FontStyles.Italic;
                c.name.color = filled ? Color.white : Soft;
                c.name.fontSize = filled ? 34 : 26;
                c.name.alpha = filled ? 1f : 0.55f;
                c.name.rectTransform.anchoredPosition = new Vector2(filled && team[i].host ? 78f : 40f, 0f);

                // Chủ phòng bấm thẻ để chuyển người đó sang đội kia (khi đội kia còn chỗ)
                bool canMove = iAmHost && filled && otherCount < teamSize;
                c.button.interactable = canMove;
                c.hint.gameObject.SetActive(canMove);
                c.hint.text = "MOVE TO " + (t == 0 ? "BLUE" : "RED");

                // Nút KICK: chỉ chủ phòng thấy, trên thẻ của NGƯỜI KHÁC (không tự kick mình).
                // Nằm đúng chỗ chữ YOU — thẻ người khác thì chỗ đó vốn trống.
                bool canKick = iAmHost && filled && !team[i].host && !team[i].you;
                c.kick.gameObject.SetActive(canKick);
                if (!canKick) c.kickArmedUntil = 0f;
            }
        }

        _hostHint.gameObject.SetActive(iAmHost);
        if (switchTeamButton != null) switchTeamButton.SetActive(iAmHost);
        // NetworkRunnerHandler cũng ẩn nút Start với Client — làm thêm ở đây cho chắc,
        // vì dòng "Waiting for host" nằm đúng chỗ nút đó.
        if (startButton != null) startButton.gameObject.SetActive(iAmHost);
        _waiting.gameObject.SetActive(!iAmHost);

        // Gợi ý cho chủ phòng ngay dưới nút Start. CHỈ NHẮC, không chặn bấm (chủ project chốt 16/10):
        // vẫn cho bắt đầu lệch người, ví dụ để tập một mình hay đấu 2 đánh 1 cho vui.
        int red = players.FindAll(p => p.team == 0).Count;
        int blue = players.Count - red;
        string startHint = players.Count <= 1 ? "You're alone in the room · share the room code"
                         : red != blue ? $"Teams are uneven ({red} vs {blue}) · you can still start"
                         : "";
        _startHint.text = startHint;
        _startHint.gameObject.SetActive(iAmHost && startHint.Length > 0);
    }

    private void OnCardClicked(int team, int index)
    {
        Card c = _cards[team, index];
        if (c.player == null) return;
        NetworkRunnerHandler h = NetworkRunnerHandler.Instance;
        if (h == null || !h.IsHost) return;
        c.player.TryMoveToTeam(team == 0 ? 1 : 0);
        _nextRefresh = 0f;
    }

    /// <summary>
    /// Bấm KICK: lần đầu chỉ hỏi lại ("SURE?"), bấm lần nữa trong 2,5 giây mới đuổi thật.
    /// Đuổi nhầm bạn chơi thì họ phải vào lại từ đầu — đáng một cú bấm xác nhận.
    /// </summary>
    private void OnKickClicked(int team, int index)
    {
        Card c = _cards[team, index];
        if (c.player == null) return;
        NetworkRunnerHandler h = NetworkRunnerHandler.Instance;
        if (h == null || !h.IsHost) return;

        if (Time.unscaledTime >= c.kickArmedUntil)
        {
            c.kickArmedUntil = Time.unscaledTime + 2.5f;
            return;
        }
        c.kickArmedUntil = 0f;
        _kickedByMe.Add(c.player.PlayerRef);   // để lúc họ biến mất thì báo "was kicked" thay vì "left"
        c.player.Kick();
        _nextRefresh = 0f;
    }

    /// <summary>Chủ phòng bấm 1v1 / 2v2. Phòng đông hơn sức chứa chế độ mới thì báo phải kick bớt.</summary>
    private void OnModeClicked(int teamSize)
    {
        NetworkRunnerHandler h = NetworkRunnerHandler.Instance;
        if (h == null || !h.IsHost) return;

        if (!RoomPlayer.TrySetTeamSize(teamSize, out string why) && !string.IsNullOrEmpty(why))
        {
            ShowNotice(why);
        }
        _nextRefresh = 0f;
    }

    /// <summary>
    /// Hiện một dòng báo ngắn dưới hàng đầu bảng, tự mờ đi sau 3,5 giây. Mặc định màu đỏ (cảnh báo);
    /// tin bình thường như "X joined" thì truyền màu khác.
    /// </summary>
    public void ShowNotice(string text, Color? color = null)
    {
        if (_notice == null) return;
        _notice.text = text;
        _notice.color = color ?? RedText;
        _noticeUntil = Time.unscaledTime + 3.5f;
    }

    private void OnCopyClicked()
    {
        NetworkRunnerHandler h = NetworkRunnerHandler.Instance;
        string code = h != null ? h.CurrentRoomCode : _code.text;
        if (string.IsNullOrEmpty(code)) return;
        GUIUtility.systemCopyBuffer = code;
        _copiedUntil = Time.unscaledTime + 1.5f;
    }

    // ==================== DỰNG GIAO DIỆN ====================

    private void Build()
    {
        if (_built) return;
        _built = true;

        // Ẩn ô chữ cũ — NetworkRunnerHandler vẫn ghi vào, chỉ là không ai thấy nữa
        NetworkRunnerHandler h = FindFirstObjectByType<NetworkRunnerHandler>(FindObjectsInactive.Include);
        RectTransform oldTitle = null;
        if (h != null)
        {
            if (h.teamRedText != null) h.teamRedText.gameObject.SetActive(false);
            if (h.teamBlueText != null) h.teamBlueText.gameObject.SetActive(false);
            if (h.roomTitleText != null) { oldTitle = h.roomTitleText.rectTransform; h.roomTitleText.gameObject.SetActive(false); }
        }

        // --- Đầu bảng: ROOM  51901  [COPY]   [1V1|2V2]   2/4 PLAYERS ---
        // (16/10) Cả hàng dịch sang trái 170 để chừa chỗ cho nút chọn chế độ.
        RectTransform panel = (RectTransform)transform;
        Vector2 headerPos = oldTitle != null ? oldTitle.anchoredPosition : new Vector2(0f, 420f);
        RectTransform header = NewRect(panel, "LV_Header", headerPos, new Vector2(900f, 90f));
        Txt(header, "Label", "ROOM", fontBold, 22, Soft, new Vector2(-420f, 0f), new Vector2(140f, 40f), 10f, TextAlignmentOptions.Right);
        _code = Txt(header, "Code", "-----", fontBlack, 64, Color.white, new Vector2(-230f, 2f), new Vector2(260f, 80f), 6f, TextAlignmentOptions.Center);

        Button copy = NewButton(header, "Copy", new Vector2(-40f, 0f), new Vector2(120f, 46f), new Color(Cyan.r, Cyan.g, Cyan.b, 0.35f));
        _copyLabel = Txt((RectTransform)copy.transform, "Label", "COPY", fontBlack, 20, Color.white, Vector2.zero, new Vector2(120f, 46f), 6f, TextAlignmentOptions.Center);
        copy.onClick.AddListener(OnCopyClicked);

        // Nút chọn chế độ dạng hai ô dính nhau. Chủ phòng bấm được, Client chỉ nhìn.
        for (int m = 1; m <= RoomPlayer.MaxTeamSize; m++)
        {
            int size = m;
            Button b = NewButton(header, "Mode" + m, new Vector2(95f + (m - 1) * 96f, 0f), new Vector2(92f, 46f), Color.white);
            // Nền trơn, không dùng khung ảnh: khung vốn tối màu, tô cyan lên nó vẫn ra tối — ô đang chọn không nổi
            ((Image)b.targetGraphic).sprite = null;
            _modeLabels[m] = Txt((RectTransform)b.transform, "Label", $"{m}V{m}", fontBlack, 20, Color.white, Vector2.zero, new Vector2(92f, 46f), 4f, TextAlignmentOptions.Center);
            ColorBlock cb = b.colors;
            cb.disabledColor = Color.white;   // Client: giữ nguyên màu đã tô, không xám đi
            b.colors = cb;
            b.onClick.AddListener(() => OnModeClicked(size));
            _modeButtons[m] = b;
        }

        _count = Txt(header, "Count", "0/4 PLAYERS", fontBold, 20, Soft, new Vector2(370f, 0f), new Vector2(200f, 40f), 6f, TextAlignmentOptions.Left);

        // Dòng báo ngắn ngay dưới hàng đầu bảng (vd. "kick bớt người trước khi đổi 1v1")
        _notice = Txt(panel, "LV_Notice", "", fontBold, 19, RedText, headerPos + new Vector2(0f, -50f), new Vector2(900f, 32f), 2f, TextAlignmentOptions.Center);
        _notice.gameObject.SetActive(false);

        // --- Hai đội ---
        BuildTeam(0, redTeamPanel);
        BuildTeam(1, blueTeamPanel);

        // --- Dòng chờ chủ phòng (Client) đặt đúng chỗ nút Start ---
        Vector2 startPos = startButton != null ? startButton.anchoredPosition : new Vector2(-480f, -330f);
        _waiting = Txt(panel, "LV_WaitingHost", "Waiting for host to start...", fontMedium, 22, Soft, startPos + new Vector2(60f, 0f),
                       new Vector2(420f, 50f), 2f, TextAlignmentOptions.Center);
        _waiting.fontStyle = FontStyles.Italic;

        // Gợi ý dưới nút Start (chỉ chủ phòng thấy) — xem cuối Show()
        _startHint = Txt(panel, "LV_StartHint", "", fontMedium, 17, Gold, startPos + new Vector2(0f, -62f),
                         new Vector2(520f, 30f), 1f, TextAlignmentOptions.Center);
        _startHint.alpha = 0.85f;
        _startHint.gameObject.SetActive(false);
    }

    private void BuildTeam(int t, RectTransform teamPanel)
    {
        if (teamPanel == null) return;
        // Khung RectTransform của ô đội to hơn phần hình nhìn thấy (cao 940 nhưng hình chỉ ~550),
        // nên giới hạn theo phần nhìn thấy — không thì tiêu đề và thẻ bị đẩy ra ngoài ô.
        Vector2 size = teamPanel.rect.size;
        float w = Mathf.Min(size.x, 690f) - 70f;
        float top = Mathf.Min(size.y * 0.5f, 275f);
        Color textCol = t == 0 ? RedText : BlueText;

        _teamHeader[t] = Txt(teamPanel, "LV_TeamHeader", t == 0 ? "RED TEAM" : "BLUE TEAM", fontBlack, 34, textCol,
                             new Vector2(0f, top - 48f), new Vector2(w, 50f), 4f, TextAlignmentOptions.Left);
        if (t == 0)
        {
            _hostHint = Txt(teamPanel, "LV_HostHint", "Host: click a player to move them", fontMedium, 17, Soft,
                            new Vector2(0f, -top + 34f), new Vector2(w, 30f), 1f, TextAlignmentOptions.Left);
            _hostHint.alpha = 0.6f;
        }

        for (int i = 0; i < RoomPlayer.MaxTeamSize; i++)
        {
            float y = top - 150f - i * 124f;
            Card c = new Card();
            c.button = NewButton(teamPanel, "LV_Card" + i, new Vector2(0f, y), new Vector2(w, 108f), Color.white);
            c.root = c.button.gameObject;
            c.bg = c.root.GetComponent<Image>();
            if (cardSprite != null) { c.bg.sprite = cardSprite; c.bg.type = Image.Type.Sliced; }

            RectTransform cr = (RectTransform)c.root.transform;
            c.bar = Img(cr, "Bar", null, Color.white, new Vector2(-w * 0.5f + 12f, 0f), new Vector2(7f, 70f));
            c.crown = Img(cr, "Crown", crownSprite, Gold, new Vector2(-w * 0.5f + 52f, 2f), new Vector2(34f, 34f));
            c.crown.preserveAspect = true;

            c.name = Txt(cr, "Name", "", fontBold, 34, Color.white, new Vector2(40f, 0f), new Vector2(w - 60f, 60f), 1f, TextAlignmentOptions.Left);
            RectTransform nrt = c.name.rectTransform;
            nrt.anchorMin = nrt.anchorMax = new Vector2(0f, 0.5f);
            nrt.pivot = new Vector2(0f, 0.5f);

            c.tag = Txt(cr, "You", "YOU", fontBlack, 18, Cyan, new Vector2(w * 0.5f - 70f, 16f), new Vector2(90f, 30f), 6f, TextAlignmentOptions.Right);
            // Mép phải thẳng hàng với chữ YOU (cách mép thẻ 28) — trước đây khung chạm đúng mép thẻ,
            // mà "MOVE TO BLUE" dài hơn khung 200 nên chữ lòi ra ngoài viền thẻ.
            c.hint = Txt(cr, "Hint", "MOVE TO BLUE", fontBold, 17, Soft, new Vector2(w * 0.5f - 28f - 120f, -20f), new Vector2(240f, 28f), 3f, TextAlignmentOptions.Right);
            c.hint.alpha = 0.7f;

            // Nút KICK (chỉ chủ phòng thấy) — đúng chỗ chữ YOU, vì thẻ người khác không có chữ đó.
            // Là nút con nằm trên thẻ nên nhận cú bấm trước thẻ: bấm KICK không bị hiểu thành "chuyển đội".
            c.kick = NewButton(cr, "Kick", new Vector2(w * 0.5f - 72f, 18f), new Vector2(96f, 32f), new Color(Red.r, Red.g, Red.b, 0.3f));
            c.kickLabel = Txt((RectTransform)c.kick.transform, "Label", "KICK", fontBlack, 16, RedText, Vector2.zero, new Vector2(96f, 32f), 4f, TextAlignmentOptions.Center);
            c.kick.gameObject.SetActive(false);
            int kt = t, ki = i;
            c.kick.onClick.AddListener(() => OnKickClicked(kt, ki));

            // Màu khi rê chuột / bấm — chỉ có tác dụng khi thẻ bấm được (Host)
            ColorBlock cb = c.button.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(0.75f, 0.92f, 1f);
            cb.pressedColor = new Color(0.55f, 0.8f, 1f);
            cb.disabledColor = Color.white;
            c.button.colors = cb;

            int team = t, index = i;
            c.button.onClick.AddListener(() => OnCardClicked(team, index));
            c.baseY = y;
            _cards[t, i] = c;
        }
    }

    // ---------- tiện ích dựng giao diện ----------

    private static RectTransform NewRect(RectTransform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    private TMP_Text Txt(RectTransform parent, string name, string text, TMP_FontAsset font, float size, Color color,
                         Vector2 pos, Vector2 box, float spacing, TextAlignmentOptions align)
    {
        // Khung phải cao hơn dòng chữ, không thì TextMeshPro cắt mất cả dòng (đã dính với mã phòng)
        box.y = Mathf.Max(box.y, size * 1.6f);
        RectTransform rt = NewRect(parent, name, pos, box);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.characterSpacing = spacing;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Ellipsis;
        t.raycastTarget = false;
        return t;
    }

    private static Image Img(RectTransform parent, string name, Sprite sprite, Color color, Vector2 pos, Vector2 size)
    {
        RectTransform rt = NewRect(parent, name, pos, size);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private Button NewButton(RectTransform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        RectTransform rt = NewRect(parent, name, pos, size);
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        if (cardSprite != null) { img.sprite = cardSprite; img.type = Image.Type.Sliced; }
        Button b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        return b;
    }

#if UNITY_EDITOR
    /// <summary>Chỉ để chụp ảnh xem trước trong Editor.</summary>
    public void PreviewShow(List<Entry> players, string code, bool asHost, int teamSize = RoomPlayer.MaxTeamSize, string notice = null)
    {
        _previewMode = true;
        Show(players, code, asHost, teamSize);
        if (!string.IsNullOrEmpty(notice)) { ShowNotice(notice); _noticeUntil = float.MaxValue; _notice.gameObject.SetActive(true); }
        else { _noticeUntil = 0f; _notice.gameObject.SetActive(false); }
    }

    /// <summary>Chỉ để chụp ảnh: nút KICK đang ở trạng thái hỏi lại "SURE?".</summary>
    public void PreviewArmKick(int team, int index)
    {
        Card c = _cards[team, index];
        if (c != null && c.kickLabel != null) c.kickLabel.text = "SURE?";
    }
#endif
}
