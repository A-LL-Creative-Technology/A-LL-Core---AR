using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARController : MonoBehaviour
{
    private static ARController instance;

    public static ARController GetInstance()
    {
        return instance;
    }

    public ARSession arSession;
    public ARSessionOrigin arSessionOrigin;

    public Camera arCamera;

    public ARPlaneManager arPlaneManager;

    private bool isCurrentlyPaused = false;

    private DateTime pauseTimestamp;

    public static event EventHandler OnARReset;


    private void Awake()
    {
        instance = this;
    }

    IEnumerator Start()
    {
        // check for AR compatibility
        if (ARSession.state == ARSessionState.None ||
            ARSession.state == ARSessionState.CheckingAvailability)
        {
            yield return ARSession.CheckAvailability();
        }

        if (ARSession.state == ARSessionState.Unsupported)
        {
            // Start some fallback experience for unsupported devices
            Debug.LogError("This device does not support AR technology.");
        }
        else
        {
            // Start the AR session
            arSession.enabled = true;

            GlobalController.LogMe("AR Session Origin is enabled");

        }
    }

    public void ResetAR()
    {
        // resetting AR Session
        arSession.Reset();

        if (OnARReset != null)
        {
            OnARReset(this, EventArgs.Empty);
        }
    }

    void OnApplicationPause(bool isPaused)
    {
        if (!isCurrentlyPaused && isPaused)
        {
            // we just entered background
            GlobalController.LogMe("Application just entered background");

            // we save the time
            pauseTimestamp = DateTime.Now;

        }

        if (isCurrentlyPaused && !isPaused)
        {
            // we just entered foreground
            GlobalController.LogMe("Application just entered foreground");

            // if the pause was long, we reset everything
            long elapsedTicks = DateTime.Now.Ticks - pauseTimestamp.Ticks;
            TimeSpan elapsedPauseSpan = new TimeSpan(elapsedTicks);

            if (elapsedPauseSpan.Seconds > 30)
            {
                ResetAR();
            }
        }

        isCurrentlyPaused = isPaused;

    }

}