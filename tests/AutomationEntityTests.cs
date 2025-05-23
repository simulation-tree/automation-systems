using System.Numerics;
using Worlds;

namespace Automations.Systems.Tests
{
    public class AutomationEntityTests : AutomationSystemTests
    {
        [Test]
        public void CreateAutomationWithKeyframes()
        {
            AutomationEntity<Vector3> testAutomation = new(world,
            [
                (0, Vector3.Zero),
                (1f, Vector3.UnitX),
                (2f, Vector3.UnitY),
                (3f, Vector3.UnitZ),
                (4f, Vector3.One),
            ]);

            Assert.That(testAutomation.Count, Is.EqualTo(5));
            Assert.That(testAutomation[0].time, Is.EqualTo(0f));
            Assert.That(testAutomation[0].value, Is.EqualTo(Vector3.Zero));
            Assert.That(testAutomation[1].time, Is.EqualTo(1f));
            Assert.That(testAutomation[1].value, Is.EqualTo(Vector3.UnitX));
            Assert.That(testAutomation[2].time, Is.EqualTo(2f));
            Assert.That(testAutomation[2].value, Is.EqualTo(Vector3.UnitY));
            Assert.That(testAutomation[3].time, Is.EqualTo(3f));
            Assert.That(testAutomation[3].value, Is.EqualTo(Vector3.UnitZ));
            Assert.That(testAutomation[4].time, Is.EqualTo(4f));
            Assert.That(testAutomation[4].value, Is.EqualTo(Vector3.One));
        }

        [Test]
        public void MoveTransformAutomation()
        {
            AutomationEntity<Vector3> testAutomation = new(world, InterpolationMethod.Vector3Linear,
            [
                (0, Vector3.Zero),
                (1f, Vector3.UnitX),
                (2f, Vector3.UnitY),
                (3f, Vector3.UnitZ),
                (4f, Vector3.One),
            ]);

            Entity thingToMove = new(world);
            thingToMove.AddComponent<Position>();

            AutomationPlayer thingPlayer = thingToMove.Become<AutomationPlayer>();
            thingPlayer.SetAutomationForComponent<Position>(testAutomation);
            thingPlayer.Play();

            double delta = 0.1;
            double time = 0;
            while (time < 5)
            {
                Simulator.Update(delta);
                time += delta;
                Vector3 currentPosition = thingToMove.GetComponent<Position>().value;
                if (time == 0.5)
                {
                    Assert.That(currentPosition.X, Is.EqualTo(0.5f).Within(0.01f));
                    Assert.That(currentPosition.Y, Is.EqualTo(0f).Within(0.01f));
                    Assert.That(currentPosition.Z, Is.EqualTo(0f).Within(0.01f));
                }
                else if (time == 1)
                {
                    Assert.That(currentPosition.X, Is.EqualTo(1f).Within(0.01f));
                    Assert.That(currentPosition.Y, Is.EqualTo(0f).Within(0.01f));
                    Assert.That(currentPosition.Z, Is.EqualTo(0f).Within(0.01f));
                }
                else if (time == 1.5)
                {
                    Assert.That(currentPosition.X, Is.EqualTo(0.5f).Within(0.01f));
                    Assert.That(currentPosition.Y, Is.EqualTo(0.5f).Within(0.01f));
                    Assert.That(currentPosition.Z, Is.EqualTo(0f).Within(0.01f));
                }
                else if (time == 2)
                {
                    Assert.That(currentPosition.X, Is.EqualTo(0f).Within(0.01f));
                    Assert.That(currentPosition.Y, Is.EqualTo(1f).Within(0.01f));
                    Assert.That(currentPosition.Z, Is.EqualTo(0f).Within(0.01f));
                }
                else if (time == 2.5)
                {
                    Assert.That(currentPosition.X, Is.EqualTo(0f).Within(0.01f));
                    Assert.That(currentPosition.Y, Is.EqualTo(0.5f).Within(0.01f));
                    Assert.That(currentPosition.Z, Is.EqualTo(0.5f).Within(0.01f));
                }
                else if (time == 3)
                {
                    Assert.That(currentPosition.X, Is.EqualTo(0f).Within(0.01f));
                    Assert.That(currentPosition.Y, Is.EqualTo(0f).Within(0.01f));
                    Assert.That(currentPosition.Z, Is.EqualTo(1f).Within(0.01f));
                }
                else if (time == 3.5)
                {
                    Assert.That(currentPosition.X, Is.EqualTo(0.5f).Within(0.01f));
                    Assert.That(currentPosition.Y, Is.EqualTo(0.5f).Within(0.01f));
                    Assert.That(currentPosition.Z, Is.EqualTo(1f).Within(0.01f));
                }
                else if (time == 4)
                {
                    Assert.That(currentPosition.X, Is.EqualTo(1f).Within(0.01f));
                    Assert.That(currentPosition.Y, Is.EqualTo(1f).Within(0.01f));
                    Assert.That(currentPosition.Z, Is.EqualTo(1f).Within(0.01f));
                }
            }

            Vector3 finalPosition = thingToMove.GetComponent<Position>().value;
            Assert.That(finalPosition.X, Is.EqualTo(1f).Within(0.01f));
            Assert.That(finalPosition.Y, Is.EqualTo(1f).Within(0.01f));
            Assert.That(finalPosition.Z, Is.EqualTo(1f).Within(0.01f));
        }

