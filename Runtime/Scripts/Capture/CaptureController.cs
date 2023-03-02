using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using Lofelt.NiceVibrations;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class CaptureController : MonoBehaviour
{
    public static readonly float ANIMATION_FADE_DURATION = 0.4F;

    private static CaptureController instance;

    public static CaptureController GetInstance()
    {
        return instance;
    }


    [Header("Containers")]
    public GameObject uiContainer;
    public GameObject watermarkContainer;

    [Header("Coachings")]
    public GameObject coachingTextStopVideo;
    public GameObject coachingTextVideoSaved;
    public GameObject coachingTextPhotoSaved;

    [Header("Buttons")]
    public Button photoSwitchButton;
    public Button videoSwitchButton;
    public Button actionButton;

    [Header("Backgrounds and Images")]
    public GameObject flash;
    public GameObject watermark;

    [Header("Optional")]
    public GameObject captureSlidingToggleMode; // in the case we have a photo/video sliding toggle in iOS native way
    public bool isUsingTransparentHeader;

    private bool writeToGalleryPermissionGranted;


    public enum CaptureModes {
        Photo,
        Video
    };

    private CaptureModes currentCaptureMode = CaptureModes.Photo;

    public enum CaptureState
    {
        Empty,
        Permissions_Granted,
        No_Camera_Permissions,
    }

    private CaptureState _arState = CaptureState.Empty;

    public CaptureState arState
    {
        get { return _arState; } 
        set
        {
            if (_arState == value)
                return;

            _arState = value;

        }
    }
    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {

        InitUI();

        ALLCoreConfig.OnALLCoreReady += OnALLCoreReadyCallback;

        if (arState == CaptureState.Empty)
        {
#if UNITY_IOS

            StartCoroutine(CheckIOSCamera());

#endif
#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                arState = CaptureState.No_Camera_Permissions;
                return;
            }else{
                arState = CaptureState.Permissions_Granted;
            }
#endif
        }

    }

    private void OnDestroy()
    {
        ALLCoreConfig.OnALLCoreReady -= OnALLCoreReadyCallback;
    }

    private void OnALLCoreReadyCallback(object sender, EventArgs e)
    {
        Debug.Log("A-LL Core ready: checking permissions");

        // Check permissions
        if (NativeGallery.CheckPermission(NativeGallery.PermissionType.Write, NativeGallery.MediaType.Image | NativeGallery.MediaType.Video) != NativeGallery.Permission.Granted)
        {
            NativeGallery.RequestPermission(NativeGallery.PermissionType.Write, NativeGallery.MediaType.Image | NativeGallery.MediaType.Video);
        } 
    }

    private void InitUI()
    {
        
        coachingTextStopVideo.SetActive(false);
        coachingTextVideoSaved.SetActive(false);
        coachingTextPhotoSaved.SetActive(false);

        flash.SetActive(false);
        watermark.SetActive(false);

        ActivateRecordingUI(false);
    }

    private IEnumerator CheckIOSCamera()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            arState = CaptureState.No_Camera_Permissions;
        }
        else
        {
            arState = CaptureState.Permissions_Granted;
        }
    }

    public void OnOpenPhotoGallery()
    {

#if UNITY_IOS

        Application.OpenURL("photos-redirect://");

#endif

#if UNITY_ANDROID
        bool fail = false;
        //string bundleId = "com.android.gallery3d"; //target bundle id for gallery!?
        string bundleId = "com.google.android.apps.photos"; 
        AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject ca = up.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject packageManager = ca.Call<AndroidJavaObject>("getPackageManager");

        AndroidJavaObject launchIntent = null;
        try
        {
            launchIntent = packageManager.Call<AndroidJavaObject>("getLaunchIntentForPackage", bundleId);
        }
        catch (System.Exception e)
        {
            fail = true;
        }

        if (fail || launchIntent == null)
        { //open app in store
            Application.OpenURL("https://play.google.com/store/apps/details?id=com.google.android.apps.photos&hl=en&gl=US");
        }
        else //I want to open Gallery App? But what activity?
            ca.Call("startActivity", launchIntent);

        up.Dispose();
        ca.Dispose();
        packageManager.Dispose();
        launchIntent.Dispose();
#endif
    }

    // in the case we only have 2 buttons to switch between photo and video (no sliding effect)
    public void OnSwitchCaptureMode(int captureMode)
    {
        if (captureMode == (int) currentCaptureMode)
            return;
        else
            currentCaptureMode = (CaptureModes)captureMode;
    }

    // in the case we want a sliding effect between the photo and video modes
    public void OnToggleCaptureMode(int captureMode)
    {
        if (captureMode == (int) currentCaptureMode)
            return;
        else
            currentCaptureMode = (CaptureModes) captureMode;

        //Grey out other options
        foreach (Transform mode in captureSlidingToggleMode.transform)
        {
            TextMeshProUGUI text = mode.transform.GetComponent<TextMeshProUGUI>();
            text.color = new Color(1f, 1f, 1f, 0.4f);
            //text.fontStyle ^= FontStyles.Bold; //remove bold
        }

        TextMeshProUGUI currentModeText = captureSlidingToggleMode.transform.GetChild(captureMode).GetComponent<TextMeshProUGUI>();
        currentModeText.color = new Color(1f, 1f, 1f, 1f);
        //currentModeText.fontStyle |= FontStyles.Bold; //add bold

        MoveCurrentCaptureModeToggle(captureSlidingToggleMode.transform.GetChild(captureMode));

        //switch (currentCaptureMode)
        //{
        //    case CaptureModes.Video: VideoCaptureController.GetInstance().PrepareRecording(false);
        //        break;
        //}

        
        
    }

    private void MoveCurrentCaptureModeToggle(Transform selectedMode)
    {
        LeanTween.moveLocalX(captureSlidingToggleMode, -selectedMode.localPosition.x, .2f).setEaseInOutExpo();
    }

    public IEnumerable SaveToGallery()
    {
        if (writeToGalleryPermissionGranted)
        {
            GameObject textToDisplay = (currentCaptureMode == CaptureModes.Photo) ? coachingTextPhotoSaved : coachingTextVideoSaved;
            textToDisplay.SetActive(true);
            yield return new WaitForSeconds(5f);
        }
    }


    public void OnActionButtonPressed()
    {

        switch (currentCaptureMode)
        {
            case CaptureModes.Photo:

                PhotoController.GetInstance().TriggerPhotoCapture();

                break;
            case CaptureModes.Video:

                VideoCaptureController.GetInstance().TriggerVideoCapture();

                break;
        }
    }
            
    public IEnumerator FadeInAndOut(GameObject text)
    {
        text.SetActive(true);
        CanvasGroup textCanvasGroup = text.GetComponent<CanvasGroup>();
        textCanvasGroup.alpha = 0f;

        LeanTween.alphaCanvas(textCanvasGroup, 1.0f, ANIMATION_FADE_DURATION).setEaseInOutExpo();

        yield return new WaitForSeconds(ANIMATION_FADE_DURATION + 2f);

        LeanTween.alphaCanvas(textCanvasGroup, 0f, ANIMATION_FADE_DURATION).setEaseInOutExpo();

        yield return new WaitForSeconds(ANIMATION_FADE_DURATION + .1f);

        text.SetActive(false);
    }

    public void ActivateRecordingUI(bool shallDisplay)
    {
        // is recording
        if (shallDisplay)
        {
            // hide AR UI
            watermarkContainer.SetActive(true);
            uiContainer.SetActive(false);
        }
        else // hide at the end of the recording
        {
            // update UIs
            watermarkContainer.SetActive(false);
            uiContainer.SetActive(true);

        }
    }

}
