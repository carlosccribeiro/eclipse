using System;
using UnityEngine;

namespace Eclipse.Data.Skills
{
    /// <summary>
    /// Named number of a definition (CSV column "params", "key=value|key=value"). The key carries the unit
    /// (_m metres, _s seconds, _f frames, _deg degrees, _pct percent as a number: 60 = 60%). Runtime Deliveries/Effects read by key.
    /// </summary>
    [Serializable]
    public struct SkillParam
    {
        [SerializeField] private string key;
        [SerializeField] private float value;

        public SkillParam(string key, float value)
        {
            this.key = key;
            this.value = value;
        }

        public string Key => key;
        public float Value => value;
    }
}
