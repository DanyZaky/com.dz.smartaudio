using System.Collections.Generic;
using UnityEngine;
using DZ.SmartAudio.Internal;

namespace DZ.SmartAudio
{
    /// <summary>
    /// Extension methods on AudioClip and collections providing fluent audio playback.
    /// </summary>
    public static class SmartAudioExtensions
    {
        #region AudioClip Extensions

        /// <summary>
        /// Plays this audio clip in 2D (non-spatial, UI or general sound effect).
        /// </summary>
        public static AudioHandle Play2D(this AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return AudioHandle.Invalid;
            return SmartAudioService.Instance != null
                ? SmartAudioService.Instance.Play2D(clip, volume, pitch)
                : AudioHandle.Invalid;
        }

        /// <summary>
        /// Plays this audio clip at a specific 3D world position.
        /// </summary>
        public static AudioHandle PlayAt(this AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f, float spatialBlend = 1f)
        {
            if (clip == null) return AudioHandle.Invalid;
            return SmartAudioService.Instance != null
                ? SmartAudioService.Instance.PlayAt(clip, position, volume, pitch, spatialBlend)
                : AudioHandle.Invalid;
        }

        /// <summary>
        /// Plays this audio clip attached to a moving Transform, automatically updating position as the target moves.
        /// Safely completes or cleans up if the target is destroyed during gameplay.
        /// </summary>
        public static AudioHandle PlayAttached(this AudioClip clip, Transform target, Vector3 offset = default, float volume = 1f, float pitch = 1f, float spatialBlend = 1f)
        {
            if (clip == null) return AudioHandle.Invalid;
            return SmartAudioService.Instance != null
                ? SmartAudioService.Instance.PlayAttached(clip, target, offset, volume, pitch, spatialBlend)
                : AudioHandle.Invalid;
        }

        /// <summary>
        /// Plays this audio clip as background music with seamless dual-channel crossfade.
        /// </summary>
        public static void PlayBGM(this AudioClip clip, float fadeDuration = 1.5f, bool loop = true, float volume = 1f)
        {
            SmartAudio.PlayBGM(clip, fadeDuration, loop, volume);
        }

        #endregion

        #region Array & List Extensions (Anti-Monotony)

        /// <summary>
        /// Plays a random AudioClip from the array without repeating the same clip twice consecutively.
        /// </summary>
        public static AudioHandle PlayRandom2D(this AudioClip[] clips, float volume = 1f, float pitch = 1f)
        {
            if (clips == null || clips.Length == 0) return AudioHandle.Invalid;
            AudioClip clip = SmartAudioService.Instance?.ClipRandomizer.GetRandomClip(clips);
            return clip.Play2D(volume, pitch);
        }

        /// <summary>
        /// Plays a random AudioClip from the list without repeating the same clip twice consecutively.
        /// </summary>
        public static AudioHandle PlayRandom2D(this IList<AudioClip> clips, float volume = 1f, float pitch = 1f)
        {
            if (clips == null || clips.Count == 0) return AudioHandle.Invalid;
            AudioClip clip = SmartAudioService.Instance?.ClipRandomizer.GetRandomClip(clips);
            return clip.Play2D(volume, pitch);
        }

        /// <summary>
        /// Plays a random AudioClip from the array at a 3D position without repeating the same clip twice consecutively.
        /// </summary>
        public static AudioHandle PlayRandomAt(this AudioClip[] clips, Vector3 position, float volume = 1f, float pitch = 1f, float spatialBlend = 1f)
        {
            if (clips == null || clips.Length == 0) return AudioHandle.Invalid;
            AudioClip clip = SmartAudioService.Instance?.ClipRandomizer.GetRandomClip(clips);
            return clip.PlayAt(position, volume, pitch, spatialBlend);
        }

        /// <summary>
        /// Plays a random AudioClip from the list at a 3D position without repeating the same clip twice consecutively.
        /// </summary>
        public static AudioHandle PlayRandomAt(this IList<AudioClip> clips, Vector3 position, float volume = 1f, float pitch = 1f, float spatialBlend = 1f)
        {
            if (clips == null || clips.Count == 0) return AudioHandle.Invalid;
            AudioClip clip = SmartAudioService.Instance?.ClipRandomizer.GetRandomClip(clips);
            return clip.PlayAt(position, volume, pitch, spatialBlend);
        }

        /// <summary>
        /// Plays a random AudioClip from the array attached to a target Transform without repeating twice consecutively.
        /// </summary>
        public static AudioHandle PlayRandomAttached(this AudioClip[] clips, Transform target, Vector3 offset = default, float volume = 1f, float pitch = 1f, float spatialBlend = 1f)
        {
            if (clips == null || clips.Length == 0) return AudioHandle.Invalid;
            AudioClip clip = SmartAudioService.Instance?.ClipRandomizer.GetRandomClip(clips);
            return clip.PlayAttached(target, offset, volume, pitch, spatialBlend);
        }

        /// <summary>
        /// Plays a random AudioClip from the list attached to a target Transform without repeating twice consecutively.
        /// </summary>
        public static AudioHandle PlayRandomAttached(this IList<AudioClip> clips, Transform target, Vector3 offset = default, float volume = 1f, float pitch = 1f, float spatialBlend = 1f)
        {
            if (clips == null || clips.Count == 0) return AudioHandle.Invalid;
            AudioClip clip = SmartAudioService.Instance?.ClipRandomizer.GetRandomClip(clips);
            return clip.PlayAttached(target, offset, volume, pitch, spatialBlend);
        }

        /// <summary>
        /// Picks a random clip from the array guaranteed not to repeat the last picked index.
        /// </summary>
        public static AudioClip GetRandomClip(this AudioClip[] clips)
        {
            return SmartAudioService.Instance?.ClipRandomizer.GetRandomClip(clips);
        }

        /// <summary>
        /// Picks a random clip from the list guaranteed not to repeat the last picked index.
        /// </summary>
        public static AudioClip GetRandomClip(this IList<AudioClip> clips)
        {
            return SmartAudioService.Instance?.ClipRandomizer.GetRandomClip(clips);
        }

        #endregion
    }
}
