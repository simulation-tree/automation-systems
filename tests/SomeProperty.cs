using System.Numerics;
using System.Runtime.InteropServices;
using Worlds;

namespace Automations.Systems.Tests
{
    [ArrayElement]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SomeProperty
    {
        public byte first;
        public float dontTouchMe;
        public Vector4 iDareYou;
        public uint toTouchThat;

        public SomeProperty(float dontTouchMe, Vector4 iDareYou, uint toTouchThat)
        {
            first = 32;
            this.dontTouchMe = dontTouchMe;
            this.iDareYou = iDareYou;
            this.toTouchThat = toTouchThat;
        }
    }
}