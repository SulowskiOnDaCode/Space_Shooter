using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class CinemachineShake : MonoBehaviour
{
    public static CinemachineShake Instance { get; private set; }
    
    private CinemachineBasicMultiChannelPerlin cinemachineBasicMultiChannelPerlin;
    private float shakeTimer;
    private float shakeTimerTotal;
    private float startingIntensity;
    
    private void Awake()
    {
        Instance = this;
        cinemachineBasicMultiChannelPerlin = GetComponentInChildren<CinemachineBasicMultiChannelPerlin>();
    }

    public void ShakeCamera(float intensity, float time)
    {
        if (cinemachineBasicMultiChannelPerlin == null)
        {
            cinemachineBasicMultiChannelPerlin = GetComponentInChildren<CinemachineBasicMultiChannelPerlin>();
        }

        if (cinemachineBasicMultiChannelPerlin != null)
        {
            cinemachineBasicMultiChannelPerlin.AmplitudeGain = intensity;
            startingIntensity = intensity;
            shakeTimerTotal = time;
            shakeTimer = time;
        }
    }

    private void Update()
    {
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;

            if (cinemachineBasicMultiChannelPerlin != null)
            {
                cinemachineBasicMultiChannelPerlin.AmplitudeGain = Mathf.Lerp(startingIntensity, 0f, 1 - (shakeTimer / shakeTimerTotal));
            }
        }
    }
}
