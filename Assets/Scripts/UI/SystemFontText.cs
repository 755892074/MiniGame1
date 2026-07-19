using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 继承 UI.Text，启用时自动套用系统字体（GameFont）。
/// 用于小游戏静态/动态文本，避免打包后中文不显示。
/// 因为 TT.GetSystemFont 不是默认字体，必须对每个 Text 单独设 .font —— 本组件自动完成。
/// </summary>
public class SystemFontText : Text
{
    protected override void OnEnable()
    {
        base.OnEnable();
        GameFont.Apply(this);
    }
}
