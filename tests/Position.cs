using System.Numerics;
using Worlds;

namespace Automations.Systems.Tests
{
    [Component]
    [ArrayElement]
    public struct Position
    {
        public Vector3 value;

        public Position(Vector3 value)
        {
            this.value = value;
        }

        public Position(float x, float y, float z)
        {
            value = new(x, y, z);
        }
    }
}