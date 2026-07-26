using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>面接官の種別（ランキング集計や演出の分岐に利用）。</summary>
public enum InterviewerType
{
    /// <summary>人事面接官（人柄＝メンタルを評価）。</summary>
    Hr,

    /// <summary>技術面接官（技術＝スキルを評価）。</summary>
    Technical,

    /// <summary>社長面接官（総合評価）。</summary>
    President,
}

/// <summary>
/// 面接での1つの質問。テキストは「AIで作成」する想定なので、
/// <see cref="Interviewer.SetQuestions"/> で外部から差し込める。
/// </summary>
[Serializable]
public class InterviewQuestion
{
    [SerializeField]
    private string text;

    public InterviewQuestion(string text)
    {
        this.text = text;
    }

    /// <summary>質問文。</summary>
    public string Text => text;
}

/// <summary>1問ごとの受け答え結果（UIでの表示用）。</summary>
public readonly struct QuestionOutcome
{
    public QuestionOutcome(InterviewQuestion question, bool answeredWell)
    {
        Question = question;
        AnsweredWell = answeredWell;
    }

    public InterviewQuestion Question { get; }

    /// <summary>その質問にうまく答えられたか。</summary>
    public bool AnsweredWell { get; }
}

/// <summary>面接1回分の結果。ランキング送信のスコア源になる。</summary>
public class InterviewResult
{
    public InterviewResult(
        InterviewerType type,
        string interviewerName,
        IReadOnlyList<QuestionOutcome> outcomes)
    {
        Type = type;
        InterviewerName = interviewerName;
        Outcomes = outcomes;

        int correct = 0;
        foreach (QuestionOutcome outcome in outcomes)
        {
            if (outcome.AnsweredWell)
            {
                correct++;
            }
        }
        CorrectAnswers = correct;
    }

    public InterviewerType Type { get; }
    public string InterviewerName { get; }
    public IReadOnlyList<QuestionOutcome> Outcomes { get; }

    /// <summary>出題数。</summary>
    public int TotalQuestions => Outcomes.Count;

    /// <summary>うまく答えられた質問数（＝素点）。</summary>
    public int CorrectAnswers { get; }

    /// <summary>正答率をもとにした 0〜100 の正規化スコア。</summary>
    public float NormalizedScore =>
        TotalQuestions == 0 ? 0f : (float)CorrectAnswers / TotalQuestions * 100f;
}

/// <summary>
/// 面接官（ボス）の共通基底クラス。人事・技術・社長の各面接官はこれを継承する。
///
/// 仕様（7/16作戦会議メモより）:
///  - 各面接官は AI で作成した質問を数問出題し、受け答えにランダム要素が入る。
///  - 面接官ごとに評価する軸が異なる（人事＝人柄/メンタル、技術＝スキル、社長＝総合）。
///  - 結果のスコアを合算してランキングへ送信する。
/// </summary>
public abstract class Interviewer : MonoBehaviour
{
    [Header("バランス調整")]
    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("能力が最低でもこの確率では答えられる、という下駄。")]
    private float baselineSuccessRate = 0.1f;

    /// <summary>この面接官の種別。</summary>
    public abstract InterviewerType Type { get; }

    /// <summary>画面表示用の名前（例：人事面接官）。</summary>
    public abstract string DisplayName { get; }

    /// <summary>出題数。</summary>
    public abstract int QuestionCount { get; }

    /// <summary>能力が最低でも確保される正答率の下限。</summary>
    protected float BaselineSuccessRate => baselineSuccessRate;

    /// <summary>実際に出題する質問。未設定なら仮の質問を自動生成する。</summary>
    private readonly List<InterviewQuestion> questions = new List<InterviewQuestion>();

    /// <summary>面接が完了したときに発火する（結果を渡す）。</summary>
    public event Action<InterviewResult> InterviewCompleted;

    /// <summary>
    /// AI 生成などで用意した質問を差し込む。null や空を渡した場合は自動生成にフォールバックする。
    /// </summary>
    public void SetQuestions(IEnumerable<InterviewQuestion> newQuestions)
    {
        questions.Clear();
        if (newQuestions != null)
        {
            questions.AddRange(newQuestions);
        }
    }

    /// <summary>
    /// プレイヤーを面接し、結果を返す。各質問はプレイヤーの能力から算出した正答率と
    /// ランダム要素で成否が決まる。
    /// </summary>
    public InterviewResult Interview(Player player)
    {
        EnsureQuestions();

        float successRate = Mathf.Clamp01(GetAnswerSuccessRate(player));
        var outcomes = new List<QuestionOutcome>(questions.Count);
        foreach (InterviewQuestion question in questions)
        {
            bool answeredWell = UnityEngine.Random.value < successRate;
            outcomes.Add(new QuestionOutcome(question, answeredWell));
        }

        var result = new InterviewResult(Type, DisplayName, outcomes);
        InterviewCompleted?.Invoke(result);
        return result;
    }

    /// <summary>
    /// この面接官が評価する軸に基づく、1問あたりの正答率（0〜1）を返す。
    /// 派生クラスで評価ロジックを実装する。
    /// </summary>
    protected abstract float GetAnswerSuccessRate(Player player);

    /// <summary>質問が未設定なら仮の質問を <see cref="QuestionCount"/> 問だけ生成する。</summary>
    private void EnsureQuestions()
    {
        if (questions.Count > 0)
        {
            return;
        }

        for (int i = 1; i <= QuestionCount; i++)
        {
            questions.Add(new InterviewQuestion($"{DisplayName}からの質問 {i}"));
        }
    }
}
