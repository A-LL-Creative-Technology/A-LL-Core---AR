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

    public static VideoCaptureController Instance
    {
        get
        {
            return instance;
        }
    }

    public static event EventHandler OnTakeVideo;
    public static event EventHandler OnVideoTaken;

    private IScreenRecorder m_recorder;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            //DontDestroyOnLoad(gameObject); // To keep the instance across different scenes
        }
        else if (instance != this)
        {
            Debug.LogError("Another instance of VideoCaptureController has been created! Destroying this one.");
            Destroy(this.gameObject);
        }
    }

    private void Update()
    {
        if (m_recorder == null)
            return;

        if (!m_recorder.IsRecording())
            return;

        if (ARGestureController.Instance.OneFingerTapDetection(out Vector2 tapPosition) && m_recorder.IsRecording())
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
        //Dispose of any recorder instance created earlier
        Cleanup();
        VideoRecorderRuntimeSettings settings = new VideoRecorderRuntimeSettings(enableMicrophone: true);
        ScreenRecorderBuilder builder = ScreenRecorderBuilder.CreateVideoRecorder(settings);
        m_recorder = builder.Build();

        Debug.Log("Video Recorder Created.");

        PrepareVideoRecording();
    }

    private void PrepareVideoRecording()
    {
        // prepare the video recorder and start recording on success
        m_recorder.PrepareRecording(callback: (success, error) =>
        {
            if (success)
            {
                Debug.Log("Prepare recording successful.");
                StartRecording();
            }
            else
            {
                Debug.Log($"Prepare recording failed with error [{error}]");
            }
        });
    }

    public bool IsAvailable()
    {
        // bool isRecordingAPIAvailable = ReplayKitManager.IsRecordingAPIAvailable();
        bool isRecordingAPIAvailable = m_recorder.CanRecord();

        string message = isRecordingAPIAvailable ? "Replay Kit recording API is available!" : "Replay Kit recording API is not available.";

        Debug.Log(message);
        return isRecordingAPIAvailable;
    }

    public void StartRecording()
    {
        Debug.Log("Start recording");
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
        ARPlaneController.Instance.isRecording = true;

        //Hide UI
        NavigationController.GetInstance().HideHeader(.4f);
        NavigationController.GetInstance().HideFooter(.4f);

        //Show coaching
        yield return StartCoroutine(CaptureController.Instance.FadeInAndOut(CaptureController.Instance.coachingTextStopVideo));

        //Display Countdown
        //TextMeshProUGUI countdownText = ARUIController.GetInstance().coachingTextCountdown.GetComponent<TextMeshProUGUI>();
        //ARUIController.GetInstance().coachingTextCountdown.SetActive(true);

        //for (int i = 3; i >= 1; i--)
        //{
        //    countdownText.text = i + "...";
        //    yield return StartCoroutine(ARUIController.GetInstance().FadeInAndOut(ARUIController.GetInstance().coachingTextCountdown));
        //}
        CaptureController.Instance.ActivateRecordingUI(true);
        CanvasGroup watermark = CaptureController.Instance.watermarkContainer.GetComponent<CanvasGroup>();
        watermark.alpha = 0f;

        LeanTween.alphaCanvas(watermark, 1f, CaptureController.ANIMATION_FADE_DURATION).setEaseInOutExpo();

        yield return new WaitForSeconds(CaptureController.ANIMATION_FADE_DURATION + .1f);

        m_recorder.StartRecording(callback: (success, error) =>
        {
            // seems like the callback is never reached on iOS
            if (success)
            {
                Debug.Log("Started Recording");
                CheckRecordingStatus();
            }
            else
            {
                Debug.Log($"Start recording failed with error [{error}]");
            }
        });
#if UNITY_IOS
        CheckRecordingStatus();
#endif
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
             Debug.Log("VideoCaptureController PERMISSION REFUSED, RESETING UI");
             //UI already disable in UI Controller, if failed record put UI back
             CaptureController.Instance.ActivateRecordingUI(false);
             return;
         }

        Debug.Log("VideoCaptureController PERMISSION GRANTED START RECORDING");
    }

    public void StopRecording()
    {
        if (!m_recorder.IsRecording())
        {
            //Recording not started"
            Debug.Log("Recording not started");
            return;
        }

        Debug.Log("Stop the recording");

        //Stop recording
        m_recorder.StopRecording((success, error) =>
        {
            if (success)
            {
                Debug.Log("Stopped recording");
                SaveRecording();
            }
            else
            {
                Debug.Log($"Stop recording failed with error: {error}");
                CaptureController.Instance.ActivateRecordingUI(false);
                NavigationController.GetInstance().ShowHeader(.4f, false, !CaptureController.Instance.isUsingTransparentHeader);
                NavigationController.GetInstance().ShowFooter(.4f);
                ARPlaneController.Instance.isRecording = false;

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
                Debug.Log("Saved recording successfully :" + result.Path);
                StartCoroutine(CaptureController.Instance.FadeInAndOut(CaptureController.Instance.coachingTextVideoSaved));
                CaptureController.Instance.ActivateRecordingUI(false);

                ShowRecordingPreview();

            }
            else
            {
                Debug.Log($"Failed saving recording [{error}]");
            }
        });
    }

    private void ShowRecordingPreview()
    {
        m_recorder.OpenRecording((success, error) =>
        {
            if (success)
            {
                Debug.Log($"Open recording successful");
            }
            else
            {
                Debug.Log($"Open recording failed with error [{error}]");
            }

            ARPlaneController.Instance.isRecording = false;
            CaptureController.Instance.ActivateRecordingUI(false);
            NavigationController.GetInstance().ShowHeader(0.4f, false, !CaptureController.Instance.isUsingTransparentHeader);
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
