<h1 align="center">Semsamoon Unity DOTS Perception</h1>

AI Perception framework for Unity's Data-Oriented Technology Stack. It is designed to be simple,
high-performance, and easy to extend. The framework has a clear structure, effectively utilizes job
system and Burst compiler, and provides lots of tools and options for specific situations.

## Installation

The framework can be installed via Unity's Package Manager using a Git reference:

```
https://github.com/Semsamoon/unity-dots-perception.git
```

Another way is to add this line to the `Packages/manifest.json` file manually:

```
"com.semsamoon.unity-dots-perception": "https://github.com/Semsamoon/unity-dots-perception.git",
```

## AI Perception

The perception system allows AI agents to *perceive* the game world as humans do in real life.
In practice, there are two most valuable senses that allow AI agents to behave naturally:

- **Sight** - ability to see physical objects inside cone of vision
- **Hearing** - ability to hear sounds from close sources

Entities that can perceive are **Receivers** and entities that can be perceived are **Sources**.

The framework is mostly inspired by the Unreal Engine's
[AI Perception](https://dev.epicgames.com/documentation/unreal-engine/ai-perception-in-unreal-engine)
module, but since the Entity-Component-System approach significantly differs from the Object-Oriented
one, the resulting structure is hardly the same as in the Unreal Engine's solution.

## Framework - Elementary

### Sight Sense

To make an entity a sight receiver, there are four components required:

- `TagSightReceiver` - marks the entity a sight receiver
- `ComponentSightCone` - specifies the receiver's cone of vision
- `ComponentTeamFilter` - filters sources from selected teams
- `TagSightRaySingle` or `TagSightRayMultiple` - defines a way to detect sources

Use the `EntityManager` to add these components and specify their settings:

```csharp
// 'entityManager' is a variable of EntityManager
// 'receiver' is an entity of sight receiver

entityManager.AddComponent(receiver, new TagSightReceiver());

entityManager.AddComponent(receiver, new ComponentSightCone
{
    // Unity's physics collision filter for raycasting
    // Ensure the receiver is collidable with sources
    CollisionFilter = CollisionFilter.Default,
    
    // Cosines of cone of vision's half angles: horizontal [-1; 1] and vertical [0; 1]
    // x = 0 means x-half-angle = 90 degrees, so horizontal angle is 180 degrees
    // y = 0.5 means y-half-angle = 30 degrees, so vertical angle is 60 degrees
    AnglesCos = new float2(0, 0.5f),
    
    // Squared value of cone of vision's radius [0; infinity]
    // 10000 means radius = 100 units
    RadiusSquared = 10000,
    
    // Squared value of cone of vision's clipping radius [0; RadiusSquared]
    // Everything closer than this radius will be ignored and cannot be seen
    // 4 means clipping radius = 2
    ClipSquared = 0
});

entityManager.AddComponent(receiver, new ComponentTeamFilter
{
    // Mask represents teams of sources [0; 31] receiver can perceive
    // 1 << 0 means the receiver perceives sources from the first team (which index is 0)
    // Use bitwise 'or' operator to combine a few masks
    Perceives = 1 << 0 | 1 << 1 | 1 << 2,
});

// Uses only single ray cast to detect visibility
entityManager.AddComponent(receiver, new TagSightRaySingle());

// Uses default ray and additional rays casts to detect visibility
entityManager.AddComponent(receiver, new TagSightRayMultiple());

// This buffer is required for built-in multicast system
// It adds defined offsets to the source's position
entityManager.AddBuffer<BufferSightRayOffset>(receiver);
entityManager.GetBuffer<BufferSightRayOffset>(receiver).Add(new BufferSightRayOffset
{
    // If default ray cast fails, built-in system will try to cast a ray
    // from receiver's position to source's position plus this offset 
    Value = new float3(0, 0.5f, 0)
});
```

> [!WARNING]
> The framework systems never check values in components for consistency and correctness.
> Using meaningless or out-of-range values may cause errors of the perception system.

There are a few optional components and buffers may be added to the receiver:

- `ComponentSightExtend` - adds an extended cone of vision for currently perceived sources
- `ComponentSightOffest` - adds a local offset to the receiver's position
- `ComponentSightMemory` - adds a receiver's memory for lost sources
- `BufferSightChild` - stores entities whose colliders counts as receiver's

```csharp
// 'entityManager' is a variable of EntityManager
// 'receiver' is an entity of sight receiver

entityManager.AddComponent(receiver, new ComponentSightExtend
{
    // Cosines of extended cone of vision's half angles:
    // horizontal [-1; ComponentSightCone.AnglesCos.x] and vertical [0; ComponentSightCone.AnglesCos.y]
    // x = -1 means horizontal angle is 360 degrees - sees everything around
    // y = 0 means vertical angle is 180 degrees - sees everything above and under
    AnglesCos = new float2(-1, 0),
    
    // Squared value of extended cone of vision's radius [ComponentSightCone.RadiusSquared; infinity]
    RadiusSquared = 20000,
    
    // Squared value of extended cone of vision's clipping radius [0; ComponentSightCone.ClipSquared]
    // Everything closer will be ignored and cannot be seen
    // 0 means no clipping
    ClipSquared = 0
});

entityManager.AddComponent(receiver, new ComponentSightOffset
{
    // Offset is added to receiver's cone of vision's origin
    // By default cone of vision's origin is LocalToWorld.Position
    Receiver = new float3(0, 1, 0),
});

entityManager.AddComponent(receiver, new ComponentSightMemory
{
    // Time in seconds [0; infinity] of how long does receiver can remember lost source
    Time = 1
});

entityManager.AddBuffer<BufferSightChild>(receiver);
entityManager.GetBuffer<BufferSightChild>(receiver).Add(new BufferSightChild
{
    // Collider on 'receiverChild' will be counted as a part of the receiver's during raycasting
    Value = receiverChild
});
```

The perception system outputs an array of currently perceived and memorized
sources for each receiver. They are stored in associated entity buffers:

- `BufferSightPerceive` - contains perceived sources and their positions
- `BufferSightMemory` - contains memorized sources, their last seen positions and keeping durations

> [!CAUTION]
> Never modify perception system's buffers if not absolutely sure about safety of the operation.
> Some data inside buffers is used by framework's systems, and overwriting it may cause errors.

These buffers are added automatically to each entity with `TagSightReceiver` component.
Get the buffer and iterate over it to proceed custom logic on data:

```csharp
// 'entityManager' is a variable of EntityManager
// 'receiver' is an entity of sight receiver

foreach (var perceive in entityManager.GetBuffer<BufferSightPerceive>(receiver))
{
    // perceive.Source is a source entity itself
    // perceive.Position is a source's position where it was seen
}

foreach (var memory in entityManager.GetBuffer<BufferSightMemory>(receiver))
{
    // memory.Source is a source entity itself
    // memory.Position is a source's position where it was seen last time it was perceived
    // memory.Time is time in seconds of how much longer the source will be kept in memory 
}
```

> [!WARNING]
> After removing a source, receivers that perceived it will store it in memory, so ensure
> the source's existence with `EntityManager.Exists` before using it to avoid errors.

To make entity a sight source, there are two components required:

- `TagSightSource` - marks the entity a sight source
- `ComponentTeamFilter` - specifies team of the source

And there are one optional component and one optional buffer:

- `ComponentSightOffset` - adds a local offset to the source's position
- `BufferSightChild` - stores entities whose colliders counts as source's

```csharp
// 'entityManager' is a variable of EntityManager
// 'source' is an entity of sight source

entityManager.AddComponent(source, new TagSightSource());

entityManager.AddComponent(source, new ComponentTeamFilter
{
    // Mask represents teams [0; 31] the source belongs to
    // uint.MaxValue means the source belongs to all the teams
    BelongsTo = uint.MaxValue,
});

entityManager.AddComponent(source, new ComponentSightOffset
{
    // Offset is added to the source's position
    // By default source's position is LocalToWorld.Position
    Source = new float3(1, 0, 0),
});

entityManager.AddBuffer<BufferSightChild>(source);
entityManager.GetBuffer<BufferSightChild>(source).Add(new BufferSightChild
{
    // Collider on 'sourceChild' will be counted as a part of the source's during raycasting
    Value = sourceChild
});
```

> [!IMPORTANT]
> `ComponentTeamFilter` and `ComponentSightOffset` components and `BufferSightChild` are used
> for both receiver and source, but the fields and values differs. If an entity must be a
> receiver and a source at the same time, do not forget to fill all fields appropriately.

To simplify creating sight receivers snd sources, there exists a `SightSenseAuthoring` class.
Attach it to the Game Object that is going to be baked and customize. All settings are displayed
in human-readable form, the authoring class converts them to components and buffers automatically.
When the game object is selected, debugging lines display cone of vision and source's position.

> [!NOTE]
> `SightSenseAuthoring` does not add unnecessary components or buffers. For example, if 'offset'
> both for receiver and source is set to (0, 0, 0), no `ComponentSightOffset` will be attached.

Drawing lines for selected receivers works only in Editor Mode. To enable visual debugging
lines at runtime, the special enableable component `TagSightDebug` is added automatically
to each receiver. Enable the component to show the debugging lines and disable to hide.
This component only exists in Editor and will be stripped when build the project.

> [!CAUTION]
> All systems use jobs. To avoid flickering, the debugging system forces all jobs to complete.
> After building, this synchronization point disappears, and if other systems try to write to
> or read from buffers that are currently used by jobs, exception will be thrown. To prevent
> this, either use jobs in systems too or ensure awaiting the completion of previous jobs.

### Hearing Sense

To make entity a hearing receiver, there are two components required:

- `TagHearingReceiver` - marks the entity a hearing receiver
- `ComponentTeamFilter` - filters sources from selected teams

And there are two optional components:

- `ComponentHearingOffset` - adds a local offset to the receiver's position
- `ComponentHearingMemory` - adds a receiver's memory for heard sources

```csharp
// 'entityManager' is a variable of EntityManager
// 'receiver' is an entity of hearing receiver

entityManager.AddComponent(receiver, new TagHearingReceiver());

entityManager.AddComponent(receiver, new ComponentTeamFilter
{
    // Mask represents teams of sources [0; 31] receiver can perceive
    Perceives = 1 << 3 | 1 << 5,
});

entityManager.AddComponent(receiver, new ComponentHearingOffset
{
    // Offset is added to the receiver's position
    Value = new float3(1, 0, 0),
});

entityManager.AddComponent(receiver, new ComponentHearingMemory
{
    // Time in seconds [0; infinity] of how long does receiver can remember heard source
    Time = 1
});
```

> [!NOTE]
> Keeping such a similarity in structure and naming is a reasoned choice
> to simplify learning of and working with different senses.

Similar to sight receivers, currently perceived and memorized sources
for each hearing receiver are stored in associated entity buffers:

- `BufferHearingPerceive` - contains perceived sources and their positions
- `BufferHearingMemory` - contains memorized sources, their last seen positions and keeping durations

These buffers are added automatically to each entity with `TagHearingReceiver` component.
Get the buffer and iterate over it to proceed custom logic on data:

```csharp
// 'entityManager' is a variable of EntityManager
// 'receiver' is an entity of hearing receiver

foreach (var perceive in entityManager.GetBuffer<BufferHearingPerceive>(receiver))
{
    // perceive.Source is a source entity itself
    // perceive.Position is a source's position where it was heard
}

foreach (var memory in entityManager.GetBuffer<BufferHearingMemory>(receiver))
{
    // memory.Source is a source entity itself
    // memory.Position is a source's position where it was heard last time it was perceived
    // memory.Time is time in seconds of how much longer the source will be kept in memory 
}
```

To make entity a hearing source, there are three components required:

- `TagHearingSource` - marks the entity a hearing source
- `ComponentHearingSphere` - configures sound spherical wave's propagation
- `ComponentTeamFilter` - specifies team of the source

And there is only one optional component:

- `ComponentHearingOffset` - adds a local offset to the source's position

```csharp
// 'entityManager' is a variable of EntityManager
// 'source' is an entity of hearing source

entityManager.AddComponent(source, new TagHearingSource());

entityManager.AddComponent(source, new ComponentHearingSphere
{
    // Speed of the sound wave's propagation per second [0; infinity]
    Speed = 1000,
    
    // Squared value of the sound wave's max propagation distance [0; infinity]
    // 40000 means radius = 200 units
    RangeSquared = 40000,
    
    // Time in seconds [0; infinity] of the sound's duration
    // 0 means no duration, sound is instant
    Duration = 0
});

entityManager.AddComponent(source, new ComponentTeamFilter
{
    // Mask represents teams [0; 31] the source belongs to
    BelongsTo = 1 << 5,
});

entityManager.AddComponent(source, new ComponentHearingOffset
{
    // Offset is added to the source's position
    Value = new float3(1, 0, 0),
});
```

> [!NOTE]
> `ComponentTeamFilter` is common for both sight and hearing senses.
> Its filtering layers are always applied to all senses on the entity.

> [!NOTE]
> `ComponentHearingOffset` differs from `ComponentSightOffset` because
> it does not separate the offset to receiver's and source's ones.

A sound wave consists of two spheres: an external and an internal one. The external sphere starts
propagation first. After the `ComponentHearingSphere.Duration` delay, the internal sphere begins
propagation. Both stop when reach the `ComponentHearingSphere.RangeSquared` distance. When the
internal sphere finishes propagating, the source is automatically removed.

> [!TIP]
> When some receiver have to make noise, it is better practice to create a new
> entity-source each time and add some project-specific relationships between
> them to avoid automatic removing and keep the structure more flexible.

To simplify creating hearing receivers and sources, there exists a `HearingSenseAuthoring` class.
Attach it to the Game Object that is going to be baked and customize. All settings are displayed
in human-readable form, the authoring class converts them to components and buffers automatically.
To set duration equal to *infinity*, set negative value to its authoring class' field (it snaps to
-1). When the game object is selected, debugging lines display receiver's position and sound wave.

