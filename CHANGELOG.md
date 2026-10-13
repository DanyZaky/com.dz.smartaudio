# Changelog

All notable changes to the **SmartAudio** package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-10-13

### Added
- **Fluent Playback API**:
  - `Play2D()`, `PlayAt()`, and `PlayAttached()` extensions for `AudioClip`.
  - Fluent chained modifiers: `.WithVolume()`, `.WithPitch()`, `.WithDelay()`, `.WithSpatialBlend()`, `.WithMinMaxDistance()`, `.WithLoop()`, `.WithChannel()`, and `.ToGroup()`.
  - Lightweight struct `AudioHandle` with generational index safety and controls (`Stop()`, `Pause()`, `Resume()`, `FadeOut()`, `FadeIn()`).
- **Audio Dynamics & Anti-Monotony**:
  - `WithRandomPitch()`, `WithPitchVariation()`, and `WithRandomVolume()`.
  - `PlayRandom2D()`, `PlayRandomAt()`, and `PlayRandomAttached()` extensions on `AudioClip[]` and `IList<AudioClip>` guaranteeing zero consecutive repeats.
- **Voice Limiting & Concurrency Guard**:
  - Per-clip debounce cooldowns via `.WithCooldown()` and `SmartAudio.SetClipCooldown()`.
  - Maximum concurrent instances guard with `IgnoreNew` and `RecycleOldest` voice stealing strategies.
- **Dual-Channel BGM & Audio Ducking**:
  - Automatic crossfading between BGM tracks with configurable fade durations.
  - Dynamic audio ducking system (`SmartAudio.DuckBGM()` and `.WithDucking()`).
- **Internal Object Pooling & Zero Setup**:
  - High-performance zero-GC `AudioSourcePool`.
  - Automatic DontDestroyOnLoad root container (`[SmartAudio_Root]`).
  - Safe attached tracking preventing errors when target game objects are destroyed during playback.
- **Mixer Routing & Category Channels**:
  - Master, SFX, BGM, and UI channel volumes with instant dynamic updates.
  - Unity `AudioMixerGroup` routing support.
- **Automated Test Suite**:
  - Complete NUnit test coverage for playback, pooling, dynamics, concurrency, and BGM transitions.
