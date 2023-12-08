using UnityEngine;

[CreateAssetMenu(fileName = "AppConfig", menuName = "Matterless/AppConfig", order = 0)]
public class AppConfig : ScriptableObject
{
    public Material handMaterial;
    public GameObject handParticlesPrefab;
    public GameObject fingerParticlesPrefab;
    public InstrumentConfig instrumentConfig;
}

[System.Serializable]
public class ConjureKitConfig
{
    public string appKey;
    public string appSecret;
}