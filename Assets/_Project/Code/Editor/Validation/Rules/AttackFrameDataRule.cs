using Eclipse.Data.Combat;

namespace Eclipse.Editor.Validation
{
    /// <summary>Attack frame data must be positive: startup, active and recovery greater than 0 (06 section 3.5).</summary>
    [ValidationRule]
    public sealed class AttackFrameDataRule : IValidationRule
    {
        public string Name => "AttackFrameData";

        public void Validate(ValidationContext context)
        {
            foreach (var d in context.Definitions)
            {
                if (!(d is AttackChainDefinition attack)) continue;
                AttackStep s = attack.Step;
                if (s.StartupFrames <= 0) context.Report.Error(Name, attack, "startup_f must be > 0, found " + s.StartupFrames);
                if (s.ActiveFrames <= 0) context.Report.Error(Name, attack, "active_f must be > 0, found " + s.ActiveFrames);
                if (s.RecoveryFrames <= 0) context.Report.Error(Name, attack, "recovery_f must be > 0, found " + s.RecoveryFrames);
            }
        }
    }
}
