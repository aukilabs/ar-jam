
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem.EnhancedTouch;

class HandInstrument
{
    public bool isPlaying { get; private set; }

    public FingerInstrument[] fingers => m_Fingers;
    public Vector3 handPosition => m_HandPosition;

    private FingerInstrument[] m_Fingers = null;
    private float m_VolumeMultiplier = 0.0f;
    private AudioMixerGroup m_MixerGroup;
    private Transform m_CameraTransform;
    private Vector3 m_HandPosition;
    private float m_HeightParam = 0.5f;
    private ParticleSystem m_HandParticles;

    public void Init(InstrumentConfig instrumentConfig, Transform cameraTransform,
                     GameObject handParticlesPrefab, GameObject fingerParticlesPrefab)
    {
        m_MixerGroup = instrumentConfig.mixerGroup;
        m_CameraTransform = cameraTransform;

        if (m_Fingers == null)
        {
            m_Fingers = new FingerInstrument[5];
            for (int i = 0; i < 5; i++)
            {
                m_Fingers[i] = new FingerInstrument(fingerParticlesPrefab);
            }
        }

        if (m_HandParticles == null)
        {
            m_HandParticles = Object.Instantiate(handParticlesPrefab).GetComponent<ParticleSystem>();
            m_HandParticles.transform.localScale = Vector3.one * 0.04f;
        }

        var emission = m_HandParticles.emission;
        emission.enabled = false;

        m_Fingers[0].InitAudio(instrumentConfig.channel0);
        m_Fingers[1].InitAudio(instrumentConfig.channel1);
        m_Fingers[2].InitAudio(instrumentConfig.channel2);
        m_Fingers[3].InitAudio(instrumentConfig.channel3);
        m_Fingers[4].InitAudio(instrumentConfig.channel4);
    }

    public void UpdateTracking(TrackedHand hand)
    {
        // For landmark order see documentation:
        // https://conjurekit.dev/unity/com.aukilabs.unity.ur/#landmark
        var landmarks = hand.worldLandmarks;
        m_HandPosition = (landmarks[0] + landmarks[5] + landmarks[9] + landmarks[13]) / 4f; // Approximately palm center
        m_Fingers[0].UpdateTracking(landmarks[1], landmarks[2], landmarks[3], landmarks[4]);
        m_Fingers[1].UpdateTracking(landmarks[5], landmarks[6], landmarks[7], landmarks[8]);
        m_Fingers[2].UpdateTracking(landmarks[9], landmarks[10], landmarks[11], landmarks[12]);
        m_Fingers[3].UpdateTracking(landmarks[13], landmarks[14], landmarks[15], landmarks[16]);
        m_Fingers[4].UpdateTracking(landmarks[17], landmarks[18], landmarks[19], landmarks[20]);
    }

    public void Start()
    {
        isPlaying = true;
        m_HeightParam = 0.5f;

        var emission = m_HandParticles.emission;
        emission.enabled = true;
    }

    public void Stop()
    {
        isPlaying = false;

        var emission = m_HandParticles.emission;
        emission.enabled = false;
    }

    public void UpdateInstrument()
    {
        if (isPlaying)
        {
            Vector3 towardsHand = m_HandPosition - m_CameraTransform.position;
            float heightParam = Vector3.SignedAngle(m_CameraTransform.transform.forward, towardsHand,
                m_CameraTransform.transform.right);
            heightParam = Mathf.Clamp(heightParam / 40.0f, -1.0f, 1.0f) * 0.5f + 0.5f;
            heightParam = Mathf.Clamp01(1 - heightParam);
            m_HeightParam = Mathf.Lerp(m_HeightParam, heightParam, 0.2f);

            float lowpass = 18000;
            if (heightParam < 0.4f)
            {
                float factor = Mathf.Clamp01(0.4f - heightParam) / 0.4f;
                lowpass = Mathf.Lerp(18000, 100, Mathf.Pow(factor, 0.3f));
            }
            m_MixerGroup.audioMixer.SetFloat(m_MixerGroup.name + "_lowpass", lowpass);

            float highpass = 10;
            if (heightParam > 0.6f)
            {
                float factor = Mathf.Clamp01(heightParam - 0.6f) / 0.4f;
                highpass = Mathf.Lerp(10, 3000, factor);
            }
            m_MixerGroup.audioMixer.SetFloat(m_MixerGroup.name + "_highpass", highpass);
        }

        m_VolumeMultiplier = Mathf.Lerp(m_VolumeMultiplier, isPlaying ? 1.0f : 0.0f, 0.15f);
        m_Fingers[0].UpdateInstrument(m_VolumeMultiplier);
        m_Fingers[1].UpdateInstrument(m_VolumeMultiplier);
        m_Fingers[2].UpdateInstrument(m_VolumeMultiplier);
        m_Fingers[3].UpdateInstrument(m_VolumeMultiplier);
        m_Fingers[4].UpdateInstrument(m_VolumeMultiplier);

        if (m_HandParticles != null)
        {
            m_HandParticles.transform.position = m_HandPosition;
            var emission = m_HandParticles.emission;
            float intensity = 0;
            foreach (var finger in m_Fingers)
            {
                intensity += finger.audioVolume + finger.audioImpulse * 0.5f;
            }
            intensity /= m_Fingers.Length;
            if (intensity < 0)
                intensity = 0;

            var rate = emission.rateOverTime;
            rate.constant = Mathf.Clamp(intensity * intensity * 15, 0, 80);
            emission.rateOverTime = rate;
        }
    }

    public void DrawDebug_OnGui(int handIndex, int totalHands)
    {
        //        Debug.Log("HandTracker HandInstrument DrawDebug_OnGui, handIndex: " + handIndex + ", totalHands: " + totalHands);

        if (isPlaying)
        {
            for (int i = 0; i < 5; i++)
            {
                m_Fingers[i].DrawDebug_OnGui(handIndex * 5 + 4 - i, totalHands * 5);
            }
        }

        GUI.color = Color.white;
        int posX = 10 + 16 * handIndex;
        float height = Screen.height * 0.4f * (m_HeightParam * 2 - 1);
        if (height > 0)
            GUI.DrawTexture(new Rect(posX, Screen.height / 2 - height, 10, height), Texture2D.whiteTexture);
        else
            GUI.DrawTexture(new Rect(posX, Screen.height / 2, 10, -height), Texture2D.whiteTexture);
    }
}