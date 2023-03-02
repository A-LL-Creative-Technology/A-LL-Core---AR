using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VoxelBusters.CoreLibrary;
using VoxelBusters.ScreenRecorderKit;
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

    private IScreenRecorder m_recorder;

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        if (m_recorder == null)
            return;

        if (!m_recorder.CanRecord())
            return;

        if (ARGestureController.GetInstance().OneFingerTapDetection(out Vector2 tapPosition) && m_recorder.IsRecording())
            StopRecording();
    }

    public void TriggerVideoCapture()
    {
        // Invoke this method to start the video capture process

        if (NativeGallery.CheckPermission(NativeGallery.PermissionType.Write, NativeGallery.MediaType.Image | NativeGallery.MediaType.Video) != NativeGallery.Permission.Granted)
        {
            NavigationController.GetInstance().OnNotificationOpen(false, -1f, "Permission error subtitle", "Permission error (allow save to camera roll)", "Crea Tech");
            return;
        }

        HapticPatterns.PlayPreset(HapticPatterns.PresetType.LightImpact);

        //Invoke("CreateVideoRecorder", .5f);
        CreateVideoRecorder();
    }

    public void CreateVideoRecorder()
    {
        //Dispose if any recorder instance created earlier
        Cleanup();
        VideoRecorderRuntimeSettings settings = new VideoRecorderRuntimeSettings(enableMicrophone: true);
        ScreenRecorderBuilder builder = ScreenRecorderBuilder.CreateVideoRecorder(settings);
        m_recorder = builder.Build();

        GlobalController.LogMe("Video Recorder Created.");

        PrepareVideoRecording();
    }

    private void PrepareVideoRecording()
    {
        // prepare the video recorder and start recording on success
        m_recorder.PrepareRecording(callback: (success, error) =>
        {
            if (success)
            {
                GlobalController.LogMe("Prepare recording successful.");
                StartRecording();
            }
            else
            {
                GlobalController.LogMe($"Prepare recording failed with error [{error}]");
            }
        });
    }

    public bool IsAvailable()
    {
        // bool isRecordingAPIAvailable = ReplayKitManager.IsRecordingAPIAvailable();
        bool isRecordingAPIAvailable = m_recorder.CanRecord();

        string message = isRecordingAPIAvailable ? "Replay Kit recording API is available!" : "Replay Kit recording API is not available.";

        GlobalController.LogMe(message);
        return isRecordingAPIAvailable;
    }

    public void StartRecording()
    {
        GlobalController.LogMe("Start recording");
        if (m_recorder.IsRecording())
        {
            //Recording already in progress
            Debug.Log("Recording already in progress");
            return;
        }

        //ToggleRecordingUI(true);

        OnTakeVideo?.Invoke(this, null);


        StartCoroutine(LaunchRecodingWorkflow());
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

        m_recorder.StartRecording(); // optional callback

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
        if (!m_recorder.IsRecording())
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
        if (!m_recorder.IsRecording())
        {
            //Recording not started"
            Debug.Log("Recording not started");
            return;
        }

        GlobalController.LogMe("Stop the recording");



        //Stop recording

        m_recorder.StopRecording((success, error) =>
        {
            if (success)
            {
                GlobalController.LogMe("Stopped recording");
                SaveRecording();
            }
            else
            {
                GlobalController.LogMe($"Stop recording failed with error: {error}");
                CaptureController.GetInstance().ActivateRecordingUI(false);
                NavigationController.GetInstance().ShowHeader(.4f, false, !CaptureController.GetInstance().isUsingTransparentHeader);
                NavigationController.GetInstance().ShowFooter(.4f);
                ARPlaneController.GetInstance().isRecording = false;

                OnVideoTaken?.Invoke(this, null);
            }
        });

    }

    private void SaveRecording()
    {
        m_recorder.SaveRecording(null, (result, error) =>
        {
            if (error == null)
            {
                GlobalController.LogMe("Saved recording successfully :" + result.Path);
                StartCoroutine(CaptureController.GetInstance().FadeInAndOut(CaptureController.GetInstance().coachingTextVideoSaved));
                CaptureController.GetInstance().ActivateRecordingUI(false);

                ShowRecordingPreview();

            }
            else
            {
                GlobalController.LogMe($"Failed saving recording [{error}]");
            }
        });
    }

    private void ShowRecordingPreview()
    {
        m_recorder.OpenRecording((success, error) =>
        {
            if (success)
            {
                GlobalController.LogMe($"Open recording successful");
            }
            else
            {
                GlobalController.LogMe($"Open recording failed with error [{error}]");
            }

            ARPlaneController.GetInstance().isRecording = false;
            CaptureController.GetInstance().ActivateRecordingUI(false);
            NavigationController.GetInstance().ShowHeader(0.4f, false, !CaptureController.GetInstance().isUsingTransparentHeader);
            NavigationController.GetInstance().ShowFooter(0.4f);

            OnVideoTaken?.Invoke(this, null);
        });
    }

    private void Cleanup()
    {
        if (m_recorder != null)
        {
            if (m_recorder.IsRecording())
            {
                m_recorder.StopRecording();
            }

            m_recorder.Flush();
        }

        m_recorder = null;
    }
}
