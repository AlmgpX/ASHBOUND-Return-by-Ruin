namespace OUT_RayMicro.World;

public readonly struct OutmMapValidationReport
{
    public readonly int ErrorCount;
    public readonly int WarningCount;
    public readonly string Summary;

    public OutmMapValidationReport(int errorCount, int warningCount, string summary)
    {
        ErrorCount = errorCount;
        WarningCount = warningCount;
        Summary = summary;
    }

    public bool HasErrors => ErrorCount > 0;
}

public static class OutmMapValidator
{
    public static OutmMapValidationReport Validate(OutmMapDef def, Action<string>? log = null)
    {
        int errors = 0;
        int warnings = 0;
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var doors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Error(string message)
        {
            errors++;
            log?.Invoke("OUTMAP ERROR: " + message);
        }

        void Warn(string message)
        {
            warnings++;
            log?.Invoke("OUTMAP WARN: " + message);
        }

        if (string.IsNullOrWhiteSpace(def.Id))
            Error("map id is empty");

        ValidateVector(def.PlayerStart, 3, "playerStart", Error);

        for (int i = 0; i < def.Boxes.Length; i++)
        {
            OutmBoxDef box = def.Boxes[i];
            string label = string.IsNullOrWhiteSpace(box.Id) ? $"boxes[{i}]" : box.Id;
            RegisterId(label, box.Id, ids, Error);
            ValidateVector(box.Center, 3, $"{label}.center", Error);
            ValidateVector(box.Size, 3, $"{label}.size", Error);
            ValidateColor(box.Color, $"{label}.color", Error);
            if (box.Size.Any(v => v <= 0.0f))
                Error($"{label}.size must be positive");
        }

        for (int i = 0; i < def.Doors.Length; i++)
        {
            OutmDoorDef door = def.Doors[i];
            string label = string.IsNullOrWhiteSpace(door.Id) ? $"doors[{i}]" : door.Id;
            RegisterId(label, door.Id, ids, Error);
            if (!string.IsNullOrWhiteSpace(door.Id))
                doors.Add(door.Id);
            ValidateVector(door.Center, 3, $"{label}.center", Error);
            ValidateVector(door.Size, 3, $"{label}.size", Error);
            ValidateColor(door.Color, $"{label}.color", Error);
            if (door.Size.Any(v => v <= 0.0f))
                Error($"{label}.size must be positive");
        }

        for (int i = 0; i < def.Triggers.Length; i++)
        {
            OutmTriggerDef trigger = def.Triggers[i];
            string label = string.IsNullOrWhiteSpace(trigger.Id) ? $"triggers[{i}]" : trigger.Id;
            RegisterId(label, trigger.Id, ids, Error);
            ValidateVector(trigger.Center, 3, $"{label}.center", Error);
            ValidateVector(trigger.Size, 3, $"{label}.size", Error);
            if (trigger.Size.Any(v => v <= 0.0f))
                Error($"{label}.size must be positive");

            if (string.Equals(trigger.Kind, "door_toggle", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trigger.Kind, "use_door", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(trigger.Target))
                    Error($"{label}.target is empty");
                else if (!doors.Contains(trigger.Target))
                    Error($"{label}.target points to missing door '{trigger.Target}'");
            }
            else
            {
                Warn($"{label}.kind '{trigger.Kind}' is not implemented yet");
            }
        }

        for (int i = 0; i < def.Meshes.Length; i++)
        {
            OutmMeshRefDef mesh = def.Meshes[i];
            string label = string.IsNullOrWhiteSpace(mesh.Id) ? $"meshes[{i}]" : mesh.Id;
            RegisterId(label, mesh.Id, ids, Error);
            ValidateVector(mesh.Position, 3, $"{label}.position", Error);
            ValidateVector(mesh.Rotation, 3, $"{label}.rotation", Error);
            ValidateVector(mesh.Scale, 3, $"{label}.scale", Error);
            if (mesh.Scale.Any(v => Math.Abs(v) <= 0.0001f))
                Error($"{label}.scale cannot contain zero");
            if (string.IsNullOrWhiteSpace(mesh.Path))
                Warn($"{label}.path is empty");
        }

        string summary = $"outmap validation: {errors} errors, {warnings} warnings";
        log?.Invoke(summary);
        return new OutmMapValidationReport(errors, warnings, summary);
    }

    private static void RegisterId(string label, string id, HashSet<string> ids, Action<string> error)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            error($"{label}.id is empty");
            return;
        }

        if (!ids.Add(id))
            error($"duplicate id '{id}'");
    }

    private static void ValidateVector(float[]? value, int length, string label, Action<string> error)
    {
        if (value == null || value.Length < length)
        {
            error($"{label} must have {length} numbers");
            return;
        }

        for (int i = 0; i < length; i++)
        {
            if (float.IsNaN(value[i]) || float.IsInfinity(value[i]))
                error($"{label}[{i}] must be finite");
        }
    }

    private static void ValidateColor(int[]? value, string label, Action<string> error)
    {
        if (value == null || value.Length < 3)
        {
            error($"{label} must have at least RGB numbers");
            return;
        }

        for (int i = 0; i < Math.Min(value.Length, 4); i++)
        {
            if (value[i] < 0 || value[i] > 255)
                error($"{label}[{i}] must be 0..255");
        }
    }
}
