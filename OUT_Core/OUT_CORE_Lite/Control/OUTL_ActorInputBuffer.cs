using System;
using UnityEngine;

[Serializable]
public sealed class OUTL_ActorInputBuffer
{
    public OUTL_ActorInputFrame Current;
    public OUTL_ActorInputFrame Previous;
    public bool HasCurrent;
    public float LastInputTime;

    public float JumpPressedAt = -999f;
    public float UsePressedAt = -999f;
    public float ReloadPressedAt = -999f;
    public float PrimaryFirePressedAt = -999f;
    public float SecondaryFirePressedAt = -999f;
    public float MeleePressedAt = -999f;
    public float LedgeDropPressedAt = -999f;
    public float AbilityPrimaryPressedAt = -999f;
    public float AbilitySecondaryPressedAt = -999f;

    public bool JumpConsumed = true;
    public bool UseConsumed = true;
    public bool ReloadConsumed = true;
    public bool PrimaryFireConsumed = true;
    public bool SecondaryFireConsumed = true;
    public bool MeleeConsumed = true;
    public bool LedgeDropConsumed = true;
    public bool AbilityPrimaryConsumed = true;
    public bool AbilitySecondaryConsumed = true;

    public void Push(in OUTL_ActorInputFrame frame)
    {
        Previous = Current;
        Current = frame;
        HasCurrent = true;
        LastInputTime = frame.Timestamp;

        if (frame.JumpPressed) MarkPressed(ref JumpPressedAt, ref JumpConsumed, frame.Timestamp);
        if (frame.UsePressed) MarkPressed(ref UsePressedAt, ref UseConsumed, frame.Timestamp);
        if (frame.ReloadPressed) MarkPressed(ref ReloadPressedAt, ref ReloadConsumed, frame.Timestamp);
        if (frame.FirePrimaryPressed) MarkPressed(ref PrimaryFirePressedAt, ref PrimaryFireConsumed, frame.Timestamp);
        if (frame.FireSecondaryPressed) MarkPressed(ref SecondaryFirePressedAt, ref SecondaryFireConsumed, frame.Timestamp);
        if (frame.MeleePressed) MarkPressed(ref MeleePressedAt, ref MeleeConsumed, frame.Timestamp);
        if (frame.LedgeDropPressed) MarkPressed(ref LedgeDropPressedAt, ref LedgeDropConsumed, frame.Timestamp);
        if (frame.AbilityPrimaryPressed) MarkPressed(ref AbilityPrimaryPressedAt, ref AbilityPrimaryConsumed, frame.Timestamp);
        if (frame.AbilitySecondaryPressed) MarkPressed(ref AbilitySecondaryPressedAt, ref AbilitySecondaryConsumed, frame.Timestamp);
    }

    public void Clear(float time)
    {
        Previous = Current;
        Current = OUTL_ActorInputFrame.Empty(time);
        HasCurrent = false;
        LastInputTime = time;

        JumpPressedAt = -999f;
        UsePressedAt = -999f;
        ReloadPressedAt = -999f;
        PrimaryFirePressedAt = -999f;
        SecondaryFirePressedAt = -999f;
        MeleePressedAt = -999f;
        LedgeDropPressedAt = -999f;
        AbilityPrimaryPressedAt = -999f;
        AbilitySecondaryPressedAt = -999f;

        JumpConsumed = true;
        UseConsumed = true;
        ReloadConsumed = true;
        PrimaryFireConsumed = true;
        SecondaryFireConsumed = true;
        MeleeConsumed = true;
        LedgeDropConsumed = true;
        AbilityPrimaryConsumed = true;
        AbilitySecondaryConsumed = true;
    }

    public bool ConsumeJump(float now, float bufferSeconds) { return Consume(now, bufferSeconds, ref JumpPressedAt, ref JumpConsumed); }
    public bool ConsumeUse(float now, float bufferSeconds) { return Consume(now, bufferSeconds, ref UsePressedAt, ref UseConsumed); }
    public bool ConsumeReload(float now, float bufferSeconds) { return Consume(now, bufferSeconds, ref ReloadPressedAt, ref ReloadConsumed); }
    public bool ConsumePrimaryFire(float now, float bufferSeconds) { return Consume(now, bufferSeconds, ref PrimaryFirePressedAt, ref PrimaryFireConsumed); }
    public bool ConsumeSecondaryFire(float now, float bufferSeconds) { return Consume(now, bufferSeconds, ref SecondaryFirePressedAt, ref SecondaryFireConsumed); }
    public bool ConsumeMelee(float now, float bufferSeconds) { return Consume(now, bufferSeconds, ref MeleePressedAt, ref MeleeConsumed); }
    public bool ConsumeLedgeDrop(float now, float bufferSeconds) { return Consume(now, bufferSeconds, ref LedgeDropPressedAt, ref LedgeDropConsumed); }
    public bool ConsumeAbilityPrimary(float now, float bufferSeconds) { return Consume(now, bufferSeconds, ref AbilityPrimaryPressedAt, ref AbilityPrimaryConsumed); }
    public bool ConsumeAbilitySecondary(float now, float bufferSeconds) { return Consume(now, bufferSeconds, ref AbilitySecondaryPressedAt, ref AbilitySecondaryConsumed); }

    public Vector2 MoveDelta { get { return Current.Move - Previous.Move; } }
    public Vector2 LookDelta { get { return Current.Look - Previous.Look; } }

    private static void MarkPressed(ref float pressedAt, ref bool consumed, float time)
    {
        pressedAt = time;
        consumed = false;
    }

    private static bool Consume(float now, float bufferSeconds, ref float pressedAt, ref bool consumed)
    {
        if (consumed) return false;
        if (now - pressedAt > Mathf.Max(0f, bufferSeconds)) return false;
        consumed = true;
        return true;
    }
}
