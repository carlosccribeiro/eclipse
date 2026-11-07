namespace Eclipse.Gameplay.Simulation
{
    /// <summary>Object stepped by <see cref="TickScheduler"/> once per fixed 60 Hz tick.</summary>
    public interface IFixedTickable
    {
        void Tick(in SimulationFrame frame);
    }
}
