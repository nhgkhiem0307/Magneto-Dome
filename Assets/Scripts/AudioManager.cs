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
    public AudioClip sfxCharge;       // nạp điện cho vật trung tính
    public AudioClip sfxLaunch;       // đẩy hoặc bắn vật đi
    public AudioClip sfxExplosion;    // TNT nổ
    public AudioClip sfxConvertTNT;   // chai xăng chế vật thành TNT

    [Header("Vòng đấu")]
    public AudioClip sfxBuyPhase;     // bắt đầu pha chuẩn bị
    public AudioClip sfxRoundStart;   // rào hạ, vào chiến đấu
    public AudioClip sfxRoundWin;
    public AudioClip sfxRoundLose;

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

    public static void Charge(Vector3 pos) { if (Instance != null) Instance.PlayAt(Instance.sfxCharge, pos); }
    public static void Launch(Vector3 pos) { if (Instance != null) Instance.PlayAt(Instance.sfxLaunch, pos); }
    public static void Explosion(Vector3 pos) { if (Instance != null) Instance.PlayAt(Instance.sfxExplosion, pos, 1.2f); }
    public static void ConvertTNT(Vector3 pos) { if (Instance != null) Instance.PlayAt(Instance.sfxConvertTNT, pos); }

    public static void BuyPhase() { if (Instance != null) Instance.PlayFlat(Instance.sfxBuyPhase); }
    public static void RoundStart() { if (Instance != null) Instance.PlayFlat(Instance.sfxRoundStart); }
    public static void RoundWin() { if (Instance != null) Instance.PlayFlat(Instance.sfxRoundWin); }
    public static void RoundLose() { if (Instance != null) Instance.PlayFlat(Instance.sfxRoundLose); }

    public static void ButtonClick() { if (Instance != null) Instance.PlayFlat(Instance.sfxButtonClick); }
    public static void Buy() { if (Instance != null) Instance.PlayFlat(Instance.sfxBuy); }
    public static void UseItem() { if (Instance != null) Instance.PlayFlat(Instance.sfxUseItem); }
    public static void Error() { if (Instance != null) Instance.PlayFlat(Instance.sfxError); }
}
