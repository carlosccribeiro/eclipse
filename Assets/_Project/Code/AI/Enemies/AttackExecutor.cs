using Eclipse.AI.Telegraphs;

namespace Eclipse.AI.Enemies
{
    /// <summary>What the executor asks the world to do. The Unity implementation is <see cref="EnemyAttackDelivery"/>; tests record the calls.</summary>
    public interface IAttackDelivery
    {
        /// <summary>The wind-up began: show the telegraph for this step.</summary>
        void TelegraphStart(AttackPlan plan, in AttackAim aim);
        /// <summary>Fill progress 0..1 of the running telegraph (centre to edge).</summary>
        void TelegraphProgress(float progress);
        /// <summary>The telegraph is over; <paramref name="completed"/> false when it was cancelled or interrupted.</summary>
        void TelegraphEnd(bool completed);
        /// <summary>Lobbed attacks: release the arc now so that it lands when the wind-up ends.</summary>
        void Launch(AttackPlan plan, in AttackAim aim);
        /// <summary>The fill touched the border: the hit/area/projectile happens on this tick (06 section 3.10).</summary>
        void Impact(AttackPlan plan, in AttackAim aim);
    }

    public enum ExecutorPhase : byte { Idle, Windup, Active, StepRecovery }

    public enum ExecutorResult : byte { None, Completed, Interrupted, Cancelled }

    /// <summary>
    /// Runs one attack (or a combo chained by <c>chain_next</c>) in simulated time: wind-up through a
    /// <see cref="TelegraphTimeline"/> (Impact on the exact tick the fill reaches the border), active window, recovery between
    /// strikes, then the next strike aimed at the target's current position. Pure C#; no allocation after construction.
    /// The cyan window (<see cref="TryInterrupt"/>) cancels an interruptible wind-up; <see cref="Cancel"/> ends everything
    /// (death, stun).
    /// </summary>
    public sealed class AttackExecutor
    {
        private const float Tolerance = 1e-5f;

        private readonly TelegraphTimeline _timeline = new TelegraphTimeline();
        private readonly IAttackDelivery _delivery;
        private AttackAim _aim;
        private AttackPlan _plan;
        private float _elapsed;
        private float _launchAt;
        private bool _launched;
        private float _timer;

        public AttackExecutor(IAttackDelivery delivery)
        {
            _delivery = delivery;
        }

        public ExecutorPhase Phase { get; private set; }
        public ExecutorResult LastResult { get; private set; }
        public bool IsBusy => Phase != ExecutorPhase.Idle;
        /// <summary>The strike being executed (null when idle).</summary>
        public AttackPlan Current => IsBusy ? _plan : null;
        /// <summary>True while a wind-up with the cyan window is running.</summary>
        public bool InInterruptWindow => Phase == ExecutorPhase.Windup && _timeline.IsRunning && _timeline.Interruptible;
        public float TelegraphProgress => Phase == ExecutorPhase.Windup ? _timeline.Progress : 0f;

        /// <summary>Starts the first strike of <paramref name="head"/>. False when already busy.</summary>
        public bool Begin(AttackPlan head, in AttackAim aim)
        {
            if (head == null || IsBusy) return false;
            LastResult = ExecutorResult.None;
            StartStep(head, in aim);
            return true;
        }

        /// <summary>Advances time. <paramref name="current"/> is where the target is now (used to aim the next strike of a combo).</summary>
        public void Tick(float dt, in AttackAim current)
        {
            switch (Phase)
            {
                case ExecutorPhase.Windup:
                    _elapsed += dt;
                    if (!_launched && _plan.IsLobbed && _elapsed + Tolerance >= _launchAt)
                    {
                        _launched = true;
                        _delivery.Launch(_plan, in _aim);
                    }
                    TelegraphEvent ev = _timeline.Advance(dt);
                    _delivery.TelegraphProgress(_timeline.Progress);
                    if (ev == TelegraphEvent.Impact)
                    {
                        _delivery.TelegraphEnd(true);
                        _delivery.Impact(_plan, in _aim);
                        Phase = ExecutorPhase.Active;
                        _timer = _plan.ActiveSeconds;
                        if (_timer <= Tolerance) FinishStrike(in current);
                    }
                    break;
                case ExecutorPhase.Active:
                    _timer -= dt;
                    if (_timer <= Tolerance) FinishStrike(in current);
                    break;
                case ExecutorPhase.StepRecovery:
                    _timer -= dt;
                    if (_timer <= Tolerance) StartStep(_plan.Next, in current);
                    break;
            }
        }

        /// <summary>Stagger in the cyan window: cancels an interruptible wind-up. True when it was cancelled.</summary>
        public bool TryInterrupt()
        {
            if (Phase != ExecutorPhase.Windup || !_timeline.TryInterrupt()) return false;
            _delivery.TelegraphEnd(false);
            End(ExecutorResult.Interrupted);
            return true;
        }

        /// <summary>Unconditional stop (death, stun/Staggered).</summary>
        public void Cancel()
        {
            if (!IsBusy) return;
            if (Phase == ExecutorPhase.Windup)
            {
                _timeline.Cancel();
                _delivery.TelegraphEnd(false);
            }
            End(ExecutorResult.Cancelled);
        }

        private void FinishStrike(in AttackAim current)
        {
            if (_plan.Next != null)
            {
                Phase = ExecutorPhase.StepRecovery;
                _timer = _plan.RecoverSeconds;
                if (_timer <= Tolerance) StartStep(_plan.Next, in current);
                return;
            }
            End(ExecutorResult.Completed);
        }

        private void StartStep(AttackPlan plan, in AttackAim aim)
        {
            _plan = plan;
            _aim = aim;
            _elapsed = 0f;
            _launched = false;
            float flight = plan.IsLobbed ? AttackGeometry.FlightSeconds(plan, in aim) : 0f;
            _launchAt = flight >= plan.WindupSeconds ? 0f : plan.WindupSeconds - flight;
            Phase = ExecutorPhase.Windup;
            _timeline.Start(plan.WindupSeconds, plan.Interruptible);
            _delivery.TelegraphStart(plan, in _aim);
        }

        private void End(ExecutorResult result)
        {
            Phase = ExecutorPhase.Idle;
            LastResult = result;
        }
    }
}
