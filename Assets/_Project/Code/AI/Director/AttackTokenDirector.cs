using System;

namespace Eclipse.AI.Director
{
    /// <summary>
    /// Limits simultaneous attackers (09 section 3.2). Time is injected through <see cref="Now"/> (seconds,
    /// from SimulationClock); no allocation after construction. Denied callers should orbit within
    /// <see cref="TokenPolicy.Orbit"/>.
    /// </summary>
    public sealed class AttackTokenDirector : Utility.ITokenGate
    {
        private readonly TokenTicket[] _tickets;
        private TokenPolicy _policy;
        private int _melee, _ranged, _ambush;

        public AttackTokenDirector(TokenPolicy policy, int maxEnemies = 256)
        {
            if (maxEnemies <= 0) throw new ArgumentOutOfRangeException(nameof(maxEnemies));
            _policy = policy;
            _tickets = new TokenTicket[maxEnemies];
        }

        /// <summary>Current time in seconds; advance every tick.</summary>
        public double Now { get; set; }

        public TokenPolicy Policy { get => _policy; set => _policy = value; }
        public int ActiveMelee => _melee;
        public int ActiveRanged => _ranged;
        public int ActiveAmbush => _ambush;

        /// <summary>Registers an enemy. Returns false when the table is full. Re-registering updates the class.</summary>
        public bool Register(int ownerId, TokenClass tokenClass)
        {
            int i = Find(ownerId);
            if (i < 0)
            {
                for (int k = 0; k < _tickets.Length; k++)
                    if (!_tickets[k].Used) { i = k; break; }
                if (i < 0) return false;
                _tickets[i] = new TokenTicket { Used = true, OwnerId = ownerId };
            }
            else if (_tickets[i].Class != tokenClass)
            {
                ReleaseAt(i);
            }
            _tickets[i].Class = tokenClass;
            return true;
        }

        /// <summary>Removes an enemy, returning any held token. Unknown ids are ignored.</summary>
        public void Unregister(int ownerId)
        {
            int i = Find(ownerId);
            if (i < 0) return;
            ReleaseAt(i);
            _tickets[i] = default;
        }

        /// <summary>Taunted enemies ignore tokens (06 section 3.10); the minimum interval still applies.</summary>
        public void SetTaunted(int ownerId, bool taunted)
        {
            int i = Find(ownerId);
            if (i >= 0) _tickets[i].Taunted = taunted;
        }

        public bool IsHolding(int ownerId)
        {
            int i = Find(ownerId);
            return i >= 0 && _tickets[i].Held != TokenPool.None;
        }

        public bool TryAcquire(int ownerId, bool ranged)
        {
            int i = Find(ownerId);
            if (i < 0) return false;
            ref TokenTicket t = ref _tickets[i];
            if (t.Held != TokenPool.None) return true;

            if (t.Class == TokenClass.Boss)
            {
                Grant(ref t, TokenPool.Free);
                return true;
            }

            if (t.HasGranted && Now - t.LastGrantTime < _policy.MinIntervalSeconds) return false;

            if (t.Taunted || t.Class == TokenClass.Elite || t.Class == TokenClass.Guardian)
            {
                Grant(ref t, TokenPool.Free);
                return true;
            }

            if (t.Class == TokenClass.Assassin)
            {
                if (_ambush >= _policy.AmbushTokens) return false;
                _ambush++;
                Grant(ref t, TokenPool.Ambush);
                return true;
            }

            if (ranged)
            {
                if (_ranged >= _policy.RangedTokens) return false;
                _ranged++;
                Grant(ref t, TokenPool.Ranged);
            }
            else
            {
                if (_melee >= _policy.MeleeTokens) return false;
                _melee++;
                Grant(ref t, TokenPool.Melee);
            }
            return true;
        }

        public void Release(int ownerId)
        {
            int i = Find(ownerId);
            if (i >= 0) ReleaseAt(i);
        }

        /// <summary>Drops every held token (encounter reset).</summary>
        public void ReleaseAll()
        {
            for (int k = 0; k < _tickets.Length; k++)
                if (_tickets[k].Used) ReleaseAt(k);
            _melee = _ranged = _ambush = 0;
        }

        private void Grant(ref TokenTicket t, TokenPool pool)
        {
            t.Held = pool;
            t.LastGrantTime = Now;
            t.HasGranted = true;
        }

        private void ReleaseAt(int i)
        {
            switch (_tickets[i].Held)
            {
                case TokenPool.Melee: _melee--; break;
                case TokenPool.Ranged: _ranged--; break;
                case TokenPool.Ambush: _ambush--; break;
            }
            _tickets[i].Held = TokenPool.None;
        }

        private int Find(int ownerId)
        {
            for (int k = 0; k < _tickets.Length; k++)
                if (_tickets[k].Used && _tickets[k].OwnerId == ownerId) return k;
            return -1;
        }
    }
}
