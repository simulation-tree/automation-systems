using Automations.Messages;
using Simulation.Tests;
using Types;
using Worlds;

namespace Automations.Systems.Tests
{
    public abstract class AutomationSystemTests : SimulationTests
    {
        public World world;

        static AutomationSystemTests()
        {
            MetadataRegistry.Load<AutomationsMetadataBank>();
            MetadataRegistry.Load<AutomationsSystemsTestsMetadataBank>();
        }

        protected override void SetUp()
        {
            base.SetUp();
            Schema schema = new();
            schema.Load<AutomationsSchemaBank>();
            schema.Load<AutomationsSystemsTestsSchemaBank>();
            world = new(schema);
            Simulator.Add(new StateMachineSystem(Simulator, world));
            Simulator.Add(new StateAutomationSystem(Simulator, world));
            Simulator.Add(new AutomationPlayingSystem(Simulator, world));
        }

        protected override void TearDown()
        {
            Simulator.Remove<AutomationPlayingSystem>();
            Simulator.Remove<StateAutomationSystem>();
            Simulator.Remove<StateMachineSystem>();
            world.Dispose();
            base.TearDown();
        }

        protected override void Update(double deltaTime)
        {
            Simulator.Broadcast(new AutomationUpdate(deltaTime));
        }
    }
}