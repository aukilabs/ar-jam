using System;
using System.Collections;
using Auki.Ur;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class AppMain : MonoBehaviour
{
    [SerializeField] private AppConfig appConfig;
    [SerializeField] private Camera arCamera;
    [SerializeField] private ARCameraManager arCameraManager;
    [SerializeField] private ARRaycastManager arRaycastManager;
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private ARSession arSession;

    private HandService m_HandService;

    void Start()
    {
        Application.targetFrameRate = 30;
        m_HandService = new HandService(arSession, arCamera, arRaycastManager, appConfig.handParticlesPrefab, appConfig.fingerParticlesPrefab);
        m_HandService.SetInstrumentConfig(appConfig.instrumentConfig);

        StartCoroutine(StartHandTrackingWithDelay());
    }

    IEnumerator StartHandTrackingWithDelay()
    {
        bool started = false;
        while (!started)
        {
            // Wait for hand calibration
            yield return new WaitForSeconds(0.2f);
            started = m_HandService.TryStart();
        }

        if (appConfig.handMaterial != null)
            HandTracker.GetInstance().SetMaterialOverride(appConfig.handMaterial);
    }

    void Update()
    {
        HandTracker.GetInstance().Update();
        m_HandService?.UpdateLocalInstruments();
    }

    private void OnGUI()
    {
        m_HandService?.OnGuiHelper();
    }


}
