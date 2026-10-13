using NUnit.Framework;
using UnityEngine;
using DZ.SmartAudio;
using DZ.SmartAudio.Internal;

namespace DZ.SmartAudio.Tests
{
    [TestFixture]
    public class DynamicsAndRandomTests
    {
        private AudioClip[] _clips;

        [SetUp]
        public void SetUp()
        {
            _clips = new AudioClip[4];
            for (int i = 0; i < _clips.Length; i++)
            {
                _clips[i] = AudioClip.Create($"RandomClip_{i}", 44100, 1, 44100, false);
            }
            SmartAudio.StopAll();
        }

        [TearDown]
        public void TearDown()
        {
            SmartAudio.StopAll();
            for (int i = 0; i < _clips.Length; i++)
            {
                if (_clips[i] != null)
                {
                    Object.DestroyImmediate(_clips[i]);
                }
            }
        }

        [Test]
        public void RandomNoRepeat_NeverPlaysSameClipTwiceConsecutively()
        {
            AudioClip lastClip = null;

            for (int i = 0; i < 50; i++)
            {
                AudioClip picked = _clips.GetRandomClip();
                Assert.IsNotNull(picked);

                if (lastClip != null)
                {
                    Assert.AreNotSame(lastClip, picked, $"Clip was repeated consecutively on iteration {i}!");
                }

                lastClip = picked;
            }
        }

        [Test]
        public void SingleClipCollection_SafelyReturnsOnlyClip()
        {
            AudioClip[] singleArr = new AudioClip[] { _clips[0] };
            for (int i = 0; i < 10; i++)
            {
                AudioClip picked = singleArr.GetRandomClip();
                Assert.AreSame(_clips[0], picked);
            }
        }

        [Test]
        public void PitchVariation_StaysWithinRange()
        {
            for (int i = 0; i < 20; i++)
            {
                AudioHandle handle = _clips[0].Play2D().WithPitchVariation(0.1f);
                var voice = SmartAudioService.Instance.FindVoice(handle.Id, handle.Generation);
                Assert.IsNotNull(voice);
                Assert.GreaterOrEqual(voice.BasePitch, 0.9f);
                Assert.LessOrEqual(voice.BasePitch, 1.1f);
                handle.Stop();
            }
        }

        [Test]
        public void RandomVolume_StaysWithinSpecifiedRange()
        {
            for (int i = 0; i < 20; i++)
            {
                AudioHandle handle = _clips[0].Play2D().WithRandomVolume(0.4f, 0.8f);
                var voice = SmartAudioService.Instance.FindVoice(handle.Id, handle.Generation);
                Assert.IsNotNull(voice);
                Assert.GreaterOrEqual(voice.BaseVolume, 0.4f - 0.001f);
                Assert.LessOrEqual(voice.BaseVolume, 0.8f + 0.001f);
                handle.Stop();
            }
        }
    }
}
