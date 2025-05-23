using System;
using Worlds;

namespace Automations.Systems.Tests
{
    public class StateMachineTests : AutomationSystemTests
    {
#if DEBUG
        [Test]
        public void ThrowWhenReadingStateBeforeInitializing()
        {
            StateMachine machine = new(world);
            Assert.Throws<InvalidOperationException>(() => Console.WriteLine(machine.EntryState));
        }
#endif

        [Test]
        public void SimpleStateMachine()
        {
            StateMachine machine = new(world);

            machine.AddState("Entry State");
            machine.AddState("Other State");
            machine.AddTransition("Entry State", "Other State", "pastrami", Transition.Condition.GreaterThan, 0f);
            machine.EntryState = "Entry State";

            Assert.That(machine.EntryState.ToString(), Is.EqualTo("Entry State"));

            Entity entity = new(world);
            entity.AddComponent(0f);
            Stateful stateful = entity.Become<Stateful>();
            stateful.StateMachine = machine;
            stateful.AddParameter("pastrami", 0f);

            Simulator.Update(1);

            Assert.That(stateful.CurrentState.ToString(), Is.EqualTo("Entry State"));

            stateful.SetParameter("pastrami", 0.05f);

            Simulator.Update(1);

            Assert.That(stateful.CurrentState.ToString(), Is.EqualTo("Other State"));
        }

        [Test]
        public void StatefulEntityWithAutomations()
        {
            AutomationEntity<float> defaultAutomation = new(world, InterpolationMethod.FloatLinear, [new(0f, 0f)]);
            AutomationEntity<float> triangleWave = new(world, InterpolationMethod.FloatLinear, [new(0f, 0f), new(1f, 1f), new(2f, 0f)], true);
            StateMachine machine = new(world);
            machine.AddState("Entry State");
            machine.AddState("Other State");
            machine.AddTransition("Entry State", "Other State", "pastrami", Transition.Condition.GreaterThan, 0f);
            machine.AddTransition("Other State", "Entry State", "pastrami", Transition.Condition.LessThanOrEqual, 0f);
            machine.EntryState = "Entry State";

            Assert.That(machine.EntryState.ToString(), Is.EqualTo("Entry State"));

            Entity entity = new(world);
            entity.AddComponent(0f);
            StatefulAutomationPlayer stateful = entity.Become<StatefulAutomationPlayer>();
            stateful.StateMachine = machine;
            stateful.AddParameter("pastrami", 0f);
            stateful.AddOrSetLinkToComponent<float>("Entry State", defaultAutomation);
            stateful.AddOrSetLinkToComponent<float>("Other State", triangleWave);

            Assert.That(stateful.CurrentState.ToString(), Is.EqualTo("Entry State"));

            Simulator.Update(0.1);

            Assert.That(entity.GetComponent<float>(), Is.EqualTo(0f).Within(0.01f));

            stateful.SetParameter("pastrami", 1f);
            Simulator.Update(0.1);
            Simulator.Update(0.1);
            Simulator.Update(0.1);
            Simulator.Update(0.1);
            Simulator.Update(0.1);

            Assert.That(entity.GetComponent<float>(), Is.EqualTo(0.5f).Within(0.01f));

            Simulator.Update(0.5);

            Assert.That(entity.GetComponent<float>(), Is.EqualTo(1f).Within(0.01f));

            Simulator.Update(0.5);

            Assert.That(entity.GetComponent<float>(), Is.EqualTo(0.5f).Within(0.01f));

            stateful.SetParameter("pastrami", 0f);

            Simulator.Update(0.5);

            Assert.That(entity.GetComponent<float>(), Is.EqualTo(0f).Within(0.01f));
        }
    }
}
