using UnityEngine;

/// <summary>
/// 人事面接官。人柄＝メンタルの高さで受け答えの成否が決まる。
/// 出題数は JSON の askCount（未読込時は 3）に合わせる。
/// UI 進行は選択肢採点を使い、ランダム成否はフォールバック用。
/// </summary>
public class HrInterviewer : Interviewer
{
    private const int DefaultAskCount = 3;

    public override InterviewerType Type => InterviewerType.Hr;
    public override string DisplayName => "人事面接官";

    public override int QuestionCount =>
        LoadedFileData?.interviewer != null && LoadedFileData.interviewer.askCount > 0
            ? LoadedFileData.interviewer.askCount
            : DefaultAskCount;

    protected override float GetAnswerSuccessRate(Player player)
    {
        if (player == null || Player.MaxMental <= 0)
        {
            return BaselineSuccessRate;
        }

        // メンタルの割合（0〜1）を下駄の上に載せる。
        float mentalRatio = (float)player.Mental / Player.MaxMental;
        return BaselineSuccessRate + (1f - BaselineSuccessRate) * mentalRatio;
    }
}