Drawing lines for selected receivers and sources works only in Editor Mode. To enable visuals
at runtime, the special enableable component `TagHearingDebug` is added automatically to each
receiver and source. Enable the component to show the debugging lines and disable to hide.
This component only exists in Editor and will be stripped when build the project.

> [!IMPORTANT]
> All remarks about according components, buffers and authoring about
> the sight sense are absolutely truthful for the hearing sense.

## Framework - Advanced

### Internal Components and Buffers

To make the systems work, a few internal components and buffers are added automatically:

- `ComponentSightPosition` - stores total world-space receiver's and source's position

Position component is added to apply position modifications, such as with
`ComponentSightOffset` component. This also allows to overwrite receiver's or
source's position from user's systems without need to edit built-in systems.

```csharp
public struct ComponentSightPosition
{
    public float3 Receiver;     // Receiver's total world-space position
    public float3 Source;       // Source's total world-space position
}
```

- `ComponentHearingPosition` - stores current and previous positions

The hearing detection system uses both current and previous positions in calculations
to avoid sound wave 'flying through' the receiver without being perceived.

```csharp
public struct ComponentHearingPosition
{
    public float3 Current;      // Receiver's or source's total world-space position
    public float3 Previous;     // Receiver's or source's total world-space position
}
```

- `ComponentHearingRadius` - holds current and previous radii of the sound wave and current duration

