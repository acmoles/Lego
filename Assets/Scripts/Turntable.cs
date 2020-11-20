using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap;
using Leap.Unity;
using Leap.Unity.Swizzle;
using Leap.Unity.Attributes;
using Leap.Unity.RuntimeGizmos;

//, IRuntimeGizmoComponent
public class Turntable : MonoBehaviour {

  //private PinchGesture pinchL, pinchR;

  [SerializeField]
  private LeapProvider _provider;

  [SerializeField]
  private bool _drawGizmos = true;

    [SerializeField]
    private PinchDetector _pinchDetectorA;
    public PinchDetector PinchDetectorA
    {
        get
        {
            return _pinchDetectorA;
        }
        set
        {
            _pinchDetectorA = value;
        }
    }

    [SerializeField]
    private PinchDetector _pinchDetectorB;
    public PinchDetector PinchDetectorB
    {
        get
        {
            return _pinchDetectorB;
        }
        set
        {
            _pinchDetectorB = value;
        }
    }

    public Transform[] proximityObjects;
    public float maxProximityDistance = 0.05f;
    public bool useProximity = false;

    // [HideInInspector]
    public Transform _transformToRotate;

  [Header("Turntable Shape")]
  [SerializeField]
  [Tooltip("The local height of the upper section of the turntable.")]
  private float _tableHeight;

  [MinValue(0)]
  [SerializeField]
  [Tooltip("The radius of the upper section of the turntable.")]
  private float _tableRadius;

  [MinValue(0)]
  [SerializeField]
  [Tooltip("The length of the edge that connects the upper and lower sections of the turntable.")]
  private float _edgeLength;

  //[Range(0, 90)]
  //[SerializeField]
  //[Tooltip("The angle the edge forms with the upper section of the turntable.")]
  //private float _edgeAngle = 45;

  [Header("Turntable Motion")]
  [MinValue(0)]
  [SerializeField]
  [Tooltip("How much to scale the rotational motion by.  A value of 1 causes no extra scale.")]
  private float _rotationScale = 1.5f;

  [MinValue(0.00001f)]
  [SerializeField]
  [Tooltip("How much to smooth the velocity while the user is touching the turntable.")]
  private float _rotationSmoothing = 0.1f;

  [Range(0, 1)]
  [SerializeField]
  [Tooltip("The damping factor to use to damp the rotational velocity of the turntable.")]
  private float _rotationDamping = 0.95f;

  [MinValue(0)]
  [SerializeField]
  [Tooltip("The speed under which the turntable will stop completely.")]
  private float _minimumSpeed = 0.01f;

  //private Transform circle;

  public Action OnTouch, OnRelease;
  private bool inContact = false;

  private float _outerRadius {
    get {
      return _tableRadius + _edgeLength;
    }
  }

  //Maps a finger from a specific finger to the world tip position when it first entered the turntable
  private Dictionary<FingerPointKey, Vector3> _currTipPoints = new Dictionary<FingerPointKey, Vector3>();
  private Dictionary<FingerPointKey, Vector3> _prevTipPoints = new Dictionary<FingerPointKey, Vector3>();

  private SmoothedFloat _smoothedVelocity;
  private float _rotationalVelocity;
  private Vector3 initOffset;
  public Transform main;

  private bool isTouching = false;

  private void Awake() {
    if (_provider == null) {
      _provider = Hands.Provider;
    }

    _smoothedVelocity = new SmoothedFloat();
    _smoothedVelocity.delay = _rotationSmoothing;
  }

    private bool isHandPinching(Hand hand)
    {
        if (hand.IsLeft)
            return _pinchDetectorA.IsActive;
        else return _pinchDetectorB.IsActive;
    }


void Start()
	{
    //pinchL = HandUIManager.pinchLeft;
    //pinchR = HandUIManager.pinchRight;
    
    //circle = transform.GetChild(0);
    //_tableRadius = circle.localScale.x;
    initOffset = transform.position - main.position;
	}

