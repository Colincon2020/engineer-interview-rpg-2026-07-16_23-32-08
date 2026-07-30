using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ActionScene の練習 UI を制御する。
/// ドロップダウンで選んだ言語を練習し、スキル一覧・スタミナ・メンタルへ結果を反映する。
/// </summary>
public class ActionSceneController : MonoBehaviour
{
    /// <summary>この値以下で夕背景（背景_夕）にするスタミナ閾値（2〜3）。</summary>
    private const int EveningStaminaThreshold = 3;

    /// <summary>この値以下で夜背景（背景_夜）にするスタミナ閾値。</summary>
    private const int NightStaminaThreshold = 1;

    /// <summary>特訓終了後に遷移する面接シーン名。</summary>
    private const string InterviewSceneName = "InterviewScene";

    [SerializeField]
    private Player player;

    [SerializeField]
    private SkillDropdown skillDropdown;

    [SerializeField]
    private Button practiceButton;

    [SerializeField]
    private Button sleepButton;

    [SerializeField]
    private SkillSheetUI skillSheetUI;

    [SerializeField]
    private TMP_Text staminaValueText;

    [SerializeField]
    private TMP_Text mentalValueText;

    [SerializeField]
    private TMP_Text timeText;

    [SerializeField]
    private TMP_Text daysText;

    private const string MorningLabel = "朝 AM";
    private const string EveningLabel = "夕 PM";
    private const string NightLabel = "夜";

    /// <summary>CurrentDay（1〜7）に対応する曜日表示。</summary>
    private static readonly string[] WeekdayLabels =
    {
        string.Empty,
        "月曜日",
        "火曜日",
        "水曜日",
        "木曜日",
        "金曜日",
        "土曜日",
        "日曜日",
    };

    [Header("背景切替")]
    [SerializeField]
    private Image backgroundImage;

    [SerializeField]
    private Sprite eveningBackground;

    [SerializeField]
    private Sprite nightBackground;

    /// <summary>シーン開始時の背景（スタミナが閾値より高いときの復帰用）。</summary>
    private Sprite defaultBackground;

    [Header("キャラ表情切替")]
    [SerializeField]
    private Image characterImage;

    [Header("キャラ表情切替（男性）")]
    [SerializeField]
    private Sprite maleNormalFace;

    [SerializeField]
    private Sprite maleTiredFace;

    [SerializeField]
    private Sprite maleExhaustedFace;

    [Header("キャラ表情切替（女性）")]
    [SerializeField]
    private Sprite femaleNormalFace;

    [SerializeField]
    private Sprite femaleTiredFace;

    [SerializeField]
    private Sprite femaleExhaustedFace;

    [Header("デバッグ（ActionScene単体テスト用）")]
    [SerializeField]
    [Tooltip("ON のとき GameSession を無視し、下の Test Gender を使う（Editor のみ）")]
    private bool overrideGenderForTest;

    [SerializeField]
    private PlayerGender testGender = PlayerGender.Female;

    private void Awake()
    {
        ResolveReferences();

        if (player == null)
        {
            player = gameObject.AddComponent<Player>();
        }

        // タイトルで選んだ性別を Player へ反映する。
        ApplySessionGender();

        if (skillSheetUI != null)
        {
            skillSheetUI.Bind(player);
        }

        if (practiceButton != null)
        {
            practiceButton.onClick.AddListener(OnPracticeClicked);
        }

        if (sleepButton != null)
        {
            sleepButton.onClick.AddListener(OnSleepClicked);
        }

        if (backgroundImage != null)
        {
            defaultBackground = backgroundImage.sprite;
        }
    }

    private void OnEnable()
    {
        if (player != null)
        {
            player.StateChanged += RefreshStatusTexts;
            player.WeekFinished += OnWeekFinished;
        }

        RefreshStatusTexts();
    }

    private void OnDisable()
    {
        if (player != null)
        {
            player.StateChanged -= RefreshStatusTexts;
            player.WeekFinished -= OnWeekFinished;
        }
    }

