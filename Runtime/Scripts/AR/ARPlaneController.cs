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

    private bool canMove = true; //whether the user can move the ar model around or not, set by the feature controller
    private bool canScale = true; //whether the user can rescale the ar model or not, set by the feature controller
    private bool canRotate = true;//whether the user can rotate the ar model or not, set by the feature controller

    [SerializeField] private int maxNbSpawnedARModelsAllowed;

    private void Awake()
    {
        instance = this;

        arRaycastManager = GetComponent<ARRaycastManager>();
    }

    public void SetCanMove(bool value)
    {
        canMove = value;
    }

    public void SetCanScale(bool value)
    {
        canScale = value;
    }

    public void SetCanRotate(bool value)
    {
        canRotate = value;
    }

    // Update is called once per frame
    void Update()
    {
        TryToSpawnARModel();

        MoveARModel();

        ScaleRotateARModel();
    }

    //called by the featurecontroller to set the ar model to be placed depending on the experiment
    //when more than 1 model are available for an experiment, the object picker will be enabled and the first item will be selected by default
    public void SetModel(GameObject model)
    {
        arModelToBePlacedPrefab = model;
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

            float distanceToHit = raycastHit.distance;
            spawnedARModelGameObject.transform.localScale *= distanceToHit * 0.3f; //adapt local scale depending on prefab scale and distance to plane

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
        else if (arRaycastManager.Raycast(sourceRay, arRaycastHits, TrackableType.PlaneWithinInfinity) //second clause won't be evaluated when the first clause is true
        || arRaycastManager.Raycast(sourceRay, arRaycastHits, TrackableType.PlaneEstimated)) //use plane estimated when infinity doesn't work (Android builds)
        {
            foreach (ARRaycastHit currentARRaycastHit in arRaycastHits)
            {
                // highest priority - FORCE PLANE
                if (forcePlane && currentARRaycastHit.trackableId == forcePlaneID)
                {
                    raycastHit = currentARRaycastHit;
                    return true;
                }

                if (currentARRaycastHit.trackableId == currentlyTrackedPlaneID)
                {
                    raycastHit = currentARRaycastHit; // we return the same plane as priority that was last tracked with polygon
                    return true;
                }
            }

            raycastHit = arRaycastHits[0]; // Raycast hits are sorted by distance, so the first one will be the closest hit
            return true;
        }
        //ScenesController.LogMe("ARPlaneController", "No plane");
        raycastHit = default;
        return false;
    }

    private void MoveARModel()
    {
        if (selectedARModel == null || isRecording || !canMove)
            return;

        if (arModelAnimator != null) //the model may not have an animator component (eg. for the bar), so we don't want to trigger anything here
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

    private void ScaleRotateARModel()
    {
        if (selectedARModel == null || selectedARModel.arModel == null)
            return;

        // Scale Rotate AR Model
        if (ARGestureController.GetInstance().PinchDetection(out Vector2 pinch0PointOrigin, out Vector2 pinch1PointOrigin, out float pinchScaleFactorDelta, out float pinchRotationDifferenceDelta))
        {
            GameObject selectedARModelChild = selectedARModel.arModel.transform.gameObject;

            if (canScale) selectedARModelChild.transform.localScale *= pinchScaleFactorDelta;
            if (canRotate) selectedARModel.arModel.transform.Rotate(0, pinchRotationDifferenceDelta, 0);
        }
    }

    private class ARModel
    {
        public GameObject arModel;
        public TrackableId arModelCurrentPlane;
    }

}

