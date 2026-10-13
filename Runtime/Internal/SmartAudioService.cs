using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace DZ.SmartAudio.Internal
{
    /// <summary>
    /// Central runtime engine and DontDestroyOnLoad host for Smart Audio.
    /// Manages voice pooling, active voice ticks, volume channels, BGM, and concurrency guards.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    internal sealed class SmartAudioService : MonoBehaviour
    {
        private static SmartAudioService _instance;
        private static bool _isApplicationQuitting;

        public static SmartAudioService Instance
        {
            get
            {
                if (_isApplicationQuitting)
                    return null;

                if (_instance == null)
                {
                    InitializeService();
                }
                return _instance;
            }
        }

        private AudioSourcePool _pool;
        private ConcurrencyGuard _concurrencyGuard;
        private ClipRandomizer _clipRandomizer;
        private BgmService _bgmService;

        private readonly List<AudioVoice> _activeVoices = new List<AudioVoice>(32);

        private float _masterVolume = 1f;
        private float _sfxVolume = 1f;
        private float _bgmVolume = 1f;
        private float _uiVolume = 1f;

        public ConcurrencyGuard ConcurrencyGuard => _concurrencyGuard;
        public ClipRandomizer ClipRandomizer => _clipRandomizer;
        public BgmService BgmService => _bgmService;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnRuntimeMethodLoad()
        {
            if (_instance == null && !_isApplicationQuitting)
            {
                InitializeService();
            }
        }

        private static void InitializeService()
        {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
            var existing = FindFirstObjectByType<SmartAudioService>();
#else
            var existing = FindObjectOfType<SmartAudioService>();
#endif
            if (existing != null)
            {
                _instance = existing;
                return;
            }

            GameObject hostGo = new GameObject("[SmartAudio_Root]");
            DontDestroyOnLoad(hostGo);
            _instance = hostGo.AddComponent<SmartAudioService>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            _pool = new AudioSourcePool(transform, 16);
            _concurrencyGuard = new ConcurrencyGuard();
            _clipRandomizer = new ClipRandomizer();
            _bgmService = new BgmService(transform);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            _isApplicationQuitting = true;
        }

        public AudioHandle Play2D(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            return PlayInternal(clip, Vector3.zero, null, Vector3.zero, volume, pitch, 0f, AudioChannel.SFX);
        }

        public AudioHandle PlayAt(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f, float spatialBlend = 1f)
        {
            return PlayInternal(clip, position, null, Vector3.zero, volume, pitch, spatialBlend, AudioChannel.SFX);
        }

        public AudioHandle PlayAttached(AudioClip clip, Transform target, Vector3 offset = default, float volume = 1f, float pitch = 1f, float spatialBlend = 1f)
        {
            Vector3 initialPos = target != null ? target.position + offset : Vector3.zero;
            return PlayInternal(clip, initialPos, target, offset, volume, pitch, spatialBlend, AudioChannel.SFX);
        }

        public AudioHandle PlayInternal(
            AudioClip clip,
            Vector3 position,
            Transform followTarget,
            Vector3 followOffset,
            float volume,
            float pitch,
            float spatialBlend,
            AudioChannel channel)
        {
            if (clip == null)
                return AudioHandle.Invalid;

            // Debounce check
            if (_concurrencyGuard.IsOnCooldown(clip))
            {
                return AudioHandle.Invalid;
            }

            AudioVoice voice = _pool.Rent();
            voice.Setup(clip, channel, volume, pitch, position, followTarget, followOffset, spatialBlend);

            float channelMultiplier = GetChannelMultiplier(channel);
            voice.UpdateVolume(channelMultiplier);
            voice.Play();

            _activeVoices.Add(voice);
            _concurrencyGuard.RegisterPlay(clip, voice);

            return new AudioHandle(voice.Id, voice.Generation);
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // 1. Tick Active Voices
            for (int i = _activeVoices.Count - 1; i >= 0; i--)
            {
                AudioVoice voice = _activeVoices[i];
                float multiplier = GetChannelMultiplier(voice.Channel);
                voice.Tick(dt, multiplier);

                if (voice.HasCompletedPlayback())
                {
                    DespawnVoiceAtIndex(i);
                }
            }

            // 2. Tick BGM
            _bgmService.Tick(dt, _bgmVolume, _masterVolume);
        }

        private void DespawnVoiceAtIndex(int index)
        {
            if (index < 0 || index >= _activeVoices.Count)
                return;

            AudioVoice voice = _activeVoices[index];
            _concurrencyGuard.UnregisterVoice(voice.Clip, voice);
            _pool.Return(voice);
            _activeVoices.RemoveAt(index);
        }

        public void DespawnVoice(AudioVoice voice)
        {
            int index = _activeVoices.IndexOf(voice);
            if (index >= 0)
            {
                DespawnVoiceAtIndex(index);
            }
        }

        public AudioVoice FindVoice(int id, int generation)
        {
            for (int i = 0; i < _activeVoices.Count; i++)
            {
                var v = _activeVoices[i];
                if (v.Id == id && v.Generation == generation && v.IsActive)
                {
                    return v;
                }
            }
            return null;
        }

        public bool IsVoiceValid(int id, int generation)
        {
            return FindVoice(id, generation) != null;
        }

        public bool IsVoicePlaying(int id, int generation)
        {
            var voice = FindVoice(id, generation);
            return voice != null && !voice.IsPaused && voice.AudioSource.isPlaying;
        }

        public void StopVoice(int id, int generation)
        {
            var voice = FindVoice(id, generation);
            if (voice != null)
            {
                DespawnVoice(voice);
            }
        }

        public void PauseVoice(int id, int generation)
        {
            var voice = FindVoice(id, generation);
            voice?.Pause();
        }

        public void ResumeVoice(int id, int generation)
        {
            var voice = FindVoice(id, generation);
            voice?.Resume();
        }

        public void FadeVoiceOut(int id, int generation, float duration)
        {
            var voice = FindVoice(id, generation);
            if (voice != null)
            {
                float mult = GetChannelMultiplier(voice.Channel);
                voice.StartFade(0f, duration, true, mult);
            }
        }

        public void FadeVoiceIn(int id, int generation, float duration)
        {
            var voice = FindVoice(id, generation);
            if (voice != null)
            {
                float mult = GetChannelMultiplier(voice.Channel);
                voice.StartFade(voice.BaseVolume, duration, false, mult);
            }
        }

        public void SetVoiceVolume(int id, int generation, float volume)
        {
            var voice = FindVoice(id, generation);
            if (voice != null)
            {
                float mult = GetChannelMultiplier(voice.Channel);
                voice.SetVolume(volume, mult);
            }
        }

        public void SetVoicePitch(int id, int generation, float pitch)
        {
            var voice = FindVoice(id, generation);
            voice?.SetPitch(pitch);
        }

        public void SetVoiceLoop(int id, int generation, bool loop)
        {
            var voice = FindVoice(id, generation);
            voice?.SetLoop(loop);
        }

        public void SetVoiceDelay(int id, int generation, float delay)
        {
            var voice = FindVoice(id, generation);
            voice?.SetDelay(delay);
        }

        public void SetVoiceSpatialBlend(int id, int generation, float blend)
        {
            var voice = FindVoice(id, generation);
            voice?.SetSpatialBlend(blend);
        }

        public void SetVoiceMinMaxDistance(int id, int generation, float minDistance, float maxDistance)
        {
            var voice = FindVoice(id, generation);
            voice?.SetMinMaxDistance(minDistance, maxDistance);
        }

        public void SetVoiceChannel(int id, int generation, AudioChannel channel)
        {
            var voice = FindVoice(id, generation);
            if (voice != null)
            {
                voice.SetChannel(channel);
                voice.UpdateVolume(GetChannelMultiplier(channel));
            }
        }

        public void SetVoiceMixerGroup(int id, int generation, AudioMixerGroup group)
        {
            var voice = FindVoice(id, generation);
            voice?.SetMixerGroup(group);
        }

        public void SetVoiceConcurrency(int id, int generation, int maxInstances, VoiceStealStrategy strategy)
        {
            var voice = FindVoice(id, generation);
            if (voice != null)
            {
                voice.SetConcurrency(maxInstances, strategy);

                // Enforce max count immediately
                if (_concurrencyGuard.GetActiveCount(voice.Clip) > maxInstances)
                {
                    if (_concurrencyGuard.TryAcquireSlot(voice.Clip, maxInstances, strategy, out var stolen) && stolen != null && stolen != voice)
                    {
                        DespawnVoice(stolen);
                    }
                }
            }
        }

        public void SetVoiceCooldown(int id, int generation, float cooldown)
        {
            var voice = FindVoice(id, generation);
            if (voice != null)
            {
                voice.SetCooldown(cooldown);
                _concurrencyGuard.SetClipCooldown(voice.Clip, cooldown);
            }
        }

        public void TriggerDucking(float duckFactor, float duration, float restoreDuration = 0.5f)
        {
            _bgmService.Duck(duckFactor, duration, restoreDuration);
        }

        public void SetChannelVolume(AudioChannel channel, float volume)
        {
            float clamped = Mathf.Clamp01(volume);
            switch (channel)
            {
                case AudioChannel.Master:
                    _masterVolume = clamped;
                    break;
                case AudioChannel.SFX:
                    _sfxVolume = clamped;
                    break;
                case AudioChannel.BGM:
                    _bgmVolume = clamped;
                    _bgmService.SetVolume(clamped);
                    break;
                case AudioChannel.UI:
                    _uiVolume = clamped;
                    break;
            }

            // Update all active voices matching channel or master
            for (int i = 0; i < _activeVoices.Count; i++)
            {
                var v = _activeVoices[i];
                if (channel == AudioChannel.Master || v.Channel == channel)
                {
                    v.UpdateVolume(GetChannelMultiplier(v.Channel));
                }
            }
        }

        public float GetChannelVolume(AudioChannel channel)
        {
            switch (channel)
            {
                case AudioChannel.Master: return _masterVolume;
                case AudioChannel.SFX: return _sfxVolume;
                case AudioChannel.BGM: return _bgmVolume;
                case AudioChannel.UI: return _uiVolume;
                default: return 1f;
            }
        }

        public float GetChannelMultiplier(AudioChannel channel)
        {
            float chan = GetChannelVolume(channel);
            return chan * _masterVolume;
        }

        public int GetActiveVoiceCount()
        {
            return _activeVoices.Count;
        }

        public void StopAll()
        {
            for (int i = _activeVoices.Count - 1; i >= 0; i--)
            {
                DespawnVoiceAtIndex(i);
            }

            _bgmService.StopBGM(0f);
            _concurrencyGuard.Clear();
            _clipRandomizer.Clear();
        }
    }
}