The hearing detection system uses both current and previous radii of the external and internal spheres.
It also operates with countdown to determine the time when internal sphere begins propagation.

```csharp
public struct ComponentHearingRadius
{
    public float CurrentSquared;            // Current external sphere's radius squared
    public float PreviousSquared;           // Previous external sphere's radius squared
    public float InternalCurrentSquared;    // Current internal sphere's radius squared
    public float InternalPreviousSquared;   // Previous internal sphere's radius squared
    public float CurrentDuration;           // Countdown to begin internal sphere's spreading
}
```

- `BufferSightCone` - contains all sources that are inside receiver's cone of vision

This buffer transits array of sources from the inside-cone system to the raycast system.
All these sources are inside the cone of vision, but not all of them are perceived.

```csharp
public struct BufferSightCone
{
    public Entity Source;       // Source entity itself
    public float3 Position;     // Source's position
}
```

### System Groups

All framework's systems update in special system groups in the following order:

- `FixedPerceptionSystemGroup` - base group updates in `FixedStepSimulationSystemGroup`

    - `FixedSightSystemGroup` - group for sight systems that should update with a fixed time step

        - `SystemSightPosition` - calculates current `ComponentSightPosition` values
        - `SystemSightCone` - fills `BufferSightCone` buffers with sources inside the cones of vision
        - `SystemSightPerceiveSingle` - fills `BufferSightPerceive` and `BufferSightMemory` buffers
          after the single-ray casting
        - `SystemSightPerceiveMultiple` - fills `BufferSightPerceive` and `BufferSightMemory` buffers
          after the multiple-rays casting

