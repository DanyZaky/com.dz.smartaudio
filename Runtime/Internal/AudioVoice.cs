using UnityEngine;
using UnityEngine.Audio;

namespace DZ.SmartAudio.Internal
{
    /// <summary>
    /// Represents an individual voice slot managing an AudioSource component, its spatial tracking,
    /// volume calculations, delays, fades, and lifecycle.
    /// </summary>
    internal sealed class AudioVoice
    {
        public int Id { get; private set; }
        public int Generation { get; private set; }
        public GameObject GameObject { get; private set; }
        public Transform Transform { get; private set; }
        public AudioSource AudioSource { get; private set; }

        public AudioClip Clip { get; private set; }
        public AudioChannel Channel { get; private set; } = AudioChannel.SFX;
        public float BaseVolume { get; private set; } = 1f;
        public float BasePitch { get; private set; } = 1f;
        public bool IsLoop { get; private set; }
        public bool IsPaused { get; private set; }
        public float Age { get; private set; }

        // Delays
        public float DelayRemaining { get; private set; }
        public bool IsDelayed => DelayRemaining > 0f;

        // Spatial / Attached tracking
        public Transform FollowTarget { get; private set; }
        public Vector3 FollowOffset { get; private set; }
        public bool WasAttached { get; private set; }

        // Fading
        public bool IsFading { get; private set; }
        public float FadeStartVolume { get; private set; }
        public float FadeEndVolume { get; private set; }
        public float FadeDuration { get; private set; }
        public float FadeElapsed { get; private set; }
        public bool StopOnFadeEnd { get; private set; }

        // Concurrency settings
        public int MaxInstances { get; private set; }
        public VoiceStealStrategy StealStrategy { get; private set; }
        public float CooldownDuration { get; private set; }

        public bool IsActive { get; private set; }

        public void Initialize(int id, GameObject gameObject, AudioSource audioSource)
        {
            Id = id;
            Generation = 1;
            GameObject = gameObject;
            Transform = gameObject.transform;
            AudioSource = audioSource;
            AudioSource.playOnAwake = false;
        }

        public void Setup(
            AudioClip clip,
            AudioChannel channel,
            float volume,
            float pitch,
            Vector3 position,
            Transform followTarget,
            Vector3 followOffset,
            float spatialBlend)
        {
            Clip = clip;
            Channel = channel;
            BaseVolume = volume;
            BasePitch = pitch;
            IsLoop = false;
            IsPaused = false;
            Age = 0f;
            DelayRemaining = 0f;
            IsFading = false;
            MaxInstances = 0;
            StealStrategy = VoiceStealStrategy.RecycleOldest;
            CooldownDuration = 0f;

            FollowTarget = followTarget;
            FollowOffset = followOffset;
            WasAttached = followTarget != null;

            Transform.position = position;

            AudioSource.clip = clip;
            AudioSource.volume = volume;
            AudioSource.pitch = pitch;
            AudioSource.loop = false;
            AudioSource.spatialBlend = spatialBlend;
            AudioSource.outputAudioMixerGroup = null;

            IsActive = true;
        }

        public void Play()
        {
            if (IsDelayed)
                return;

            AudioSource.Play();
        }

        public void Pause()
        {
            if (!IsActive || IsPaused)
                return;

            IsPaused = true;
            AudioSource.Pause();
        }

        public void Resume()
        {
            if (!IsActive || !IsPaused)
                return;

            IsPaused = false;
            AudioSource.UnPause();
        }

        public void Stop()
        {
            if (!IsActive)
                return;

            AudioSource.Stop();
            IsActive = false;
            IsFading = false;
            DelayRemaining = 0f;
        }

        public void SetVolume(float volume, float channelMultiplier)
        {
            BaseVolume = Mathf.Clamp01(volume);
            if (!IsFading)
            {
                AudioSource.volume = BaseVolume * channelMultiplier;
            }
        }

        public void SetPitch(float pitch)
        {
            BasePitch = pitch;
            AudioSource.pitch = pitch;
        }

        public void SetLoop(bool loop)
        {
            IsLoop = loop;
            AudioSource.loop = loop;
        }

