using UnityEngine;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance;

    public AudioSource audioSource;
    public AudioClip[] bgmClips;
    public int currentTrackIndex = 0;

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

        // 自动播放第一首 BGM
        if (bgmClips.Length > 0)
        {
            PlayBGM(currentTrackIndex);
        }
    }

    public void PlayBGM(int index)
    {
        if (index >= 0 && index < bgmClips.Length)
        {
            audioSource.clip = bgmClips[index];
            audioSource.loop = true;
            audioSource.Play();
            currentTrackIndex = index;
        }
    }

    public void StopBGM()
    {
        audioSource.Stop();
    }

    public void NextTrack()
    {
        int next = (currentTrackIndex + 1) % bgmClips.Length;
        PlayBGM(next);
    }
}
