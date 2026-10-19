using System;
using DriveTheCityII.Core.Control;
using DriveTheCityII.Core.Motion;
using DriveTheCityII.Diagnostics;
using Game;
using UnityEngine;
using UnityEngine.Audio;
using UTime = UnityEngine.Time;

namespace DriveTheCityII.Player
{
    /// <summary>
    /// Procedural sound for the vehicle the player drives (ART_AND_ANIMATION / AUDIO): an engine tone whose pitch follows a
    /// virtual gearbox, rolling/wind noise rising with speed, and a short thump on impacts. No audio files are shipped:
    /// the waveform is synthesized on the audio thread. Output goes through the game's own mixer ("World" group), so
    /// the game's master and in-game volume sliders apply. Silent when not driving, paused, or when turned off in Options.
    /// </summary>
    public partial class VehicleAudioSystem : GameSystemBase
    {
        private const int kRate = 44100;

        private GameObject m_Host;
        private AudioSource m_Source;
        private bool m_Failed;
        private float m_ImpactPending;

        // Written on the main thread, read on the audio thread.
        private volatile float m_TargetFreq = 30f;
        private volatile float m_TargetLoad;
        private volatile float m_TargetNoise;
        private volatile float m_TargetGain;
        private volatile float m_Impact;

        // Audio-thread state.
        private float m_Freq = 30f;
        private float m_Load;
        private float m_Noise;
        private float m_Gain;
        private double m_Phase;
        private float m_Lp;
        private float m_NoiseLp;
        private float m_ImpactEnv;
        private uint m_Rng = 2463534242u;

        private static readonly float[] s_CarBands = { 0f, 6f, 12f, 18f, 26f, 99f };
        private static readonly float[] s_HeavyBands = { 0f, 4f, 8f, 13f, 19f, 99f };

        protected override void OnCreate()
        {
            base.OnCreate();
        }

        private bool EnsureSource()
        {
            if (m_Source != null)
            {
                return true;
            }
            if (m_Failed)
            {
                return false;
            }
            try
            {
                m_Host = new GameObject("DriveTheCityII.VehicleAudio");
                UnityEngine.Object.DontDestroyOnLoad(m_Host);
                m_Source = m_Host.AddComponent<AudioSource>();
                m_Source.clip = AudioClip.Create("dtc2-vehicle", kRate, 1, kRate, true, OnAudioRead);
                m_Source.loop = true;
                m_Source.spatialBlend = 0f;
                m_Source.volume = 1f;
                m_Source.playOnAwake = false;
                AudioMixer mixer = Resources.Load<AudioMixer>("Audio/MasterMixer");
                AudioMixerGroup[] groups = mixer != null ? mixer.FindMatchingGroups("World") : null;
                if (groups != null && groups.Length > 0)
                {
                    m_Source.outputAudioMixerGroup = groups[0];
                }
                DtcLog.Info($"VehicleAudio: procedural engine sound ready (mixer group: {(m_Source.outputAudioMixerGroup != null ? m_Source.outputAudioMixerGroup.name : "none")})");
                PlayerSystem p = PlayerSystem.Instance;
                if (p != null)
                {
                    p.VehicleImpact += r => m_ImpactPending = Math.Max(m_ImpactPending, Mathf.Clamp01((r.ImpactSpeed - 1.5f) / 8f));
                }
                return true;
            }
            catch (Exception ex)
            {
                m_Failed = true;
                DtcLog.Error("VehicleAudio: could not create the audio source; vehicle sound disabled", ex);
                return false;
            }
        }

