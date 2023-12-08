
using System;
using System.Linq;
using UnityEngine;

internal class TrackedHand
{
    public Vector3 worldPosition => worldLandmarks[0];
    public Vector3[] worldLandmarks { get; private set; }
    public Action OnHandAppear;
    public Action OnHandDisappear;
    
    public bool isCurrentlyTracked {get; private set; } = false;
    int consecutiveTrackedFrames = 0;
    int consecutiveUntrackedFrames = 0;

    public void UpdateTracking(DetectedHand detectedHand)
    {
        consecutiveUntrackedFrames = 0;
        consecutiveTrackedFrames++;
        worldLandmarks = detectedHand.landmarksWorld.ToArray();
        if(!isCurrentlyTracked && consecutiveTrackedFrames > 3)
        {
            isCurrentlyTracked = true;
            OnHandAppear?.Invoke();
        }
    }

    public void UpdateNotTracking()
    {
        consecutiveTrackedFrames = 0;
        consecutiveUntrackedFrames++;
        worldLandmarks = null;
        if (isCurrentlyTracked && consecutiveUntrackedFrames > 5)
        {
            isCurrentlyTracked = false;
            OnHandDisappear?.Invoke();
        }
    }
}