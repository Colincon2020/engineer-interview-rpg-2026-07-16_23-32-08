using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// TMP_Dropdown に <see cref="SkillType"/> の全スキルを選択肢として登録する。
/// Dropdown と同じ GameObject に付ける想定。
/// </summary>
public class SkillDropdown : MonoBehaviour
{
    [SerializeField]
    private TMP_Dropdown dropdown;

    private void Awake()
    {
        if (dropdown == null)
        {
            dropdown = GetComponent<TMP_Dropdown>();
        }

        Populate();
    }

    /// <summary><see cref="SkillType"/> の全要素で選択肢を再構築する。</summary>
    public void Populate()
    {
        if (dropdown == null)
        {
            Debug.LogWarning("SkillDropdown: TMP_Dropdown が見つかりません。");
            return;
        }

        dropdown.ClearOptions();

        var options = new List<TMP_Dropdown.OptionData>();
        foreach (SkillType skill in Enum.GetValues(typeof(SkillType)))
        {
            options.Add(new TMP_Dropdown.OptionData(skill.GetDisplayName()));
        }

        dropdown.AddOptions(options);
        dropdown.SetValueWithoutNotify(0);
        dropdown.RefreshShownValue();
    }

    /// <summary>現在選択中のスキルを返す。</summary>
    public SkillType GetSelectedSkill()
    {
        if (dropdown == null)
        {
            return default;
        }

        return (SkillType)dropdown.value;
    }
}
