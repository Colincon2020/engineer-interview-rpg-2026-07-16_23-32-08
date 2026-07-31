using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// InterviewScene の制御。
/// 性別に応じた主人公画像の切替と、面接（質問・選択肢・採点・合否）を担当する。
/// 質問は QuestText (TMP)、答えは AnswerButton_1〜3 に表示する。
/// interviewType パラメータで人事・技術・社長面接を切り替えられる。
/// </summary>
public class InterviewSceneController : MonoBehaviour
{
    private const string ActionSceneName = "ActionScene";
    private const string TechInterviewSceneName = "TechInterviewScene";
    private const string PresidentInterviewSceneName = "PresidentInterviewScene";
    private const string EndSceneName = "EndScene";
    private const string TitleSceneName = "StartScence";
    private const string QuestTextObjectName = "QuestText (TMP)";
    private const float FailReturnDelaySeconds = 2f;
    private const float NextQuestionDelaySeconds = 1.5f;
    private const float PassTransitionDelaySeconds = 2f;
    private const float StartVoiceDelaySeconds = 1f;
    private const float ScoreBlinkIntervalSeconds = 0.15f;
    private const int AnswerButtonCount = 3;
    private const int SkillMasteryThreshold = 20;

    private static readonly string[] AnswerButtonNames =
    {
        "AnswerButton_1",
        "AnswerButton_2",
        "AnswerButton_3",
    };

    [Header("面接種別")]
    [SerializeField]
    [Tooltip("このシーンで実施する面接の種別")]
    private InterviewerType interviewType = InterviewerType.Hr;

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
    private TMP_Text questionCountText;

    [SerializeField]
    private TMP_Text reactionText;

    [SerializeField]
    private Button giveUpButton;

