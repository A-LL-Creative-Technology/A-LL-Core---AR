using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Lofelt.NiceVibrations;
using System;

public class PhotoController : MonoBehaviour
{
    private static PhotoController instance;

    public static PhotoController GetInstance()
    {
        return instance;
    }

    public static event EventHandler OnPhotoCaptured;
    public static event EventHandler OnCapturePhoto;

    private void Awake()
    {
        instance = this;
    }

    public void TriggerPhotoCapture()
    {
        if (NativeGallery.CheckPermission(NativeGallery.PermissionType.Write, NativeGallery.MediaType.Image | NativeGallery.MediaType.Video) != NativeGallery.Permission.Granted)
        {
            NavigationController.GetInstance().OnNotificationOpen(false, -1f, "Permission error subtitle", "Permission error (allow save to camera roll)", "Crea Tech");
            return;
        }

        StartCoroutine(TakeScreenshotAndSave());
    }


    public IEnumerator TakeScreenshotAndSave()
    {
        if(OnCapturePhoto != null)
        {
            OnCapturePhoto(this, null);
        }

        // deactivate touch gesture in ARPlaneController
        ARPlaneController.GetInstance().isRecording = true;

        HapticPatterns.PlayPreset(HapticPatterns.PresetType.LightImpact);

        NavigationController.GetInstance().HideHeader(0.4f);
        NavigationController.GetInstance().HideFooter(0.4f);
        CaptureController.GetInstance().flash.SetActive(true);
        yield return new WaitForSeconds(.1f);

        CaptureController.GetInstance().ActivateRecordingUI(true);
        CanvasGroup watermark = CaptureController.GetInstance().watermarkContainer.GetComponent<CanvasGroup>();
        watermark.alpha = 1f;

        CaptureController.GetInstance().flash.SetActive(false);

        yield return new WaitForEndOfFrame();

        Texture2D texture2D = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        texture2D.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        texture2D.Apply();

        yield return new WaitForSeconds(.5f);

        // Save the screenshot to Gallery/Photos
        //StartCoroutine(SaveToGallery(ss));

        NavigationController.GetInstance().OnGlobalSceneLoaderOpen();

        yield return new WaitForSeconds(.1f);

        NativeGallery.SaveImageToGallery(texture2D, "AR Captures", "AR Picture.png");

        yield return new WaitForSeconds(.1f);

        NavigationController.GetInstance().OnGlobalSceneLoaderClose();

        yield return new WaitForSeconds(.1f);

        yield return StartCoroutine(CaptureController.GetInstance().FadeInAndOut(CaptureController.GetInstance().coachingTextPhotoSaved));

        //Fade Image
        LeanTween.alphaCanvas(watermark, 0f, CaptureController.ANIMATION_FADE_DURATION).setEaseInOutExpo().setOnComplete(() => {
            CaptureController.GetInstance().ActivateRecordingUI(false);
        });

        yield return new WaitForSeconds(CaptureController.ANIMATION_FADE_DURATION + .1f);


        NavigationController.GetInstance().ShowHeader(.4f, false, !CaptureController.GetInstance().isUsingTransparentHeader);
        NavigationController.GetInstance().ShowFooter(.4f);

        // To avoid memory leaks
        Destroy(texture2D);
        
        ARPlaneController.GetInstance().isRecording = false;

        if (OnPhotoCaptured != null)
        {
            OnPhotoCaptured(this, null);
        }

    }
}
