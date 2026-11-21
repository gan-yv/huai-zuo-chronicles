using Demo02.Player;
using UnityEngine;

namespace Demo02.Core
{
    [DefaultExecutionOrder(100)]
    public sealed class GameAudio : MonoBehaviour
    {
        private const string MutedPreference = "demo02.audio.muted";
        private const int SampleRate = 44100;

        private AudioSource ambientSource;
        private AudioSource oneShotSource;
        private PlayerPartyController party;
        private AudioClip interactionClip;
        private AudioClip footstepClip;
        private AudioClip hitClip;
        private AudioClip hurtClip;
        private AudioClip portalClip;
        private AudioClip confirmClip;
        private Vector3 lastPlayerPosition;
        private float nextFootstepTime;
        private bool hasPlayerPosition;

        public static GameAudio Instance { get; private set; }
        public bool IsMuted { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            IsMuted = PlayerPrefs.GetInt(MutedPreference, 0) == 1;

            ambientSource = CreateSource("Ambient Wind", loop: true, volume: 0.17f);
            oneShotSource = CreateSource("Gameplay Feedback", loop: false, volume: 0.55f);

            ambientSource.clip = CreateAmbientClip();
            interactionClip = CreateChimeClip("Interaction", 520f, 780f, 0.22f);
            footstepClip = CreateFootstepClip();
            hitClip = CreateImpactClip("Attack Hit", 92f, 0.18f, 0.72f);
            hurtClip = CreateImpactClip("Player Hurt", 68f, 0.28f, 0.9f);
            portalClip = CreatePortalClip();
            confirmClip = CreateChimeClip("Save Confirm", 700f, 1040f, 0.3f);

            ApplyMuteState();
            ambientSource.Play();
        }

        private void Start()
        {
            party = FindAnyObjectByType<PlayerPartyController>();
        }

        private void Update()
        {
            if (IsMuted
                || Time.timeScale <= 0f
                || party == null
                || party.ActiveCharacter == null)
            {
                hasPlayerPosition = false;
                return;
            }

            Transform player = party.ActiveCharacter;
            CharacterController controller = player.GetComponent<CharacterController>();
            if (!hasPlayerPosition || Vector3.Distance(lastPlayerPosition, player.position) > 3f)
            {
                lastPlayerPosition = player.position;
                hasPlayerPosition = true;
                return;
            }

            Vector3 delta = player.position - lastPlayerPosition;
            delta.y = 0f;
            lastPlayerPosition = player.position;
            float speed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);

            if (speed < 0.35f
                || (controller != null && !controller.isGrounded)
                || Time.time < nextFootstepTime)
            {
                return;
            }

            float interval = speed > 5.4f ? 0.31f : 0.46f;
            nextFootstepTime = Time.time + interval;
            oneShotSource.pitch = Random.Range(0.92f, 1.06f);
            oneShotSource.PlayOneShot(footstepClip, speed > 5.4f ? 0.42f : 0.3f);
        }

        public static void PlayInteraction()
        {
            PlayOneShot(Instance?.interactionClip, 0.55f);
        }

        public static void PlayHit()
        {
            PlayOneShot(Instance?.hitClip, 0.8f);
        }

        public static void PlayPlayerHurt()
        {
            PlayOneShot(Instance?.hurtClip, 0.8f);
        }

        public static void PlayPortal()
        {
            PlayOneShot(Instance?.portalClip, 0.65f);
        }

        public static void PlayConfirm()
        {
            PlayOneShot(Instance?.confirmClip, 0.55f);
        }

        public static bool ToggleMuted()
        {
            if (Instance == null)
            {
                return false;
            }

            Instance.IsMuted = !Instance.IsMuted;
            PlayerPrefs.SetInt(MutedPreference, Instance.IsMuted ? 1 : 0);
            PlayerPrefs.Save();
            Instance.ApplyMuteState();
            return Instance.IsMuted;
        }

        private void ApplyMuteState()
        {
            AudioListener.volume = IsMuted ? 0f : GameSettings.MasterVolume;
        }

        public static void RefreshVolume()
        {
            Instance?.ApplyMuteState();
        }

        private AudioSource CreateSource(string sourceName, bool loop, float volume)
        {
            GameObject sourceObject = new(sourceName);
            sourceObject.transform.SetParent(transform, worldPositionStays: false);
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.volume = volume;
            return source;
        }

