using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#if UNITY_IOS
using UnityEngine.Apple.ReplayKit;
#endif

public class ARPlaneController : MonoBehaviour
{
    private static ARPlaneController instance;

    public static ARPlaneController GetInstance()
    {
        return instance;
    }

    public static event EventHandler OnARModelSpawned;
    public bool isRecording = false;

    private ARRaycastManager arRaycastManager;
    public ARPlaneManager arPlaneManager = null;

    [SerializeField] private GameObject arModelToBePlacedPrefab = null;

    private readonly float AR_MODEL_MOVE_SPEED = 1f;
    static List<ARRaycastHit> arRaycastHits = new List<ARRaycastHit>();
    private List<ARModel> spawnedPlanarARModels = new List<ARModel>();

    TrackableId currentlyTrackedPlaneID;

    private ARModel selectedARModel;
    private Animator arModelAnimator; //so that we don't call GetComponent every frame when moving the character around

    [SerializeField] private int maxNbSpawnedARModelsAllowed;

    private void Awake()
    {
        instance = this;

        arRaycastManager = GetComponent<ARRaycastManager>();

    }

    // Update is called once per frame
    void Update()
    {
        TryToSpawnARModel();

        MoveARModel();

    }

    public void DestroySpawnedARModels()
    {
        for (int i = 0; i < spawnedPlanarARModels.Count; i++)
        {
            ARModel currentARModel = spawnedPlanarARModels[i];

            Destroy(currentARModel.arModel);

            spawnedPlanarARModels.RemoveAt(i);
            i--;
        }
    }

    public void DestroyLastSpawnedARModel()
    {
        int lastItemIndex = spawnedPlanarARModels.Count - 1;

        ARModel lastItem = spawnedPlanarARModels[lastItemIndex];

        Destroy(lastItem.arModel);

        spawnedPlanarARModels.RemoveAt(lastItemIndex);

        selectedARModel = null;
        arModelAnimator = null;
    }

    public int GetNbSpawnedARmodels()
    {
        return spawnedPlanarARModels.Count;
    }

    private bool TryToSpawnARModel()
    {
        if (GetNbSpawnedARmodels() == maxNbSpawnedARModelsAllowed)
            return false;

        if (!ARGestureController.GetInstance().OneFingerTapDetection(out Vector2 tapPosition))
            return false;

        Ray planeRay = ARController.GetInstance().arCamera.ScreenPointToRay(tapPosition);
        if (TryToHitWithClosestPlane(planeRay, out ARRaycastHit raycastHit))
        {

            Vector3 modelHitPosition = raycastHit.pose.position;
            Quaternion modelHitRotation = raycastHit.pose.rotation;

            GameObject spawnedARModelGameObject = Instantiate(arModelToBePlacedPrefab, modelHitPosition, modelHitRotation); // we first place it far away so we don't see it before its position is ajusted according to the animation

            // make sure the new AR Model points towards the AR Camera
            Vector3 arCameraToARModel = (spawnedARModelGameObject.transform.position - ARController.GetInstance().arCamera.transform.position).normalized;
            arCameraToARModel.y = 0; // we don't want to take into account the height
            Vector3 newDirection = -Vector3.RotateTowards(spawnedARModelGameObject.transform.forward, arCameraToARModel, 2f * (float)Math.PI, 0.0f);
            spawnedARModelGameObject.transform.rotation = Quaternion.LookRotation(newDirection);

            //spawnedARModelGameObject.transform.localScale = 0.1f * Vector3.one;

            // store in the list of spawned models
            ARModel spawnedARModel = new ARModel();
            spawnedARModel.arModel = spawnedARModelGameObject;
            spawnedARModel.arModelCurrentPlane = raycastHit.trackableId;

            selectedARModel = spawnedARModel;//preselect the newly spawned model
            arModelAnimator = spawnedARModelGameObject.GetComponent<Animator>();

            spawnedPlanarARModels.Add(spawnedARModel);

            Handheld.Vibrate();

            GlobalController.LogMe("New AR Model spawned: " + spawnedARModelGameObject.name);

            // fires an event
            if (OnARModelSpawned != null)
            {
                OnARModelSpawned(this, EventArgs.Empty);

                return true;
            }
        }

        return false;
    }

