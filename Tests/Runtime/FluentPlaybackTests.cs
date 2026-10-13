using NUnit.Framework;
using UnityEngine;
using DZ.SmartAudio;
using DZ.SmartAudio.Internal;

namespace DZ.SmartAudio.Tests
{
    [TestFixture]
    public class FluentPlaybackTests
    {
        private AudioClip _testClip;

        [SetUp]
        public void SetUp()
        {
            _testClip = AudioClip.Create("FluentTestClip", 44100, 1, 44100, false);
            SmartAudio.StopAll();
        }

        [TearDown]
        public void TearDown()
        {
            SmartAudio.StopAll();
            if (_testClip != null)
            {
                Object.DestroyImmediate(_testClip);
            }
        }

        [Test]
        public void Play2D_ReturnsValidHandle_AndConfiguresVoice()
        {
            AudioHandle handle = _testClip.Play2D(0.75f, 1.25f);

            Assert.IsTrue(handle.IsValid);
            var voice = SmartAudioService.Instance.FindVoice(handle.Id, handle.Generation);
            Assert.IsNotNull(voice);
            Assert.AreEqual(0.75f, voice.BaseVolume, 0.001f);
            Assert.AreEqual(1.25f, voice.BasePitch, 0.001f);
            Assert.AreEqual(0f, voice.AudioSource.spatialBlend, 0.001f);
        }

        [Test]
        public void PlayAt_SetsPositionAndSpatialBlend()
        {
            Vector3 targetPos = new Vector3(10f, 5f, -2f);
            AudioHandle handle = _testClip.PlayAt(targetPos, 0.5f, 1f, 1f);

            Assert.IsTrue(handle.IsValid);
            var voice = SmartAudioService.Instance.FindVoice(handle.Id, handle.Generation);
            Assert.IsNotNull(voice);
            Assert.AreEqual(targetPos, voice.Transform.position);
            Assert.AreEqual(1f, voice.AudioSource.spatialBlend, 0.001f);
        }

        [Test]
        public void PlayAttached_FollowsTargetTransform()
        {
            GameObject targetGo = new GameObject("TestTarget");
            targetGo.transform.position = new Vector3(1f, 2f, 3f);
            Vector3 offset = new Vector3(0f, 1f, 0f);

            AudioHandle handle = _testClip.PlayAttached(targetGo.transform, offset);

            Assert.IsTrue(handle.IsValid);
            var voice = SmartAudioService.Instance.FindVoice(handle.Id, handle.Generation);
            Assert.IsNotNull(voice);
            Assert.AreEqual(new Vector3(1f, 3f, 3f), voice.Transform.position);

            // Move target
            targetGo.transform.position = new Vector3(5f, 5f, 5f);
            voice.Tick(0.016f, 1f);
            Assert.AreEqual(new Vector3(5f, 6f, 5f), voice.Transform.position);

            Object.DestroyImmediate(targetGo);
        }

        [Test]
        public void FluentModifiers_Chaining_ConfiguresAllProperties()
        {
            AudioHandle handle = _testClip.Play2D()
                .WithVolume(0.8f)
                .WithPitch(1.2f)
                .WithDelay(0.5f)
                .WithSpatialBlend(0.5f)
                .WithMinMaxDistance(2f, 30f)
                .WithLoop(true)
                .WithChannel(AudioChannel.UI);

            Assert.IsTrue(handle.IsValid);
            var voice = SmartAudioService.Instance.FindVoice(handle.Id, handle.Generation);
            Assert.IsNotNull(voice);
            Assert.AreEqual(0.8f, voice.BaseVolume, 0.001f);
            Assert.AreEqual(1.2f, voice.BasePitch, 0.001f);
            Assert.AreEqual(0.5f, voice.DelayRemaining, 0.001f);
            Assert.AreEqual(0.5f, voice.AudioSource.spatialBlend, 0.001f);
            Assert.AreEqual(2f, voice.AudioSource.minDistance, 0.001f);
            Assert.AreEqual(30f, voice.AudioSource.maxDistance, 0.001f);
            Assert.IsTrue(voice.IsLoop);
            Assert.AreEqual(AudioChannel.UI, voice.Channel);
        }

        [Test]
        public void HandleControls_Stop_Pause_Resume()
        {
            AudioHandle handle = _testClip.Play2D();
            Assert.IsTrue(handle.IsValid);

            var voice = SmartAudioService.Instance.FindVoice(handle.Id, handle.Generation);
            Assert.IsNotNull(voice);

            handle.Pause();
            Assert.IsTrue(voice.IsPaused);

            handle.Resume();
            Assert.IsFalse(voice.IsPaused);

            handle.Stop();
            Assert.IsFalse(handle.IsValid);
        }
    }
}
