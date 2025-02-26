using Automations.Components;
using Collections.Generic;
using Simulation;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Unmanaged;
using Worlds;
using Array = Collections.Array;

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
                            Evaluate(world, entity, player, automationEntity);
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

        private readonly void Evaluate(World world, uint playerEntity, IsAutomationPlayer player, uint automationEntity)
        {
            DataType dataType = player.target.targetType;
            TimeSpan time = player.time;
            ThrowIfDataTypeKindNotSupported(dataType.kind);

            ushort dataTypeSize = dataType.size;
            IsAutomation automationComponent = world.GetComponent<IsAutomation>(automationEntity);
            DataType keyframeType = automationComponent.keyframeType;
            Array keyframeValues = world.GetArray(automationEntity, keyframeType);
            USpan<float> keyframeTimes = world.GetArray<KeyframeTime>(automationEntity).AsSpan<float>();
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

            uint current = 0;
            for (uint i = 0; i < keyframeValues.Length; i++)
            {
                float keyframeTime = keyframeTimes[i];
                if (timeInSeconds >= keyframeTime)
                {
                    current = i;
                }
            }

            bool loop = automationComponent.loop;
            uint next = current + 1;
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

            Allocation currentKeyframe = keyframeValues.Items.Read(current * keyframeSize);
            Allocation nextKeyframe = keyframeValues.Items.Read(next * keyframeSize);
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
                Allocation target;
                if (dataType.kind == DataType.Kind.ArrayElement)
                {
                    uint bytePosition = player.target.bytePosition;
                    Array array = world.GetArray(playerEntity, dataType);

                    ThrowIfOutOfArrayRange(bytePosition, array.Length * dataTypeSize);

                    target = array.Items.Read(bytePosition);
                }
                else
                {
                    target = world.GetComponent(playerEntity, dataType);
                    target = target.Read(player.target.bytePosition);
                }

                currentKeyframe.CopyTo(target, keyframeSize);
            }
            else
            {
                byte index = automationComponent.interpolationMethod.value;
                index--;

                Allocation target;
                if (dataType.kind == DataType.Kind.ArrayElement)
                {
                    uint bytePosition = player.target.bytePosition;
                    Array array = world.GetArray(playerEntity, dataType);

                    ThrowIfOutOfArrayRange(bytePosition, array.Length * dataTypeSize);

                    target = array.Items.Read(bytePosition);
                }
                else
                {
                    target = world.GetComponent(playerEntity, dataType);
                    target = target.Read(player.target.bytePosition);
                }

                Interpolation interpolation = interpolationFunctions[index];
                interpolation.Invoke(currentKeyframe, nextKeyframe, timeProgress, target, keyframeSize);
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfDataTypeKindNotSupported(DataType.Kind kind)
        {
            if (kind != DataType.Kind.Component && kind != DataType.Kind.ArrayElement)
            {
                throw new NotSupportedException("Only components and array elements are supported");
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfOutOfArrayRange(uint bytePosition, uint byteLength)
        {
            if (bytePosition >= byteLength)
            {
                throw new IndexOutOfRangeException("Index is out of range");
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Temp
        {
            public byte first;
            public float second;
            public Vector4 third;
            public uint fourth;
        }
    }
}