using System;
using UnityEngine;
using UnityEngine.Audio;
using DZ.SmartAudio.Internal;

namespace DZ.SmartAudio
{
    /// <summary>
    /// Lightweight zero-allocation struct handle representing an active audio playback.
    /// Provides fluent chainable modifiers and playback controls (Stop, Pause, FadeOut, etc.).
    /// </summary>
    public readonly struct AudioHandle : IEquatable<AudioHandle>
    {
        public readonly int Id;
        public readonly int Generation;

        public static readonly AudioHandle Invalid = new AudioHandle(0, 0);

        public AudioHandle(int id, int generation)
        {
            Id = id;
            Generation = generation;
        }

        /// <summary>
        /// True if this handle points to an active voice matching its generation.
        /// </summary>
        public bool IsValid => Id > 0 && SmartAudioService.Instance != null && SmartAudioService.Instance.IsVoiceValid(Id, Generation);

        /// <summary>
        /// True if this voice is currently playing audio.
        /// </summary>
        public bool IsPlaying => IsValid && SmartAudioService.Instance.IsVoicePlaying(Id, Generation);

        #region Playback Controls

        /// <summary>
        /// Stops the voice and immediately recycles it back to the pool.
        /// </summary>
        public void Stop()
        {
            if (IsValid)
            {
                SmartAudioService.Instance.StopVoice(Id, Generation);
            }
        }

        /// <summary>
        /// Pauses the audio voice playback.
        /// </summary>
        public void Pause()
        {
            if (IsValid)
            {
                SmartAudioService.Instance.PauseVoice(Id, Generation);
            }
        }

        /// <summary>
        /// Resumes paused audio playback.
        /// </summary>
        public void Resume()
        {
            if (IsValid)
            {
                SmartAudioService.Instance.ResumeVoice(Id, Generation);
            }
        }

        /// <summary>
        /// Smoothly fades out the volume over duration seconds, then stops and recycles the voice.
        /// </summary>
        public void FadeOut(float duration)
        {
            if (IsValid)
            {
                SmartAudioService.Instance.FadeVoiceOut(Id, Generation, duration);
            }
        }

        /// <summary>
        /// Smoothly fades in the volume over duration seconds.
        /// </summary>
        public void FadeIn(float duration)
        {
            if (IsValid)
            {
                SmartAudioService.Instance.FadeVoiceIn(Id, Generation, duration);
            }
        }

        /// <summary>
        /// Sets the voice volume directly.
        /// </summary>
        public void SetVolume(float volume)
        {
            if (IsValid)
            {
                SmartAudioService.Instance.SetVoiceVolume(Id, Generation, volume);
            }
        }

        /// <summary>
        /// Sets the voice pitch directly.
        /// </summary>
        public void SetPitch(float pitch)
        {
            if (IsValid)
            {
                SmartAudioService.Instance.SetVoicePitch(Id, Generation, pitch);
            }
        }

        #endregion

        #region Fluent Modifiers

        /// <summary>
        /// Sets the base volume of the audio voice.
        /// </summary>
        public AudioHandle WithVolume(float volume)
        {
            if (IsValid)
            {
                SmartAudioService.Instance.SetVoiceVolume(Id, Generation, volume);
            }
            return this;
        }

        /// <summary>
        /// Sets the base pitch of the audio voice.
        /// </summary>
        public AudioHandle WithPitch(float pitch)
        {
            if (IsValid)
            {
                SmartAudioService.Instance.SetVoicePitch(Id, Generation, pitch);
            }
            return this;
        }

        /// <summary>
        /// Modulates the pitch with a random value between min and max.
        /// </summary>
        public AudioHandle WithRandomPitch(float min, float max)
        {
            if (IsValid)
            {
                float randomPitch = UnityEngine.Random.Range(min, max);
                SmartAudioService.Instance.SetVoicePitch(Id, Generation, randomPitch);
            }
            return this;
        }

        /// <summary>
        /// Modulates the pitch within a range of basePitch +/- variation (e.g. 0.1f = +/-10%).
        /// </summary>
        public AudioHandle WithPitchVariation(float variation)
        {
            if (IsValid)
            {
                float factor = UnityEngine.Random.Range(1f - variation, 1f + variation);
                var voice = SmartAudioService.Instance.FindVoice(Id, Generation);
                float basePitch = voice != null ? voice.BasePitch : 1f;
                SmartAudioService.Instance.SetVoicePitch(Id, Generation, basePitch * factor);
            }
            return this;
        }

        /// <summary>
        /// Modulates the volume with a random value between min and max.
        /// </summary>
        public AudioHandle WithRandomVolume(float min, float max)
        {
            if (IsValid)
            {
                float randomVolume = UnityEngine.Random.Range(min, max);
                SmartAudioService.Instance.SetVoiceVolume(Id, Generation, randomVolume);
            }
            return this;
        }

        /// <summary>
        /// Delays the playback start by the specified number of seconds.
        /// </summary>
        public AudioHandle WithDelay(float delaySeconds)
        {
            if (IsValid)
            {
                SmartAudioService.Instance.SetVoiceDelay(Id, Generation, delaySeconds);
            }
            return this;
        }

        /// <summary>
        /// Sets the spatial blend: 0 = fully 2D, 1 = fully 3D.
        /// </summary>
        public AudioHandle WithSpatialBlend(float blend)
        {
            if (IsValid)
            {
                SmartAudioService.Instance.SetVoiceSpatialBlend(Id, Generation, blend);
            }
            return this;
        }

        /// <summary>
        /// Sets 3D audio attenuation minimum and maximum distances.
        /// </summary>
        public AudioHandle WithMinMaxDistance(float minDistance, float maxDistance)
        {
            if (IsValid)
            {
                SmartAudioService.Instance.SetVoiceMinMaxDistance(Id, Generation, minDistance, maxDistance);
            }
            return this;
        }

        /// <summary>
        /// Sets whether this sound should loop.
        /// </summary>
        public AudioHandle WithLoop(bool loop = true)
        {
            if (IsValid)
            {
                SmartAudioService.Instance.SetVoiceLoop(Id, Generation, loop);
            }
            return this;
        }

        /// <summary>
        /// Assigns this voice to a specific audio category channel.
        /// </summary>
        public AudioHandle WithChannel(AudioChannel channel)
        {
            if (IsValid)
            {
                SmartAudioService.Instance.SetVoiceChannel(Id, Generation, channel);
            }
            return this;
        }

        /// <summary>
        /// Routes the audio output to a specific AudioMixerGroup.
        /// </summary>
        public AudioHandle ToGroup(AudioMixerGroup group)
        {
            if (IsValid)
            {
                SmartAudioService.Instance.SetVoiceMixerGroup(Id, Generation, group);
            }
            return this;
        }

        /// <summary>
        /// Limits the maximum concurrent instances for this clip and specifies stealing strategy.
        /// </summary>
        public AudioHandle WithMaxInstances(int maxInstances, VoiceStealStrategy strategy = VoiceStealStrategy.RecycleOldest)
        {
            if (IsValid)
            {
                SmartAudioService.Instance.SetVoiceConcurrency(Id, Generation, maxInstances, strategy);
            }
            return this;
        }

        /// <summary>
        /// Enforces a debounce cooldown window in seconds on this clip.
        /// </summary>
        public AudioHandle WithCooldown(float cooldownSeconds)
        {
            if (IsValid)
            {
                SmartAudioService.Instance.SetVoiceCooldown(Id, Generation, cooldownSeconds);
            }
            return this;
        }

        /// <summary>
        /// Ducks the background music (BGM) while this voice plays.
        /// </summary>
        public AudioHandle WithDucking(float duckFactor = 0.3f, float duration = 1.0f, float restoreDuration = 0.5f)
        {
            if (IsValid)
            {
                SmartAudioService.Instance.TriggerDucking(duckFactor, duration, restoreDuration);
            }
            return this;
        }

        #endregion

        #region Equality

        public bool Equals(AudioHandle other)
        {
            return Id == other.Id && Generation == other.Generation;
        }

        public override bool Equals(object obj)
        {
            return obj is AudioHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Id * 397) ^ Generation;
            }
        }

        public static bool operator ==(AudioHandle left, AudioHandle right) => left.Equals(right);
        public static bool operator !=(AudioHandle left, AudioHandle right) => !left.Equals(right);

        public override string ToString() => $"AudioHandle(Id: {Id}, Gen: {Generation})";

        #endregion
    }
}
