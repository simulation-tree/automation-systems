using Automations.Components;
using Simulation;
using System;
using Unmanaged;
using Worlds;

namespace Automations.Systems
{
    public readonly partial struct StateMachineSystem : ISystem
    {
        void ISystem.Start(in SystemContainer systemContainer, in World world)
        {
        }

        void ISystem.Update(in SystemContainer systemContainer, in World world, in TimeSpan delta)
        {
            ComponentQuery<IsStateful> query = new(world);
            foreach (var r in query)
            {
                ref IsStateful stateful = ref r.component1;
                uint statefulEntity = r.entity;
                if (stateful.stateMachineReference == default)
                {
                    throw new InvalidOperationException($"Stateful entity `{statefulEntity}` does not have a state machine reference");
                }

                USpan<Parameter> parameters = world.GetArray<Parameter>(statefulEntity);
                uint stateMachineEntity = world.GetReference(statefulEntity, stateful.stateMachineReference);
                USpan<AvailableState> availableStates = world.GetArray<AvailableState>(stateMachineEntity);
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
                USpan<Transition> transitions = world.GetArray<Transition>(stateMachineEntity);
                foreach (Transition transition in transitions)
                {
                    if (transition.sourceStateHash == currentStateHash)
                    {
                        if (IsConditionMet(transition, parameters))
                        {
                            if (TryGetAvailableStateIndex(transition.destinationStateHash, availableStates, out uint newStateIndex))
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

        void ISystem.Finish(in SystemContainer systemContainer, in World world)
        {
        }

        private static bool IsConditionMet(Transition transition, USpan<Parameter> parameters)
        {
            float value = transition.value;
            Transition.Condition condition = transition.condition;
            float parameterValue = GetParameterValue(transition.parameterHash, parameters);
            if (condition == Transition.Condition.Equal)
            {
                return parameterValue == value;
            }
            else if (condition == Transition.Condition.NotEqual)
            {
                return parameterValue != value;
            }
            else if (condition == Transition.Condition.GreaterThan)
            {
                return parameterValue > value;
            }
            else if (condition == Transition.Condition.GreaterThanOrEqual)
            {
                return parameterValue >= value;
            }
            else if (condition == Transition.Condition.LessThan)
            {
                return parameterValue < value;
            }
            else if (condition == Transition.Condition.LessThanOrEqual)
            {
                return parameterValue <= value;
            }
            else if (condition == Transition.Condition.None)
            {
                return false;
            }
            else
            {
                throw new NotSupportedException($"Unsupported condition `{condition}`");
            }
        }

        private static float GetParameterValue(int nameHash, USpan<Parameter> parameters)
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

        private static bool TryGetAvailableStateIndex(int nameHash, USpan<AvailableState> availableStates, out uint index)
        {
            for (uint i = 0; i < availableStates.Length; i++)
            {
                if (availableStates[i].name.GetHashCode() == nameHash)
                {
                    index = i;
                    return true;
                }
            }

            index = default;
            return false;
        }
    }
}