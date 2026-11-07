using Eclipse.Data.Definitions;
using UnityEngine;

namespace Eclipse.Editor.DataPipeline.Tests
{
    public class FakePipelineDef : Definition
    {
        [SerializeField] private int power;
        [SerializeField] private string label;
        [SerializeField] private FakePipelineDef partner;

        public int Power { get => power; set => power = value; }
        public string Label { get => label; set => label = value; }
        public FakePipelineDef Partner { get => partner; set => partner = value; }
    }
}
