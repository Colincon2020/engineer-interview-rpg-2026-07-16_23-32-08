using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// タイトル画面の制御。
/// タイトル表示 → 性別選択 → 特訓シーンへ進む。
/// </summary>
public class StartSceneController : MonoBehaviour
{
    [Header("シーン遷移")]
    [SerializeField]
    private string gameSceneName = "ActionScene";

    [Header("性別選択（未設定なら実行時に生成）")]
    [SerializeField]
    private Button maleButton;

    [SerializeField]
    private Button femaleButton;

    [SerializeField]
    private Image selectionHighlight;

    [SerializeField]
    private TMP_Text instructionText;

    [SerializeField]
    private Color selectedColor = new Color(1f, 0.92f, 0.4f, 1f);

    [SerializeField]
    private Color normalColor = Color.white;

    [Header("画面切替")]
    public GameObject[] titleElements;
    public GameObject[] genderSelectElements;

    [Header("BGM設定")]
    [SerializeField]
    [Tooltip("タイトル画面で再生するBGM")]
    private AudioClip bgmClip;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("BGMの音量")]
    private float bgmVolume = 0.5f;

    [SerializeField]
    [Tooltip("ループ再生するか")]
    private bool bgmLoop = true;

    [Header("効果音設定")]
    [SerializeField]
    [Tooltip("クリック・決定時の効果音")]
    private AudioClip clickSE;

    [SerializeField]
    [Tooltip("選択切替時の効果音")]
    private AudioClip selectSE;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("効果音の音量")]
    private float seVolume = 1.0f;

    [SerializeField]
    [Tooltip("シーン遷移前の待機時間（秒）")]
    private float transitionWaitTime = 0.5f;

    private PlayerGender selectedGender = PlayerGender.Male;
    private bool isTransitioning;
    private AudioSource bgmAudioSource;
    private AudioSource seAudioSource;

    private void Awake()
    {
        SetupAudioSources();
        WireButtons();
        ApplySelectionVisual();
    }

    private void Start()
    {
        PlayBGM();
    }

    private void SetupAudioSources()
    {
        // BGM用AudioSourceをセットアップ
        bgmAudioSource = GetComponent<AudioSource>();
        if (bgmAudioSource == null)
        {
            bgmAudioSource = gameObject.AddComponent<AudioSource>();
        }

        bgmAudioSource.playOnAwake = false;
        bgmAudioSource.loop = bgmLoop;

        // SE用AudioSourceをセットアップ
        seAudioSource = gameObject.AddComponent<AudioSource>();
        seAudioSource.playOnAwake = false;
        seAudioSource.loop = false;
    }

    /// <summary>BGMを再生する。</summary>
    public void PlayBGM()
    {
        if (bgmClip == null || bgmAudioSource == null)
        {
            return;
        }

        bgmAudioSource.clip = bgmClip;
        bgmAudioSource.volume = bgmVolume;
        bgmAudioSource.Play();
    }

    /// <summary>BGMを停止する。</summary>
    public void StopBGM()
    {
        if (bgmAudioSource != null)
        {
            bgmAudioSource.Stop();
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

    /// <summary>クリック・決定効果音を再生する。</summary>
    private void PlayClickSE()
    {
        PlaySE(clickSE);
    }

    /// <summary>選択切替効果音を再生する。</summary>
    private void PlaySelectSE()
    {
        PlaySE(selectSE);
    }

    private void Update()
    {
        if (isTransitioning)
        {
            return;
        }

        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        // タイトル表示中は Enter / Space で性別選択へ
        if (IsTitleVisible())
        {
            if (keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame
                || keyboard.spaceKey.wasPressedThisFrame)
            {
                ShowGenderSelect();
            }

            return;
        }

        if (keyboard.digit1Key.wasPressedThisFrame
            || keyboard.leftArrowKey.wasPressedThisFrame
            || keyboard.aKey.wasPressedThisFrame)
        {
            PlaySelectSE();
            SelectMale();
        }
        else if (keyboard.digit2Key.wasPressedThisFrame
                 || keyboard.rightArrowKey.wasPressedThisFrame
                 || keyboard.dKey.wasPressedThisFrame)
        {
            PlaySelectSE();
            SelectFemale();
        }
        else if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
        {
            PlayClickSE();
            ConfirmAndStart();
        }
    }

    /// <summary>タイトル要素を隠し、性別選択 UI を表示する。</summary>
    public void ShowGenderSelect()
    {
        PlayClickSE();

        if (titleElements != null)
        {
            foreach (GameObject obj in titleElements)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }

        if (genderSelectElements != null)
        {
            foreach (GameObject obj in genderSelectElements)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }
        }
    }

    /// <summary>男性を選択する（UI ボタンからも呼べる）。</summary>
    public void SelectMale()
    {
        selectedGender = PlayerGender.Male;
        ApplySelectionVisual();
    }

    /// <summary>女性を選択する（UI ボタンからも呼べる）。</summary>
    public void SelectFemale()
    {
        selectedGender = PlayerGender.Female;
        ApplySelectionVisual();
    }

    /// <summary>シーン上の男性ボタンから呼ばれる。</summary>
    public void OnSelectMale()
    {
        PlayClickSE();
        selectedGender = PlayerGender.Male;
        ApplySelectionVisual();
        PersistGenderAndLoadScene(PlayerGender.Male);
    }

    /// <summary>シーン上の女性ボタンから呼ばれる。</summary>
    public void OnSelectFemale()
    {
        PlayClickSE();
        selectedGender = PlayerGender.Female;
        ApplySelectionVisual();
        PersistGenderAndLoadScene(PlayerGender.Female);
    }

    /// <summary>選択した性別を保存し、特訓シーンへ遷移する。</summary>
    public void ConfirmAndStart()
    {
        PersistGenderAndLoadScene(selectedGender);
    }

    private void PersistGenderAndLoadScene(PlayerGender gender)
    {
        if (isTransitioning)
        {
            return;
        }

        isTransitioning = true;
        GameSession.SetSelectedGender(gender);

        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.SelectedGender = gender == PlayerGender.Female
                ? GameDataManager.Gender.Female
                : GameDataManager.Gender.Male;
        }

        // 効果音が再生されてからシーン遷移
        Invoke(nameof(DoSceneTransition), transitionWaitTime);
    }

    /// <summary>実際のシーン遷移処理。</summary>
    private void DoSceneTransition()
    {
        StopBGM();
        SceneTransition.Load(gameSceneName);
    }

    private bool IsTitleVisible()
    {
        return titleElements != null
            && titleElements.Length > 0
            && titleElements[0] != null
            && titleElements[0].activeSelf;
    }

    private void WireButtons()
    {
        if (maleButton != null)
        {
            maleButton.onClick.RemoveListener(SelectMale);
            maleButton.onClick.AddListener(SelectMale);
        }

        if (femaleButton != null)
        {
            femaleButton.onClick.RemoveListener(SelectFemale);
            femaleButton.onClick.AddListener(SelectFemale);
        }
    }

    private void ApplySelectionVisual()
    {
        if (maleButton != null)
        {
            SetButtonColor(maleButton, selectedGender == PlayerGender.Male ? selectedColor : normalColor);
        }

        if (femaleButton != null)
        {
            SetButtonColor(femaleButton, selectedGender == PlayerGender.Female ? selectedColor : normalColor);
        }

        if (instructionText != null)
        {
            string genderLabel = selectedGender == PlayerGender.Female ? "女性" : "男性";
            instructionText.text = $"主人公: {genderLabel}\n←→ または 1/2 で選択 / Enter で開始";
        }
    }

    private static void SetButtonColor(Button button, Color color)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.selectedColor = color;
        colors.highlightedColor = color;
        button.colors = colors;

        Image image = button.targetGraphic as Image;
        if (image != null)
        {
            image.color = color;
        }
    }

}
