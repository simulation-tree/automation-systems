using Simulation.Tests;
using Types;
using Worlds;

namespace Automations.Systems.Tests
{
    public abstract class AutomationSystemTests : SimulationTests
    {
        static AutomationSystemTests()
        {
            MetadataRegistry.Load<AutomationsMetadataBank>();
            MetadataRegistry.Load<AutomationsSystemsTestsMetadataBank>();
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
            Simulator.Add(new StateMachineSystem());
            Simulator.Add(new StateAutomationSystem());
            Simulator.Add(new AutomationPlayingSystem());
        }

        protected override void TearDown()
        {
            Simulator.Remove<AutomationPlayingSystem>();
            Simulator.Remove<StateAutomationSystem>();
            Simulator.Remove<StateMachineSystem>();
            base.TearDown();
        }
    }
}