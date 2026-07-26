using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤー（転生したエンジニア）を定義するオブジェクト。
/// 1週間の特訓中の状態（日数・ライフポイント・メンタル・各スキルの習熟度）を保持し、
/// 「練習」「就寝」といったゲームの基本アクションを提供する。
///
/// 仕様（7/16作戦会議メモより）:
///  - 1週間（<see cref="TotalDays"/>日）特訓し、最終日に面接へ挑む。
///  - 練習は1日に何回でも可能。1回につきライフポイントを1消費してスキルが上昇する。
///  - ライフポイントは就寝（=翌日へ）で最大まで回復する。
///  - ライフポイントが尽きた状態で練習を続けると、代わりにメンタルを消費する。
///  - メンタルは回復しない。0になると、その日はもう練習できず就寝するしかない。
/// </summary>
public class Player : MonoBehaviour
{
    // ---- 定数（バランス調整用の上限値） ----

    /// <summary>特訓期間（日数）。1週間。</summary>
    public const int TotalDays = 7;

    /// <summary>1日あたりのライフポイント上限。就寝でここまで回復する。</summary>
    public const int MaxLifePoints = 5;

    /// <summary>メンタルの総量。回復手段はない。</summary>
    public const int MaxMental = 10;

    /// <summary>練習1回でスキルが上昇する値（上昇値）。</summary>
    public const int SkillGainPerPractice = 1;

    // ---- 現在の状態 ----

    [Header("現在の状態（実行中に確認用）")]
    [SerializeField]
    private int currentDay = 1;

    [SerializeField]
    private int lifePoints = MaxLifePoints;

    [SerializeField]
    private int mental = MaxMental;

    /// <summary>各スキルの習熟度。キーが存在しない場合は 0 とみなす。</summary>
    private readonly Dictionary<SkillType, int> skillLevels = new Dictionary<SkillType, int>();

    // ---- 公開プロパティ ----

    /// <summary>現在の日数（1 〜 <see cref="TotalDays"/>）。</summary>
    public int CurrentDay => currentDay;

    /// <summary>残りライフポイント。</summary>
    public int LifePoints => lifePoints;

    /// <summary>残りメンタル。</summary>
    public int Mental => mental;

    /// <summary>特訓期間が終了し、面接フェーズへ進めるか。</summary>
    public bool IsWeekFinished => currentDay > TotalDays;

    /// <summary>これ以上その日に練習できない（ライフもメンタルも尽きた）状態か。</summary>
    public bool IsExhausted => lifePoints <= 0 && mental <= 0;

    // ---- イベント（UI / スキルシート更新用） ----

    /// <summary>状態（日数・ライフ・メンタル・スキル）が変化したときに発火する。</summary>
    public event Action StateChanged;

    /// <summary>スキルが上昇したときに発火する（種別, 上昇後のレベル）。</summary>
    public event Action<SkillType, int> SkillLeveledUp;

    /// <summary>就寝して翌日へ進んだときに発火する（新しい日数）。</summary>
    public event Action<int> DayAdvanced;

    /// <summary>特訓期間が終了した（面接へ）ときに発火する。</summary>
    public event Action WeekFinished;

    private void Awake()
    {
        ResetState();
    }

    /// <summary>状態を初期値に戻す（ゲーム開始 / リトライ時に呼ぶ）。</summary>
    public void ResetState()
    {
        currentDay = 1;
        lifePoints = MaxLifePoints;
        mental = MaxMental;
        skillLevels.Clear();
        StateChanged?.Invoke();
    }

    /// <summary>
    /// 指定スキルを練習する。ライフポイントがあればライフを、無ければメンタルを消費し、
    /// スキルを <see cref="SkillGainPerPractice"/> だけ上昇させる。
    /// </summary>
    /// <returns>練習できた場合は true。ライフもメンタルも尽きていて練習できない場合は false。</returns>
    public bool Practice(SkillType skill)
    {
        if (IsWeekFinished)
        {
            Debug.LogWarning("特訓期間は終了しています。面接へ進んでください。");
            return false;
        }

        if (lifePoints > 0)
        {
            lifePoints--;
        }
        else if (mental > 0)
        {
            // ライフが尽きた状態での練習はメンタルを削る。
            mental--;
        }
        else
        {
            Debug.Log("ライフもメンタルも尽きています。就寝してください。");
            return false;
        }

        int newLevel = GetSkillLevel(skill) + SkillGainPerPractice;
        skillLevels[skill] = newLevel;

        SkillLeveledUp?.Invoke(skill, newLevel);
        StateChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 就寝して翌日へ進む。ライフポイントを最大まで回復する（メンタルは回復しない）。
    /// 最終日を過ぎた場合は特訓終了として <see cref="WeekFinished"/> を発火する。
    /// </summary>
    public void Sleep()
    {
        if (IsWeekFinished)
        {
            return;
        }

        currentDay++;
        lifePoints = MaxLifePoints;

        if (IsWeekFinished)
        {
            StateChanged?.Invoke();
            WeekFinished?.Invoke();
            return;
        }

        DayAdvanced?.Invoke(currentDay);
        StateChanged?.Invoke();
    }

    /// <summary>指定スキルの現在の習熟度を返す（未習得は 0）。</summary>
    public int GetSkillLevel(SkillType skill)
    {
        return skillLevels.TryGetValue(skill, out int level) ? level : 0;
    }

    /// <summary>
    /// スキルシート表示用に、全スキルの習熟度スナップショットを返す。
    /// 練習していないスキルも 0 として含める。
    /// </summary>
    public IReadOnlyDictionary<SkillType, int> GetSkillSheet()
    {
        var sheet = new Dictionary<SkillType, int>();
        foreach (SkillType skill in Enum.GetValues(typeof(SkillType)))
        {
            sheet[skill] = GetSkillLevel(skill);
        }
        return sheet;
    }

    /// <summary>全スキルの習熟度の合計（面接スコアの基礎値などに利用）。</summary>
    public int GetTotalSkillLevel()
    {
        int total = 0;
        foreach (int level in skillLevels.Values)
        {
            total += level;
        }
        return total;
    }
}