        [Test]
        public void AnimateArrayElement()
        {
            AutomationEntity<Vector3> testAutomation = new(world, InterpolationMethod.Vector3Linear,
            [
                (0, Vector3.Zero),
                (1f, Vector3.UnitX),
                (2f, Vector3.UnitY),
                (3f, Vector3.UnitZ),
                (4f, Vector3.One),
            ]);

            Entity thingToAnimate = new(world);
            Values<Position> array = thingToAnimate.CreateArray<Position>(4);
            array[0] = new(5, 0, 0);
            array[1] = new(0, 5, 0);
            array[2] = new(0, 0, 5);
            array[3] = new(5, 5, 5);

            int index = 1;
            AutomationPlayer thingPlayer = thingToAnimate.Become<AutomationPlayer>();
            thingPlayer.SetAutomationForArrayElement<Position>(testAutomation, index);
            thingPlayer.Play();

            double delta = 0.1;
            double time = 0;
            while (time < 5)
            {
                Simulator.Update(delta);
                time += delta;

                Vector3 currentPosition = array[index].value;
                if (time == 0.5)
                {
                    Assert.That(currentPosition.X, Is.EqualTo(0.5f).Within(0.01f));
                    Assert.That(currentPosition.Y, Is.EqualTo(0f).Within(0.01f));
                    Assert.That(currentPosition.Z, Is.EqualTo(0f).Within(0.01f));
                }
                else if (time == 1)
                {
                    Assert.That(currentPosition.X, Is.EqualTo(1f).Within(0.01f));
                    Assert.That(currentPosition.Y, Is.EqualTo(0f).Within(0.01f));
                    Assert.That(currentPosition.Z, Is.EqualTo(0f).Within(0.01f));
                }
                else if (time == 1.5)
                {
                    Assert.That(currentPosition.X, Is.EqualTo(0.5f).Within(0.01f));
                    Assert.That(currentPosition.Y, Is.EqualTo(0.5f).Within(0.01f));
                    Assert.That(currentPosition.Z, Is.EqualTo(0f).Within(0.01f));
                }
                else if (time == 2)
                {
                    Assert.That(currentPosition.X, Is.EqualTo(0f).Within(0.01f));
                    Assert.That(currentPosition.Y, Is.EqualTo(1f).Within(0.01f));
                    Assert.That(currentPosition.Z, Is.EqualTo(0f).Within(0.01f));
                }
                else if (time == 2.5)
                {
                    Assert.That(currentPosition.X, Is.EqualTo(0f).Within(0.01f));
                    Assert.That(currentPosition.Y, Is.EqualTo(0.5f).Within(0.01f));
                    Assert.That(currentPosition.Z, Is.EqualTo(0.5f).Within(0.01f));
                }
                else if (time == 3)
                {
                    Assert.That(currentPosition.X, Is.EqualTo(0f).Within(0.01f));
                    Assert.That(currentPosition.Y, Is.EqualTo(0f).Within(0.01f));
                    Assert.That(currentPosition.Z, Is.EqualTo(1f).Within(0.01f));
                }
                else if (time == 3.5)
                {
                    Assert.That(currentPosition.X, Is.EqualTo(0.5f).Within(0.01f));
                    Assert.That(currentPosition.Y, Is.EqualTo(0.5f).Within(0.01f));
                    Assert.That(currentPosition.Z, Is.EqualTo(1f).Within(0.01f));
                }
                else if (time == 4)
                {
                    Assert.That(currentPosition.X, Is.EqualTo(1f).Within(0.01f));
                    Assert.That(currentPosition.Y, Is.EqualTo(1f).Within(0.01f));
                    Assert.That(currentPosition.Z, Is.EqualTo(1f).Within(0.01f));
                }
            }

            Vector3 finalPosition = array[index].value;
            Assert.That(finalPosition.X, Is.EqualTo(1f).Within(0.01f));
            Assert.That(finalPosition.Y, Is.EqualTo(1f).Within(0.01f));
            Assert.That(finalPosition.Z, Is.EqualTo(1f).Within(0.01f));
        }

