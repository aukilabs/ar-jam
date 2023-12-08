
using UnityEngine;
using UnityEngine.Audio;

class FingerInstrument
{
    private readonly ParticleSystem m_Particles;
    private float m_FingerBendSpeed = 0.0f; // Bend speed can be used for more sudden effects, like impulse sounds.
    private float m_FingerBend = 180.0f;
    private float m_PreviousUpdateTrackingTime = 0.0f;
    private AudioSource m_AudioSource;
    private float m_MaxVolume = 1.0f;
    private AudioMixerGroup m_MixerGroup;
    private Vector3 m_FingerPosition;

    public FingerInstrument(GameObject fingerParticlesPrefab)
    {
        if (fingerParticlesPrefab != null)
        {
            m_Particles = Object.Instantiate(fingerParticlesPrefab).GetComponent<ParticleSystem>();
            m_Particles.transform.localScale = Vector3.one * 0.03f;
            var emission = m_Particles.GetComponent<ParticleSystem>().emission;
            emission.enabled = false;
        }
    }

    internal float audioVolume => 1 - Mathf.Clamp01(m_FingerBend / 130f);
    internal float audioImpulse => Mathf.Clamp(-m_FingerBendSpeed / 360f, -1f, 1f);

    public void InitAudio(InstrumentChannelConfig instrumentConfigChannel)
    {
        m_MixerGroup = instrumentConfigChannel.mixerGroup;

        if (m_AudioSource == null)
        {
            var gameobject = new GameObject("Finger Audio Source");
            m_AudioSource = gameobject.AddComponent<AudioSource>();
            m_AudioSource.spatialBlend = 1.0f;
            m_AudioSource.minDistance = 0.2f;
            m_AudioSource.maxDistance = 2f;
            m_AudioSource.loop = true;
            m_AudioSource.outputAudioMixerGroup = m_MixerGroup;
        }
        m_AudioSource.clip = instrumentConfigChannel.audioClip;
        m_AudioSource.volume = audioVolume * instrumentConfigChannel.maxVolume;

        m_MaxVolume = instrumentConfigChannel.maxVolume;

        if (m_AudioSource.isPlaying)
            m_AudioSource.Stop();
        m_AudioSource.Play();
    }

    public void UpdateTracking(Vector3 knuckle, Vector3 joint1, Vector3 joint2, Vector3 tip)
    {
        float bend1 = Vector3.Angle(joint1 - knuckle, joint2 - joint1);
        float bend2 = Vector3.Angle(joint2 - joint1, tip - joint2);
        float bend = bend1 + bend2;

        float dt = Time.time - m_PreviousUpdateTrackingTime;
        m_PreviousUpdateTrackingTime = Time.time;
        if (m_PreviousUpdateTrackingTime == 0.0f)
        {
            dt = 0.0f; // Avoid big change on first frame.
        }

        float bendDelta = bend - m_FingerBend;
        if (dt > 0)
        {
            float bendSpeed = dt > 0 ? bendDelta / dt : 0.0f;
            m_FingerBendSpeed = Mathf.Lerp(m_FingerBendSpeed, bendSpeed, 0.15f);
        }

        m_FingerBend = Mathf.Lerp(m_FingerBend, bend, 0.15f);

        m_FingerPosition = (joint1 + joint2 + tip) / 3f;

        m_PreviousUpdateTrackingTime = Time.time;
    }

    public void UpdateInstrument(float volumeMultiplier)
    {
        m_AudioSource.volume = (audioVolume * audioVolume * m_MaxVolume) * volumeMultiplier;

        m_Particles.transform.position = m_FingerPosition;
        var emission = m_Particles.emission;
        emission.enabled = audioVolume > 0.1f;

        float intensity = audioVolume + audioImpulse;
        var rate = emission.rateOverTime;
        rate.constant = intensity * 20f;
    }

    public void DrawDebug_OnGui(int fingerIndex, int totalFingers)
    {
        float positionX = Mathf.Lerp(0.2f, 0.8f, (float)fingerIndex / totalFingers) * Screen.width;
        float positionY = 0.9f * Screen.height;

        Color col1 = new Color(0.1f, 0.8f, 0.9f, 0.5f);
        Color col2 = new Color(1, 1, 1, 0.8f);

        // Bend visualisation bar
        float height = audioVolume * 0.1f * Screen.height;
        GUI.color = Color.Lerp(col1, col2, audioVolume);
        GUI.DrawTexture(new Rect(positionX - 25, positionY - height, 20, height), Texture2D.whiteTexture);

        // Bend speed visualisation bar
        float speedHeight = audioImpulse * 0.1f * Screen.height;
        if (audioImpulse > 0)
            GUI.color = Color.Lerp(col1, col2, audioImpulse);
        else
            GUI.color = Color.Lerp(col1, new Color(0.9f, 0.5f, 0.3f, 0.7f), -audioImpulse);
        GUI.DrawTexture(new Rect(positionX + 5, positionY - speedHeight, 20, speedHeight), Texture2D.whiteTexture);
    }
}