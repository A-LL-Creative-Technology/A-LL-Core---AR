using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ARGestureController : MonoBehaviour
{
    private static ARGestureController instance;

    public static ARGestureController GetInstance()
    {
        return instance;
    }

    // pinch variables
    private Vector2 pinchInitialVector;
    private bool pinchInitialized = false;

    // swipe variables
    //private Vector2 swipeInitialPosition;
    //private bool swipeInProgress = false;

    public void Awake()
    {
        instance = this;
    }

    public bool LongPressDetection(){
        if(Input.touchCount == 1){
            Touch touch = Input.GetTouch(0);

            if(ShouldDiscardTouchOnUI(touch.position)){
                return false;
            }

            return touch.phase != TouchPhase.Began && touch.phase != TouchPhase.Ended;
        }
        return false;
    }

    public bool OneFingerTapDetection(out Vector2 tapPosition)
    {
        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Touch touch = Input.GetTouch(0);

            if (ShouldDiscardTouchOnUI(touch.position))
            {
                tapPosition = default;
                return false;
            }

            tapPosition = touch.position;

            return true;
        }

        tapPosition = default;
        return false;
    }

    public bool ManyFingersTapDetection(int nbTouches)
    {
        if (Input.touchCount == nbTouches && Input.GetMouseButtonDown(0))
        {
            return true;
        }

        return false;
    }

    public bool PinchDetection(out Vector2 pinch0PointOrigin, out Vector2 pinch1PointOrigin, out float scaleFactorDelta, out float rotationDifferenceDelta)
    {
        if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            pinch0PointOrigin = touch0.position;
            pinch1PointOrigin = touch1.position;

            if (ShouldDiscardTouchOnUI(touch0.position) || ShouldDiscardTouchOnUI(touch1.position))
            {
                scaleFactorDelta = -1;
                rotationDifferenceDelta = 0;
                return false;
            }

            Vector2 swipeVector = (touch1.position - touch0.position);

            if (!pinchInitialized) // we just entered pinching
            {
                pinchInitialVector = swipeVector;
            }
            pinchInitialized = true;

            float currentPinchDistance = swipeVector.magnitude;

            scaleFactorDelta = currentPinchDistance / pinchInitialVector.magnitude;
            rotationDifferenceDelta = Vector2.Angle(pinchInitialVector, swipeVector);

            // cross product to determine the rotation direction
            if (Vector3.Cross(pinchInitialVector, swipeVector).z > 0) // rotate anticlockwise
            {
                rotationDifferenceDelta *= -1;
            }

            pinchInitialVector = swipeVector;

            return true;
        }

        pinchInitialized = false;

        pinch0PointOrigin = default;
        pinch1PointOrigin = default;
        scaleFactorDelta = -1;
        rotationDifferenceDelta = 0;
        return false;
    }

    public bool SwipeDetection(out Vector2 swipePointOrigin, out Vector2 swipePointEnd)
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (ShouldDiscardTouchOnUI(touch.position))
            {
                swipePointOrigin = default;
                swipePointEnd = default;
                return false;
            }

            if (touch.phase == TouchPhase.Moved)
            {
                swipePointEnd = touch.position;
                swipePointOrigin = swipePointEnd - touch.deltaPosition;

                return true;
            }
        }

        //swipeInProgress = false;
        swipePointOrigin = default;
        swipePointEnd = default;
        return false;
    }

    public List<Touch> GetAllTouches()
    {
        List<Touch> validTouches = new List<Touch>();

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch currentTouch = Input.GetTouch(i);

            if (ShouldDiscardTouchOnUI(currentTouch.position))
                continue;

            validTouches.Add(currentTouch);
        }

        return validTouches;
    }

    private bool ShouldDiscardTouchOnUI(Vector2 touchPos)
    {
        PointerEventData touch = new PointerEventData(EventSystem.current);
        touch.position = touchPos;
        List<RaycastResult> hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(touch, hits);
        return (hits.Count > 0); // discard swipe if an UI element is beneath
    }
}
