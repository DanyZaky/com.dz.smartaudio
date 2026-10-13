using System.Collections.Generic;
using UnityEngine;

namespace DZ.SmartAudio.Internal
{
    /// <summary>
    /// High-performance zero-GC object pool for AudioVoice instances.
    /// Manages allocation, prewarming, and recycling of pooled AudioSources.
    /// </summary>
    internal sealed class AudioSourcePool
    {
        private readonly Transform _rootTransform;
        private readonly Stack<AudioVoice> _availableVoices = new Stack<AudioVoice>(32);
        private readonly List<AudioVoice> _allVoices = new List<AudioVoice>(32);
        private int _nextVoiceId = 1;

        public AudioSourcePool(Transform rootTransform, int initialCapacity = 16)
        {
            _rootTransform = rootTransform;
            Prewarm(initialCapacity);
        }

        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                AudioVoice voice = CreateNewVoice();
                _availableVoices.Push(voice);
            }
        }

        public AudioVoice Rent()
        {
            if (_availableVoices.Count > 0)
            {
                return _availableVoices.Pop();
            }

            // Expand pool if necessary
            return CreateNewVoice();
        }

        public void Return(AudioVoice voice)
        {
            if (voice == null)
                return;

            voice.Recycle();
            _availableVoices.Push(voice);
        }

        private AudioVoice CreateNewVoice()
        {
            int id = _nextVoiceId++;
            GameObject go = new GameObject($"[AudioVoice_{id}]");
            go.transform.SetParent(_rootTransform, false);

            AudioSource audioSource = go.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            AudioVoice voice = new AudioVoice();
            voice.Initialize(id, go, audioSource);

            _allVoices.Add(voice);
            return voice;
        }

        public void Clear()
        {
            for (int i = 0; i < _allVoices.Count; i++)
            {
                var voice = _allVoices[i];
                if (voice?.GameObject != null)
                {
                    Object.Destroy(voice.GameObject);
                }
            }

            _allVoices.Clear();
            _availableVoices.Clear();
            _nextVoiceId = 1;
        }
    }
}