- `PerceptionSystemGroup` - base group updates in `SimulationSystemGroup` after `TransformSystemGroup`

    - `SightSystemGroup` - group for sight systems that should update each frame

        - `SystemSightMemory` - updates `BufferSightMemory.Time` and removes run out memories
        - `SystemSightDebug` - draws debugging lines for receivers with `TagSightDebug` enabled

    - `HearingSystemGroup` - group for all hearing systems that should update each frame

        - `SystemHearingMemory` - updates `BufferHearingMemory.Time` and removes run out memories
        - `SystemHearingPosition` - calculates current `ComponentHearingPosition` values
        - `SystemHearingSphere` - propagates sound waves with `ComponentHearingRadius`
        - `SystemHearingUpdate` - fills `BufferHearingPerceive` and `BufferHearingMemory` buffers
          after hearing detecting
        - `SystemHearingDebug` - draws debugging lines for entities with `TagHearingDebug` enabled

> [!NOTE]
> The `FixedPerceptionSystemGroup` exists because raycasting is more accurate with a fixed time
> step. Other perception's systems should update each frame to provide a quick response to events.

> [!NOTE]
> Debugging systems `SystemSightDebug` and `SystemHearingDebug` are always ordered last in their
> groups (`OrderLast = true`) to ensure other systems have completed. They exist only in Editor.

