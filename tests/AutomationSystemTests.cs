using Simulation.Tests;
using Types;
using Types.Functions;
using Worlds;
using Worlds.Functions;

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

        public readonly struct AutomationsSystemsTestsTypeBank : ITypeBank
        {
            readonly void ITypeBank.Load(Register register)
            {
                register.Invoke<Position>();
            }
        }

        public readonly struct AutomationsSystemsTestsSchemaBank : ISchemaBank
        {
            readonly void ISchemaBank.Load(RegisterDataType function)
            {
                function.Invoke(TypeRegistry.Get<Position>(), DataType.Kind.Component);
                function.Invoke(TypeRegistry.Get<Position>(), DataType.Kind.ArrayElement);
            }
        }
    }
}