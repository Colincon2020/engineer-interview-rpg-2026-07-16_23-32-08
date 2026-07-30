using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// InterviewScene の制御。
/// 性別に応じた主人公画像の切替と、人事面接（質問・選択肢・採点・合否）を担当する。
/// 質問は QuestText (TMP)、答えは AnswerButton_1〜3 に表示する。
/// </summary>
public class InterviewSceneController : MonoBehaviour
{
    private const string ActionSceneName = "ActionScene";
    private const string QuestTextObjectName = "QuestText (TMP)";
    private const float FailReturnDelaySeconds = 2f;
    private const float NextQuestionDelaySeconds = 1.5f;
    private const int AnswerButtonCount = 3;

    private static readonly string[] AnswerButtonNames =
    {
        "AnswerButton_1",
        "AnswerButton_2",
        "AnswerButton_3",
    };

    [Header("キャラ画像")]
    [SerializeField]
    private Image characterImage;

    [SerializeField]
    private Sprite maleFace;

    [SerializeField]
    private Sprite femaleFace;

    [Header("面接 UI（未設定なら名前で解決）")]
    [SerializeField]
    private TMP_Text questText;

    [SerializeField]
    private Button[] answerButtons;

    [SerializeField]
    private TMP_Text[] answerLabels;

    [SerializeField]
    private TMP_Text valueText;

    [SerializeField]
    private TMP_Text scoreText;

    [SerializeField]
    private Button giveUpButton;

    [Header("デバッグ（InterviewScene単体テスト用）")]
    [SerializeField]
    [Tooltip("ON のとき GameSession を無視し、下の Test Gender を使う（Editor のみ）")]
    private bool overrideGenderForTest;

    [SerializeField]
    private PlayerGender testGender = PlayerGender.Female;

    private ScoringRuleData scoring;
    private List<InterviewQuestionData> questions = new List<InterviewQuestionData>();
    private readonly List<InterviewChoiceData> displayedChoices = new List<InterviewChoiceData>(AnswerButtonCount);
    private int currentIndex;
    private int totalScore;
    private int maxScore;
    private int passScore;
    private bool waitingForNext;
    private bool interviewFinished;
    private Coroutine advanceCoroutine;

    private void Awake()
    {
        ResolveReferences();
        RefreshCharacterFace();
    }

    private void Start()
    {
        WireUiCallbacks();
        BeginHrInterview();
    }

    private void OnDestroy()
    {
        UnwireUiCallbacks();
        if (advanceCoroutine != null)
        {
            StopCoroutine(advanceCoroutine);
        }
    }

    /// <summary>
    /// 性別に応じて CharacterImage を切り替える。
    /// Editor で overrideGenderForTest が ON のときは Test Gender を優先する。
    /// </summary>
    private void RefreshCharacterFace()
    {
        if (characterImage == null)
        {
            return;
        }

        PlayerGender gender = ResolveGender();
        Sprite face = gender == PlayerGender.Female ? femaleFace : maleFace;
        if (face != null)
        {
            characterImage.sprite = face;
        }
    }

    private PlayerGender ResolveGender()
    {
#if UNITY_EDITOR
        if (overrideGenderForTest)
        {
            return testGender;
        }
#endif
        return GameSession.SelectedGender;
    }

    private void ResolveReferences()
    {
        if (characterImage == null)
        {
            GameObject characterObject = GameObject.Find("CharacterImage");
            if (characterObject == null)
            {
                characterObject = GameObject.Find("PlayerImage");
            }

            if (characterObject != null)
            {
                characterImage = characterObject.GetComponent<Image>();
            }
        }

        if (questText == null)
        {
            questText = FindTmpByName(QuestTextObjectName);
        }

        if (valueText == null)
        {
            valueText = FindTmpByName("ValueText (TMP)");
        }

        if (scoreText == null)
        {
            scoreText = FindTmpByName("ScoreText (TMP)");
        }

        if (giveUpButton == null)
        {
            GameObject giveUpObject = GameObject.Find("GiveUpButton");
            if (giveUpObject != null)
            {
                giveUpButton = giveUpObject.GetComponent<Button>();
            }
        }

        ResolveAnswerButtons();
    }