### Jobs and Limits

To improve performance of the perception system, all its systems schedule parallel jobs.
These jobs operate on chunks, so it may be useful to limit the number of chunks processed
per update. Here is why `ComponentSightLimit` and `ComponentHearingLimit` singletons exist.
Create these singletons and specify the limit for each system separately:

```csharp
// 'entityManager' is a variable of EntityManager

entityManager.CreateSingleton(new ComponentSightLimit
{
    ChunksAmountPosition = 0,           // Does not limit position system
    ChunksAmountMemory = 0,             // Does not limit memory system
    ChunksAmountCone = 20,              // Limits cone system to 20 chunks per update
    ChunksAmountPerceiveSingle = 10,    // Limits single-ray perceive system to 10 chunks per update
    ChunksAmountPerceiveOffset = 10     // Limits multiple-ray perceive system to 10 chunks per update
});

entityManager.CreateSingleton(new ComponentHearingLimit
{
    ChunksAmountPosition = 0,           // Does not limit position system
    ChunksAmountMemory = 0,             // Does not limit memory system
    ChunksAmountSphere = 0,             // Does not limit sphere system
    ChunksAmountPerceive = 10           // Limits perceive system to 10 chunks per update
});
```

> [!TIP]
> Position and memory systems (for both sight and hearing senses) and `SystemHearingRadius`
> are quite lightweight, so it makes no sense to limit them in most cases.

> [!TIP]
> Update limits at runtime to change CPU load dynamically.

> [!TIP]
> To keep detection high-quality it is better practice to set chunks limit of prior systems bigger
> than for the following ones. For example, if `ChunksAmountPerceiveSingle` value equals to 10 and
> `ChunksAmountPerceiveMultiple` equals to 10, then `ChunkAmountCone` should be greater than or
> equal to 10 + 10 = 20 to uphold both systems (because they depend on cone system's results).

### Useful Stuff

`DebugAdvanced` static class extends standard `Debug` class and provides the following methods:

- `DrawCurve` - draws a curve with specified position, rotation, radius, angle, color
  and sparsity (the less sparsity, the more lines will be used to display the curve)
- `DrawSphere` - draws a sphere of three circles with position, rotation, radius and sparsity
- `DrawOctahedron` - draws an octahedron (diamond) with set position, sizes and color

Also `SightSenseAuthoring` has static `DrawCone` method that draws cone of vision with specified
position, rotation, clipping distance, cone's total distance, half-angles, color and sparsity.

> [!IMPORTANT]
> All drawing methods exist only in Editor. Do not call them in building code.

Attribute `[TeamFilterMask]` turns `uint` variable into a team mask with a custom property drawer.

To serialize `CollisionFilter` or `TeamFilter`, use`CollisionFilterSerializable`
and `TeamFilterSerializable` structs respectively and cast them to filters.

`Constants` static class contains a collection of predefined colors that are *softer* than in `Color`.

## FAQ

#### How to implement other multi-rays casting systems?

Use custom tag instead of `TagSightRayMultiple` and create system that operates on the receivers
with this tag. Refer to the `SystemSightPerceiveOffset` source code and adapt its structure. The
core difference will be in jobs - carefully update them to achieve desirable behavior. A lot of
commonly-used methods are already written in `SystemSightPerceiveSingle` with public access.

#### How to implement other senses?

A **smell** sense is already implemented - use the hearing system to simulate odor propagation.
Other senses could be developed with the same common structure and specific components and buffers.