    private bool TryToHitWithClosestPlane(Ray sourceRay, out ARRaycastHit raycastHit, bool forcePlane = false, TrackableId forcePlaneID = default)
    {

        // we first give priority to planes within polygon (if forcePlane, we enforce the PlaneWithinInfinityForSpeed)
        if (!forcePlane && arRaycastManager.Raycast(sourceRay, arRaycastHits, TrackableType.PlaneWithinPolygon))
        {
            raycastHit = arRaycastHits[0]; // Raycast hits are sorted by distance, so the first one will be the closest hit
            currentlyTrackedPlaneID = arRaycastHits[0].trackableId;

            return true;

        }
        else if (arRaycastManager.Raycast(sourceRay, arRaycastHits, TrackableType.PlaneWithinInfinity))
        {

            foreach (ARRaycastHit currentARRaycastHit in arRaycastHits)
            {
                // highest priority - FORCE PLANE
                if (forcePlane && currentARRaycastHit.trackableId == forcePlaneID)
                {
                    raycastHit = currentARRaycastHit;

                    //ScenesController.LogMe("ARPlaneController", "Force Plane");
                    return true;
                }

                if (currentARRaycastHit.trackableId == currentlyTrackedPlaneID)
                {
                    raycastHit = currentARRaycastHit; // we return the same plane as priority that was last tracked with polygon
                    //ScenesController.LogMe("ARPlaneController", "Same plane priority");
                    return true;
                }
            }

            raycastHit = arRaycastHits[0]; // Raycast hits are sorted by distance, so the first one will be the closest hit
            //ScenesController.LogMe("ARPlaneController", "Default first plane");
            return true;

        }
        //ScenesController.LogMe("ARPlaneController", "No plane");
        raycastHit = default;
        return false;
    }

    private void MoveARModel()
    {

        // DEACTIVATE FOR ANDROID
#if UNITY_ANDROID
        return;
#endif

        if (selectedARModel == null || arModelAnimator == null)
            return;

        if (isRecording)
        {
            return;
        }


        arModelAnimator.SetBool("isFloating", ARGestureController.GetInstance().LongPressDetection());

        // Move AR Model
        if (ARGestureController.GetInstance().SwipeDetection(out Vector2 swipePointOrigin, out Vector2 swipePointEnd))
        {

            // infinite plane intersection
            Ray touchRayOrigin = ARController.GetInstance().arCamera.ScreenPointToRay(swipePointOrigin);
            Ray touchRayEnd = ARController.GetInstance().arCamera.ScreenPointToRay(swipePointEnd);

            if (TryToHitWithClosestPlane(touchRayOrigin, out ARRaycastHit raycastHitOrigin, true, selectedARModel.arModelCurrentPlane) && TryToHitWithClosestPlane(touchRayEnd, out ARRaycastHit raycastHitEnd))
            {
                Vector3 projectedVector = raycastHitEnd.pose.position - raycastHitOrigin.pose.position;

                TrackableId destinationPlane = raycastHitEnd.trackableId;

                // normalize vector depending on the distance to avoid very big value when at the end of the plane
                float meanDistance = (raycastHitOrigin.distance + raycastHitEnd.distance) * 0.5f;
                float moveThreshold = AR_MODEL_MOVE_SPEED / meanDistance;
                if (projectedVector.magnitude > moveThreshold && selectedARModel.arModelCurrentPlane == destinationPlane)
                {
                    projectedVector.Normalize();
                    projectedVector *= moveThreshold;
                }

                selectedARModel.arModel.transform.position += projectedVector;

                selectedARModel.arModelCurrentPlane = destinationPlane;

            }
        }
    }

    private class ARModel
    {
        public GameObject arModel;
        public TrackableId arModelCurrentPlane;
    }

}

