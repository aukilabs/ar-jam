using System;
using System.Collections.Generic;
using System.Linq;
using Auki.Ur;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Object = UnityEngine.Object;

internal class HandService
{

    private const int MAX_HAND_COUNT = 2;

    private readonly Camera m_ArCamera;
    private readonly GameObject m_HandParticlesPrefab;
    private readonly GameObject m_FingerParticlesPrefab;
    private TrackedHand[] m_Hands;
    private HandInstrument[] m_HandInstruments;

    public HandService(ARSession arSession, Camera arCamera, ARRaycastManager arRaycastManager,
        GameObject handParticlesPrefab, GameObject fingerParticlesPrefab)
    {
        m_ArCamera = arCamera;
        m_HandParticlesPrefab = handParticlesPrefab;
        m_FingerParticlesPrefab = fingerParticlesPrefab;

        HandTracker.GetInstance().SetARSystem(arSession, arCamera, arRaycastManager);
        HandTracker.GetInstance().OnUpdate += OnUpdate;

        m_HandInstruments = new HandInstrument[MAX_HAND_COUNT];

        for (int i = 0; i < MAX_HAND_COUNT; i++)
        {
            m_HandInstruments[i] = new HandInstrument();
        }

        m_Hands = new TrackedHand[MAX_HAND_COUNT];

        for (int i = 0; i < MAX_HAND_COUNT; i++)
        {
            m_Hands[i] = new TrackedHand();
            m_Hands[i].OnHandAppear += m_HandInstruments[i].Start;
            m_Hands[i].OnHandDisappear += m_HandInstruments[i].Stop;
        }
    }


    public void SetInstrumentConfig(InstrumentConfig instrumentConfig)
    {
        for (int i = 0; i < MAX_HAND_COUNT; i++)
        {
            m_HandInstruments[i].Init(instrumentConfig, m_ArCamera.transform, m_HandParticlesPrefab, m_FingerParticlesPrefab);
        }
    }
    public bool TryStart()
    {
        var status = HandTracker.GetInstance().GetCalibrationStatus();
        if (status != HandTracker.CalibrationStatus.CALIBRATED &&
            status != HandTracker.CalibrationStatus.MANUAL_CALIBRATION)
        {
            Debug.Log("HandTracker not yet ready to start, needs calibration.");
            return false;
        }

        Debug.Log("Calling HandTracker.Start()");

        HandTracker.GetInstance().Start();
        return true;
    }

    private void OnUpdate(float[] landmarks, float[] translations, int[] isRightHands, float[] scores)
    {

        List<DetectedHand> detectedHands = new List<DetectedHand>();
        int handCountInArrays = landmarks.Length / 3 / HandTracker.LandmarksCount;
        for (int handIndex = 0; handIndex < handCountInArrays; handIndex++)
        {
            float score = scores[handIndex];
            if (score < 0.3f)
            {
                continue;
            }

            detectedHands.Add(DetectedHand.FromArraysWithOffset(landmarks, translations, handIndex, m_ArCamera.transform));
        }

        if (detectedHands.Count > 0)
            HandTracker.GetInstance().ShowHandMesh();
        else
            HandTracker.GetInstance().HideHandMesh();

        // For each detected hand find the corresponding tracked hand by distance (since order is not guaranteed).
        // Sometimes we'll detect more or less hands than we have tracked hands, so we need to handle that.
        List<DetectedHand> unmatchedDetectedHands = new List<DetectedHand>(detectedHands);
        List<TrackedHand> unmatchedTrackedHands = new List<TrackedHand>(m_Hands);

        foreach (var detectedHand in detectedHands)
        {
            float minDistance = float.MaxValue;
            TrackedHand closestHand = null;
            foreach (var trackedHand in unmatchedTrackedHands)
            {
                if (trackedHand == null || detectedHand.landmarksWorld == null || trackedHand.worldLandmarks == null)
                    continue;

                float distance = Vector3.Distance(detectedHand.landmarksWorld[0], trackedHand.worldPosition);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestHand = trackedHand;
                }
            }

            if (closestHand != null && minDistance < 0.2f)
            {
                unmatchedDetectedHands.Remove(detectedHand);
                unmatchedTrackedHands.Remove(closestHand);
                closestHand.UpdateTracking(detectedHand);
            }
        }


        // Match detected hands which are still unmatched to any available TrackedHand instances (which are not tracking any hand).
        // Can happen if we regain detection of a hand and it's not close to the old tracked position.
        // Then the old TrackedHand will be non tracking and the DetectedHand will not be near it.
        var unusedHands = unmatchedTrackedHands.Where(hand => !hand.isCurrentlyTracked).ToList();
        foreach (var detectedHand in unmatchedDetectedHands)
        {
            var trackedHand = unusedHands.FirstOrDefault();
            if (trackedHand != null)
            {
                unusedHands.Remove(trackedHand);
                unmatchedTrackedHands.Remove(trackedHand);
                trackedHand.UpdateTracking(detectedHand);
            }
            else
            {
                //Debug.Log("Detected more hands than the trackable hands count, or some logic is wrong.");
            }
        }

        // Update unmatched tracked hands to not tracking.
        foreach (var trackedHand in unmatchedTrackedHands)
        {
            if (trackedHand != null)
                trackedHand.UpdateNotTracking();
        }


        for (int i = 0; i < MAX_HAND_COUNT; i++)
        {
            if (m_Hands[i] == null || m_HandInstruments[i] == null || m_Hands[i].worldLandmarks == null)
                continue;

            if (m_Hands[i].isCurrentlyTracked)
                m_HandInstruments[i].UpdateTracking(m_Hands[i]);
        }
    }

    public void UpdateLocalInstruments()
    {
        for (int i = 0; i < MAX_HAND_COUNT; i++)
        {
            if (m_HandInstruments[i] != null)
            {
                m_HandInstruments[i].UpdateInstrument();
            }
        }
    }

    public void OnGuiHelper()
    {
        //Debug.Log("HandTracker Service On GUI Helper");
        for (int i = 0; i < MAX_HAND_COUNT; i++)
        {
            if (m_HandInstruments[i] != null)
            {
                m_HandInstruments[i].DrawDebug_OnGui(i, MAX_HAND_COUNT);
            }
        }
    }
}