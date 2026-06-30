public readonly struct OUT_UseResult
{
    public readonly bool Success;
    public readonly bool Consumed;
    public readonly string Reason;

    public OUT_UseResult(bool success, bool consumed, string reason = "")
    {
        Success = success;
        Consumed = consumed;
        Reason = reason;
    }

    public static OUT_UseResult Failed(string reason = "") => new OUT_UseResult(false, false, reason);
    public static OUT_UseResult Performed(bool consumed = true, string reason = "") => new OUT_UseResult(true, consumed, reason);
}