        private static AudioClip CreateAmbientClip()
        {
            const float duration = 8f;
            int sampleCount = Mathf.RoundToInt(SampleRate * duration);
            float[] samples = new float[sampleCount];
            System.Random random = new(1837);
            float wind = 0f;
            float body = 0f;

            for (int index = 0; index < sampleCount; index++)
            {
                float time = index / (float)SampleRate;
                float noise = (float)random.NextDouble() * 2f - 1f;
                wind = Mathf.Lerp(wind, noise, 0.022f);
                body = Mathf.Lerp(body, noise, 0.0028f);
                float gust = 0.58f + 0.42f * Mathf.Sin(time * 1.37f);
                float edgeFade = Mathf.Min(
                    1f,
                    Mathf.Min(time / 0.4f, (duration - time) / 0.4f)
                );
                samples[index] = (wind * 0.72f + body * 0.28f) * gust * edgeFade * 0.34f;
            }

            AudioClip clip = AudioClip.Create("Ambient Wind", sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateChimeClip(
            string clipName,
            float firstFrequency,
            float secondFrequency,
            float duration)
        {
            int sampleCount = Mathf.RoundToInt(SampleRate * duration);
            float[] samples = new float[sampleCount];
            for (int index = 0; index < sampleCount; index++)
            {
                float time = index / (float)SampleRate;
                float envelope = Mathf.Exp(-time * 13f);
                samples[index] = (
                    Mathf.Sin(2f * Mathf.PI * firstFrequency * time) * 0.55f
                    + Mathf.Sin(2f * Mathf.PI * secondFrequency * time) * 0.3f
                ) * envelope * 0.2f;
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateFootstepClip()
        {
            const float duration = 0.17f;
            int sampleCount = Mathf.RoundToInt(SampleRate * duration);
            float[] samples = new float[sampleCount];
            System.Random random = new(509);
            float noiseBody = 0f;

            for (int index = 0; index < sampleCount; index++)
            {
                float time = index / (float)SampleRate;
                float noise = (float)random.NextDouble() * 2f - 1f;
                noiseBody = Mathf.Lerp(noiseBody, noise, 0.18f);
                float envelope = Mathf.Pow(1f - time / duration, 3f);
                float thump = Mathf.Sin(2f * Mathf.PI * 74f * time);
                samples[index] = (noiseBody * 0.16f + thump * 0.2f) * envelope;
            }

            AudioClip clip = AudioClip.Create("Footstep", sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateImpactClip(
            string clipName,
            float frequency,
            float duration,
            float emphasis)
        {
            int sampleCount = Mathf.RoundToInt(SampleRate * duration);
            float[] samples = new float[sampleCount];
            System.Random random = new(clipName.GetHashCode());
            float noiseBody = 0f;

            for (int index = 0; index < sampleCount; index++)
            {
                float time = index / (float)SampleRate;
                float noise = (float)random.NextDouble() * 2f - 1f;
                noiseBody = Mathf.Lerp(noiseBody, noise, 0.08f);
                float envelope = Mathf.Exp(-time * 18f);
                float impact = Mathf.Sin(2f * Mathf.PI * frequency * time);
                samples[index] = (impact * 0.48f + noiseBody * 0.22f) * envelope * emphasis;
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreatePortalClip()
        {
            const float duration = 0.52f;
            int sampleCount = Mathf.RoundToInt(SampleRate * duration);
            float[] samples = new float[sampleCount];
            float phase = 0f;

            for (int index = 0; index < sampleCount; index++)
            {
                float time = index / (float)SampleRate;
                float progress = time / duration;
                float frequency = Mathf.Lerp(165f, 440f, progress);
                phase += 2f * Mathf.PI * frequency / SampleRate;
                float envelope = Mathf.Sin(Mathf.PI * progress);
                samples[index] = Mathf.Sin(phase) * envelope * 0.17f;
            }

            AudioClip clip = AudioClip.Create("Portal Transition", sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static void PlayOneShot(AudioClip clip, float volume)
        {
            if (Instance == null
                || Instance.IsMuted
                || clip == null
                || Instance.oneShotSource == null)
            {
                return;
            }

            Instance.oneShotSource.pitch = 1f;
            Instance.oneShotSource.PlayOneShot(clip, volume);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            AudioListener.volume = 1f;
        }
    }
}
