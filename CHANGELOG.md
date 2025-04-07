# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