    private void OnDestroy()
    {
        if (practiceButton != null)
        {
            practiceButton.onClick.RemoveListener(OnPracticeClicked);
        }

        if (sleepButton != null)
        {
            sleepButton.onClick.RemoveListener(OnSleepClicked);
        }
    }

    /// <summary>練習開始ボタンから呼ばれる。</summary>
    public void OnPracticeClicked()
    {
        if (player == null)
        {
            Debug.LogWarning("ActionSceneController: Player がありません。");
            return;
        }

        if (skillDropdown == null)
        {
            Debug.LogWarning("ActionSceneController: SkillDropdown がありません。");
            return;
        }

        SkillType skill = skillDropdown.GetSelectedSkill();
        bool success = player.Practice(skill);
        if (!success)
        {
            Debug.Log("練習できませんでした（資源不足、または特訓期間終了）。");
        }
    }

    /// <summary>就寝ボタンから呼ばれる。</summary>
    public void OnSleepClicked()
    {
        if (player == null)
        {
            Debug.LogWarning("ActionSceneController: Player がありません。");
            return;
        }

        player.Sleep();
    }

    /// <summary>1週間の特訓が終わったら面接シーンへ遷移する。</summary>
    private void OnWeekFinished()
    {
        if (player != null)
        {
            GameSession.SetMental(player.Mental);
        }

        SceneManager.LoadScene(InterviewSceneName);
    }

    private void RefreshStatusTexts()
    {
        if (player == null)
        {
            return;
        }

        if (staminaValueText != null)
        {
            staminaValueText.text = player.LifePoints.ToString();
        }

        if (mentalValueText != null)
        {
            mentalValueText.text = player.Mental.ToString();
        }

        RefreshBackground();
        RefreshCharacterFace();
        RefreshTimeText();
        RefreshDaysText();
        RefreshSleepButton();
    }

    /// <summary>
    /// 夜（スタミナ1以下）のときだけ SleepButton を表示する。
    /// </summary>
    private void RefreshSleepButton()
    {
        if (sleepButton == null || player == null)
        {
            return;
        }

        bool isNight = player.LifePoints <= NightStaminaThreshold;
        sleepButton.gameObject.SetActive(isNight);
    }

    /// <summary>
    /// 現在の日数に応じて DaysText を月曜日〜日曜日で更新する。
    /// SleepButton で就寝すると翌日の曜日へ進む。
    /// </summary>
    private void RefreshDaysText()
    {
        if (daysText == null || player == null)
        {
            return;
        }

        int day = player.CurrentDay;
        if (day < 1)
        {
            day = 1;
        }
        else if (day > WeekdayLabels.Length - 1)
        {
            day = WeekdayLabels.Length - 1;
        }

        daysText.text = WeekdayLabels[day];
    }

    /// <summary>
    /// スタミナに応じて BackgroundImage を切り替える。
    /// 2〜3→夕、1以下→夜。就寝などで閾値より上に戻ったら開始時の背景へ復帰する。
    /// </summary>
    private void RefreshBackground()
    {
        if (backgroundImage == null || player == null)
        {
            return;
        }

        int stamina = player.LifePoints;
        if (stamina <= NightStaminaThreshold)
        {
            if (nightBackground != null)
            {
                backgroundImage.sprite = nightBackground;
            }
        }
        else if (stamina <= EveningStaminaThreshold)
        {
            if (eveningBackground != null)
            {
                backgroundImage.sprite = eveningBackground;
            }
        }
        else if (defaultBackground != null)
        {
            backgroundImage.sprite = defaultBackground;
        }
    }

    /// <summary>
    /// スタミナに応じて TimeText を切り替える。
    /// 4以上→朝 AM、2〜3→夕 PM、1以下→夜。
    /// </summary>
    private void RefreshTimeText()
    {
        if (timeText == null || player == null)
        {
            return;
        }

        int stamina = player.LifePoints;
        if (stamina <= NightStaminaThreshold)
        {
            timeText.text = NightLabel;
        }
        else if (stamina <= EveningStaminaThreshold)
        {
            timeText.text = EveningLabel;
        }
        else
        {
            timeText.text = MorningLabel;
        }
    }

