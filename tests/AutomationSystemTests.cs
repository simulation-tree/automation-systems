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
            simulator.Add(new StateMachineSystem());
            simulator.Add(new StateAutomationSystem());
            simulator.Add(new AutomationPlayingSystem());
        }

        protected override void TearDown()
        {
            simulator.Remove<AutomationPlayingSystem>();
            simulator.Remove<StateAutomationSystem>();
            simulator.Remove<StateMachineSystem>();
            base.TearDown();
        }
    }
}