using System.Numerics;
using Worlds;

namespace Automations.Systems.Tests
{
    [ArrayElement]
    public struct SomeProperty
    {
        public float dontTouchMe;
        public Vector3 iDareYou;
        public bool toTouchThat;

        public SomeProperty(float dontTouchMe, Vector3 iDareYou, bool toTouchThat)
        {
            this.dontTouchMe = dontTouchMe;
            this.iDareYou = iDareYou;
            this.toTouchThat = toTouchThat;
        }
    }
}