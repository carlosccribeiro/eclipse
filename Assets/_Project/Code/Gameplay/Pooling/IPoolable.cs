namespace Eclipse.Gameplay.Pooling
{
    /// <summary>Callbacks for pooled objects (11 §3.14). Called on Get / Release.</summary>
    public interface IPoolable
    {
        void OnSpawned();
        void OnDespawned();
    }
}
