using System.Collections.Generic;
using UnityEngine;

namespace DZ.SmartAudio.Internal
{
    /// <summary>
    /// Manages concurrency limits (anti-ear-rape) and debounce cooldowns per AudioClip.
    /// Provides voice stealing strategies (IgnoreNew vs RecycleOldest) to prevent audio distortion.
    /// </summary>
    internal sealed class ConcurrencyGuard
    {
        private readonly Dictionary<int, float> _clipLastPlayTimes = new Dictionary<int, float>();
        private readonly Dictionary<int, float> _clipCooldownDurations = new Dictionary<int, float>();
        private readonly Dictionary<int, List<AudioVoice>> _activeVoicesPerClip = new Dictionary<int, List<AudioVoice>>();

        /// <summary>
        /// Registers a default cooldown for a specific AudioClip.
        /// </summary>
        public void SetClipCooldown(AudioClip clip, float duration)
        {
            if (clip == null) return;
            _clipCooldownDurations[clip.GetInstanceID()] = Mathf.Max(0f, duration);
        }

        /// <summary>
        /// Checks whether the clip is currently on cooldown.
        /// </summary>
        public bool IsOnCooldown(AudioClip clip, float customCooldown = -1f)
        {
            if (clip == null) return false;

            int clipId = clip.GetInstanceID();
            float cooldown = customCooldown >= 0f
                ? customCooldown
                : (_clipCooldownDurations.TryGetValue(clipId, out float cd) ? cd : 0f);

            if (cooldown <= 0.0001f)
                return false;

            if (_clipLastPlayTimes.TryGetValue(clipId, out float lastTime))
            {
                return (Time.unscaledTime - lastTime) < cooldown;
            }

            return false;
        }

        /// <summary>
        /// Evaluates if a new voice instance can be spawned for the clip based on max instances.
        /// If max is reached and strategy is RecycleOldest, outputs the oldest active voice to steal.
        /// </summary>
        public bool TryAcquireSlot(
            AudioClip clip,
            int maxInstances,
            VoiceStealStrategy strategy,
            out AudioVoice voiceToSteal)
        {
            voiceToSteal = null;
            if (clip == null)
                return true;

            if (maxInstances <= 0)
                return true;

            int clipId = clip.GetInstanceID();
            if (!_activeVoicesPerClip.TryGetValue(clipId, out var activeList) || activeList.Count < maxInstances)
            {
                return true;
            }

            // Limit is reached
            if (strategy == VoiceStealStrategy.IgnoreNew)
            {
                return false;
            }

            // Strategy is RecycleOldest - pick the voice with the greatest age
            AudioVoice oldest = activeList[0];
            for (int i = 1; i < activeList.Count; i++)
            {
                if (activeList[i].Age > oldest.Age)
                {
                    oldest = activeList[i];
                }
            }

            voiceToSteal = oldest;
            return true;
        }

        /// <summary>
        /// Records that the clip has begun playing, updating cooldown timestamp and active list.
        /// </summary>
        public void RegisterPlay(AudioClip clip, AudioVoice voice)
        {
            if (clip == null) return;

            int clipId = clip.GetInstanceID();
            _clipLastPlayTimes[clipId] = Time.unscaledTime;

            if (!_activeVoicesPerClip.TryGetValue(clipId, out var activeList))
            {
                activeList = new List<AudioVoice>(4);
                _activeVoicesPerClip[clipId] = activeList;
            }

            if (!activeList.Contains(voice))
            {
                activeList.Add(voice);
            }
        }

        /// <summary>
        /// Unregisters a voice when it finishes or stops.
        /// </summary>
        public void UnregisterVoice(AudioClip clip, AudioVoice voice)
        {
            if (clip == null || voice == null) return;

            int clipId = clip.GetInstanceID();
            if (_activeVoicesPerClip.TryGetValue(clipId, out var activeList))
            {
                activeList.Remove(voice);
            }
        }

        public int GetActiveCount(AudioClip clip)
        {
            if (clip == null) return 0;
            return _activeVoicesPerClip.TryGetValue(clip.GetInstanceID(), out var list) ? list.Count : 0;
        }

        public void Clear()
        {
            _clipLastPlayTimes.Clear();
            _clipCooldownDurations.Clear();
            _activeVoicesPerClip.Clear();
        }
    }
}
