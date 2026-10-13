using NUnit.Framework;
using UnityEngine;
using DZ.SmartAudio;
using DZ.SmartAudio.Internal;

namespace DZ.SmartAudio.Tests
{
    [TestFixture]
    public class ConcurrencyGuardTests
    {
        private AudioClip _clip;

        [SetUp]
        public void SetUp()
        {
            _clip = AudioClip.Create("ConcurrencyTestClip", 44100, 1, 44100, false);
            SmartAudio.StopAll();
        }

        [TearDown]
        public void TearDown()
        {
            SmartAudio.StopAll();
            if (_clip != null)
            {
                Object.DestroyImmediate(_clip);
            }
        }

        [Test]
        public void Cooldown_RejectsRapidConsecutivePlayback()
        {
            SmartAudio.SetClipCooldown(_clip, 2.0f);

            AudioHandle first = _clip.Play2D();
            Assert.IsTrue(first.IsValid);

            // Immediate next play should be rejected by cooldown guard
            AudioHandle second = _clip.Play2D();
            Assert.IsFalse(second.IsValid);
        }

        [Test]
        public void MaxInstances_IgnoreNew_RejectsWhenLimitReached()
        {
            AudioHandle h1 = _clip.Play2D().WithMaxInstances(2, VoiceStealStrategy.IgnoreNew);
            AudioHandle h2 = _clip.Play2D().WithMaxInstances(2, VoiceStealStrategy.IgnoreNew);

            Assert.IsTrue(h1.IsValid);
            Assert.IsTrue(h2.IsValid);
            Assert.AreEqual(2, SmartAudioService.Instance.ConcurrencyGuard.GetActiveCount(_clip));

            // Third play with max 2 and IgnoreNew
            AudioVoice stolenVoice;
            bool canAcquire = SmartAudioService.Instance.ConcurrencyGuard.TryAcquireSlot(
                _clip, 2, VoiceStealStrategy.IgnoreNew, out stolenVoice);

            Assert.IsFalse(canAcquire);
            Assert.IsNull(stolenVoice);
        }

        [Test]
        public void MaxInstances_RecycleOldest_StealsOldestVoice()
        {
            AudioHandle h1 = _clip.Play2D().WithMaxInstances(2, VoiceStealStrategy.RecycleOldest);
            var voice1 = SmartAudioService.Instance.FindVoice(h1.Id, h1.Generation);
            voice1.Tick(1.0f, 1f); // Age voice 1 by 1 second

            AudioHandle h2 = _clip.Play2D().WithMaxInstances(2, VoiceStealStrategy.RecycleOldest);
            var voice2 = SmartAudioService.Instance.FindVoice(h2.Id, h2.Generation);
            voice2.Tick(0.1f, 1f); // Age voice 2 by 0.1 second

            AudioVoice stolenVoice;
            bool canAcquire = SmartAudioService.Instance.ConcurrencyGuard.TryAcquireSlot(
                _clip, 2, VoiceStealStrategy.RecycleOldest, out stolenVoice);

            Assert.IsTrue(canAcquire);
            Assert.AreSame(voice1, stolenVoice, "Oldest voice should be selected for stealing!");
        }
    }
}
