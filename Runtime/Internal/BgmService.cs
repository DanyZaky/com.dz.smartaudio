using UnityEngine;
using UnityEngine.Audio;

namespace DZ.SmartAudio.Internal
{
    /// <summary>
    /// Dual-channel background music (BGM) service providing seamless automatic crossfading,
    /// dynamic audio ducking, pause/resume, and volume routing.
    /// </summary>
    internal sealed class BgmService
    {
        private readonly AudioSource _sourceA;
        private readonly AudioSource _sourceB;

        private AudioSource _activeSource;
        private AudioSource _fadingSource;

        private float _targetVolume = 1f;
        private bool _isCrossfading;
        private float _crossfadeDuration = 1.5f;
        private float _crossfadeElapsed;
        private float _fadingStartVolume;

        // Fade out stop
        private bool _isStopping;
        private float _stopFadeDuration;
        private float _stopFadeElapsed;
        private float _stopStartVolume;

        // Audio Ducking
        private float _currentDuckFactor = 1f;
        private float _targetDuckFactor = 1f;
        private float _duckHoldTimer;
        private float _duckRestoreDuration = 0.5f;

        public bool IsPlaying => (_activeSource != null && _activeSource.isPlaying) || (_fadingSource != null && _fadingSource.isPlaying);
        public AudioClip CurrentClip => _activeSource != null ? _activeSource.clip : null;

        public BgmService(Transform root)
        {
            GameObject goA = new GameObject("[BGM_ChannelA]");
            goA.transform.SetParent(root, false);
            _sourceA = goA.AddComponent<AudioSource>();
            _sourceA.playOnAwake = false;
            _sourceA.loop = true;
            _sourceA.spatialBlend = 0f; // 2D BGM

            GameObject goB = new GameObject("[BGM_ChannelB]");
            goB.transform.SetParent(root, false);
            _sourceB = goB.AddComponent<AudioSource>();
            _sourceB.playOnAwake = false;
            _sourceB.loop = true;
            _sourceB.spatialBlend = 0f; // 2D BGM

            _activeSource = _sourceA;
            _fadingSource = _sourceB;
        }

        public void PlayBGM(AudioClip clip, float fadeDuration = 1.5f, bool loop = true, float volume = 1f)
        {
            if (clip == null)
            {
                StopBGM(fadeDuration);
                return;
            }

            _targetVolume = Mathf.Clamp01(volume);
            _isStopping = false;

            // If already playing this clip on active channel
            if (_activeSource.clip == clip && _activeSource.isPlaying)
            {
                _activeSource.loop = loop;
                return;
            }

            if (fadeDuration <= 0.0001f)
            {
                _sourceA.Stop();
                _sourceB.Stop();

                _activeSource = _sourceA;
                _fadingSource = _sourceB;

                _activeSource.clip = clip;
                _activeSource.loop = loop;
                _activeSource.volume = _targetVolume;
                _activeSource.Play();

                _isCrossfading = false;
                return;
            }

            // Swap active and fading channels
            var temp = _activeSource;
            _activeSource = _fadingSource;
            _fadingSource = temp;

            _fadingStartVolume = _fadingSource.isPlaying ? _fadingSource.volume : 0f;

            _activeSource.clip = clip;
            _activeSource.loop = loop;
            _activeSource.volume = 0f;
            _activeSource.Play();

            _crossfadeDuration = fadeDuration;
            _crossfadeElapsed = 0f;
            _isCrossfading = true;
        }

        public void PauseBGM()
        {
            if (_activeSource.isPlaying) _activeSource.Pause();
            if (_fadingSource.isPlaying) _fadingSource.Pause();
        }

        public void ResumeBGM()
        {
            if (_activeSource.clip != null && !_activeSource.isPlaying) _activeSource.UnPause();
            if (_fadingSource.clip != null && !_fadingSource.isPlaying) _fadingSource.UnPause();
        }

