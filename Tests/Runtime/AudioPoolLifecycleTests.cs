using NUnit.Framework;
using UnityEngine;
using DZ.SmartAudio;
using DZ.SmartAudio.Internal;

namespace DZ.SmartAudio.Tests
{
    [TestFixture]
    public class AudioPoolLifecycleTests
    {
        private AudioClip _clip;

        [SetUp]
        public void SetUp()
        {
            _clip = AudioClip.Create("PoolTestClip", 44100, 1, 44100, false);
            SmartAudio.StopAll();
        }

        [TearDown]
        public void TearDown()
        {
            SmartAudio.StopAll();
            if (_clip != null) Object.DestroyImmediate(_clip);
        }

        [Test]
        public void Pool_ReusesVoice_AndIncrementsGeneration()
        {
            AudioHandle handle1 = _clip.Play2D();
            int voiceId = handle1.Id;
            int gen1 = handle1.Generation;

            Assert.IsTrue(handle1.IsValid);
            Assert.AreEqual(1, SmartAudio.ActiveVoiceCount);

            // Stop voice
            handle1.Stop();
            Assert.IsFalse(handle1.IsValid);
            Assert.AreEqual(0, SmartAudio.ActiveVoiceCount);

            // Play second sound - should reuse pooled voice
            AudioHandle handle2 = _clip.Play2D();
            Assert.AreEqual(voiceId, handle2.Id);
            Assert.AreEqual(gen1 + 1, handle2.Generation);
            Assert.IsTrue(handle2.IsValid);

            // Ensure old handle cannot affect new sound
            handle1.Stop();
            Assert.IsTrue(handle2.IsValid, "Old handle cannot stop the reused voice!");
        }

        [Test]
        public void SafeAttachedCleanup_WhenTargetDestroyed_DoesNotThrowException()
        {
            GameObject target = new GameObject("DestructibleTarget");
            target.transform.position = new Vector3(3f, 4f, 5f);

            AudioHandle handle = _clip.PlayAttached(target.transform).WithLoop(true);
            var voice = SmartAudioService.Instance.FindVoice(handle.Id, handle.Generation);
            Assert.IsNotNull(voice);
            Assert.AreEqual(new Vector3(3f, 4f, 5f), voice.Transform.position);

            // Destroy target object
            Object.DestroyImmediate(target);

            // Ticking voice must NOT throw MissingReferenceException or NullReferenceException
            Assert.DoesNotThrow(() =>
            {
                voice.Tick(0.016f, 1f);
            });

            // Since it was looping attached sound, it safely stops when target is destroyed
            Assert.IsFalse(voice.IsActive);
        }

        [Test]
        public void ChannelVolume_UpdatesActiveVoiceVolumeDynamically()
        {
            SmartAudio.SetVolume(AudioChannel.SFX, 1.0f);
            SmartAudio.MasterVolume = 1.0f;

            AudioHandle handle = _clip.Play2D(0.8f);
            var voice = SmartAudioService.Instance.FindVoice(handle.Id, handle.Generation);
            Assert.AreEqual(0.8f, voice.AudioSource.volume, 0.001f);

            // Change SFX channel volume
            SmartAudio.SetVolume(AudioChannel.SFX, 0.5f);
            Assert.AreEqual(0.4f, voice.AudioSource.volume, 0.001f);

            // Change Master volume
            SmartAudio.MasterVolume = 0.5f;
            Assert.AreEqual(0.2f, voice.AudioSource.volume, 0.001f);
        }
    }
}
