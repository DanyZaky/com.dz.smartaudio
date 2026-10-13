using System.Collections.Generic;
using UnityEngine;

namespace DZ.SmartAudio.Internal
{
    /// <summary>
    /// Anti-monotony clip selector guaranteeing that the same audio clip is never selected
    /// twice consecutively if the collection contains two or more clips.
    /// Uses zero-allocation hashing and O(1) uniform index mapping.
    /// </summary>
    internal sealed class ClipRandomizer
    {
        private readonly Dictionary<int, int> _lastPickedIndices = new Dictionary<int, int>();

        /// <summary>
        /// Selects a random AudioClip from the array without repeating the previously selected index.
        /// </summary>
        public AudioClip GetRandomClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
                return null;

            int count = clips.Length;
            if (count == 1)
                return clips[0];

            int collectionKey = clips.GetHashCode();
            int selectedIndex = SelectNoRepeatIndex(collectionKey, count);
            return clips[selectedIndex];
        }

        /// <summary>
        /// Selects a random AudioClip from the list without repeating the previously selected index.
        /// </summary>
        public AudioClip GetRandomClip(IList<AudioClip> clips)
        {
            if (clips == null || clips.Count == 0)
                return null;

            int count = clips.Count;
            if (count == 1)
                return clips[0];

            int collectionKey = clips.GetHashCode();
            int selectedIndex = SelectNoRepeatIndex(collectionKey, count);
            return clips[selectedIndex];
        }

        private int SelectNoRepeatIndex(int collectionKey, int count)
        {
            if (!_lastPickedIndices.TryGetValue(collectionKey, out int lastIndex))
            {
                int firstPick = Random.Range(0, count);
                _lastPickedIndices[collectionKey] = firstPick;
                return firstPick;
            }

            // Uniformly pick one of the (count - 1) indices that are not lastIndex
            int pick = Random.Range(0, count - 1);
            if (pick >= lastIndex)
            {
                pick++;
            }

            _lastPickedIndices[collectionKey] = pick;
            return pick;
        }

        /// <summary>
        /// Resets all tracked history.
        /// </summary>
        public void Clear()
        {
            _lastPickedIndices.Clear();
        }
    }
}
