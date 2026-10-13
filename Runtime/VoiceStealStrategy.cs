namespace DZ.SmartAudio
{
    /// <summary>
    /// Strategy applied when active voices for a clip reach the concurrency limit.
    /// </summary>
    public enum VoiceStealStrategy
    {
        /// <summary>
        /// Ignore the new playback request and return an invalid handle.
        /// </summary>
        IgnoreNew = 0,

        /// <summary>
        /// Stop and recycle the oldest active voice playing this clip, reusing it for the new request.
        /// </summary>
        RecycleOldest = 1
    }
}
