using System;
using System.Collections.Generic;
using Eclipse.Data.Definitions;
using Eclipse.Data.Skills;
using UnityEngine;

namespace Eclipse.Data.Heroes
{
    /// <summary>
    /// Hero resource (11 section 3.4.2, 08 sections 3.2/4.1). Source: Resources.csv. Gain rules are named numbers in <see cref="Params"/>
    /// (e.g. Fury: basic_first_target, kill, ...) read by the runtime resource component (M08 T2).
    /// </summary>
    public sealed class ResourceDefinition : Definition
    {
        [SerializeField] private ResourceKind kind;
        [SerializeField] private float baseMaximum;
        [SerializeField] private float absoluteCap;
        [SerializeField] private float maximumPerMaxResourcePercent;
        [SerializeField] private float decayDelaySeconds;
        [SerializeField] private float decayPerSecond;
        [SerializeField] private float startOfRoomValue;
        [SerializeField] private List<SkillParam> parameters = new List<SkillParam>();

        public ResourceKind Kind => kind;
        public float BaseMaximum => baseMaximum;
        /// <summary>Hard ceiling after Maximum Resource (00 R01 / 08 section 3.2: Fury 150).</summary>
        public float AbsoluteCap => absoluteCap;
        /// <summary>Maximum added per 1% of the Maximum Resource attribute (08 section 3.2: +1 Fury per 1%).</summary>
        public float MaximumPerMaxResourcePercent => maximumPerMaxResourcePercent;
        /// <summary>Seconds without dealing or receiving damage before decay starts.</summary>
        public float DecayDelaySeconds => decayDelaySeconds;
        public float DecayPerSecond => decayPerSecond;
        public float StartOfRoomValue => startOfRoomValue;
        public IReadOnlyList<SkillParam> Params => parameters;

        public float GetParam(string key)
        {
            for (int i = 0; i < parameters.Count; i++)
                if (string.Equals(parameters[i].Key, key, StringComparison.Ordinal)) return parameters[i].Value;
            throw new KeyNotFoundException("Resource '" + StableIdString + "' has no param '" + key + "'");
        }

#if UNITY_EDITOR
        /// <summary>Editor/importer only.</summary>
        public void EditorSet(ResourceKind resourceKind, float baseMax, float cap, float perPercent, float decayDelay,
            float decayRate, float roomStart, List<SkillParam> newParams)
        {
            kind = resourceKind;
            baseMaximum = baseMax;
            absoluteCap = cap;
            maximumPerMaxResourcePercent = perPercent;
            decayDelaySeconds = decayDelay;
            decayPerSecond = decayRate;
            startOfRoomValue = roomStart;
            parameters = newParams;
        }
#endif
    }
}
