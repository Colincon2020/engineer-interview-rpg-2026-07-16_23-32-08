/// <summary>
/// 特訓で伸ばせるスキル（プログラミング言語）の種別。
/// JAVA、SQL、C#、C++、C、アセンブリ、Python、VBA、Swift、JavaScript の10種。
/// </summary>
public enum SkillType
{
    Java,
    Sql,
    CSharp,
    Cpp,
    C,
    Assembly,
    Python,
    Vba,
    Swift,
    JavaScript,
}

/// <summary>
/// <see cref="SkillType"/> の表示名・列挙用ヘルパー。
/// </summary>
public static class SkillTypeUtil
{
    /// <summary>UI 表示用のスキル名を返す。</summary>
    public static string GetDisplayName(this SkillType skill)
    {
        switch (skill)
        {
            case SkillType.Java:
                return "Java";
            case SkillType.Sql:
                return "SQL";
            case SkillType.CSharp:
                return "C#";
            case SkillType.Cpp:
                return "C++";
            case SkillType.C:
                return "C";
            case SkillType.Assembly:
                return "アセンブリ";
            case SkillType.Python:
                return "Python";
            case SkillType.Vba:
                return "VBA";
            case SkillType.Swift:
                return "Swift";
            case SkillType.JavaScript:
                return "JavaScript";
            default:
                return skill.ToString();
        }
    }
}
