# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
- Sight components' and buffers' extensions to simplify often operations.
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
