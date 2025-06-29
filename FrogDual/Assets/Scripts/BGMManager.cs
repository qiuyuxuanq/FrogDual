using UnityEngine;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip[] bgmClips;
    public int currentTrackIndex = 0;

    [Header("Menu BGM")]
    public AudioClip menuBGM;  // 菜单BGM（西部2）

    [Header("Game BGM")]
    public AudioClip gameBGM;  // 游戏BGM（西部无前奏）

    void Awake()
    {
        // 单例管理
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 保留跨场景
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 开始时播放菜单BGM
        PlayMenuBGM();
    }

    public void PlayBGM(int index)
    {
        if (index >= 0 && index < bgmClips.Length)
        {
            audioSource.clip = bgmClips[index];
            audioSource.loop = true;
            audioSource.Play();
            currentTrackIndex = index;
            Debug.Log($"🎵 播放BGM轨道: {index}");
        }
    }

    public void StopBGM()
    {
        audioSource.Stop();
        Debug.Log("🔇 停止BGM播放");
    }

    public void NextTrack()
    {
        int next = (currentTrackIndex + 1) % bgmClips.Length;
        PlayBGM(next);
    }

    /// <summary>
    /// 播放菜单BGM（西部2）
    /// </summary>
    public void PlayMenuBGM()
    {
        if (menuBGM != null)
        {
            audioSource.clip = menuBGM;
            audioSource.loop = true;
            audioSource.Play();
            Debug.Log("🎵 播放菜单BGM: 西部2");
        }
        else
        {
            Debug.LogWarning("⚠️ 菜单BGM未设置！请在Inspector中设置menuBGM");
        }
    }

    /// <summary>
    /// 播放游戏BGM（西部无前奏）
    /// </summary>
    public void PlayGameBGM()
    {
        if (gameBGM != null)
        {
            audioSource.clip = gameBGM;
            audioSource.loop = true;
            audioSource.Play();
            Debug.Log("🎵 播放游戏BGM: 西部无前奏");
        }
        else
        {
            Debug.LogWarning("⚠️ 游戏BGM未设置！请在Inspector中设置gameBGM");
        }
    }

    /// <summary>
    /// 切换到游戏BGM（从菜单切换到游戏时调用）
    /// </summary>
    public void SwitchToGameBGM()
    {
        StopBGM();
        PlayGameBGM();
    }

    /// <summary>
    /// 切换到菜单BGM（返回菜单时调用）
    /// </summary>
    public void SwitchToMenuBGM()
    {
        StopBGM();
        PlayMenuBGM();
    }
}
