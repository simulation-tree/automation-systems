using Automations.Components;
using Collections.Generic;
using Simulation;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Unmanaged;
using Worlds;

namespace Automations.Systems
{
    public readonly partial struct AutomationPlayingSystem : ISystem
    {
        private readonly List<Interpolation> interpolationFunctions;

        public AutomationPlayingSystem()
        {
            interpolationFunctions = new(4);
            foreach (Interpolation interpolation in BuiltInInterpolations.all)
            {
                interpolationFunctions.Add(interpolation);
            }
        }

        public readonly void Dispose()
        {
            interpolationFunctions.Dispose();
        }

        void ISystem.Start(in SystemContext context, in World world)
        {
        }

        void ISystem.Update(in SystemContext context, in World world, in TimeSpan delta)
        {
            int componentType = world.Schema.GetComponentType<IsAutomationPlayer>();
            foreach (Chunk chunk in world.Chunks)
            {
                if (chunk.Definition.ContainsComponent(componentType))
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    ComponentEnumerator<IsAutomationPlayer> components = chunk.GetComponents<IsAutomationPlayer>(componentType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        ref IsAutomationPlayer player = ref components[i];
                        if (player.automationReference != default)
                        {
                            uint entity = entities[i];
                            player.time += delta;
                            uint automationEntity = world.GetReference(entity, player.automationReference);
                            Evaluate(world, entity, player, automationEntity);
                        }
                    }
                }
            }
        }

        void ISystem.Finish(in SystemContext context, in World world)
        {
        }

        public readonly InterpolationMethod AddInterpolation(Interpolation interpolation)
        {
            interpolationFunctions.Add(interpolation);
            return new((byte)interpolationFunctions.Count);
        }

        private readonly void Evaluate(World world, uint playerEntity, IsAutomationPlayer player, uint automationEntity)
        {
            DataType dataType = player.target.targetType;
            TimeSpan time = player.time;
            ThrowIfDataTypeKindNotSupported(dataType.kind);

            ushort dataTypeSize = dataType.size;
            IsAutomation automationComponent = world.GetComponent<IsAutomation>(automationEntity);
            DataType keyframeType = automationComponent.keyframeType;
            Values keyframeValues = world.GetArray(automationEntity, keyframeType.index);
            Span<float> keyframeTimes = world.GetArray<KeyframeTime>(automationEntity).AsSpan<float>();
            if (keyframeValues.Length == 0)
            {
                return;
            }

            ushort keyframeSize = keyframeType.size;
            float timeInSeconds = (float)time.TotalSeconds;
            float finalKeyframeTime = keyframeTimes[keyframeValues.Length - 1];
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

            int current = 0;
            for (int i = 0; i < keyframeValues.Length; i++)
            {
                float keyframeTime = keyframeTimes[i];
                if (timeInSeconds >= keyframeTime)
                {
                    current = i;
                }
            }

            bool loop = automationComponent.loop;
            int next = current + 1;
            if (next == keyframeValues.Length)
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

            MemoryAddress currentKeyframe = keyframeValues[current];
            MemoryAddress nextKeyframe = keyframeValues[next];
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
                MemoryAddress target;
                if (dataType.kind == DataType.Kind.Array)
                {
                    int bytePosition = player.target.bytePosition;
                    Values array = world.GetArray(playerEntity, dataType.index);

                    ThrowIfOutOfArrayRange(bytePosition, array.Length * dataTypeSize);

                    target = array.Read(bytePosition);
                }
                else
                {
                    target = world.GetComponent(playerEntity, dataType.index);
                    target = target.Read(player.target.bytePosition);
                }

                currentKeyframe.CopyTo(target, keyframeSize);
            }
            else
            {
                byte index = automationComponent.interpolationMethod.value;
                index--;

                MemoryAddress target;
                if (dataType.kind == DataType.Kind.Array)
                {
                    int bytePosition = player.target.bytePosition;
                    Values array = world.GetArray(playerEntity, dataType.index);

                    ThrowIfOutOfArrayRange(bytePosition, array.Length * dataTypeSize);

                    target = array.Read(bytePosition);
                }
                else
                {
                    target = world.GetComponent(playerEntity, dataType.index);
                    target = target.Read(player.target.bytePosition);
                }

                Interpolation interpolation = interpolationFunctions[index];
                interpolation.Invoke(currentKeyframe, nextKeyframe, timeProgress, target, keyframeSize);
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfDataTypeKindNotSupported(DataType.Kind kind)
        {
            if (kind != DataType.Kind.Component && kind != DataType.Kind.Array)
            {
                throw new NotSupportedException("Only components and arrays are supported");
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfOutOfArrayRange(int bytePosition, int byteLength)
        {
            if (bytePosition >= byteLength)
            {
                throw new IndexOutOfRangeException("Index is out of range");
            }
        }
    }
}