    [Header("効果音設定")]
    [SerializeField]
    [Tooltip("回答クリック時の効果音")]
    private AudioClip answerClickSE;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("効果音の音量")]
    private float seVolume = 1.0f;

    [Header("キャラクターボイス")]
    [SerializeField]
    [Tooltip("面接開始時のボイス")]
    private AudioClip startVoice;

    [SerializeField]
    [Tooltip("合格時のボイス")]
    private AudioClip passVoice;

    [SerializeField]
    [Tooltip("不合格時のボイス")]
    private AudioClip failVoice;

    [SerializeField]
    [Range(0f, 2f)]
    [Tooltip("ボイスの音量（1.0が通常、2.0で2倍）")]
    private float voiceVolume = 1.5f;

    [Header("内定パネル（社長面接用）")]
    [SerializeField]
    [Tooltip("内定おめでとうパネル")]
    private GameObject offerPanel;

    [SerializeField]
    [Tooltip("内定メッセージテキスト")]
    private TMP_Text offerMessageText;

    [SerializeField]
    [Tooltip("BGM用AudioSource（停止用）")]
    private AudioSource bgmAudioSource;

    [SerializeField]
    [Tooltip("タイトルに戻るボタン（内定パネル内）")]
    private Button returnToTitleButton;

    private AudioSource seAudioSource;
    private AudioSource voiceAudioSource;

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
    private bool isScoreBlinking;
    private bool isShowingOfferPanel;
    private Color scoreOriginalColor;
    private Coroutine advanceCoroutine;
    private Coroutine scoreBlinkCoroutine;
    private Player player;

    private void Awake()
    {
        ResolveReferences();
        SetupAudioSource();
        RefreshCharacterFace();
    }

    private void SetupAudioSource()
    {
        seAudioSource = GetComponent<AudioSource>();
        if (seAudioSource == null)
        {
            seAudioSource = gameObject.AddComponent<AudioSource>();
        }

        seAudioSource.playOnAwake = false;
        seAudioSource.loop = false;

        // ボイス用AudioSourceを別途作成
        voiceAudioSource = gameObject.AddComponent<AudioSource>();
        voiceAudioSource.playOnAwake = false;
        voiceAudioSource.loop = false;
    }

    /// <summary>効果音を再生する。</summary>
    private void PlaySE(AudioClip clip)
    {
        if (clip == null || seAudioSource == null)
        {
            return;
        }

        seAudioSource.PlayOneShot(clip, seVolume);
    }

    /// <summary>ボイスを再生する。</summary>
    private void PlayVoice(AudioClip clip)
    {
        if (clip == null || voiceAudioSource == null)
        {
            return;
        }

        voiceAudioSource.PlayOneShot(clip, voiceVolume);
    }

    /// <summary>開始ボイスを遅延再生する。</summary>
    private IEnumerator PlayStartVoiceAfterDelay()
    {
        yield return new WaitForSeconds(StartVoiceDelaySeconds);
        PlayVoice(startVoice);
    }

    private void Start()
    {
        // 内定パネルを初期状態で非表示
        if (offerPanel != null)
        {
            offerPanel.SetActive(false);
        }

        WireUiCallbacks();
        BeginInterview();

        // 開始1秒後にボイスを再生
        StartCoroutine(PlayStartVoiceAfterDelay());
    }

    private void Update()
    {
        // 内定パネル表示中にEnter/Spaceキーでタイトルに戻る
        if (isShowingOfferPanel)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                {
                    OnReturnToTitleClicked();
                }
            }
        }
    }

    /// <summary>面接種別に応じた面接を開始する。</summary>
    private void BeginInterview()
    {
        switch (interviewType)
        {
            case InterviewerType.Hr:
                BeginHrInterview();
                break;
            case InterviewerType.Technical:
                BeginTechInterview();
                break;
            case InterviewerType.President:
                BeginPresidentInterview();
                break;
        }
    }

    /// <summary>面接種別に応じた表示名を返す。</summary>
    private string GetInterviewDisplayName()
    {
        return interviewType switch
        {
            InterviewerType.Hr => "人事面接",
            InterviewerType.Technical => "技術面接",
            InterviewerType.President => "社長面接",
            _ => "面接",
        };
    }

    private void OnDestroy()
    {
        UnwireUiCallbacks();
        if (advanceCoroutine != null)
        {
            StopCoroutine(advanceCoroutine);
        }

        if (scoreBlinkCoroutine != null)
        {
            StopCoroutine(scoreBlinkCoroutine);
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
        if (player == null)
        {
            player = FindAnyObjectByType<Player>();
        }

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

        if (questionCountText == null)
        {
            questionCountText = FindTmpByName("QuestionCountText (TMP)");
        }

        if (reactionText == null)
        {
            reactionText = FindTmpByName("ReactionText (TMP)");
        }

        if (giveUpButton == null)
        {
            GameObject giveUpObject = GameObject.Find("GiveUpButton");
            if (giveUpObject != null)
            {
                giveUpButton = giveUpObject.GetComponent<Button>();
            }
        }

        // 内定パネル関連（社長面接用）
        if (offerPanel == null)
        {
            GameObject panelObject = GameObject.Find("OfferPanel");
            if (panelObject != null)
            {
                offerPanel = panelObject;
            }
        }

        if (offerMessageText == null)
        {
            offerMessageText = FindTmpByName("OfferMessageText (TMP)");
        }

        if (bgmAudioSource == null)
        {
            GameObject bgmObject = GameObject.Find("BGMAudioSource");
            if (bgmObject != null)
            {
                bgmAudioSource = bgmObject.GetComponent<AudioSource>();
            }
        }

        if (returnToTitleButton == null)
        {
            GameObject buttonObject = GameObject.Find("ReturnToTitleButton");
            if (buttonObject != null)
            {
                returnToTitleButton = buttonObject.GetComponent<Button>();
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

    private void BeginTechInterview()
    {
        InterviewMetaFileData metaData = InterviewDataLoader.LoadMeta();
        scoring = metaData?.scoring;

        InterviewerFileData techData = InterviewDataLoader.LoadTechnical();
        if (techData?.interviewer == null)
        {
            Debug.LogError("InterviewSceneController: 技術面接データを読み込めませんでした。");
            if (questText != null)
            {
                questText.text = "技術面接データの読み込みに失敗しました。";
            }

            SetAnswerButtonsActive(false);
            return;
        }

        questions = InterviewDataLoader.ToQuestionDataList(techData);
        maxScore = techData.interviewer.maxScore > 0
            ? techData.interviewer.maxScore
            : questions.Count * 10;
        passScore = InterviewDataLoader.GetStagePassScore(maxScore, scoring);
        currentIndex = 0;
        totalScore = 0;
        waitingForNext = false;
        interviewFinished = false;

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

    private void BeginPresidentInterview()
    {
        InterviewMetaFileData metaData = InterviewDataLoader.LoadMeta();
        scoring = metaData?.scoring;

        InterviewerFileData presidentData = InterviewDataLoader.LoadPresident();
        if (presidentData?.interviewer == null)
        {
            Debug.LogError("InterviewSceneController: 社長面接データを読み込めませんでした。");
            if (questText != null)
            {
                questText.text = "社長面接データの読み込みに失敗しました。";
            }

            SetAnswerButtonsActive(false);
            return;
        }

        questions = InterviewDataLoader.ToQuestionDataList(presidentData);
        maxScore = presidentData.interviewer.maxScore > 0
            ? presidentData.interviewer.maxScore
            : questions.Count * 10;
        passScore = InterviewDataLoader.GetStagePassScore(maxScore, scoring);
        currentIndex = 0;
        totalScore = 0;
        waitingForNext = false;
        interviewFinished = false;

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

        // 新しい質問が表示されるときはリアクションをクリア
        ClearReactionDisplay();

        RefreshMentalDisplay();
        RefreshScoreDisplay();
        RefreshQuestionCountDisplay();

        // 技術面接で得意分野（スキルレベル20以上）の場合、正解を赤字で表示
        bool showCorrectAnswer = ShouldHighlightCorrectAnswer(question);

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
                string labelText = string.IsNullOrEmpty(choice.id)
                    ? choice.text
                    : $"{choice.id}. {choice.text}";

                // 得意分野かつ正解の場合は赤字で表示
                if (showCorrectAnswer && choice.isCorrect)
                {
                    answerLabels[i].text = $"<color=red>{labelText}</color>";
                }
                else
                {
                    answerLabels[i].text = labelText;
                }
            }
        }
    }

    /// <summary>
    /// 技術面接で、質問のhintSkillに対応するプレイヤーのスキルレベルが閾値以上かどうか。
    /// </summary>
    private bool ShouldHighlightCorrectAnswer(InterviewQuestionData question)
    {
        // 技術面接以外は表示しない
        if (interviewType != InterviewerType.Technical)
        {
            return false;
        }

        if (question == null)
        {
            return false;
        }

        // hintSkillがない質問は対象外
        if (!InterviewDataLoader.TryParseHintSkill(question.hintSkill, out SkillType skillType))
        {
            return false;
        }

        // GameSessionからスキルレベルを取得して判定
        return GameSession.GetSkillLevel(skillType) >= SkillMasteryThreshold;
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

        // 効果音を再生
        PlaySE(answerClickSE);

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

        // 面接官のリアクションを表示
        ShowReaction(choice.reaction);

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
            FinishInterview();
            yield break;
        }

        ShowCurrentQuestion();
    }

    private void FinishInterview()
    {
        interviewFinished = true;
        waitingForNext = false;
        SetAnswerButtonsActive(false);
        ClearReactionDisplay();
        StopScoreBlink();

        bool passed = InterviewDataLoader.IsStagePass(totalScore, maxScore, scoring);
        string interviewName = GetInterviewDisplayName();

        // 面接種別に応じて結果を保存
        SaveInterviewResult(passed);

        if (passed)
        {
            // 合格ボイスを再生
            PlayVoice(passVoice);

            // 社長面接合格時は内定パネルを表示
            if (interviewType == InterviewerType.President)
            {
                ShowOfferPanel();
                return;
            }

            string nextMessage = GetPassMessage();
            if (questText != null)
            {
                questText.text =
                    $"{interviewName} 通過\n獲得 {totalScore} / 通過ライン {passScore} / 満点 {maxScore}\n{nextMessage}";
            }

            RefreshMentalDisplay();
            RefreshScoreDisplay();
            StartCoroutine(GoToNextSceneAfterDelay());
            return;
        }

        // 不合格ボイスを再生
        PlayVoice(failVoice);

        // 不合格時は結果をクリア
        ClearInterviewResult();

        if (questText != null)
        {
            questText.text =
                $"{interviewName} 不合格\n獲得 {totalScore} / 通過ライン {passScore} / 満点 {maxScore}\n特訓に戻ります…";
        }

        RefreshMentalDisplay();
        RefreshScoreDisplay();
        StartCoroutine(ReturnToActionSceneAfterDelay());
    }

    /// <summary>面接種別に応じて結果を保存する。</summary>
    private void SaveInterviewResult(bool passed)
    {
        switch (interviewType)
        {
            case InterviewerType.Hr:
                GameSession.SetHrInterviewResult(totalScore, maxScore, passScore, passed);
                break;
            case InterviewerType.Technical:
                GameSession.SetTechInterviewResult(totalScore, maxScore, passScore, passed);
                break;
            case InterviewerType.President:
                GameSession.SetPresidentInterviewResult(totalScore, maxScore, passScore, passed);
                break;
        }
    }

    /// <summary>面接種別に応じて結果をクリアする。</summary>
    private void ClearInterviewResult()
    {
        switch (interviewType)
        {
            case InterviewerType.Hr:
                GameSession.ClearHrInterviewResult();
                break;
            case InterviewerType.Technical:
                GameSession.ClearTechInterviewResult();
                break;
            case InterviewerType.President:
                GameSession.ClearPresidentInterviewResult();
                break;
        }
    }

    /// <summary>合格時の次へ進むメッセージを取得する。</summary>
    private string GetPassMessage()
    {
        return interviewType switch
        {
            InterviewerType.Hr => "技術面接へ進みます…",
            InterviewerType.Technical => "社長面接へ進みます…",
            InterviewerType.President => "最終合格！内定獲得！",
            _ => "次へ進みます…",
        };
    }

    /// <summary>合格時に次のシーンへ遷移する。</summary>
    private IEnumerator GoToNextSceneAfterDelay()
    {
        yield return new WaitForSeconds(PassTransitionDelaySeconds);

        string nextScene = interviewType switch
        {
            InterviewerType.Hr => TechInterviewSceneName,
            InterviewerType.Technical => PresidentInterviewSceneName,
            InterviewerType.President => EndSceneName,
            _ => ActionSceneName,
        };

        SceneTransition.Load(nextScene);
    }

    /// <summary>ValueText にメンタル値を表示する。</summary>
    private void RefreshMentalDisplay()
    {
        if (valueText != null)
        {
            valueText.text = GameSession.Mental.ToString();
        }
    }

    /// <summary>ScoreText に得点と満点を表示する。通過ラインを超えたら点滅開始。</summary>
    private void RefreshScoreDisplay()
    {
        if (scoreText == null)
        {
            return;
        }

        scoreText.text = $"得点： {totalScore}/{maxScore}";

        // 通過ラインを超えたら点滅開始（まだ点滅していない場合のみ）
        if (!isScoreBlinking && totalScore >= passScore)
        {
            StartScoreBlink();
        }
    }

    /// <summary>スコアテキストの点滅を開始する。</summary>
    private void StartScoreBlink()
    {
        if (isScoreBlinking || scoreText == null)
        {
            return;
        }

        // 元の色を保存
        scoreOriginalColor = scoreText.color;
        isScoreBlinking = true;
        scoreBlinkCoroutine = StartCoroutine(ScoreBlinkCoroutine());
    }

    /// <summary>スコアテキストの点滅を停止する。</summary>
    private void StopScoreBlink()
    {
        if (scoreBlinkCoroutine != null)
        {
            StopCoroutine(scoreBlinkCoroutine);
            scoreBlinkCoroutine = null;
        }

        isScoreBlinking = false;

        // 元の色に戻す
        if (scoreText != null)
        {
            scoreText.color = scoreOriginalColor;
        }
    }

    /// <summary>スコアテキストを赤白で点滅させるコルーチン。</summary>
    private IEnumerator ScoreBlinkCoroutine()
    {
        bool isRed = false;
        while (isScoreBlinking && scoreText != null)
        {
            isRed = !isRed;
            scoreText.color = isRed ? Color.red : Color.white;
            yield return new WaitForSeconds(ScoreBlinkIntervalSeconds);
        }

        // 終了時は元の色に戻す
        if (scoreText != null)
        {
            scoreText.color = scoreOriginalColor;
        }
    }

    /// <summary>QuestionCountText に現在の質問番号と総数を表示する。</summary>
    private void RefreshQuestionCountDisplay()
    {
        if (questionCountText != null)
        {
            questionCountText.text = $"質問数：{currentIndex + 1}/{questions.Count}";
        }
    }

    /// <summary>内定おめでとうパネルを表示し、BGMを停止する。</summary>
    private void ShowOfferPanel()
    {
        // BGMを停止
        StopBgm();

        // 内定パネル表示中フラグを立てる
        isShowingOfferPanel = true;

        // 年収を計算
        int salary = SalaryCalculator.CalculateSalaryFromSession();

        // 内定パネルを表示
        if (offerPanel != null)
        {
            offerPanel.SetActive(true);
        }

        // メッセージを設定（Enterで戻る案内を追加）
        if (offerMessageText != null)
        {
            offerMessageText.text = $"内定おめでとう！\n\nあなたの年収は\n{salary}万円です\n\n<size=70%>Enter/Spaceでタイトルへ</size>";
        }

        // 質問テキストを非表示
        if (questText != null)
        {
            questText.gameObject.SetActive(false);
        }

        // スコアテキストも非表示
        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(false);
        }

        // 質問数テキストも非表示
        if (questionCountText != null)
        {
            questionCountText.gameObject.SetActive(false);
        }
    }

    /// <summary>BGMを停止する。</summary>
    private void StopBgm()
    {
        if (bgmAudioSource != null && bgmAudioSource.isPlaying)
        {
            bgmAudioSource.Stop();
        }
    }

    /// <summary>面接官のリアクションを表示する。QuestTextは非表示にする。</summary>
    private void ShowReaction(string reaction)
    {
        // 質問テキストを非表示
        if (questText != null)
        {
            questText.gameObject.SetActive(false);
        }

        if (reactionText == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(reaction))
        {
            reactionText.text = string.Empty;
            reactionText.gameObject.SetActive(false);
            return;
        }

        reactionText.text = reaction;
        reactionText.gameObject.SetActive(true);
    }

    /// <summary>リアクション表示をクリアし、QuestTextを再表示する。</summary>
    private void ClearReactionDisplay()
    {
        // リアクションテキストを非表示
        if (reactionText != null)
        {
            reactionText.text = string.Empty;
            reactionText.gameObject.SetActive(false);
        }

        // 質問テキストを再表示
        if (questText != null)
        {
            questText.gameObject.SetActive(true);
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
        SceneTransition.Load(ActionSceneName);
    }

    /// <summary>タイトルに戻るボタンがクリックされたときの処理。</summary>
    private void OnReturnToTitleClicked()
    {
        // ゲームセッションをリセット
        GameSession.Reset();

        // タイトル画面へ遷移
        SceneTransition.Load(TitleSceneName);
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

        if (returnToTitleButton != null)
        {
            returnToTitleButton.onClick.AddListener(OnReturnToTitleClicked);
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

        if (returnToTitleButton != null)
        {
            returnToTitleButton.onClick.RemoveListener(OnReturnToTitleClicked);
        }
    }

    private static TMP_Text FindTmpByName(string objectName)
    {
        GameObject go = GameObject.Find(objectName);
        return go != null ? go.GetComponent<TMP_Text>() : null;
    }
}