    private void ResolveAnswerButtons()
    {
        bool needsResolve = answerButtons == null
            || answerButtons.Length < AnswerButtonCount
            || answerLabels == null
            || answerLabels.Length < AnswerButtonCount;

        if (!needsResolve)
        {
            for (int i = 0; i < AnswerButtonCount; i++)
            {
                if (answerButtons[i] == null || answerLabels[i] == null)
                {
                    needsResolve = true;
                    break;
                }
            }
        }

        if (!needsResolve)
        {
            return;
        }

        answerButtons = new Button[AnswerButtonCount];
        answerLabels = new TMP_Text[AnswerButtonCount];

        for (int i = 0; i < AnswerButtonCount; i++)
        {
            GameObject buttonObject = GameObject.Find(AnswerButtonNames[i]);
            if (buttonObject == null)
            {
                Debug.LogError($"InterviewSceneController: {AnswerButtonNames[i]} が見つかりません。");
                continue;
            }

            answerButtons[i] = buttonObject.GetComponent<Button>();
            answerLabels[i] = buttonObject.GetComponentInChildren<TMP_Text>(true);
            if (answerLabels[i] == null)
            {
                Debug.LogError($"InterviewSceneController: {AnswerButtonNames[i]} 内に TMP_Text がありません。");
            }
        }
    }

    private void BeginHrInterview()
    {
        InterviewMetaFileData metaData = InterviewDataLoader.LoadMeta();
        scoring = metaData?.scoring;

        InterviewerFileData hrData = InterviewDataLoader.LoadHr();
        if (hrData?.interviewer == null)
        {
            Debug.LogError("InterviewSceneController: 人事面接データを読み込めませんでした。");
            if (questText != null)
            {
                questText.text = "人事面接データの読み込みに失敗しました。";
            }

            SetAnswerButtonsActive(false);
            return;
        }

        questions = InterviewDataLoader.ToQuestionDataList(hrData);
        maxScore = hrData.interviewer.maxScore > 0
            ? hrData.interviewer.maxScore
            : questions.Count * 10;
        passScore = InterviewDataLoader.GetStagePassScore(maxScore, scoring);
        currentIndex = 0;
        totalScore = 0;
        waitingForNext = false;
        interviewFinished = false;

        // TitleText はシーン上の固定文言（例: 人事面接）を維持し、上書きしない。
        RefreshMentalDisplay();
        RefreshScoreDisplay();

        if (questions.Count == 0)
        {
            if (questText != null)
            {
                questText.text = "出題できる質問がありません。";
            }

            SetAnswerButtonsActive(false);
            return;
        }

        ShowCurrentQuestion();
    }

    private void ShowCurrentQuestion()
    {
        waitingForNext = false;
        if (answerButtons == null || answerButtons.Length < AnswerButtonCount)
        {
            Debug.LogError("InterviewSceneController: 回答ボタンが不足しています。");
            return;
        }

        InterviewQuestionData question = questions[currentIndex];
        BuildDisplayedChoices(question);

        if (questText != null)
        {
            questText.text = question.question;
        }

        RefreshMentalDisplay();
        RefreshScoreDisplay();

        for (int i = 0; i < AnswerButtonCount; i++)
        {
            Button button = answerButtons[i];
            if (button == null)
            {
                continue;
            }

            bool hasChoice = i < displayedChoices.Count && displayedChoices[i] != null;
            button.gameObject.SetActive(hasChoice);
            button.interactable = hasChoice;
            if (hasChoice && answerLabels != null && i < answerLabels.Length && answerLabels[i] != null)
            {
                InterviewChoiceData choice = displayedChoices[i];
                answerLabels[i].text = string.IsNullOrEmpty(choice.id)
                    ? choice.text
                    : $"{choice.id}. {choice.text}";
            }
        }
    }

    /// <summary>
    /// シーンは回答ボタンが3つなので、JSON の先頭から最大3択を表示する。
    /// </summary>
    private void BuildDisplayedChoices(InterviewQuestionData question)
    {
        displayedChoices.Clear();
        if (question?.choices == null)
        {
            return;
        }

        for (int i = 0; i < question.choices.Length && displayedChoices.Count < AnswerButtonCount; i++)
        {
            InterviewChoiceData choice = question.choices[i];
            if (choice != null && !string.IsNullOrEmpty(choice.text))
            {
                displayedChoices.Add(choice);
            }
        }
    }

    private void OnAnswerClicked(int buttonIndex)
    {
        if (interviewFinished || waitingForNext || currentIndex < 0 || currentIndex >= questions.Count)
        {
            return;
        }

        if (buttonIndex < 0 || buttonIndex >= displayedChoices.Count)
        {
            return;
        }

        InterviewQuestionData question = questions[currentIndex];
        InterviewChoiceData choice = displayedChoices[buttonIndex];
        if (choice == null)
        {
            return;
        }

        int gained = InterviewDataLoader.CalculateChoiceScore(question, choice, scoring);
        totalScore += gained;
        waitingForNext = true;
        SetAnswerButtonsInteractable(false);

        RefreshMentalDisplay();
        RefreshScoreDisplay();

        if (advanceCoroutine != null)
        {
            StopCoroutine(advanceCoroutine);
        }

        advanceCoroutine = StartCoroutine(AdvanceAfterDelay());
    }

