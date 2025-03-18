using Automations.Components;
using Simulation;
using System;
using Worlds;

namespace Automations.Systems
{
    public readonly partial struct StateAutomationSystem : ISystem
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
            int automationComponentType = world.Schema.GetComponentType<IsAutomationPlayer>();
            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.ContainsComponent(statefulComponentType) && definition.ContainsComponent(automationComponentType))
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    ComponentEnumerator<IsStateful> statefulComponents = chunk.GetComponents<IsStateful>(statefulComponentType);
                    ComponentEnumerator<IsAutomationPlayer> automationComponents = chunk.GetComponents<IsAutomationPlayer>(automationComponentType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        ref IsStateful statefulComponent = ref statefulComponents[i];
                        ref IsAutomationPlayer automationComponent = ref automationComponents[i];
                        if (statefulComponent.state == default)
                        {
                            //state not yet assigned
                            return;
                        }

                        uint statefulEntity = entities[i];
                        rint stateMachineReference = statefulComponent.stateMachineReference;
                        uint stateMachineEntity = world.GetReference(statefulEntity, stateMachineReference);
                        Values<AvailableState> states = world.GetArray<AvailableState>(stateMachineEntity);
                        AvailableState state = states[statefulComponent.state - 1];
                        int stateNameHash = state.name.GetHashCode();
                        Values<StateAutomationLink> links = world.GetArray<StateAutomationLink>(statefulEntity);
                        for (int l = 0; l < links.Length; l++)
                        {
                            StateAutomationLink link = links[l];
                            if (link.stateNameHash == stateNameHash)
                            {
                                ref rint automationReference = ref automationComponent.automationReference;
                                uint desiredAutomationEntity = world.GetReference(statefulEntity, link.automationReference);
                                if (automationReference == default)
                                {
                                    automationComponent.time = default;
                                    automationComponent.target = link.target;
                                    automationReference = world.AddReference(statefulEntity, desiredAutomationEntity);
                                }
                                else
                                {
                                    uint currentAutomationEntity = world.GetReference(statefulEntity, automationReference);
                                    if (currentAutomationEntity != desiredAutomationEntity)
                                    {
                                        automationComponent.time = default;
                                        automationComponent.target = link.target;
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
            }
        }

        void ISystem.Finish(in SystemContext context, in World world)
        {
        }
    }
}