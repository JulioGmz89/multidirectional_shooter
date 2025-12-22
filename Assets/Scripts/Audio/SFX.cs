using UnityEngine;

namespace ProjectMayhem.Audio
{
    public static class SFX
    {
        public static void Play(AudioEvent evt)
        {
            var m = AudioManager.Instance;
            if (m != null) m.Play(evt);
        }

        public static void Play(AudioEvent evt, Vector3 worldPosition)
        {
            var m = AudioManager.Instance;
            if (m != null) m.PlayAt(evt, worldPosition);
        }

        public static int PlayLoop(AudioEvent evt, Transform followTarget)
        {
            var m = AudioManager.Instance;
            return m != null ? m.PlayLoopAt(evt, followTarget) : -1;
        }

        public static void StopLoop(int loopId)
        {
            var m = AudioManager.Instance;
            if (m != null) m.StopLoop(loopId);
        }
    }
}
