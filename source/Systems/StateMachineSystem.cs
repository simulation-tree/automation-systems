using Automations.Components;
using Automations.Messages;
using Simulation;
using System;
using Worlds;

namespace Automations.Systems
{
    public partial class StateMachineSystem : SystemBase, IListener<AutomationUpdate>
    {
        private readonly World world;
        private readonly int statefulType;
        private readonly int stateMachineType;
        private readonly int parameterArrayType;
        private readonly int transitionArrayType;
        private readonly int availableStatesArrayType;

        public StateMachineSystem(Simulator simulator, World world) : base(simulator)
        {
            this.world = world;
            Schema schema = world.Schema;
            statefulType = schema.GetComponentType<IsStateful>();
            stateMachineType = schema.GetComponentType<IsStateMachine>();
            parameterArrayType = schema.GetArrayType<Parameter>();
            transitionArrayType = schema.GetArrayType<Transition>();
            availableStatesArrayType = schema.GetArrayType<AvailableState>();
        }

        public override void Dispose()
        {
        }

        void IListener<AutomationUpdate>.Receive(ref AutomationUpdate message)
        {
            foreach (Chunk chunk in world.Chunks)
            {
                if (chunk.Definition.ContainsComponent(statefulType))
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    ComponentEnumerator<IsStateful> components = chunk.GetComponents<IsStateful>(statefulType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        ref IsStateful stateful = ref components[i];
                        uint statefulEntity = entities[i];
                        if (stateful.stateMachineReference == default)
                        {
                            throw new InvalidOperationException($"Stateful entity `{statefulEntity}` does not have a state machine reference");
                        }

                        Values<Parameter> parameters = world.GetArray<Parameter>(statefulEntity, parameterArrayType);
                        uint stateMachineEntity = world.GetReference(statefulEntity, stateful.stateMachineReference);
                        Values<AvailableState> availableStates = world.GetArray<AvailableState>(stateMachineEntity, availableStatesArrayType);
                        if (stateful.state == default)
                        {
                            stateful.state = world.GetComponent<IsStateMachine>(stateMachineEntity, stateMachineType).entryState;
                            if (stateful.state == default)
                            {
                                throw new InvalidOperationException($"State machine `{stateMachineEntity}` does not have an entry state assigned");
                            }
                        }

                        AvailableState currentState = availableStates[stateful.state - 1];
                        int currentStateHash = currentState.name.GetHashCode();
                        Values<Transition> transitions = world.GetArray<Transition>(stateMachineEntity, transitionArrayType);
                        foreach (Transition transition in transitions)
                        {
                            if (transition.sourceStateHash == currentStateHash)
                            {
                                if (IsConditionMet(transition, parameters))
                                {
                                    if (TryGetAvailableStateIndex(transition.destinationStateHash, availableStates, out int newStateIndex))
                                    {
                                        stateful.state = newStateIndex + 1;
                                        break;
                                    }
                                    else
                                    {
                                        throw new InvalidOperationException($"State with name hash `{transition.destinationStateHash}` on state machine `{stateMachineEntity}` couldn't be found");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static bool IsConditionMet(Transition transition, Span<Parameter> parameters)
        {
            float value = transition.value;
            Transition.Condition condition = transition.condition;
            float parameterValue = GetParameterValue(transition.parameterHash, parameters);
            return condition switch
            {
                Transition.Condition.Equal => parameterValue == value,
                Transition.Condition.NotEqual => parameterValue != value,
                Transition.Condition.GreaterThan => parameterValue > value,
                Transition.Condition.GreaterThanOrEqual => parameterValue >= value,
                Transition.Condition.LessThan => parameterValue < value,
                Transition.Condition.LessThanOrEqual => parameterValue <= value,
                Transition.Condition.None => false,
                _ => throw new NotSupportedException($"Unsupported condition `{condition}`")
            };
        }

        private static float GetParameterValue(int nameHash, Span<Parameter> parameters)
        {
            foreach (Parameter parameter in parameters)
            {
                if (parameter.name.GetHashCode() == nameHash)
                {
                    return parameter.value;
                }
            }

            throw new InvalidOperationException($"Parameter with name hash `{nameHash}` not found");
        }

        private static bool TryGetAvailableStateIndex(int nameHash, Span<AvailableState> availableStates, out int index)
        {
            for (int i = 0; i < availableStates.Length; i++)
            {
                if (availableStates[i].name.GetHashCode() == nameHash)
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }
    }
}