using NUnit.Framework;
using UnityEngine;
using DZ.SmartAudio;
using DZ.SmartAudio.Internal;

namespace DZ.SmartAudio.Tests
{
    [TestFixture]
    public class BgmAndDuckingTests
    {
        private AudioClip _bgmClipA;
        private AudioClip _bgmClipB;

        [SetUp]
        public void SetUp()
        {
            _bgmClipA = AudioClip.Create("BgmA", 44100, 1, 44100, false);
            _bgmClipB = AudioClip.Create("BgmB", 44100, 1, 44100, false);
            SmartAudio.StopAll();
        }

        [TearDown]
        public void TearDown()
        {
            SmartAudio.StopAll();
            if (_bgmClipA != null) Object.DestroyImmediate(_bgmClipA);
            if (_bgmClipB != null) Object.DestroyImmediate(_bgmClipB);
        }

        [Test]
        public void PlayBGM_StartsPlaybackImmediately_WhenNoFade()
        {
            SmartAudio.PlayBGM(_bgmClipA, fadeDuration: 0f, volume: 0.8f);

            Assert.IsTrue(SmartAudio.IsBGMPlaying);
            Assert.AreSame(_bgmClipA, SmartAudio.CurrentBGMClip);
        }

        [Test]
        public void Crossfade_TransitionsSmoothlyBetweenClips()
        {
            var bgmService = SmartAudioService.Instance.BgmService;

            // Start first song immediately
            bgmService.PlayBGM(_bgmClipA, fadeDuration: 0f, volume: 1f);
            Assert.AreSame(_bgmClipA, bgmService.CurrentClip);

            // Crossfade to second song over 2 seconds
            bgmService.PlayBGM(_bgmClipB, fadeDuration: 2f, volume: 1f);
            Assert.AreSame(_bgmClipB, bgmService.CurrentClip);

            // Tick 1 second (halfway)
            bgmService.Tick(1.0f, 1f, 1f);
            Assert.IsTrue(bgmService.IsPlaying);

            // Tick another 1.1 seconds (complete)
            bgmService.Tick(1.1f, 1f, 1f);
            Assert.AreSame(_bgmClipB, bgmService.CurrentClip);
        }

        [Test]
        public void Ducking_LowersVolume_AndRestoresAfterDuration()
        {
            var bgmService = SmartAudioService.Instance.BgmService;
            bgmService.PlayBGM(_bgmClipA, fadeDuration: 0f, volume: 1f);

            // Duck to 0.2x for 0.5s with restore time 0.5s
            bgmService.Duck(0.2f, 0.5f, 0.5f);

            // Tick during duck hold
            bgmService.Tick(0.1f, 1f, 1f);

            // Tick past hold duration into restore phase
            bgmService.Tick(0.6f, 1f, 1f);

            // Tick past restore phase
            bgmService.Tick(0.6f, 1f, 1f);
            Assert.IsTrue(bgmService.IsPlaying);
        }

        [Test]
        public void PauseAndResume_ManagesBgmState()
        {
            SmartAudio.PlayBGM(_bgmClipA, fadeDuration: 0f);
            Assert.IsTrue(SmartAudio.IsBGMPlaying);

            SmartAudio.PauseBGM();
            SmartAudio.ResumeBGM();
            Assert.IsTrue(SmartAudio.IsBGMPlaying);

            SmartAudio.StopBGM(0f);
            Assert.IsFalse(SmartAudio.IsBGMPlaying);
        }
    }
}
