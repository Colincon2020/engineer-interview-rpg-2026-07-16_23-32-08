using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲームオーバー画面（EndScene）の制御。
/// Enterキーでタイトル画面へ戻る。BGMの再生も管理する。
/// </summary>
public class EndSceneController : MonoBehaviour
{
    [Header("シーン遷移")]
    [SerializeField]
    [Tooltip("戻り先のタイトルシーン名")]
    private string titleSceneName = "StartScence";

    [Header("BGM設定")]
    [SerializeField]
    [Tooltip("このシーンで再生するBGM")]
    private AudioClip bgmClip;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("BGMの音量")]
    private float bgmVolume = 0.5f;

    [SerializeField]
    [Tooltip("ループ再生するか")]
    private bool loop = true;

    [Header("効果音設定")]
    [SerializeField]
    [Tooltip("Enter押下時の効果音")]
    private AudioClip enterSE;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("効果音の音量")]
    private float seVolume = 1.0f;

    [SerializeField]
    [Tooltip("Enter押下後の待機時間（秒）")]
    private float enterWaitTime = 1.0f;

    private AudioSource audioSource;
    private AudioSource seAudioSource;
    private bool isTransitioning;

    private void Awake()
    {
        // BGM用AudioSourceをセットアップ
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = loop;

        // SE用AudioSourceをセットアップ（BGMと別に再生するため）
        seAudioSource = gameObject.AddComponent<AudioSource>();
        seAudioSource.playOnAwake = false;
        seAudioSource.loop = false;
    }

    private void Start()
    {
        // BGMを再生
        PlayBGM();
    }

    private void Update()
    {
        // 遷移中は入力を受け付けない
        if (isTransitioning)
        {
            return;
        }

        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
        {
            ReturnToTitle();
        }
    }

    /// <summary>BGMを再生する。</summary>
    public void PlayBGM()
    {
        if (bgmClip == null || audioSource == null)
        {
            return;
        }

        audioSource.clip = bgmClip;
        audioSource.volume = bgmVolume;
        audioSource.Play();
    }

    /// <summary>BGMを停止する。</summary>
    public void StopBGM()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    /// <summary>BGMの音量を設定する。</summary>
    public void SetBGMVolume(float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = Mathf.Clamp01(volume);
        }
    }

    /// <summary>効果音を再生する。</summary>
    public void PlaySE(AudioClip clip)
    {
        if (clip == null || seAudioSource == null)
        {
            return;
        }

        seAudioSource.PlayOneShot(clip, seVolume);
    }

    /// <summary>タイトル画面へ遷移する。</summary>
    public void ReturnToTitle()
    {
        if (isTransitioning)
        {
            return;
        }

        isTransitioning = true;

        // 次回タイトルで性別を選び直せるようにする。
        GameSession.Reset();

        // 効果音を再生
        if (enterSE != null)
        {
            PlaySE(enterSE);
        }

        // 1秒待ってからシーン遷移
        Invoke(nameof(DoSceneTransition), enterWaitTime);
    }

    /// <summary>実際のシーン遷移処理。</summary>
    private void DoSceneTransition()
    {
        StopBGM();
        SceneManager.LoadScene(titleSceneName);
    }
}
