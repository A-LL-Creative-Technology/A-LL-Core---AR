using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VoxelBusters.ReplayKit;
using Lofelt.NiceVibrations;
#if UNITY_IOS
using UnityEngine.Apple.ReplayKit;
#endif


public class VideoCaptureController : MonoBehaviour
{

    private static VideoCaptureController instance;

    public static VideoCaptureController GetInstance()
    {
        return instance;
    }

    public static event EventHandler OnTakeVideo;
    public static event EventHandler OnVideoTaken;

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        if (!ReplayKitManager.IsRecordingAPIAvailable())
            return;

        if (ARGestureController.GetInstance().OneFingerTapDetection(out Vector2 tapPosition) && ReplayKitManager.IsRecording())
            StopRecording();
    }

    void OnEnable()
    {
        if(IsAvailable())
        {
            ReplayKitManager.DidInitialise += DidInitialise;
            ReplayKitManager.DidRecordingStateChange += DidRecordingStateChange;
        }
            
            
    }
    void OnDisable()
    {
        if(IsAvailable())
        {
            ReplayKitManager.DidInitialise -= DidInitialise;
            ReplayKitManager.DidRecordingStateChange -= DidRecordingStateChange;
        }
            
    }

    private void DidInitialise(ReplayKitInitialisationState state, string message)
    {
        GlobalController.LogMe("Received Event Callback : DidInitialise [State:" + state.ToString() + " " + "Message:" + message);

        switch (state)
        {
            case ReplayKitInitialisationState.Failed:
                GlobalController.LogMe("ReplayKitManager.DidInitialise : Initialisation Failed with message[" + message + "]");
                break;
            default:
                GlobalController.LogMe("Unknown State");
                break;
        }
    }


    public void TriggerVideoCapture()
    {
        if (NativeGallery.CheckPermission(NativeGallery.PermissionType.Write, NativeGallery.MediaType.Image | NativeGallery.MediaType.Video) != NativeGallery.Permission.Granted)
        {
            NavigationController.GetInstance().OnNotificationOpen(false, -1f, "Permission error subtitle", "Permission error (allow save to camera roll)", "Crea Tech");
            return;
        }

        HapticPatterns.PlayPreset(HapticPatterns.PresetType.LightImpact);

        Invoke("PrepareVideoRecording", .5f);
    }

    private void PrepareVideoRecording()
    {
        //Hide UI
        StartRecording();
    }

    private void DidRecordingStateChange(ReplayKitRecordingState state, string message)
    {
        Debug.Log("Received Event Callback : DidRecordingStateChange [State:" + state.ToString() + " " + "Message:" + message);

        switch (state)
        {
            case ReplayKitRecordingState.Failed:
                NavigationController.GetInstance().OnNotificationOpen(false, -1f, "string:Erreur de permission", "Permission error (allow screen capture)", "Crea Tech");
                NavigationController.GetInstance().ShowHeader(.4f, false, !CaptureController.GetInstance().isUsingTransparentHeader);
                NavigationController.GetInstance().ShowFooter(.4f);
                break;
            default:
                Debug.Log("Unknown State");
                break;
        }
    }



    public bool IsAvailable()
    {
        bool isRecordingAPIAvailable = ReplayKitManager.IsRecordingAPIAvailable();

        string message = isRecordingAPIAvailable ? "Replay Kit recording API is available!" : "Replay Kit recording API is not available.";

        GlobalController.LogMe(message);
        return isRecordingAPIAvailable;
    }

    public void PrepareRecording(bool shallRecordMicrophone)
    {
        GlobalController.LogMe("Start preparing to record");
        ReplayKitManager.SetMicrophoneStatus(shallRecordMicrophone);

        ReplayKitManager.PrepareRecording();
    }

    public void StartRecording()
    {
        GlobalController.LogMe("Start recording");
        if (ReplayKitManager.IsRecording())
        {
            //Recording already in progress
            Debug.Log("Recording already in progress");
            return;
        }

        //ToggleRecordingUI(true);

        OnTakeVideo?.Invoke(this, null);


        StartCoroutine(LaunchRecodingWorkflow());

        //Replay kit started
    }

    private IEnumerator LaunchRecodingWorkflow()
    {
        ARPlaneController.GetInstance().isRecording = true;

        //Hide UI
        NavigationController.GetInstance().HideHeader(.4f);
        NavigationController.GetInstance().HideFooter(.4f);

        

        //Show coaching
        yield return StartCoroutine(CaptureController.GetInstance().FadeInAndOut(CaptureController.GetInstance().coachingTextStopVideo));

        //Display Countdown
        //TextMeshProUGUI countdownText = ARUIController.GetInstance().coachingTextCountdown.GetComponent<TextMeshProUGUI>();
        //ARUIController.GetInstance().coachingTextCountdown.SetActive(true);

        //for (int i = 3; i >= 1; i--)
        //{
        //    countdownText.text = i + "...";
        //    yield return StartCoroutine(ARUIController.GetInstance().FadeInAndOut(ARUIController.GetInstance().coachingTextCountdown));
        //}
        CaptureController.GetInstance().ActivateRecordingUI(true);
        CanvasGroup watermark = CaptureController.GetInstance().watermarkContainer.GetComponent<CanvasGroup>();
        watermark.alpha = 0f;

        LeanTween.alphaCanvas(watermark, 1f, CaptureController.ANIMATION_FADE_DURATION).setEaseInOutExpo();

        yield return new WaitForSeconds(CaptureController.ANIMATION_FADE_DURATION + .1f);

        ReplayKitManager.StartRecording();

        yield return new WaitForSeconds(.5f);

        CheckRecordingStatus();
    }

    private IEnumerator FadeIn(Graphic graphic, float duration)
    {
        graphic.color = new Color(graphic.color.r, graphic.color.g, graphic.color.b, 0);
        for (float i = 0; i <= duration; i += Time.deltaTime)
        {

            graphic.color = new Color(graphic.color.r, graphic.color.g, graphic.color.b, i / duration);
            yield return null;
        }
    }

    private IEnumerator FadeOut(Graphic graphic, float duration)
    {
        graphic.color = new Color(graphic.color.r, graphic.color.g, graphic.color.b, 1);
        for (float i = duration; i >= 0; i -= Time.deltaTime)
        {
            graphic.color = new Color(graphic.color.r, graphic.color.g, graphic.color.b, i / duration);
            yield return null;
        }
    }

    private void CheckRecordingStatus()
    {
        if (!ReplayKitManager.IsRecording())
         {
             GlobalController.LogMe("VideoCaptureController PERMISSION REFUSED, RESETING UI");
             //UI already disable in UI Controller, if failed record put UI back
             CaptureController.GetInstance().ActivateRecordingUI(false);
             return;
         }

        GlobalController.LogMe("VideoCaptureController PERMISSION GRANTED START RECORDING");

    }

    public void StopRecording()
    {
        if (!ReplayKitManager.IsRecording())
        {
            //Recording not started"
            Debug.Log("Recording not started");
            return;
        }

        GlobalController.LogMe("Stop recording");

       

        //Recording is stopping...
        StartCoroutine(StopAndSave());

    }

    private IEnumerator StopAndSave()
    {
        

        ReplayKitManager.StopRecording((filePath, error) => {
            if (string.IsNullOrEmpty(error))
            {
                
                StartCoroutine(SaveVideo());
            }
            else
            {
                Debug.LogError("Error : " + error);
                CaptureController.GetInstance().ActivateRecordingUI(false);
                NavigationController.GetInstance().ShowHeader(.4f, false, !CaptureController.GetInstance().isUsingTransparentHeader);
                NavigationController.GetInstance().ShowFooter(.4f);
                ARPlaneController.GetInstance().isRecording = false;

                OnVideoTaken?.Invoke(this, null);
            }
        });

        yield return null;
    }

    

    public IEnumerator SaveVideo() //Saves preview to gallery
    {
        
        if (ReplayKitManager.IsPreviewAvailable())
        {
            ReplayKitManager.SavePreview((error) =>
            {
                GlobalController.LogMe("Saved preview to gallery with error : " + ((error == null) ? "null" : error));
            });

            yield return StartCoroutine(CaptureController.GetInstance().FadeInAndOut(CaptureController.GetInstance().coachingTextVideoSaved));
            CaptureController.GetInstance().ActivateRecordingUI(false);
        }
        else
        {
            GlobalController.LogMe("Recorded file not yet available. Please wait for ReplayKitRecordingState.Available status");
        }

        ReplayKitManager.Preview();

        ARPlaneController.GetInstance().isRecording = false;
        CaptureController.GetInstance().ActivateRecordingUI(false);
        NavigationController.GetInstance().ShowHeader(0.4f, false, !CaptureController.GetInstance().isUsingTransparentHeader);
        NavigationController.GetInstance().ShowFooter(0.4f);

        OnVideoTaken?.Invoke(this, null);
    }

    //private IEnumerator PreparePreview()
    //{
    //    Debug.Log("Prepare Preview");
    //    while (ReplayKitManager.IsRecording() || !IsAvailable())
    //    {
    //        yield return null;
    //    }

    //    Debug.Log("Open Perview");
    //    if (ReplayKitManager.IsPreviewAvailable())
    //    {
    //        ReplayKitManager.Preview();
    //    }
    //    else
    //    {
    //        GlobalController.LogMe("No preview available");
    //    }        
    //}
}
