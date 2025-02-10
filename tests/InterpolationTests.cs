using System;
using System.Numerics;
using Unmanaged;
using Worlds;

namespace Automations.Systems.Tests
{
    public class InterpolationTests : AutomationSystemTests
    {
        [Test]
        public void VerifyHold()
        {
            Entity entity = new(world);
            entity.AddComponent<FixedString>();

            Automation<FixedString> animation = new(world);
            animation.AddKeyframe(0f, "this");
            animation.AddKeyframe(1f, "is");
            animation.AddKeyframe(2f, "sum");
            animation.AddKeyframe(3f, "text");

            AutomationPlayer entityPlayer = entity.Become<AutomationPlayer>();
            entityPlayer.SetAutomationForComponent<FixedString>(animation);
            entityPlayer.Play();

            Assert.That(animation.Count, Is.EqualTo(4));

            simulator.Update(TimeSpan.FromSeconds(0f));

            Assert.That(entity.GetComponent<FixedString>().ToString(), Is.EqualTo("this"));

            simulator.Update(TimeSpan.FromSeconds(0.5f));

            Assert.That(entity.GetComponent<FixedString>().ToString(), Is.EqualTo("this"));

            simulator.Update(TimeSpan.FromSeconds(0.5f));

            Assert.That(entity.GetComponent<FixedString>().ToString(), Is.EqualTo("is"));

            simulator.Update(TimeSpan.FromSeconds(1f));

            Assert.That(entity.GetComponent<FixedString>().ToString(), Is.EqualTo("sum"));

            simulator.Update(TimeSpan.FromSeconds(0.1f));

            Assert.That(entity.GetComponent<FixedString>().ToString(), Is.EqualTo("sum"));

            simulator.Update(TimeSpan.FromSeconds(0.9f));

            Assert.That(entity.GetComponent<FixedString>().ToString(), Is.EqualTo("text"));

            simulator.Update(TimeSpan.FromSeconds(0.5f));

            Assert.That(entity.GetComponent<FixedString>().ToString(), Is.EqualTo("text"));
        }

        [Test]
        public void VerifyFloatLinear()
        {
            Interpolation floatInterpolation = BuiltInInterpolations.all[InterpolationMethod.FloatLinear];
            float current = 0f;
            float next = 8f;
            float component = 0f;
            floatInterpolation.Invoke(ref current, ref next, 0f, ref component);
            Assert.That(component, Is.EqualTo(0f).Within(0.01f));

            floatInterpolation.Invoke(ref current, ref next, 0.5f, ref component);
            Assert.That(component, Is.EqualTo(4f).Within(0.01f));

            floatInterpolation.Invoke(ref current, ref next, 1f, ref component);
            Assert.That(component, Is.EqualTo(8f).Within(0.01f));
        }

        [Test]
        public void VerifyVector3Linear()
        {
            Interpolation vector3Interpolation = BuiltInInterpolations.all[InterpolationMethod.Vector3Linear];
            Vector3 current = Vector3.Zero;
            Vector3 next = Vector3.One;
            Vector3 component = Vector3.Zero;

            vector3Interpolation.Invoke(ref current, ref next, 0f, ref component);

            Assert.That(component.X, Is.EqualTo(0f).Within(0.01f));
            Assert.That(component.Y, Is.EqualTo(0f).Within(0.01f));
            Assert.That(component.Z, Is.EqualTo(0f).Within(0.01f));

            vector3Interpolation.Invoke(ref current, ref next, 0.5f, ref component);

            Assert.That(component.X, Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(component.Y, Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(component.Z, Is.EqualTo(0.5f).Within(0.01f));

            vector3Interpolation.Invoke(ref current, ref next, 1f, ref component);

            Assert.That(component.X, Is.EqualTo(1f).Within(0.01f));
            Assert.That(component.Y, Is.EqualTo(1f).Within(0.01f));
            Assert.That(component.Z, Is.EqualTo(1f).Within(0.01f));
        }

        [Test]
        public unsafe void VerifyInterpolatingFromArray()
        {
            using Allocation component = new(sizeof(float));
            using Allocation keyframes = new(sizeof(float) * 2);
            keyframes.Write(sizeof(float) * 0, 0f);
            keyframes.Write(sizeof(float) * 1, 8f);

            Allocation current = keyframes.Read(sizeof(float) * 0);
            Allocation next = keyframes.Read(sizeof(float) * 1);
            Interpolation floatInterpolation = BuiltInInterpolations.all[InterpolationMethod.FloatLinear];
            floatInterpolation.Invoke(current, next, 0.5f, component, sizeof(float));

            Assert.That(component.Read<float>(), Is.EqualTo(4f).Within(0.01f));
        }
    }
}
