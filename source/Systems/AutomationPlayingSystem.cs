using Automations.Components;
using Collections;
using Simulation;
using System;
using Unmanaged;
using Worlds;

namespace Automations.Systems
{
    public readonly partial struct AutomationPlayingSystem : ISystem
    {
        private readonly List<Interpolation> interpolationFunctions;

        private AutomationPlayingSystem(List<Interpolation> interpolationFunctions)
        {
            this.interpolationFunctions = interpolationFunctions;
        }

        void ISystem.Start(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                List<Interpolation> interpolationFunctions = new();
                foreach (Interpolation interpolation in BuiltInInterpolations.all)
                {
                    interpolationFunctions.Add(interpolation);
                }

                systemContainer.Write(new AutomationPlayingSystem(interpolationFunctions));
            }
        }

        void ISystem.Update(in SystemContainer systemContainer, in World world, in TimeSpan delta)
        {
            ComponentType componentType = world.Schema.GetComponent<IsAutomationPlayer>();
            foreach (Chunk chunk in world.Chunks)
            {
                if (chunk.Definition.Contains(componentType))
                {
                    USpan<uint> entities = chunk.Entities;
                    USpan<IsAutomationPlayer> components = chunk.GetComponents<IsAutomationPlayer>(componentType);
                    for (uint i = 0; i < entities.Length; i++)
                    {
                        ref IsAutomationPlayer player = ref components[i];
                        if (player.automationReference != default)
                        {
                            uint entity = entities[i];
                            player.time += delta;
                            uint automationEntity = world.GetReference(entity, player.automationReference);
                            Evaluate(world, entity, player.componentType, automationEntity, player.time);
                        }
                    }
                }
            }
        }

        void ISystem.Finish(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                interpolationFunctions.Dispose();
            }
        }

        public readonly InterpolationMethod AddInterpolation(Interpolation interpolation)
        {
            interpolationFunctions.Add(interpolation);
            return new((byte)interpolationFunctions.Count);
        }

        private readonly unsafe void Evaluate(World world, uint playerEntity, DataType componentType, uint automationEntity, TimeSpan time)
        {
            ushort componentSize = componentType.size;
            IsAutomation automationComponent = world.GetComponent<IsAutomation>(automationEntity);
            DataType keyframeType = automationComponent.keyframeType;
            Allocation keyframeValues = world.GetArray(automationEntity, keyframeType, out uint keyframeCount);
            USpan<float> keyframeTimes = world.GetArray<KeyframeTime>(automationEntity).As<float>();
            if (keyframeCount == 0)
            {
                return;
            }

            ushort keyframeSize = keyframeType.size;
            float timeInSeconds = (float)time.TotalSeconds;
            float finalKeyframeTime = keyframeTimes[keyframeCount - 1];
            if (timeInSeconds >= finalKeyframeTime)
            {
                if (automationComponent.loop)
                {
                    timeInSeconds %= finalKeyframeTime;
                }
                else
                {
                    timeInSeconds = finalKeyframeTime;
                }
            }

            uint current = 0;
            if (keyframeCount > 0)
            {
                for (uint i = 0; i < keyframeCount; i++)
                {
                    float keyframeTime = keyframeTimes[i];
                    if (timeInSeconds >= keyframeTime)
                    {
                        current = i;
                    }
                }
            }

            bool loop = automationComponent.loop;
            uint next = current + 1;
            if (next == keyframeCount)
            {
                if (loop)
                {
                    next = 0;
                }
                else
                {
                    next = current;
                }
            }

            void* currentKeyframe = keyframeValues.Read(current * keyframeSize);
            void* nextKeyframe = keyframeValues.Read(next * keyframeSize);
            float currentKeyframeTime = keyframeTimes[current];
            float nextKeyframeTime = keyframeTimes[next];
            float timeDelta = nextKeyframeTime - currentKeyframeTime;
            float timeProgress = (timeInSeconds - currentKeyframeTime) / timeDelta;
            if (float.IsNaN(timeProgress))
            {
                timeProgress = 0f;
            }

            if (automationComponent.interpolationMethod == default)
            {
                void* component = world.GetComponent(playerEntity, componentType);
                System.Runtime.CompilerServices.Unsafe.CopyBlock(component, currentKeyframe, componentSize);
            }
            else
            {
                byte index = automationComponent.interpolationMethod.value;
                index--;
                void* component = world.GetComponent(playerEntity, componentType);

                Interpolation interpolation = interpolationFunctions[index];
                interpolation.Invoke(currentKeyframe, nextKeyframe, timeProgress, component, componentSize);
            }
        }
    }
}