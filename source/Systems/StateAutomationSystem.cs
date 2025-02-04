using Automations.Components;
using Simulation;
using System;
using Unmanaged;
using Worlds;

namespace Automations.Systems
{
    public readonly partial struct StateAutomationSystem : ISystem
    {
        void ISystem.Start(in SystemContainer systemContainer, in World world)
        {
        }

        void ISystem.Update(in SystemContainer systemContainer, in World world, in TimeSpan delta)
        {
            ComponentQuery<IsStateful, IsAutomationPlayer> query = new(world);
            foreach (var r in query)
            {
                ref IsStateful statefulComponent = ref r.component1;
                ref IsAutomationPlayer player = ref r.component2;
                uint statefulEntity = r.entity;
                if (statefulComponent.state == default)
                {
                    //state not yet assigned
                    return;
                }

                rint stateMachineReference = statefulComponent.stateMachineReference;
                uint stateMachineEntity = world.GetReference(statefulEntity, stateMachineReference);
                USpan<AvailableState> states = world.GetArray<AvailableState>(stateMachineEntity);
                AvailableState state = states[statefulComponent.state - 1];
                int stateNameHash = state.name.GetHashCode();
                USpan<StateAutomationLink> links = world.GetArray<StateAutomationLink>(statefulEntity);
                foreach (StateAutomationLink link in links)
                {
                    if (link.stateNameHash == stateNameHash)
                    {
                        ref rint automationReference = ref player.automationReference;
                        uint desiredAutomationEntity = world.GetReference(statefulEntity, link.automationReference);
                        if (automationReference == default)
                        {
                            player.time = default;
                            player.componentType = link.componentType;
                            automationReference = world.AddReference(statefulEntity, desiredAutomationEntity);
                        }
                        else
                        {
                            uint currentAutomationEntity = world.GetReference(statefulEntity, automationReference);
                            if (currentAutomationEntity != desiredAutomationEntity)
                            {
                                player.time = default;
                                player.componentType = link.componentType;
                                world.SetReference(statefulEntity, automationReference, desiredAutomationEntity);
                            }
                            else
                            {
                                //automation already set
                            }
                        }

                        break;
                    }
                }
            }
        }

        void ISystem.Finish(in SystemContainer systemContainer, in World world)
        {
        }
    }
}