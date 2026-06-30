using UnityEngine;

[CreateAssetMenu(menuName = "OUT/Core/Mind/Thought Template Set", fileName = "OUT_ThoughtTemplateSet")]
public class OUT_ThoughtTemplateSet : ScriptableObject
{
    [TextArea(2, 4)] public string HeaderTemplate = "{name} ({role}) processed {event}.";
    [TextArea(2, 4)] public string LowTensionTemplate = "{name} thinks: {line}.";
    [TextArea(2, 4)] public string FearTemplate = "{name} thinks: {line}. Better survive first.";
    [TextArea(2, 4)] public string AggressionTemplate = "{name} thinks: {line}. If there is a target, it should be suppressed.";
    [TextArea(2, 4)] public string CuriosityTemplate = "{name} thinks: {line}. The source needs checking.";
    [TextArea(2, 4)] public string PanicTemplate = "{name} thinks: {line}. The pattern is breaking.";
}
