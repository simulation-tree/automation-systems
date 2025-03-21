using Automations.Components;
using Simulation;
using System;
using Worlds;

namespace Automations.Systems
{
    public readonly partial struct StateMachineSystem : ISystem
    {
        readonly void IDisposable.Dispose()
        {
        }

        void ISystem.Start(in SystemContext context, in World world)
        {
        }

        void ISystem.Update(in SystemContext context, in World world, in TimeSpan delta)
        {
            int statefulComponentType = world.Schema.GetComponentType<IsStateful>();
            foreach (Chunk chunk in world.Chunks)
            {
                if (chunk.Definition.ContainsComponent(statefulComponentType))
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    ComponentEnumerator<IsStateful> components = chunk.GetComponents<IsStateful>(statefulComponentType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        ref IsStateful stateful = ref components[i];
                        uint statefulEntity = entities[i];
                        if (stateful.stateMachineReference == default)
                        {
                            throw new InvalidOperationException($"Stateful entity `{statefulEntity}` does not have a state machine reference");
                        }

                        Values<Parameter> parameters = world.GetArray<Parameter>(statefulEntity);
                        uint stateMachineEntity = world.GetReference(statefulEntity, stateful.stateMachineReference);
                        Values<AvailableState> availableStates = world.GetArray<AvailableState>(stateMachineEntity);
                        if (stateful.state == default)
                        {
                            stateful.state = world.GetComponent<IsStateMachine>(stateMachineEntity).entryState;
                            if (stateful.state == default)
                            {
                                throw new InvalidOperationException($"State machine `{stateMachineEntity}` does not have an entry state assigned");
                            }
                        }

                        AvailableState currentState = availableStates[stateful.state - 1];
                        int currentStateHash = currentState.name.GetHashCode();
                        Values<Transition> transitions = world.GetArray<Transition>(stateMachineEntity);
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

        void ISystem.Finish(in SystemContext context, in World world)
        {
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