        public void StopBGM(float fadeDuration = 1f)
        {
            if (!IsPlaying)
                return;

            if (fadeDuration <= 0.0001f)
            {
                _sourceA.Stop();
                _sourceB.Stop();
                _sourceA.clip = null;
                _sourceB.clip = null;
                _isCrossfading = false;
                _isStopping = false;
                return;
            }

            _isStopping = true;
            _isCrossfading = false;
            _stopFadeDuration = fadeDuration;
            _stopFadeElapsed = 0f;
            _stopStartVolume = _activeSource.isPlaying ? _activeSource.volume : 0f;

            if (_fadingSource.isPlaying)
            {
                _fadingSource.Stop();
                _fadingSource.clip = null;
            }
        }

        public void SetVolume(float volume)
        {
            _targetVolume = Mathf.Clamp01(volume);
        }

        public void SetMixerGroup(AudioMixerGroup group)
        {
            _sourceA.outputAudioMixerGroup = group;
            _sourceB.outputAudioMixerGroup = group;
        }

        public void Duck(float duckFactor, float duration, float restoreDuration = 0.5f)
        {
            _targetDuckFactor = Mathf.Clamp01(duckFactor);
            _duckHoldTimer = Mathf.Max(0.01f, duration);
            _duckRestoreDuration = Mathf.Max(0.01f, restoreDuration);
            // Immediately start ducking towards target factor
            _currentDuckFactor = Mathf.Min(_currentDuckFactor, _targetDuckFactor);
        }

        public void Tick(float deltaTime, float bgmChannelVolume, float masterVolume)
        {
            // 1. Process Ducking
            if (_duckHoldTimer > 0f)
            {
                _duckHoldTimer -= deltaTime;
                _currentDuckFactor = Mathf.MoveTowards(_currentDuckFactor, _targetDuckFactor, deltaTime * 5f);
            }
            else
            {
                // Restore smoothly to 1.0f
                float restoreSpeed = deltaTime / _duckRestoreDuration;
                _currentDuckFactor = Mathf.MoveTowards(_currentDuckFactor, 1f, restoreSpeed);
            }

            float finalMultiplier = _currentDuckFactor * bgmChannelVolume * masterVolume;

            // 2. Process Stop Fade
            if (_isStopping)
            {
                _stopFadeElapsed += deltaTime;
                float t = Mathf.Clamp01(_stopFadeElapsed / _stopFadeDuration);
                _activeSource.volume = Mathf.Lerp(_stopStartVolume, 0f, t) * finalMultiplier;

                if (_stopFadeElapsed >= _stopFadeDuration)
                {
                    _isStopping = false;
                    _activeSource.Stop();
                    _activeSource.clip = null;
                    _fadingSource.Stop();
                    _fadingSource.clip = null;
                }
                return;
            }

            // 3. Process Crossfade
            if (_isCrossfading)
            {
                _crossfadeElapsed += deltaTime;
                float t = Mathf.Clamp01(_crossfadeElapsed / _crossfadeDuration);

                _activeSource.volume = Mathf.Lerp(0f, _targetVolume, t) * finalMultiplier;

                if (_fadingSource != null && _fadingSource.isPlaying)
                {
                    _fadingSource.volume = Mathf.Lerp(_fadingStartVolume, 0f, t) * finalMultiplier;
                }

                if (_crossfadeElapsed >= _crossfadeDuration)
                {
                    _isCrossfading = false;
                    if (_fadingSource != null)
                    {
                        _fadingSource.Stop();
                        _fadingSource.clip = null;
                    }
                }
            }
            else
            {
                if (_activeSource != null && _activeSource.isPlaying)
                {
                    _activeSource.volume = _targetVolume * finalMultiplier;
                }
            }
        }

        public void Clear()
        {
            _sourceA.Stop();
            _sourceB.Stop();
            _sourceA.clip = null;
            _sourceB.clip = null;
            _isCrossfading = false;
            _isStopping = false;
            _currentDuckFactor = 1f;
            _duckHoldTimer = 0f;
        }
    }
}
