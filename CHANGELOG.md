# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-05-07

### Added

- Package description in README file.
- Fixed step perception system group and fixed step sight system group.

### Changed

- Turn back transform force adding to hearing sources.
- Await physics every time in sight tests because systems were moved.

### Fixed

- Update sight cone, perceive single and offset systems in fixed step sight system group.
- Require memory buffer instead of memory component in sight perceive systems.

## [0.5.6] - 2025-05-05

### Added

- Dependencies to Package file.
- Clip radius in sight extend component.
- Usage of sight extend component's clip radius in authoring and systems.

### Changed

- Update sight clip debug to improve visibility.
- Extract constants into Constants static class.
- Draw shapes with gizmos only when sizes are not too small to improve performance.

### Fixed

- Update perception system group after transform system group.

## [0.5.5] - 2025-05-04

### Added

- Team filter's extensions to simplify frequent operations.
- Usage of team filter in hearing authoring, sight cone and hearing perceive systems.

### Changed

- Use burst compiler and DEBUG conditionals in advanced debug's methods.
- Move serializable collision filter from Sight folder to Common.
- Rename Sight Filter to Team Filter and move it from Sight folder to Common.
- Reduce nesting in sight authoring and update common headers in sight and hearing authoring.

## [0.5.4] - 2025-05-04

### Added

- Sight team filter component with XML documentation.
- Serializable sight team filter struct with custom attribute and drawer for mask.
- Usage of sight team filter component in authoring.

### Changed

- Merge sight clip and filter components with cone component for simplicity.
  Use merged sight cone component in systems, authoring and tests.
- Merge hearing duration component with sphere and radius components for simplicity.
  Use merged hearing sphere and radius components in systems, authoring and tests.
- Rename Debug folder to Common and use it for team filter.

### Fixed

- Set world position to hearing position component in authoring if it has no transform component.

## [0.5.3] - 2025-05-03

### Added

- Hearing limit component with XML documentation.
- Usage of hearing limit component in hearing systems to improve performance.
- Collision filter serialization in sight receiver authoring.

### Fixed

- Raise delay in hearing tests up to 0.1 seconds.
- Add transform component to queries in hearing position system.

## [0.5.2] - 2025-05-02

### Added

- Hearing components' extensions to simplify frequent operations.

### Changed

- Improve performance of hearing position, memory, sphere, perceive and debug systems with jobs.
- Remove unnecessary components in sight debug system's queries.
- Rename jobs in sight debug system.

### Fixed

- Check source existence in hearing and sight debug systems before using it from memory buffer.

## [0.5.1] - 2025-05-01

### Added

- Advanced debug draw sphere.
- Hearing authoring gizmos and custom editor.
- Hearing debug tag and debug settings component with XML documentation.
- Hearing debug system.

### Changed

- Shorten sight debug system's jobs for sources.

## [0.5.0] - 2025-04-30

### Added

- Hearing system group inside perception system group.
- Place hearing systems into hearing system group.
- Hearing receiver and source authoring.

### Changed

- Reorder settings and rename sections' headers in sight authoring.

## [0.4.3] - 2025-04-29

### Added

- Hearing memory component and memory buffer with XML documentation and extensions.
- Hearing memory system and usage of hearing memory component and memory buffer in perceive system.
- Test for hearing memory system.

### Fixed

- Correct order of systems' updates.

## [0.4.2] - 2025-04-28

### Added

- Hearing perceive buffer with XML documentation.
- Hearing perceive sources system.
- Tests for hearing perceive sources system.

### Changed

- Merge source creation and sphere setting in entity builder in tests.

## [0.4.1] - 2025-04-28

### Added

- Hearing sphere, radius and duration components with XML documentation.
- Hearing sphere of wave propagation system.
- Tests for hearing sphere of wave propagation system.

## [0.4.0] - 2025-04-27

### Added

- Hearing receiver and source tags with XML documentation.
- Hearing offset and position components with XML documentation.
- Hearing position system.
- Tests for hearing position system.

## [0.3.5] - 2025-04-23

### Added

- Sight limit and debug settings components with XML documentation.
- Usage of sight limit component in sight systems to improve performance.

### Changed

- Improve sight debug cone of vision gizmos.
- Remove redundant hit collectors.

### Fixed

- Cover try-finally blocks in sight tests to clear data even when test fails.

## [0.3.4] - 2025-04-22

### Added

- Perception system group with sight system group inside.
- Place sight systems to sight system group.
- Sight components' and buffers' extensions to simplify frequent operations.
- Usage of sight components' and buffers' extensions in sight systems to shorten.

### Changed

- Shorten sight position, memory, cone, perceive single and offset systems.
- Automatically add LocalToWorld component to receivers and sources.
- Improve performance of sight debug system with jobs.