    /// <summary>
    /// タイトル選択結果（<see cref="GameSession"/>）を Player に適用する。
    /// Editor で overrideGenderForTest が ON のときは Test Gender を優先する。
    /// </summary>
    private void ApplySessionGender()
    {
        if (player == null)
        {
            return;
        }

#if UNITY_EDITOR
        if (overrideGenderForTest)
        {
            player.SetGender(testGender);
            return;
        }
#endif
        player.SetGender(GameSession.SelectedGender);
    }

    /// <summary>
    /// 性別とスタミナに応じて CharacterImage の表情を切り替える。
    /// 2以上→通常、1→疲労、0→疲労困憊。
    /// </summary>
    private void RefreshCharacterFace()
    {
        if (characterImage == null || player == null)
        {
            return;
        }

        Sprite normal;
        Sprite tired;
        Sprite exhausted;
        if (player.Gender == PlayerGender.Female)
        {
            normal = femaleNormalFace;
            tired = femaleTiredFace;
            exhausted = femaleExhaustedFace;
        }
        else
        {
            normal = maleNormalFace;
            tired = maleTiredFace;
            exhausted = maleExhaustedFace;
        }

        int stamina = player.LifePoints;
        if (stamina <= 0)
        {
            if (exhausted != null)
            {
                characterImage.sprite = exhausted;
            }
        }
        else if (stamina == 1)
        {
            if (tired != null)
            {
                characterImage.sprite = tired;
            }
        }
        else if (normal != null)
        {
            characterImage.sprite = normal;
        }
    }

    private void ResolveReferences()
    {
        if (player == null)
        {
            player = GetComponent<Player>();
            if (player == null)
            {
                player = FindAnyObjectByType<Player>();
            }
        }

        if (skillDropdown == null)
        {
            skillDropdown = FindAnyObjectByType<SkillDropdown>();
        }

        if (practiceButton == null)
        {
            GameObject buttonObject = GameObject.Find("SartButton");
            if (buttonObject != null)
            {
                practiceButton = buttonObject.GetComponent<Button>();
            }
        }

        if (sleepButton == null)
        {
            // 非アクティブだと Find できないため、シーン内から検索する。
            sleepButton = FindSleepButtonIncludingInactive();
        }

        if (skillSheetUI == null)
        {
            skillSheetUI = GetComponent<SkillSheetUI>();
            if (skillSheetUI == null)
            {
                skillSheetUI = FindAnyObjectByType<SkillSheetUI>();
            }
        }

        if (staminaValueText == null)
        {
            staminaValueText = FindTmpByName("StaminaText (TMP)_2");
        }

        if (mentalValueText == null)
        {
            mentalValueText = FindTmpByName("MentalText (TMP)_2");
        }

        if (timeText == null)
        {
            timeText = FindTmpByName("TimeText (TMP)");
        }

        if (daysText == null)
        {
            daysText = FindTmpByName("DaysText (TMP)");
        }

        if (backgroundImage == null)
        {
            GameObject backgroundObject = GameObject.Find("BackgroundImage");
            if (backgroundObject != null)
            {
                backgroundImage = backgroundObject.GetComponent<Image>();
            }
        }

        if (characterImage == null)
        {
            GameObject characterObject = GameObject.Find("CharacterImage");
            if (characterObject != null)
            {
                characterImage = characterObject.GetComponent<Image>();
            }
        }
    }

    private static TMP_Text FindTmpByName(string objectName)
    {
        GameObject go = GameObject.Find(objectName);
        return go != null ? go.GetComponent<TMP_Text>() : null;
    }

    private static Button FindSleepButtonIncludingInactive()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button button in buttons)
        {
            if (button != null && button.name == "SleepButton")
            {
                return button;
            }
        }

        return null;
    }
}
