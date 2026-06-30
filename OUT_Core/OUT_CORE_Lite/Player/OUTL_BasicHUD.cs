using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(1220)]
public class OUTL_BasicHUD : MonoBehaviour
{
    public OUTL_EntityAdapter Player;
    public OUTL_BasicPlayerController Controller;
    public Text HealthText;
    public Text ArmorText;
    public Text StateText;
    public Text PrimaryText;
    public Text SecondaryText;
    public Text MeleeText;
    public Text ScoreText;
    public Text XPText;
    public Text TierText;
    public Text PromptText;
    public string UsePromptFormat = "[E] {0}";
    public bool AutoCreateUI = false;
    public bool AutoAddDataBinder = true;
    public Canvas CanvasRoot;
    public RectTransform EquipmentRoot;
    public Font BuiltinFont;

    private OUTL_UIDataBinder binder;
    private bool missingCanvasWarned;
    private bool missingBinderWarned;
    private string lastPromptTargetName = "\u0001";

    private void Awake()
    {
        if (Player == null) Player = GetComponentInParent<OUTL_EntityAdapter>();
        if (Controller == null) Controller = GetComponentInParent<OUTL_BasicPlayerController>();
        if (AutoCreateUI) EnsureUI();
        EnsureBinder();
    }

    private void OnEnable()
    {
        EnsureBinder();
        if (binder != null) binder.RefreshNow();
    }

    private void Update()
    {
        if (PromptText != null)
        {
            OUTL_Interactable interactable = Controller != null ? Controller.CurrentInteractable : null;
            string targetName = interactable != null ? interactable.GetDisplayName() : (Controller != null ? Controller.GetCurrentUseDisplayName() : string.Empty);
            if (targetName != lastPromptTargetName)
            {
                lastPromptTargetName = targetName;
                PromptText.text = !string.IsNullOrEmpty(targetName) ? string.Format(UsePromptFormat, targetName) : string.Empty;
            }
        }
    }

    [ContextMenu("OUT Ensure Basic HUD")]
    public void EnsureUI()
    {
        if (CanvasRoot == null)
            CanvasRoot = GetComponentInChildren<Canvas>(true);
        if (CanvasRoot == null)
        {
            WarnMissingCanvas();
            return;
        }

        if (HealthText == null) HealthText = FindText("HPLabel", "HealthText");
        if (ArmorText == null) ArmorText = FindText("ArmorLabel", "ArmorText");
        if (StateText == null) StateText = FindText("StateLabel", "StateText");
        if (ScoreText == null) ScoreText = FindText("ScoreLabel", "ScoreText");
        if (XPText == null) XPText = FindText("XPLabel", "XPText");
        if (TierText == null) TierText = FindText("TierLabel", "TierText");
        if (PrimaryText == null) PrimaryText = FindText("PrimarySlotLabel", "PrimaryText");
        if (SecondaryText == null) SecondaryText = FindText("SecondarySlotLabel", "SecondaryText");
        if (MeleeText == null) MeleeText = FindText("MeleeSlotLabel", "MeleeText");
        if (PromptText == null) PromptText = FindText("UsePromptLabel", "PromptText");
        if (EquipmentRoot == null) EquipmentRoot = FindRect("EquipmentRows");
    }

