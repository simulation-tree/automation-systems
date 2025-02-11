using Simulation.Tests;
using Types;
using Worlds;

namespace Automations.Systems.Tests
{
    public abstract class AutomationSystemTests : SimulationTests
    {
        static AutomationSystemTests()
        {
            TypeRegistry.Load<AutomationsTypeBank>();
            TypeRegistry.Load<AutomationsSystemsTestsTypeBank>();
        }

        protected override Schema CreateSchema()
        {
            Schema schema = base.CreateSchema();
            schema.Load<AutomationsSchemaBank>();
            schema.Load<AutomationsSystemsTestsSchemaBank>();
            return schema;
        }

        protected override void SetUp()
        {
            base.SetUp();
            simulator.AddSystem<StateMachineSystem>();
            simulator.AddSystem<StateAutomationSystem>();
            simulator.AddSystem<AutomationPlayingSystem>();
        }
    }
}