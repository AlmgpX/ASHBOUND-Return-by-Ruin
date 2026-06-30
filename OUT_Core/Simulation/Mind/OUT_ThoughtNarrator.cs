using UnityEngine;

public static class OUT_ThoughtNarrator
{
    public static string GenerateNarrative(GameObject entity, int depth = 2, string style = "default")
    {
        if (entity == null)
            return string.Empty;

        OUT_EntityMind mind = entity.GetComponent<OUT_EntityMind>();
        if (mind == null)
            mind = entity.GetComponentInParent<OUT_EntityMind>();

        if (mind == null)
            return entity.name + " has no OUT_EntityMind.";

        return OUT_ThoughtEngine.Generate(mind, depth, null);
    }

    public static string GenerateNarrative(OUT_EntityMind mind, int depth = 2, OUT_ThoughtTemplateSet templateSet = null)
    {
        return OUT_ThoughtEngine.Generate(mind, depth, templateSet);
    }

    public static void LogNarrative(GameObject entity, int depth = 2)
    {
        string text = GenerateNarrative(entity, depth, "default");
        OUT_AIDebugLogService.Log(entity, OUT_AIDebugLogService.AIEventKind.Brain, text);
    }
}