### Fixed

- Constraints in tests assembly definition to prevent their building.

## [0.3.3] - 2025-04-15

### Added

- Sight collision filter component with XML documentation.
- Usage of sight collision filter in perceive systems.

### Changed

- Improve performance of sight position, memory, cone, perceive single and offset systems with jobs.
- Inline static check methods in hit collectors because sight perceive systems do not need them anymore.

### Fixed

- Wait for completing jobs in sight debug system.
- Update sight perceive offset system after perceive single system.

## [0.3.2] - 2025-04-12

### Added

- Advanced debug draw octahedron.
- Sight source authoring gizmos displaying position.
- Sight debug sources in receiver's cone of vision and memory system.

### Fixed

- Reference with packages names instead of GUID in runtime and tests assembly definitions.
- Hit fraction should be less than clip to fail check in collectors.
- Move automatic adding sight perceive buffer from sight perceive single and offset systems
  to the sight cone system because it performs earlier and uses sight perceive buffer.

## [0.3.1] - 2025-04-12

### Added

- Advanced debug class that allows to draw curves.
- Assembly definition with Editor folder.
- Sight debug tag with XML documentation.
- Sight debug receiver's cone of vision system.
- Sight receiver and source authoring with custom editor inspector and gizmos displaying cone of vision.

### Changed

- Redefine angles to use cos instead of tan in tests.

### Fixed

- Store cos of the half angle instead of tan of the whole angle for cone of vision because
  tan describes angles in [-PI/2; PI/2] but cos allows to represent angles in [0; PI]. And
  as far as it is a half angle, doubling it allows to cover [-PI; PI].

## [0.3.0] - 2025-04-08

### Added

- Sight memory component and memory buffer with XML documentation.
- Usage of sight memory component and buffer in perceive systems.
- Tests for sight memory system.

## [0.2.4] - 2025-04-07

### Added

- Usage of sight clip component in perceive systems during ray casting.
- Tests for sight perceive systems with clip component.

### Changed

- Extract physics await and collider in tests.
- Separate receiver's and source's positions and offsets in according components.
- Remove position component from entity builder in tests because it is added automatically.

## [0.2.3] - 2025-04-07

### Changed

- Require sight position component for cone of vision and ray casting. No LocalToWorld component support.
- Require sight cone buffer for ray casting. Add it to all receivers.
- Rename sight 'cone clip' and 'cone extend' components to 'clip' and 'extend' accordingly.
- Remove sight cone offset component and use only offset instead.
- Satisfy requirements and renaming in tests.
- Extract data in systems to separated structures to maintain them easier.
- Check zero sight offset in perceive offset system automatically. No need to add zero offset.

### Fixed

- Destroy entities in the end of the tests to allow starting all in editor.
- Sight ray offset transforms to local coordinates.

## [0.2.2] - 2025-04-03

### Added

- Sight ray offset buffer and multiple rays cast tag with XML documentation.
- Sight perceive sources with multiple rays casting system.
- Tests for sight perceive sources with multiple rays casting system.

### Changed

- Rename sight 'inside cone' buffer to 'cone'.
- Rename sight 'perceive' with single ray casting system to 'perceive single'.

## [0.2.1] - 2025-04-03

### Added

- Sight entity's children buffer and single ray cast tag with XML documentation.
- Usage of sight entity's children buffer in perceive system during ray casting
  and single ray cast tag during querying.
- Tests for sight perceive system with entity's children buffer and single ray cast tag.

## [0.2.0] - 2025-03-31

### Added

- Sight cone near clipping plane, cone offset, and cone extend radius components with XML documentation.
- Usage of sight cone clip, offset, and extend components in sight cone of vision system.
- Tests for sight cone of vision system with cone clip, offset, and extend components.

## [0.1.3] - 2025-03-30

### Added

- Sight perceive sources buffer with XML documentation.
- Sight perceive sources system.
- Tests for sight perceive sources system.

### Changed

- Extract entity builder to simplify creating entities in tests.

## [0.1.2] - 2025-03-29

### Added

- Sight cone of vision component and inside cone of vision buffer with XML documentation.
- Sight cone of vision system.
- Tests for sight cone of vision system.

### Fixed

- Sight position system operates only on receivers and sources.
- Update tests for sight position system with receiver and source tags.

### Changed

- Rename 'entity' variables to 'receiver' and 'source' accordingly in tests.

## [0.1.1] - 2025-03-27

### Added

- Sight offset and position components with XML documentation.
- Sight position system.
- Tests for sight position system.

## [0.1.0] - 2025-03-27

### Added

- Sight receiver and source components with XML documentation.
- README, LICENSE and Package files.
- Assembly definition with Runtime folder.