  private void Update() {
    //if(main.hasChanged){
    // _tableRadius = circle.localScale.x; 
    // transform.position = main.position + initOffset;
    //} 

    Utils.Swap(ref _currTipPoints, ref _prevTipPoints);
    
    isTouching = false;

    _currTipPoints.Clear();
    foreach (var hand in _provider.CurrentFrame.Hands) {
      //if(isHandPinching(hand)) {
            foreach (var finger in hand.Fingers)
            {
                var key = new FingerPointKey()
                {
                    handId = hand.Id,
                    fingerType = finger.Type
                };

                Vector3 worldTip = finger.Bone(Bone.BoneType.TYPE_DISTAL).NextJoint.ToVector3();
                Vector3 localTip = transform.InverseTransformPoint(worldTip);
                
                if (isPointInsideTurntable(localTip) && closestPromimityDistance(worldTip) < maxProximityDistance)
                {
                    isTouching = true;
                    _currTipPoints[key] = worldTip;
                }
            }
        //}

    }

    float deltaAngleSum = 0;
    float deltaAngleWeight = 0;
    foreach (var pair in _currTipPoints) {
      Vector3 currWorldTip = pair.Value;
      Vector3 prevWorldTip;
      if (!_prevTipPoints.TryGetValue(pair.Key, out prevWorldTip)) {
        return;
      }

      Vector3 currLocalTip = transform.InverseTransformPoint(currWorldTip);
      Vector3 prevLocalTip = transform.InverseTransformPoint(prevWorldTip);

      deltaAngleSum += Vector2.SignedAngle(prevLocalTip.xz(), currLocalTip.xz()) * _rotationScale * -1.0f;
      deltaAngleWeight += 1.0f;
    }

    if(Mathf.Abs(deltaAngleSum) > 0 && !inContact){
      inContact = true;
      if(OnTouch != null) OnTouch();
    }

    if(!isTouching && inContact){
      inContact = false;
      if(OnRelease != null) OnRelease();
    }


    if (deltaAngleWeight > 0.0f) {
      float deltaAngle = deltaAngleSum / deltaAngleWeight;

      Vector3 localRotation = transform.localEulerAngles;
      localRotation.y += deltaAngle;
      transform.localEulerAngles = localRotation;

      if(_transformToRotate != null){
        Vector3 localRotationT = _transformToRotate.localEulerAngles;
        localRotationT.y += deltaAngle;
        _transformToRotate.localEulerAngles = localRotationT;
      }

      _smoothedVelocity.Update(deltaAngle / Time.deltaTime, Time.deltaTime);
      _rotationalVelocity = _smoothedVelocity.value;
    } else {
      _rotationalVelocity = _rotationalVelocity * _rotationDamping;
      if (Mathf.Abs(_rotationalVelocity) < _minimumSpeed) {
        _rotationalVelocity = 0;
      }

      Vector3 localRotation = transform.localEulerAngles;
      localRotation.y += _rotationalVelocity * Time.deltaTime;
      transform.localEulerAngles = localRotation;
      
      if(_transformToRotate != null){
        Vector3 localRotationT = _transformToRotate.localEulerAngles;
        localRotationT.y += _rotationalVelocity * Time.deltaTime;
        _transformToRotate.localEulerAngles = localRotationT;

      }
    }
  }

  private bool isPointInsideTurntable(Vector3 localPoint) {
        if (localPoint.y > _tableHeight) {
          return false;
        }

        //float heightFactor = Mathf.Clamp01(Mathf.InverseLerp(_tableHeight, _lowerLevelHeight, localPoint.y));
        //float effectiveRadius = Mathf.Lerp(_tableRadius, _lowerLevelRadius, heightFactor);
        //if (pointRadius > effectiveRadius || pointRadius < effectiveRadius - 0.05f) {
        //  return false;
        //}

        float pointRadius = new Vector2(localPoint.x, localPoint.z).magnitude;


        if (pointRadius > _outerRadius || pointRadius < _tableRadius)
        {
            return false;
        }

        return true;
  }

    private float closestPromimityDistance(Vector3 worldTip)
    {
        if (!useProximity) return 0f;
        float closestDistSqrd = float.PositiveInfinity;

        foreach (Transform proxy in proximityObjects)
        {
            float testDistanceSqrd = (proxy.position - worldTip).sqrMagnitude;
            if (testDistanceSqrd < closestDistSqrd)
            {
                closestDistSqrd = testDistanceSqrd;
            }
        }
        return closestDistSqrd;
    }

    public float visualOffset = 0.02f;
    public void OnDrawGizmos()
    {
        float offsetRadius = _tableRadius - visualOffset;
        float offsetOuterRadius = _outerRadius - visualOffset;

        Utils.DrawCircle(new Vector3(transform.position.x, transform.position.y + _tableHeight, transform.position.z), Vector3.up, offsetRadius, Color.blue);
        Utils.DrawCircle(new Vector3(transform.position.x, transform.position.y + _tableHeight, transform.position.z), Vector3.up, offsetOuterRadius, Color.red);
        for (int i = 0; i < 32; i++)
        {
            float angle = i / 32.0f * Mathf.PI * 2;
            Vector3 tablePoint = new Vector3(Mathf.Cos(angle) * offsetRadius, _tableHeight, Mathf.Sin(angle) * offsetRadius);
            Vector3 lowerPoint = new Vector3(Mathf.Cos(angle) * offsetOuterRadius, _tableHeight, Mathf.Sin(angle) * offsetOuterRadius);
            Debug.DrawLine(transform.position + tablePoint, transform.position + lowerPoint, Color.green);
        }
    }

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer)
    {
        if (!_drawGizmos)
        {
            return;
        }

        float offsetRadius = _tableRadius - visualOffset;
        float offsetOuterRadius = _outerRadius - visualOffset;

        drawer.color = Color.blue;
        drawer.RelativeTo(transform);
        drawer.DrawLineWireCircle(new Vector3(0, _tableHeight, 0), Vector3.up, offsetRadius);
        drawer.DrawLineWireCircle(new Vector3(0, _tableHeight, 0), Vector3.up, offsetOuterRadius);

        for (int i = 0; i < 16; i++)
        {
            float angle = i / 16.0f * Mathf.PI * 2;
            Vector3 tablePoint = new Vector3(Mathf.Cos(angle) * _tableRadius, _tableHeight, Mathf.Sin(angle) * offsetRadius);
            Vector3 lowerPoint = new Vector3(Mathf.Cos(angle) * _outerRadius, _tableHeight, Mathf.Sin(angle) * offsetOuterRadius);
            drawer.DrawLine(tablePoint, lowerPoint);
        }
    }

    private struct FingerPointKey : IEquatable<FingerPointKey> {
    public int handId;
    public Finger.FingerType fingerType;

    public override int GetHashCode() {
      return new Hash() {
        handId,
        (int)fingerType
      };
    }

    public override bool Equals(object obj) {
      if (obj is FingerPointKey) {
        return Equals((FingerPointKey)obj);
      } else {
        return false;
      }
    }

    public bool Equals(FingerPointKey other) {
      return handId == other.handId &&
             fingerType == other.fingerType;
    }
  }
}
