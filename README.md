<a id="smartaudio-for-unity"></a>
# SmartAudio for Unity

A zero-setup, zero-GC fluent audio playback system with chainable modifiers, voice limiting, anti-monotony clip randomizers, dual-channel BGM crossfading, and internal object pooling for Unity.

Designed for peak performance and ergonomics: no tedious scene setup, no manual `AudioSource` components to wire up, and zero garbage collection overhead. Play audio directly from `AudioClip` or collections with a fluent, chainable API.

---

### Quick Navigation
[Key Features](#key-features) | [Installation](#installation) | [Quick Start](#quick-start) | [API Reference](#api-reference) | [Changelog](CHANGELOG.md) | [License](LICENSE.md)

---

<a id="key-features"></a>
## Key Features

### 1. Fluent Playback API
Play audio directly from `AudioClip` references without attaching `AudioSource` components manually in the scene:
- **2D Playback (UI & Non-spatial FX)**:
  ```csharp
  clickClip.Play2D();
  ```
- **3D Positional Playback**:
  ```csharp
  explosionClip.PlayAt(transform.position);
  ```
- **Attached Playback (Follows Moving Transforms)**:
  ```csharp
  engineClip.PlayAttached(carTransform);
  ```
- **Fluent Modifier Chaining**:
  ```csharp
  sfxClip.Play2D()
         .WithVolume(0.8f)
         .WithPitch(1.2f)
         .WithDelay(0.2f)
         .WithSpatialBlend(1f)
         .WithMinMaxDistance(1f, 20f);
  ```
- **Lightweight Control Handle (`AudioHandle`)**:
  ```csharp
  var handle = laserClip.Play2D();
  handle.Pause();
  handle.Resume();
  handle.FadeOut(0.5f);
  handle.Stop();
  ```

---

### 2. Audio Dynamics & Anti-Monotony
Prevent sound effects from sounding repetitive or robotic (such as footsteps, gunshots, or sword swings):
- **Random Pitch & Volume Modulation**:
  ```csharp
  gunshotClip.Play2D()
             .WithRandomPitch(0.9f, 1.1f)
             .WithRandomVolume(0.8f, 1.0f);
  ```
- **Pitch Variation Shorthand**:
  ```csharp
  swingClip.Play2D().WithPitchVariation(0.1f); // 1.0 +/- 10%
  ```
- **Smart Clip Collections (No-Repeat Random)**:
  Extension methods on `AudioClip[]` and `IList<AudioClip>` guaranteed never to repeat the same clip twice consecutively:
  ```csharp
  footstepClips.PlayRandom2D();
  footstepClips.PlayRandomAt(transform.position);
  footstepClips.PlayRandomAttached(transform);
  ```

---

### 3. Voice Limiting & Concurrency Guard (Anti-Ear-Rape)
Eliminates audio distortion and speaker clipping caused by rapid stacking of simultaneous events (e.g. 20 enemies dying at once or picking up 30 coins in 0.1s):
- **Debounce Cooldown**:
  ```csharp
  coinClip.Play2D().WithCooldown(0.05f);
  ```
- **Max Instances Per Clip & Voice Stealing**:
  ```csharp
  // Limit to 3 instances; steals the oldest voice if limit is exceeded
  enemyDeathClip.Play2D().WithMaxInstances(3, VoiceStealStrategy.RecycleOldest);

  // Or ignore incoming requests if limit is reached
  spammyClip.Play2D().WithMaxInstances(2, VoiceStealStrategy.IgnoreNew);
  ```

---

### 4. Background Music (BGM) & Audio Ducking
Hassle-free background music and ambient audio without Coroutine boilerplate:
- **Dual-Channel Automatic Crossfade**:
  ```csharp
  bossBgm.PlayBGM(fadeDuration: 2.0f);
  ```
- **Global BGM Controls**:
  ```csharp
  SmartAudio.PauseBGM();
  SmartAudio.ResumeBGM();
  SmartAudio.StopBGM(fadeDuration: 1.0f);
  ```
- **Dynamic Audio Ducking**:
  Temporarily duck BGM volume when narration, dialogue, or high-priority SFX triggers, then smoothly restore it:
  ```csharp
  voiceoverClip.Play2D().WithDucking(duckFactor: 0.3f, duration: 2.0f, restoreDuration: 0.5f);
  ```

---

### 5. Performance, Pooling & Zero Setup
- **Internal Zero-GC AudioSource Pool**: Prewarmed, expandable pool that automatically returns voices upon playback completion.
- **Zero Scene Setup**: Runtime host `[SmartAudio_Root]` marked `DontDestroyOnLoad` is automatically initialized on demand.
- **Safe Attached Tracking**: If a target object is destroyed while playing `.PlayAttached(target)`, audio does not throw `MissingReferenceException` or `NullReferenceException`. It safely finishes or despawns cleanly.

---

### 6. Mixer Routing & Category Channels
- **AudioMixer Group Binding**:
  ```csharp
  gunshotClip.Play2D().ToGroup(sfxMixerGroup);
  ```
- **Channel Volume Management**:
  ```csharp
  SmartAudio.SetVolume(AudioChannel.Master, 0.8f);
  SmartAudio.SetVolume(AudioChannel.SFX, 0.9f);
  SmartAudio.SetVolume(AudioChannel.BGM, 0.6f);
  SmartAudio.SetVolume(AudioChannel.UI, 1.0f);
  ```

---

<a id="installation"></a>
## Installation

### Option 1: Via Unity Package Manager (Git URL - Recommended)
1. In Unity, navigate to **Window** > **Package Manager**.
2. Click the **+** icon in the top-left corner of the window.
3. Select **Add package from git URL...**
4. Paste the Git repository URL:
   ```
   https://github.com/DanyZaky/com.dz.smartaudio.git
   ```
   *(Optional)* To target a specific release tag or branch, append `#<tag>` (e.g. `#v1.0.0`):
   ```
   https://github.com/DanyZaky/com.dz.smartaudio.git#v1.0.0
   ```
5. Click **Add**.

#### Alternatively via `Packages/manifest.json`:
Add the repository directly to your Unity project's `Packages/manifest.json` under `"dependencies"`:
```json
{
  "dependencies": {
    "com.dz.smartaudio": "https://github.com/DanyZaky/com.dz.smartaudio.git"
  }
}
```

### Option 2: Via Unity Package Manager (Local / Embedded)
1. Clone or download this repository.
2. In Unity, open **Window** > **Package Manager**.
3. Click the **+** icon in the top-left corner.
4. Select **Add package from disk...** and choose the `package.json` file inside the package folder.

---

<a id="quick-start"></a>
## Quick Start

```csharp
using UnityEngine;
using DZ.SmartAudio;

public class PlayerAudioExample : MonoBehaviour
{
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip coinClip;
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private AudioClip levelBgm;

    private void Start()
    {
        // Start background music with crossfade
        levelBgm.PlayBGM(fadeDuration: 2.0f);
    }

    public void OnJump()
    {
        // 2D playback with pitch variation
        jumpClip.Play2D().WithPitchVariation(0.08f);
    }

    public void OnFootstep()
    {
        // Positional playback with smart random no-repeat
        footstepClips.PlayRandomAt(transform.position)
                     .WithVolume(0.6f)
                     .WithPitchVariation(0.05f);
    }

    public void OnCollectCoin()
    {
        // Debounced coin sound
        coinClip.Play2D().WithCooldown(0.04f);
    }
}
```

---

<a id="license"></a>
## License

MIT License. See [LICENSE.md](LICENSE.md) for details.