    private IEnumerator AdvanceAfterDelay()
    {
        yield return new WaitForSeconds(NextQuestionDelaySeconds);
        advanceCoroutine = null;

        if (interviewFinished || !waitingForNext)
        {
            yield break;
        }

        currentIndex++;
        if (currentIndex >= questions.Count)
        {
            FinishHrInterview();
            yield break;
        }

        ShowCurrentQuestion();
    }

    private void FinishHrInterview()
    {
        interviewFinished = true;
        waitingForNext = false;
        SetAnswerButtonsActive(false);

        bool passed = InterviewDataLoader.IsStagePass(totalScore, maxScore, scoring);
        GameSession.SetHrInterviewResult(totalScore, maxScore, passScore, passed);

        if (passed)
        {
            if (questText != null)
            {
                questText.text =
                    $"人事面接 通過\n獲得 {totalScore} / 通過ライン {passScore} / 満点 {maxScore}\n（次の面接の準備待ち）";
            }

            RefreshMentalDisplay();
            RefreshScoreDisplay();
            return;
        }

        GameSession.ClearHrInterviewResult();

        if (questText != null)
        {
            questText.text =
                $"人事面接 不合格\n獲得 {totalScore} / 通過ライン {passScore} / 満点 {maxScore}\n特訓に戻ります…";
        }

        RefreshMentalDisplay();
        RefreshScoreDisplay();
        StartCoroutine(ReturnToActionSceneAfterDelay());
    }

    /// <summary>ValueText にメンタル値を表示する。</summary>
    private void RefreshMentalDisplay()
    {
        if (valueText != null)
        {
            valueText.text = GameSession.Mental.ToString();
        }
    }

    /// <summary>ScoreText に得点と合格点数を表示する。</summary>
    private void RefreshScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = $"得点： {totalScore}/{passScore}";
        }
    }

    private IEnumerator ReturnToActionSceneAfterDelay()
    {
        yield return new WaitForSeconds(FailReturnDelaySeconds);
        ReturnToActionScene();
    }

    /// <summary>降参して特訓シーンへ戻る。</summary>
    private void OnGiveUpClicked()
    {
        if (interviewFinished)
        {
            return;
        }

        interviewFinished = true;
        waitingForNext = false;

        if (advanceCoroutine != null)
        {
            StopCoroutine(advanceCoroutine);
            advanceCoroutine = null;
        }

        SetAnswerButtonsInteractable(false);
        if (giveUpButton != null)
        {
            giveUpButton.interactable = false;
        }

        GameSession.ClearHrInterviewResult();
        ReturnToActionScene();
    }

    private static void ReturnToActionScene()
    {
        SceneManager.LoadScene(ActionSceneName);
    }

    private void SetAnswerButtonsActive(bool active)
    {
        if (answerButtons == null)
        {
            return;
        }

        foreach (Button button in answerButtons)
        {
            if (button != null)
            {
                button.gameObject.SetActive(active);
            }
        }
    }

    private void SetAnswerButtonsInteractable(bool interactable)
    {
        if (answerButtons == null)
        {
            return;
        }

        foreach (Button button in answerButtons)
        {
            if (button != null && button.gameObject.activeSelf)
            {
                button.interactable = interactable;
            }
        }
    }

    private void WireUiCallbacks()
    {
        if (answerButtons != null)
        {
            for (int i = 0; i < answerButtons.Length; i++)
            {
                int captured = i;
                if (answerButtons[i] != null)
                {
                    answerButtons[i].onClick.AddListener(() => OnAnswerClicked(captured));
                }
            }
        }

        if (giveUpButton != null)
        {
            giveUpButton.onClick.AddListener(OnGiveUpClicked);
        }
    }

    private void UnwireUiCallbacks()
    {
        if (answerButtons != null)
        {
            foreach (Button button in answerButtons)
            {
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                }
            }
        }

        if (giveUpButton != null)
        {
            giveUpButton.onClick.RemoveListener(OnGiveUpClicked);
        }
    }

    private static TMP_Text FindTmpByName(string objectName)
    {
        GameObject go = GameObject.Find(objectName);
        return go != null ? go.GetComponent<TMP_Text>() : null;
    }
}
