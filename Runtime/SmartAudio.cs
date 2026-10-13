using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using DZ.SmartAudio.Internal;

namespace DZ.SmartAudio
{
    /// <summary>
    /// Static facade providing high-level management for Smart Audio:
    /// global channel volume control, BGM orchestration, audio ducking, and global lifecycle.
    /// </summary>
    public static class SmartAudio
    {
        #region Channel Volume Control

        /// <summary>
        /// Sets volume for a specific channel (Master, SFX, BGM, UI) in range [0, 1].
        /// </summary>
        public static void SetVolume(AudioChannel channel, float volume)
        {
            SmartAudioService.Instance?.SetChannelVolume(channel, volume);
        }

        /// <summary>
        /// Gets current volume for a specific channel.
        /// </summary>
        public static float GetVolume(AudioChannel channel)
        {
            return SmartAudioService.Instance != null ? SmartAudioService.Instance.GetChannelVolume(channel) : 1f;
        }

        public static float MasterVolume
        {
            get => GetVolume(AudioChannel.Master);
            set => SetVolume(AudioChannel.Master, value);
        }

        public static float SFXVolume
        {
            get => GetVolume(AudioChannel.SFX);
            set => SetVolume(AudioChannel.SFX, value);
        }

        public static float BGMVolume
        {
            get => GetVolume(AudioChannel.BGM);
            set => SetVolume(AudioChannel.BGM, value);
        }

        public static float UIVolume
        {
            get => GetVolume(AudioChannel.UI);
            set => SetVolume(AudioChannel.UI, value);
        }

        #endregion

        #region BGM Management

        /// <summary>
        /// Plays background music with seamless dual-channel crossfade.
        /// </summary>
        public static void PlayBGM(AudioClip clip, float fadeDuration = 1.5f, bool loop = true, float volume = 1f)
        {
            SmartAudioService.Instance?.BgmService.PlayBGM(clip, fadeDuration, loop, volume);
        }

        /// <summary>
        /// Pauses the currently playing background music.
        /// </summary>
        public static void PauseBGM()
        {
            SmartAudioService.Instance?.BgmService.PauseBGM();
        }

        /// <summary>
        /// Resumes paused background music.
        /// </summary>
        public static void ResumeBGM()
        {
            SmartAudioService.Instance?.BgmService.ResumeBGM();
        }

        /// <summary>
        /// Stops background music smoothly with a fade out.
        /// </summary>
        public static void StopBGM(float fadeDuration = 1.0f)
        {
            SmartAudioService.Instance?.BgmService.StopBGM(fadeDuration);
        }

        /// <summary>
        /// Temporarily lowers BGM volume by duckFactor (e.g. 0.3f), holds for duration seconds,
        /// then smoothly restores it back to normal over restoreDuration seconds.
        /// </summary>
        public static void DuckBGM(float duckFactor = 0.3f, float duration = 1.0f, float restoreDuration = 0.5f)
        {
            SmartAudioService.Instance?.TriggerDucking(duckFactor, duration, restoreDuration);
        }

        /// <summary>
        /// Routes BGM channels to an AudioMixerGroup.
        /// </summary>
        public static void SetBGMMixerGroup(AudioMixerGroup group)
        {
            SmartAudioService.Instance?.BgmService.SetMixerGroup(group);
        }

        public static bool IsBGMPlaying => SmartAudioService.Instance != null && SmartAudioService.Instance.BgmService.IsPlaying;
        public static AudioClip CurrentBGMClip => SmartAudioService.Instance?.BgmService.CurrentClip;

        #endregion

        #region Cooldowns & Concurrency

        /// <summary>
        /// Registers a default debounce cooldown window in seconds for a specific clip.
        /// </summary>
        public static void SetClipCooldown(AudioClip clip, float cooldownSeconds)
        {
            SmartAudioService.Instance?.ConcurrencyGuard.SetClipCooldown(clip, cooldownSeconds);
        }

        #endregion

        #region Playback Shortcuts

        public static AudioHandle Play2D(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            return SmartAudioService.Instance != null ? SmartAudioService.Instance.Play2D(clip, volume, pitch) : AudioHandle.Invalid;
        }

        public static AudioHandle PlayAt(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f, float spatialBlend = 1f)
        {
            return SmartAudioService.Instance != null ? SmartAudioService.Instance.PlayAt(clip, position, volume, pitch, spatialBlend) : AudioHandle.Invalid;
        }

        public static AudioHandle PlayAttached(AudioClip clip, Transform target, Vector3 offset = default, float volume = 1f, float pitch = 1f, float spatialBlend = 1f)
        {
            return SmartAudioService.Instance != null ? SmartAudioService.Instance.PlayAttached(clip, target, offset, volume, pitch, spatialBlend) : AudioHandle.Invalid;
        }

        public static AudioHandle PlayRandom2D(AudioClip[] clips, float volume = 1f, float pitch = 1f)
        {
            AudioClip clip = SmartAudioService.Instance?.ClipRandomizer.GetRandomClip(clips);
            return Play2D(clip, volume, pitch);
        }

        public static AudioHandle PlayRandom2D(IList<AudioClip> clips, float volume = 1f, float pitch = 1f)
        {
            AudioClip clip = SmartAudioService.Instance?.ClipRandomizer.GetRandomClip(clips);
            return Play2D(clip, volume, pitch);
        }

        #endregion

        #region Global Lifecycle

        /// <summary>
        /// Returns total count of currently active pooled voices.
        /// </summary>
        public static int ActiveVoiceCount => SmartAudioService.Instance != null ? SmartAudioService.Instance.GetActiveVoiceCount() : 0;

        /// <summary>
        /// Stops all active sound effects, BGM, and resets concurrency guards.
        /// </summary>
        public static void StopAll()
        {
            SmartAudioService.Instance?.StopAll();
        }

        #endregion
    }
}
