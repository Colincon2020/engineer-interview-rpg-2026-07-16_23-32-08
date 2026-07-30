using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>主人公の性別（顔アップ・立ち絵の切替に使用）。</summary>
public enum PlayerGender
{
    Male = 0,
    Female = 1,
}

/// <summary>
/// プレイヤー（転生したエンジニア）を定義するオブジェクト。
/// 1週間の特訓中の状態（日数・ライフポイント・メンタル・各スキルの習熟度）を保持し、
/// 「就寝」および練習への委譲（<see cref="Practice"/>）を提供する。
///
/// 仕様（7/16作戦会議メモより）:
///  - 1週間（<see cref="TotalDays"/>日）特訓し、最終日に面接へ挑む。
///  - 練習ルール（消費・上昇値・メンタル代替）の本体は <see cref="Practice"/> を参照。
///  - ライフポイント上限は <see cref="MaxLifePoints"/>。就寝（=翌日へ）で最大まで回復する。
///  - メンタル上限は <see cref="MaxMental"/>。回復手段はない。
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

    // ---- 現在の状態 ----

    [Header("主人公")]
    [SerializeField]
    private PlayerGender gender = PlayerGender.Male;

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

    /// <summary>主人公の性別。</summary>
    public PlayerGender Gender => gender;

    /// <summary>現在の日数（1 〜 <see cref="TotalDays"/>）。</summary>
    public int CurrentDay => currentDay;

    /// <summary>残りライフポイント。</summary>
    public int LifePoints => lifePoints;

    /// <summary>残りメンタル。</summary>
    public int Mental => mental;

    /// <summary>特訓期間が終了し、面接フェーズへ進めるか。</summary>
    public bool IsWeekFinished => currentDay > TotalDays;

    /// <summary>ライフもメンタルも尽きた状態か（練習中にメンタルが 0 になると EndScene へ遷移する）。</summary>
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

    /// <summary>状態を初期値に戻す（ゲーム開始 / リトライ時に呼ぶ）。性別は維持する。</summary>
    public void ResetState()
    {
        currentDay = 1;
        lifePoints = MaxLifePoints;
        mental = MaxMental;
        skillLevels.Clear();
        StateChanged?.Invoke();
    }

    /// <summary>
    /// 主人公の性別を設定する（タイトル選択結果の反映などに使う）。
    /// 変更時は <see cref="StateChanged"/> を発火し、表情 UI などを更新させる。
    /// </summary>
    public void SetGender(PlayerGender newGender)
    {
        if (gender == newGender)
        {
            return;
        }

        gender = newGender;
        StateChanged?.Invoke();
    }

    /// <summary>
    /// 指定スキルを練習する。<see cref="Practice.TryExecute"/> へ委譲する薄いラッパー。
    /// </summary>
    /// <returns>練習できた場合は true。ライフもメンタルも尽きていて練習できない場合は false。</returns>
    public bool Practice(SkillType skill)
    {
        return global::Practice.TryExecute(this, skill).Success;
    }

    /// <summary>
    /// 練習用にライフポイントを消費する。足りない場合は false。
    /// イベントは発火しない（呼び出し側でまとめて通知する）。
    /// </summary>
    public bool TryConsumeLifeForPractice(int amount)
    {
        if (amount <= 0 || lifePoints < amount)
        {
            return false;
        }

        lifePoints -= amount;
        return true;
    }

    /// <summary>
    /// 練習用にメンタルを消費する。足りない場合は false。
    /// イベントは発火しない（呼び出し側でまとめて通知する）。
    /// </summary>
    public bool TryConsumeMentalForPractice(int amount)
    {
        if (amount <= 0 || mental < amount)
        {
            return false;
        }

        mental -= amount;
        return true;
    }

    /// <summary>
    /// 指定スキルの習熟度を加算し、上昇後のレベルを返す。
    /// <see cref="SkillLeveledUp"/> と <see cref="StateChanged"/> を発火する。
    /// </summary>
    public int AddSkillLevel(SkillType skill, int amount)
    {
        int newLevel = GetSkillLevel(skill) + amount;
        skillLevels[skill] = newLevel;

        SkillLeveledUp?.Invoke(skill, newLevel);
        StateChanged?.Invoke();
        return newLevel;
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