        public void SetDelay(float delay)
        {
            DelayRemaining = Mathf.Max(0f, delay);
            if (DelayRemaining > 0f && AudioSource.isPlaying)
            {
                AudioSource.Stop();
            }
        }

        public void SetSpatialBlend(float blend)
        {
            AudioSource.spatialBlend = Mathf.Clamp01(blend);
        }

        public void SetMinMaxDistance(float minDistance, float maxDistance)
        {
            AudioSource.minDistance = Mathf.Max(0f, minDistance);
            AudioSource.maxDistance = Mathf.Max(minDistance, maxDistance);
        }

        public void SetChannel(AudioChannel channel)
        {
            Channel = channel;
        }

        public void SetMixerGroup(AudioMixerGroup group)
        {
            AudioSource.outputAudioMixerGroup = group;
        }

        public void SetConcurrency(int maxInstances, VoiceStealStrategy strategy)
        {
            MaxInstances = maxInstances;
            StealStrategy = strategy;
        }

        public void SetCooldown(float cooldown)
        {
            CooldownDuration = cooldown;
        }

        public void StartFade(float targetVolume, float duration, bool stopOnEnd, float channelMultiplier)
        {
            if (duration <= 0.0001f)
            {
                BaseVolume = targetVolume;
                AudioSource.volume = BaseVolume * channelMultiplier;
                IsFading = false;
                if (stopOnEnd)
                    Stop();
                return;
            }

            IsFading = true;
            FadeStartVolume = BaseVolume;
            FadeEndVolume = Mathf.Clamp01(targetVolume);
            FadeDuration = duration;
            FadeElapsed = 0f;
            StopOnFadeEnd = stopOnEnd;
        }

        public void UpdateVolume(float channelMultiplier)
        {
            if (IsFading)
            {
                float t = Mathf.Clamp01(FadeElapsed / FadeDuration);
                float cur = Mathf.Lerp(FadeStartVolume, FadeEndVolume, t);
                AudioSource.volume = cur * channelMultiplier;
            }
            else
            {
                AudioSource.volume = BaseVolume * channelMultiplier;
            }
        }

        public void Tick(float deltaTime, float channelMultiplier)
        {
            if (!IsActive)
                return;

            Age += deltaTime;

            // Handle delay countdown
            if (DelayRemaining > 0f)
            {
                DelayRemaining -= deltaTime;
                if (DelayRemaining <= 0f)
                {
                    DelayRemaining = 0f;
                    AudioSource.Play();
                }
                else
                {
                    // Voice is still waiting for delay
                    return;
                }
            }

            // Attached target follow
            if (FollowTarget != null)
            {
                Transform.position = FollowTarget.position + FollowOffset;
            }
            else if (WasAttached)
            {
                // Target was destroyed during gameplay
                WasAttached = false;
                FollowTarget = null;
                // If it is looping, an attached sound should stop to prevent immortal orphan sounds
                if (IsLoop)
                {
                    Stop();
                    return;
                }
            }

            // Handle fading
            if (IsFading)
            {
                FadeElapsed += deltaTime;
                float t = Mathf.Clamp01(FadeElapsed / FadeDuration);
                float currentFadeVol = Mathf.Lerp(FadeStartVolume, FadeEndVolume, t);
                AudioSource.volume = currentFadeVol * channelMultiplier;

                if (FadeElapsed >= FadeDuration)
                {
                    IsFading = false;
                    BaseVolume = FadeEndVolume;
                    AudioSource.volume = BaseVolume * channelMultiplier;
                    if (StopOnFadeEnd)
                    {
                        Stop();
                        return;
                    }
                }
            }
            else
            {
                AudioSource.volume = BaseVolume * channelMultiplier;
            }
        }

        public bool HasCompletedPlayback()
        {
            if (!IsActive)
                return true;

            if (IsPaused || IsDelayed)
                return false;

            if (IsLoop)
                return false;

            // If audio has finished playing
            return !AudioSource.isPlaying;
        }

        public void Recycle()
        {
            Stop();
            Generation++;
            Clip = null;
            FollowTarget = null;
            WasAttached = false;
            AudioSource.clip = null;
            AudioSource.outputAudioMixerGroup = null;
            IsActive = false;
        }
    }
}