    [ContextMenu("OUT Ensure Data Binder")]
    public void EnsureBinder()
    {
        if (!AutoAddDataBinder) return;
        if (binder == null) binder = GetComponent<OUTL_UIDataBinder>();
        if (binder == null)
        {
            WarnMissingBinder();
            return;
        }
        binder.Entity = Player;
        binder.TargetName = Player != null ? Player.TargetName : string.Empty;
        if (binder.Bindings == null || binder.Bindings.Length == 0)
        {
            binder.Bindings = new[]
            {
                new OUTL_UIDataBinding { Id = "Health", Kind = OUTL_UIDataKind.Stat, Key = "Health", Label = "HP", Format = "{0}: {1}", TargetText = HealthText, WarningWhenLessOrEqual = true, WarningLessOrEqual = 25f },
                new OUTL_UIDataBinding { Id = "Armor", Kind = OUTL_UIDataKind.Stat, Key = "Armor", Label = "ARM", Format = "{0}: {1}", TargetText = ArmorText },
                new OUTL_UIDataBinding { Id = "Dead", Kind = OUTL_UIDataKind.DeadState, Key = "Dead", Label = "STATE", Format = "{0}: {1}", TargetText = StateText },
                new OUTL_UIDataBinding { Id = "Primary", Kind = OUTL_UIDataKind.EquipmentSlot, Key = "Primary", Label = "PRI", Format = "{0}: {1}", TargetText = PrimaryText },
                new OUTL_UIDataBinding { Id = "Secondary", Kind = OUTL_UIDataKind.EquipmentSlot, Key = "Secondary", Label = "SEC", Format = "{0}: {1}", TargetText = SecondaryText },
                new OUTL_UIDataBinding { Id = "Melee", Kind = OUTL_UIDataKind.EquipmentSlot, Key = "Melee", Label = "MEL", Format = "{0}: {1}", TargetText = MeleeText },
                new OUTL_UIDataBinding { Id = "Score", Kind = OUTL_UIDataKind.Stat, Key = OUTL_LoopKeys.Score, Label = "SCORE", Format = "{0}: {1}", TargetText = ScoreText },
                new OUTL_UIDataBinding { Id = "XP", Kind = OUTL_UIDataKind.Stat, Key = OUTL_LoopKeys.XP, Label = "XP", Format = "{0}: {1}", TargetText = XPText },
                new OUTL_UIDataBinding { Id = "Tier", Kind = OUTL_UIDataKind.Tier, Key = "Tier", Label = "TIER", Format = "{0}: {1}", TargetText = TierText }
            };
        }
        else
        {
            BindKnownTargets();
        }
    }

    private void BindKnownTargets()
    {
        if (binder == null || binder.Bindings == null) return;
        for (int i = 0; i < binder.Bindings.Length; i++)
        {
            OUTL_UIDataBinding b = binder.Bindings[i];
            if (b == null || b.TargetText != null) continue;
            switch (b.Id)
            {
                case "Health": b.TargetText = HealthText; break;
                case "Armor": b.TargetText = ArmorText; break;
                case "Dead":
                case "State": b.TargetText = StateText; break;
                case "Primary": b.TargetText = PrimaryText; break;
                case "Secondary": b.TargetText = SecondaryText; break;
                case "Melee": b.TargetText = MeleeText; break;
                case "Score": b.TargetText = ScoreText; break;
                case "XP": b.TargetText = XPText; break;
                case "Tier": b.TargetText = TierText; break;
            }
        }
    }

    private Text FindText(params string[] names)
    {
        if (CanvasRoot == null || names == null) return null;
        Text[] texts = CanvasRoot.GetComponentsInChildren<Text>(true);
        for (int n = 0; n < names.Length; n++)
        {
            string wanted = names[n];
            if (string.IsNullOrEmpty(wanted)) continue;
            for (int i = 0; i < texts.Length; i++)
                if (texts[i] != null && texts[i].name == wanted)
                    return texts[i];
        }
        return null;
    }

    private RectTransform FindRect(string name)
    {
        if (CanvasRoot == null || string.IsNullOrEmpty(name)) return null;
        RectTransform[] rects = CanvasRoot.GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rects.Length; i++)
            if (rects[i] != null && rects[i].name == name)
                return rects[i];
        return null;
    }

    private void WarnMissingCanvas()
    {
        if (missingCanvasWarned) return;
        missingCanvasWarned = true;
        Debug.LogWarning("OUTL_BasicHUD requires a preauthored CanvasRoot. Runtime UI construction is disabled by OUT CORE Lite canon.", this);
    }

    private void WarnMissingBinder()
    {
        if (missingBinderWarned) return;
        missingBinderWarned = true;
        Debug.LogWarning("OUTL_BasicHUD requires a preauthored OUTL_UIDataBinder when AutoAddDataBinder is enabled.", this);
    }
}
