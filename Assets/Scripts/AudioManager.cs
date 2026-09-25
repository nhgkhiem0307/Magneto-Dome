using UnityEngine;
using UnityEngine.SceneManagement; // tu doi nhac theo scene

/// <summary>
/// Quản lý toàn bộ âm thanh: nhạc nền và hiệu ứng.
///
/// Đặt trên một GameObject trong MenuScene, sẽ tự sống xuyên các scene.
///
/// NGUYÊN TẮC: KHÔNG truyền âm thanh qua mạng.
/// Mọi tiếng động đều được móc vào các hàm OnChangedRender sẵn có — những hàm đó
/// vốn đã chạy trên MỌI máy mỗi khi trạng thái [Networked] thay đổi. Nhờ vậy ai cũng
/// nghe thấy mà không tốn thêm một byte băng thông nào.
///
/// CẢNH BÁO: đừng bao giờ gọi các hàm phát tiếng từ FixedUpdateNetwork().
/// Fusion tua lại (resimulation) nhiều tick mỗi khung hình, âm thanh sẽ kêu chồng lên nhau.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Nhạc nền")]
    [Tooltip("Tên scene menu. Vào scene này thì phát menuMusic, mọi scene khác phát gameMusic.")]
    public string menuSceneName = "MenuScene";

    public AudioClip menuMusic;
    public AudioClip gameMusic;

    [Header("Nhân vật")]
    public AudioClip sfxDash;
    public AudioClip sfxHit;          // trúng đòn, mất máu
    public AudioClip sfxArmorHit;     // giáp chặn được
    public AudioClip sfxDeath;
    public AudioClip sfxMeleePunch;   // đấm cận chiến

    [Header("Vật thể từ tính")]
    public AudioClip sfxLaser;        // tia laze bắn ra từ găng, nhắm vào vật chưa có điện
    public AudioClip sfxCharge;       // nạp điện cho vật trung tính
    public AudioClip sfxLaunch;       // đẩy hoặc bắn vật đi
    public AudioClip sfxExplosion;    // TNT nổ
    public AudioClip sfxConvertTNT;   // chai xăng chế vật thành TNT

    [Header("Vòng đấu")]
    public AudioClip sfxBuyPhase;     // bắt đầu pha chuẩn bị
    public AudioClip sfxRoundStart;   // rào hạ, vào chiến đấu
    public AudioClip sfxRoundWin;
    public AudioClip sfxRoundLose;

    // ==================== GIỌNG THÔNG BÁO ====================
    //
    // Nhân vật ở đây là "AI điều khiển đấu trường", không phải MC thể thao: đọc bình thản,
    // không reo mừng, không tiếc nuối. Cảm xúc thắng thua để cho nhạc và hiệu ứng lo.
    //
    // Giọng đi qua NGUỒN PHÁT RIÊNG (_voiceSource) chứ không dùng chung với tiếng giao
    // diện, vì nó cần bộ lọc "loa vô tuyến" - dùng chung thì tiếng bấm nút và tiếng mua
    // hàng cũng bị rè theo.

    [Header("Giọng thông báo")]
    [Tooltip("Sinh sẵn trong Assets/Audio/VO/ bằng gen_vo.ps1. Để trống ô nào thì câu đó " +
             "im lặng, không lỗi.")]
    public AudioClip voBuyPhase;      // "Buy phase. Arm yourself."
    public AudioClip voCombat;        // "Combat engaged."
    public AudioClip voRoundWon;      // "Round won."
    public AudioClip voRoundLost;     // "Round lost."
    public AudioClip voMatchPoint;    // "Match point."
    public AudioClip voOvertime;      // "Overtime."
    public AudioClip voVictory;       // "Victory."
    public AudioClip voDefeat;        // "Defeat."

    [Tooltip("Hai câu TUỲ CHỌN, để trống thì im lặng, không lỗi. Tạo thêm trên ElevenLabs " +
             "bằng đúng giọng cũ rồi kéo vào là chạy.")]
    public AudioClip voChargeCritical;   // "Charge critical."  - điện tích vượt ngưỡng nguy hiểm
    public AudioClip voTenSeconds;       // "Ten seconds."      - sắp hết giờ pha chiến đấu

    [Tooltip("Âm lượng riêng của giọng, nhân thêm vào âm lượng hiệu ứng chung.\n\n" +
             "Để nhỉnh hơn 1 một chút: giọng nói phải nghe rõ ĐÈ LÊN tiếng nổ, không thì " +
             "đúng lúc hỗn chiến - lúc cần nghe nhất - lại không nghe ra chữ gì.")]
    [Range(0f, 2f)]
    public float voiceVolume = 1.15f;

    [Tooltip("Tốc độ đọc riêng của câu 'Round won.' — 1 là giữ nguyên, nhỏ hơn là chậm lại.\n\n" +
             "⚠️ CHẬM LẠI THÌ GIỌNG CŨNG TRẦM XUỐNG. Unity chỉ có một núm điều khiển cho cả " +
             "tốc độ lẫn cao độ, giống hệt việc quay đĩa than chậm lại. 0.92 là chậm 8%, " +
             "tương đương hạ hơn một nửa cung - nghe dày và bệ vệ hơn, thường là hợp với " +
             "nhân vật này. Xuống dưới 0.85 thì bắt đầu nghe ra là bị kéo chậm.\n\n" +
             "Muốn chậm mà KHÔNG trầm đi thì phải tạo lại câu đó: gõ 'Round... won.' có ba " +
             "chấm ở giữa, máy sẽ tự đọc chậm và ngắt đúng chỗ.")]
    [Range(0.7f, 1.2f)]
    public float roundWonSpeed = 0.92f;

    [Header("Lọc giọng thành loa vô tuyến")]
    [Tooltip("Cắt bớt tiếng trầm và tiếng siêu cao, thêm chút rè - nghe như loa thông báo " +
             "trong đấu trường chứ không như máy đọc chữ.\n\n" +
             "Đây là thứ biến khiếm khuyết của giọng máy thành CHỦ Ý: tai người nghe loa " +
             "méo thì không còn so sánh nó với giọng người thật nữa.")]
    public bool radioFilter = true;

    [Tooltip("Cắt bỏ tần số dưới mức này (Hz). Cao thì giọng mỏng và 'điện thoại' hơn.")]
    public float radioHighPass = 380f;

    [Tooltip("Cắt bỏ tần số trên mức này (Hz). Thấp thì càng giống loa cũ.")]
    public float radioLowPass = 4800f;

    [Tooltip("Độ rè. Nhẹ thôi - mạnh quá thì không nghe ra chữ.")]
    [Range(0f, 1f)]
    public float radioDistortion = 0.22f;

    [Header("Giao diện")]
    public AudioClip sfxButtonClick;
    public AudioClip sfxBuy;          // mua hàng thành công
    public AudioClip sfxUseItem;
    public AudioClip sfxError;        // thao tác bị từ chối

    // --- ÂM LƯỢNG (0 đến 1) ---
    // Lưu vào PlayerPrefs để nhớ giữa các lần chơi.
    private const string KeyMaster = "vol_master";
    private const string KeyMusic = "vol_music";
    private const string KeySfx = "vol_sfx";

    public float MasterVolume { get; private set; } = 1f;
    public float MusicVolume { get; private set; } = 0.5f;
    public float SfxVolume { get; private set; } = 1f;

    private AudioSource _musicSource;
    private AudioSource _uiSource;

    // Nguồn phát giọng thông báo và hàng đợi của nó - xem PlayVoice().
    private AudioSource _voiceSource;

    // Mỗi câu trong hàng đợi mang theo tốc độ đọc riêng của nó, vì tốc độ phải được đặt
    // ĐÚNG LÚC câu đó bắt đầu phát - đặt sớm thì câu đang phát dở bị đổi giọng giữa chừng.
    private struct VoiceItem
    {
        public AudioClip Clip;
        public float Speed;
    }

    private readonly System.Collections.Generic.Queue<VoiceItem> _voiceQueue
        = new System.Collections.Generic.Queue<VoiceItem>();

    void Awake()
    {
        // Chỉ giữ một bản duy nhất, sống xuyên scene
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Nguồn phát nhạc nền: lặp vô hạn, không theo vị trí trong không gian
        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.loop = true;
        _musicSource.playOnAwake = false;
        _musicSource.spatialBlend = 0f;

        // Nguồn phát tiếng giao diện: cũng 2D, nghe đều ở mọi hướng
        _uiSource = gameObject.AddComponent<AudioSource>();
        _uiSource.playOnAwake = false;
        _uiSource.spatialBlend = 0f;

        BuildVoiceSource();

        LoadVolumes();

        // TỰ ĐỔI NHẠC THEO SCENE (thêm 16/08).
        //
        // Trước đây PlayMenuMusic() và PlayGameMusic() có tồn tại nhưng KHÔNG AI GỌI,
        // nên game im lặng hoàn toàn dù clip đã gán đầy đủ.
        //
        // Móc vào sự kiện đổi scene thay vì đi rải lời gọi ở từng nơi: chỉ có một chỗ
        // quyết định nhạc nào chạy, và thêm scene mới sau này cũng không phải nhớ gì.
        SceneManager.sceneLoaded += HandleSceneLoaded;

        // Sự kiện trên KHÔNG bắn cho scene đang mở sẵn lúc này, nên phải tự gọi một lần.
        ApplyMusicForScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Dựng nguồn phát riêng cho giọng, kèm bộ lọc loa vô tuyến.
    ///
    /// Dựng bằng code chứ không bắt kéo thả trong Unity: ba bộ lọc phải nằm ĐÚNG THỨ TỰ
    /// trên cùng một object và chỉ được ăn vào nguồn giọng. Đặt tay thì rất dễ gắn nhầm
    /// vào object chung và làm rè cả nhạc nền.
    ///
    /// ⚠️ Bộ lọc của Unity ăn vào MỌI AudioSource nằm trên CÙNG GameObject, nên giọng
    /// phải ở một object con riêng - để chung với nhạc và tiếng giao diện là rè hết.
    /// </summary>
    private void BuildVoiceSource()
    {
        GameObject host = new GameObject("VoiceSource");
        host.transform.SetParent(transform, false);

        _voiceSource = host.AddComponent<AudioSource>();
        _voiceSource.playOnAwake = false;
        _voiceSource.loop = false;
        _voiceSource.spatialBlend = 0f;   // nghe đều, không theo vị trí trong thế giới

        if (!radioFilter) return;

        AudioHighPassFilter high = host.AddComponent<AudioHighPassFilter>();
        high.cutoffFrequency = radioHighPass;

        AudioLowPassFilter low = host.AddComponent<AudioLowPassFilter>();
        low.cutoffFrequency = radioLowPass;

        AudioDistortionFilter dist = host.AddComponent<AudioDistortionFilter>();
        dist.distortionLevel = radioDistortion;
    }

    /// <summary>
    /// Phát một câu thông báo. Đang có câu khác thì XẾP HÀNG chờ, không cắt ngang.
    ///
    /// Vì sao phải xếp hàng: có lúc hai câu đến gần như cùng lúc - ví dụ vào pha mua đồ
    /// ("Buy phase") của round quyết định ("Match point"). Phát chồng thì hai giọng đè
    /// lên nhau và không nghe ra câu nào cả.
    /// </summary>
    private void PlayVoice(AudioClip clip, float speed = 1f)
    {
        if (clip == null || _voiceSource == null) return;

        _voiceQueue.Enqueue(new VoiceItem { Clip = clip, Speed = speed });
        if (!_voiceSource.isPlaying) PlayNextVoice();
    }

    private void PlayNextVoice()
    {
        if (_voiceQueue.Count == 0) return;

        VoiceItem item = _voiceQueue.Dequeue();

        _voiceSource.clip = item.Clip;
        _voiceSource.volume = MasterVolume * SfxVolume * voiceVolume;

        // pitch của Unity điều khiển CẢ tốc độ lẫn cao độ cùng lúc, không tách được.
        _voiceSource.pitch = Mathf.Clamp(item.Speed, 0.5f, 2f);

        _voiceSource.Play();
    }

    private void Update()
    {
        // Câu vừa dứt thì lấy câu kế tiếp trong hàng đợi.
        if (_voiceSource != null && !_voiceSource.isPlaying && _voiceQueue.Count > 0)
        {
            PlayNextVoice();
        }
    }

    private void OnDestroy()
    {
        // Chỉ bản chính mới từng đăng ký. Bản trùng bị Destroy ngay trong Awake
        // thì chưa kịp đăng ký gì, gỡ ở đây sẽ gỡ nhầm của bản chính.
        if (Instance == this) SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyMusicForScene(scene.name);
    }

    /// <summary>
    /// Vào scene menu thì phát nhạc menu, mọi scene khác phát nhạc trong trận.
    ///
    /// PlayMusic() tự bỏ qua nếu đang phát đúng bản đó rồi, nên gọi lại nhiều lần
    /// cũng không làm nhạc bị cắt và chạy lại từ đầu.
    /// </summary>
    private void ApplyMusicForScene(string sceneName)
    {
        PlayMusic(sceneName == menuSceneName ? menuMusic : gameMusic);
    }

    // ==================== ÂM LƯỢNG ====================

    private void LoadVolumes()
    {
        MasterVolume = PlayerPrefs.GetFloat(KeyMaster, 1f);
        MusicVolume = PlayerPrefs.GetFloat(KeyMusic, 0.5f);
        SfxVolume = PlayerPrefs.GetFloat(KeySfx, 1f);

        ApplyMusicVolume();
    }

    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(KeyMaster, MasterVolume);
        ApplyMusicVolume();
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(KeyMusic, MusicVolume);
        ApplyMusicVolume();
    }

    public void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(KeySfx, SfxVolume);
    }

    private void ApplyMusicVolume()
    {
        if (_musicSource != null) _musicSource.volume = MasterVolume * MusicVolume;
    }

    // ==================== PHÁT ÂM THANH ====================

    /// <summary>Phát nhạc nền. Gọi lại cùng bản nhạc đang chạy thì bỏ qua.</summary>
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || _musicSource == null) return;
        if (_musicSource.clip == clip && _musicSource.isPlaying) return;

        _musicSource.clip = clip;
        ApplyMusicVolume();
        _musicSource.Play();
    }

    public void StopMusic()
    {
        if (_musicSource != null) _musicSource.Stop();
    }

    /// <summary>Phát tiếng không gắn với vị trí nào (giao diện, thông báo).</summary>
    private void PlayFlat(AudioClip clip, float scale = 1f)
    {
        if (clip == null || _uiSource == null) return;

        _uiSource.PlayOneShot(clip, MasterVolume * SfxVolume * scale);
    }

    /// <summary>Phát tiếng tại một vị trí trong thế giới, nghe xa gần theo khoảng cách.</summary>
    private void PlayAt(AudioClip clip, Vector3 position, float scale = 1f)
    {
        if (clip == null) return;

        AudioSource.PlayClipAtPoint(clip, position, MasterVolume * SfxVolume * scale);
    }

    // ==================== HÀM TĨNH CHO CÁC NƠI GỌI ====================
    //
    // Tất cả đều tự kiểm tra Instance, nên nơi gọi chỉ cần một dòng, không cần null-check.
    // Nếu chưa có AudioManager trong scene (ví dụ bấm Play thẳng vào TestScene)
    // thì các hàm này đơn giản là không làm gì cả, không gây lỗi.

    public static void PlayMenuMusic() { if (Instance != null) Instance.PlayMusic(Instance.menuMusic); }
    public static void PlayGameMusic() { if (Instance != null) Instance.PlayMusic(Instance.gameMusic); }

    public static void Dash(Vector3 pos) { if (Instance != null) Instance.PlayAt(Instance.sfxDash, pos); }
    public static void Hit(Vector3 pos) { if (Instance != null) Instance.PlayAt(Instance.sfxHit, pos); }
    public static void ArmorHit(Vector3 pos) { if (Instance != null) Instance.PlayAt(Instance.sfxArmorHit, pos); }
    public static void Death(Vector3 pos) { if (Instance != null) Instance.PlayAt(Instance.sfxDeath, pos); }
    public static void MeleePunch(Vector3 pos) { if (Instance != null) Instance.PlayAt(Instance.sfxMeleePunch, pos); }

    public static void Laser(Vector3 pos) { if (Instance != null) Instance.PlayAt(Instance.sfxLaser, pos); }
    public static void Charge(Vector3 pos) { if (Instance != null) Instance.PlayAt(Instance.sfxCharge, pos); }
    public static void Launch(Vector3 pos) { if (Instance != null) Instance.PlayAt(Instance.sfxLaunch, pos); }
    public static void Explosion(Vector3 pos) { if (Instance != null) Instance.PlayAt(Instance.sfxExplosion, pos, 1.2f); }
    public static void ConvertTNT(Vector3 pos) { if (Instance != null) Instance.PlayAt(Instance.sfxConvertTNT, pos); }

    // Mỗi mốc của trận phát HAI thứ: tiếng hiệu (chuông, còi...) rồi tới giọng đọc.
    // Ô nào để trống thì phần đó im, nên gán một trong hai cũng chạy được.
    public static void BuyPhase()
    {
        if (Instance == null) return;
        Instance.PlayFlat(Instance.sfxBuyPhase);
        Instance.PlayVoice(Instance.voBuyPhase);
    }

    public static void RoundStart()
    {
        if (Instance == null) return;
        Instance.PlayFlat(Instance.sfxRoundStart);
        Instance.PlayVoice(Instance.voCombat);
    }

    public static void RoundWin()
    {
        if (Instance == null) return;
        Instance.PlayFlat(Instance.sfxRoundWin);
        Instance.PlayVoice(Instance.voRoundWon, Instance.roundWonSpeed);
    }

    public static void RoundLose()
    {
        if (Instance == null) return;
        Instance.PlayFlat(Instance.sfxRoundLose);
        Instance.PlayVoice(Instance.voRoundLost);
    }

    /// <summary>"Charge critical." — CHỈ phát trên máy của chính người đang nhiễm nặng.</summary>
    public static void ChargeCritical() { if (Instance != null) Instance.PlayVoice(Instance.voChargeCritical); }

    /// <summary>"Ten seconds." — sắp hết giờ pha chiến đấu.</summary>
    public static void TenSeconds() { if (Instance != null) Instance.PlayVoice(Instance.voTenSeconds); }

    /// <summary>"Match point." — round tới là có thể phân định thắng thua chung cuộc.</summary>
    public static void MatchPoint() { if (Instance != null) Instance.PlayVoice(Instance.voMatchPoint); }

    /// <summary>"Overtime." — hoà ở mốc điểm quy định, đấu tiếp tới khi đủ cách biệt.</summary>
    public static void Overtime() { if (Instance != null) Instance.PlayVoice(Instance.voOvertime); }

    /// <summary>Kết thúc trận: thắng thì "Victory.", thua thì "Defeat."</summary>
    public static void MatchResult(bool won)
    {
        if (Instance == null) return;
        Instance.PlayVoice(won ? Instance.voVictory : Instance.voDefeat);
    }

    public static void ButtonClick() { if (Instance != null) Instance.PlayFlat(Instance.sfxButtonClick); }
    public static void Buy() { if (Instance != null) Instance.PlayFlat(Instance.sfxBuy); }
    public static void UseItem() { if (Instance != null) Instance.PlayFlat(Instance.sfxUseItem); }
    public static void Error() { if (Instance != null) Instance.PlayFlat(Instance.sfxError); }
}
