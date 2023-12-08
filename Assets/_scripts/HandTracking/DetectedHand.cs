
using Auki.Ur;
using UnityEngine;

public struct DetectedHand
{
    public Vector3[] landmarksWorld { get; private set; }
    
    public static DetectedHand FromArraysWithOffset(float[] landmarks, float[] translations, int handIndex, Transform cameraTransform)
    {
        var hand = new DetectedHand();

        var wristOffset = new Vector3(
            translations[handIndex * 3 + 0],
            translations[handIndex * 3 + 1],
            translations[handIndex * 3 + 2]);

        hand.landmarksWorld = new Vector3[HandTracker.LandmarksCount];
        
        for(int i = handIndex * HandTracker.LandmarksCount; i < (handIndex + 1) * HandTracker.LandmarksCount; i++)
        {
            var landmark = new Vector3(
                landmarks[i * 3 + 0],
                landmarks[i * 3 + 1],
                landmarks[i * 3 + 2]);
            
            hand.landmarksWorld[i] = cameraTransform.TransformPoint(landmark + wristOffset);
        }
        
        return hand;
    }
}