        protected override void OnUpdate()
        {
            PlayerSystem p = PlayerSystem.Instance;
            Settings.DriveTheCityIISettings settings = Settings.DriveTheCityIISettings.Instance;
            bool enabled = settings == null || settings.VehicleSounds;
            bool driving = p != null && p.State == PlayerState.Driving && p.Vehicle != null && !p.Vehicle.Lost;
            bool paused = p != null && (p.StateMachine.Overlays & PlayerOverlay.Paused) != 0;
            if (!enabled || !driving || paused)
            {
                m_TargetGain = 0f;
                if (m_Source != null && m_Source.isPlaying && m_Gain < 0.001f)
                {
                    m_Source.Stop();
                }
                return;
            }
            if (!EnsureSource())
            {
                return;
            }
            VehicleState s = p.Vehicle.State;
            float speed = Math.Abs(s.Speed);
            bool heavy = p.Vehicle.Spec.Length > 7f;
            float throttle = Mathf.Clamp01(Math.Max(p.LastInput.Throttle, s.Gear == Gear.Reverse ? p.LastInput.Brake : 0f));

            // Virtual gearbox: rpm climbs within each speed band and drops at the shift points.
            float[] bands = heavy ? s_HeavyBands : s_CarBands;
            int g = 0;
            while (g < bands.Length - 2 && speed > bands[g + 1])
            {
                g++;
            }
            float within = Mathf.Clamp01((speed - bands[g]) / Math.Max(1f, bands[g + 1] - bands[g]));
            float idleRpm = heavy ? 650f : 850f;
            float topRpm = heavy ? 2300f : 4800f;
            float rpm = speed < 0.3f ? idleRpm + throttle * 900f : Mathf.Lerp(idleRpm + 700f, topRpm, within) + throttle * 250f;
            // Firing frequency of a 4-stroke engine: rpm/60 * cylinders/2 (4 cylinders car, 6 cylinders heavy).
            m_TargetFreq = rpm / 60f * (heavy ? 3f : 2f);
            m_TargetLoad = 0.25f + 0.75f * throttle;
            m_TargetNoise = Mathf.Clamp01(speed / 25f);
            m_TargetGain = 0.22f;
            if (m_ImpactPending > 0f)
            {
                m_Impact = m_ImpactPending;
                m_ImpactPending = 0f;
            }
            if (!m_Source.isPlaying)
            {
                m_Source.Play();
            }
        }

        /// <summary>Audio thread: fills the stream with the engine waveform, rolling noise and impact thumps.</summary>
        private void OnAudioRead(float[] data)
        {
            float tf = m_TargetFreq, tl = m_TargetLoad, tn = m_TargetNoise, tg = m_TargetGain;
            float imp = m_Impact;
            if (imp > 0f)
            {
                m_ImpactEnv = Math.Max(m_ImpactEnv, imp);
                m_Impact = 0f;
            }
            const float smooth = 0.0008f;
            for (int i = 0; i < data.Length; i++)
            {
                m_Freq += (tf - m_Freq) * smooth;
                m_Load += (tl - m_Load) * smooth;
                m_Noise += (tn - m_Noise) * smooth;
                m_Gain += (tg - m_Gain) * smooth;
                m_Phase += m_Freq / kRate;
                if (m_Phase >= 1.0)
                {
                    m_Phase -= 1.0;
                }
                float ph = (float)m_Phase;
                // Engine: pulse train (combustion) plus low harmonics, softened by a one-pole low-pass that opens with load.
                float pulse = ph < 0.18f ? 1f - ph / 0.18f : 0f;
                float tone = 0.55f * pulse + 0.30f * Mathf.Sin(ph * 6.2831853f) + 0.15f * Mathf.Sin(ph * 12.566371f);
                float cutoff = 0.05f + 0.10f * m_Load;
                m_Lp += (tone - m_Lp) * cutoff;
                // Rolling and wind noise.
                m_Rng ^= m_Rng << 13;
                m_Rng ^= m_Rng >> 17;
                m_Rng ^= m_Rng << 5;
                float white = (m_Rng & 0xFFFF) / 32768f - 1f;
                m_NoiseLp += (white - m_NoiseLp) * 0.06f;
                float sample = m_Lp * (0.45f + 0.55f * m_Load) + m_NoiseLp * 0.6f * m_Noise * m_Noise;
                // Impact thump: decaying low noise burst.
                if (m_ImpactEnv > 0.0001f)
                {
                    sample += m_NoiseLp * 2.5f * m_ImpactEnv;
                    m_ImpactEnv *= 0.9993f;
                }
                data[i] = Mathf.Clamp(sample * m_Gain, -1f, 1f);
            }
        }

        /// <summary>Diagnostics for the test bridge: source state, synthesis parameters, and the listener's output level.</summary>
        public string Describe()
        {
            var buf = new float[1024];
            AudioListener.GetOutputData(buf, 0);
            double sum = 0;
            foreach (float x in buf)
            {
                sum += x * x;
            }
            float rms = (float)Math.Sqrt(sum / buf.Length);
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "playing={0} group={1} freqHz={2:0.0} targetFreqHz={3:0.0} gain={4:0.000} load={5:0.00} noise={6:0.00} listenerRms={7:0.0000} failed={8}",
                m_Source != null && m_Source.isPlaying, m_Source?.outputAudioMixerGroup?.name ?? "none", m_Freq, m_TargetFreq, m_Gain, m_Load, m_Noise, rms, m_Failed);
        }

        protected override void OnDestroy()
        {
            if (m_Host != null)
            {
                UnityEngine.Object.Destroy(m_Host);
            }
            base.OnDestroy();
        }
    }
}
