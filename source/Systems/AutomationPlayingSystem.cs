using Automations.Components;
using Automations.Messages;
using Collections.Generic;
using Simulation;
using System;
using System.Diagnostics;
using Unmanaged;
using Worlds;

namespace Automations.Systems
{
    public partial class AutomationPlayingSystem : SystemBase, IListener<AutomationUpdate>
    {
        private readonly World world;
        private readonly List<Interpolation> interpolationFunctions;
        private readonly int automationPlayerType;
        private readonly int automationType;
        private readonly int keyframeTimeArrayType;

        public AutomationPlayingSystem(Simulator simulator, World world) : base(simulator)
        {
            this.world = world;
            interpolationFunctions = new(4);
            AddBuiltInInterpolations();

            Schema schema = world.Schema;
            automationPlayerType = schema.GetComponentType<IsAutomationPlayer>();
            automationType = schema.GetComponentType<IsAutomation>();
            keyframeTimeArrayType = schema.GetArrayType<KeyframeTime>();
        }

        public override void Dispose()
        {
            interpolationFunctions.Dispose();
        }

        void IListener<AutomationUpdate>.Receive(ref AutomationUpdate message)
        {
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            for (int c = 0; c < chunks.Length; c++)
            {
                Chunk chunk = chunks[c];
                if (chunk.ComponentTypes.Contains(automationPlayerType))
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    ComponentEnumerator<IsAutomationPlayer> components = chunk.GetComponents<IsAutomationPlayer>(automationPlayerType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        ref IsAutomationPlayer player = ref components[i];
                        if (player.automationReference != default)
                        {
                            uint entity = entities[i];
                            player.time += message.deltaTime;
                            uint automationEntity = world.GetReference(entity, player.automationReference);
                            Evaluate(world, entity, player, automationEntity);
                        }
                    }
                }
            }
        }

        public InterpolationMethod AddInterpolation(Interpolation interpolation)
        {
            interpolationFunctions.Add(interpolation);
            return new((byte)interpolationFunctions.Count);
        }

        private void AddBuiltInInterpolations()
        {
            foreach (Interpolation interpolation in BuiltInInterpolations.all)
            {
                interpolationFunctions.Add(interpolation);
            }
        }

        private void Evaluate(World world, uint playerEntity, IsAutomationPlayer player, uint automationEntity)
        {
            DataType dataType = player.target.targetType;
            float time = (float)player.time;
            ThrowIfDataTypeKindNotSupported(dataType.kind);

            ushort dataTypeSize = dataType.size;
            IsAutomation automationComponent = world.GetComponent<IsAutomation>(automationEntity, automationType);
            DataType keyframeType = automationComponent.keyframeType;
            Values keyframeValues = world.GetArray(automationEntity, keyframeType.index);
            Span<float> keyframeTimes = world.GetArray<KeyframeTime>(automationEntity, keyframeTimeArrayType).AsSpan<float>();
            if (keyframeValues.Length == 0)
            {
                return;
            }

            ushort keyframeSize = keyframeType.size;
            float finalKeyframeTime = keyframeTimes[keyframeValues.Length - 1];
            if (time >= finalKeyframeTime)
            {
                if (automationComponent.loop)
                {
                    time %= finalKeyframeTime;
                }
                else
                {
                    time = finalKeyframeTime;
                }
            }

            int current = 0;
            for (int i = 0; i < keyframeValues.Length; i++)
            {
                float keyframeTime = keyframeTimes[i];
                if (time >= keyframeTime)
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
            float timeProgress = (time - currentKeyframeTime) / timeDelta;
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