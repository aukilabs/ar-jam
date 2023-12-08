
using UnityEngine;
using UnityEngine.Audio;

[System.Serializable]
public class InstrumentConfig
{
    public AudioMixerGroup mixerGroup;
    public InstrumentChannelConfig channel0, channel1, channel2, channel3, channel4;
}

[System.Serializable]
public class InstrumentChannelConfig
{
    public AudioClip audioClip;
    public AudioMixerGroup mixerGroup;
    public float maxVolume = 1.0f;
}