        [Test]
        public void AnimateFieldOfArrayElement()
        {
            AutomationEntity<Vector4> testAutomation = new(world, InterpolationMethod.Vector4Linear,
            [
                (0, Vector4.Zero),
                (1f, Vector4.UnitX),
                (2f, Vector4.UnitY),
                (3f, Vector4.UnitZ),
                (4f, Vector4.UnitW),
            ]);

            Entity thingToAnimate = new(world);
            Values<SomeProperty> array = thingToAnimate.CreateArray<SomeProperty>(4);
            array[0] = new(5, new(0), 1289718923);
            array[1] = new(3.14f, new(5), 1289718923);
            array[2] = new(0, new(0), 1289718923);
            array[3] = new(5, new(5), 1289718923);

            int index = 1;
            AutomationPlayer thingPlayer = thingToAnimate.Become<AutomationPlayer>();
            thingPlayer.SetAutomationForArrayElement<SomeProperty>(testAutomation, index, nameof(SomeProperty.iDareYou));
            thingPlayer.Play();

            double delta = 0.1f;
            double time = 0;
            while (time < 5)
            {
                Simulator.Update(delta);
                time += delta;

                SomeProperty arrayElement = array[index];
                Assert.That(arrayElement.dontTouchMe, Is.EqualTo(3.14f).Within(0.01f));
                Assert.That(arrayElement.toTouchThat, Is.EqualTo(1289718923));

                Vector4 currentPosition = arrayElement.iDareYou;
                if (time == 0.5)
                {
                    Assert.That(currentPosition.X, Is.EqualTo(0.5f).Within(0.01f));
                    Assert.That(currentPosition.Y, Is.EqualTo(0f).Within(0.01f));
                    Assert.That(currentPosition.Z, Is.EqualTo(0f).Within(0.01f));
                    Assert.That(currentPosition.W, Is.EqualTo(0f).Within(0.01f));
                }
                else if (time == 1)
                {
                    Assert.That(currentPosition.X, Is.EqualTo(1f).Within(0.01f));
                    Assert.That(currentPosition.Y, Is.EqualTo(0f).Within(0.01f));
                    Assert.That(currentPosition.Z, Is.EqualTo(0f).Within(0.01f));
                    Assert.That(currentPosition.W, Is.EqualTo(0f).Within(0.01f));
                }
                else if (time == 1.5)
                {
                    Assert.That(currentPosition.X, Is.EqualTo(0.5f).Within(0.01f));
                    Assert.That(currentPosition.Y, Is.EqualTo(0.5f).Within(0.01f));
                    Assert.That(currentPosition.Z, Is.EqualTo(0f).Within(0.01f));
                    Assert.That(currentPosition.W, Is.EqualTo(0f).Within(0.01f));
                }
                else if (time == 2)
                {
                    Assert.That(currentPosition.X, Is.EqualTo(0f).Within(0.01f));
                    Assert.That(currentPosition.Y, Is.EqualTo(1f).Within(0.01f));
                    Assert.That(currentPosition.Z, Is.EqualTo(0f).Within(0.01f));
                    Assert.That(currentPosition.W, Is.EqualTo(0f).Within(0.01f));
                }
                else if (time == 2.5)
                {
                    Assert.That(currentPosition.X, Is.EqualTo(0f).Within(0.01f));
                    Assert.That(currentPosition.Y, Is.EqualTo(0.5f).Within(0.01f));
                    Assert.That(currentPosition.Z, Is.EqualTo(0.5f).Within(0.01f));
                    Assert.That(currentPosition.W, Is.EqualTo(0f).Within(0.01f));
                }
                else if (time == 3)
                {
                    Assert.That(currentPosition.X, Is.EqualTo(0f).Within(0.01f));
                    Assert.That(currentPosition.Y, Is.EqualTo(0f).Within(0.01f));
                    Assert.That(currentPosition.Z, Is.EqualTo(1f).Within(0.01f));
                    Assert.That(currentPosition.W, Is.EqualTo(0f).Within(0.01f));
                }
                else if (time == 3.5)
                {
                    Assert.That(currentPosition.X, Is.EqualTo(0f).Within(0.01f));
                    Assert.That(currentPosition.Y, Is.EqualTo(0f).Within(0.01f));
                    Assert.That(currentPosition.Z, Is.EqualTo(0.5f).Within(0.01f));
                    Assert.That(currentPosition.W, Is.EqualTo(0.5f).Within(0.01f));
                }
                else if (time == 4)
                {
                    Assert.That(currentPosition.X, Is.EqualTo(0f).Within(0.01f));
                    Assert.That(currentPosition.Y, Is.EqualTo(0f).Within(0.01f));
                    Assert.That(currentPosition.Z, Is.EqualTo(0f).Within(0.01f));
                    Assert.That(currentPosition.W, Is.EqualTo(1f).Within(0.01f));
                }
            }

            Vector4 finalPosition = array[index].iDareYou;
            Assert.That(finalPosition.X, Is.EqualTo(0f).Within(0.01f));
            Assert.That(finalPosition.Y, Is.EqualTo(0f).Within(0.01f));
            Assert.That(finalPosition.Z, Is.EqualTo(0f).Within(0.01f));
            Assert.That(finalPosition.W, Is.EqualTo(1f).Within(0.01f));
        }
    }
}
