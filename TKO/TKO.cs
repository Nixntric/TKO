using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

/// <summary>
/// TKO - Hand and finger colliders for VRChat
/// 
/// IMPORTANT SETUP INSTRUCTIONS:
/// 1. Create a new physics layer called "HandColliders" (or similar)
/// 2. Set the handCollidersLayer field to that layer number
/// 3. Go to Edit > Project Settings > Physics
/// 4. In the Layer Collision Matrix, UNCHECK the collision between:
///    - "HandColliders" and "Player"
///    - "HandColliders" and "PlayerLocal"
///    - "HandColliders" and "MirrorReflection"
/// 5. This prevents the hand colliders from affecting the player's movement
/// 
/// For desktop users: Finger colliders are automatically disabled by default (vrOnlyFingers = true)
/// </summary>
public class TKO : UdonSharpBehaviour
{
    [Header("Hand Collider Objects")]
    [Tooltip("Drag the right hand collider GameObject here")]
    public GameObject rightHandCollider;
    
    [Tooltip("Drag the left hand collider GameObject here")]
    public GameObject leftHandCollider;
    
    [Header("Right Hand Finger Colliders")]
    public GameObject rightThumbCollider;
    public GameObject rightIndexCollider;
    public GameObject rightMiddleCollider;
    public GameObject rightRingCollider;
    public GameObject rightPinkyCollider;
    
    [Header("Left Hand Finger Colliders")]
    public GameObject leftThumbCollider;
    public GameObject leftIndexCollider;
    public GameObject leftMiddleCollider;
    public GameObject leftRingCollider;
    public GameObject leftPinkyCollider;
    
    [Header("Settings")]
    [Tooltip("How smoothly the colliders follow hands (higher = smoother but more lag)")]
    public float followSpeed = 50f;
    
    [Tooltip("Scale colliders based on avatar size")]
    public bool scaleWithAvatar = true;
    
    [Tooltip("Enable finger colliders only in VR (recommended to prevent desktop issues)")]
    public bool vrOnlyFingers = true;
    
    [Tooltip("Disable hand colliders entirely for desktop users")]
    public bool vrOnlyHands = false;
    
    [Header("Physics Layer (REQUIRED)")]
    [Tooltip("IMPORTANT: Create a layer called 'HandColliders' and set it here. Then in Edit > Project Settings > Physics, disable collision between 'HandColliders' and 'Player'/'PlayerLocal' layers.")]
    public int handCollidersLayer = 22;
    
    private VRCPlayerApi localPlayer;
    private bool isVRUser = false;
    
    private Rigidbody rightHandRigidbody;
    private Rigidbody leftHandRigidbody;
    private Rigidbody rightThumbRb;
    private Rigidbody rightIndexRb;
    private Rigidbody rightMiddleRb;
    private Rigidbody rightRingRb;
    private Rigidbody rightPinkyRb;
    private Rigidbody leftThumbRb;
    private Rigidbody leftIndexRb;
    private Rigidbody leftMiddleRb;
    private Rigidbody leftRingRb;
    private Rigidbody leftPinkyRb;
    

    
    private Vector3 rightHandOriginalScale;
    private Vector3 leftHandOriginalScale;
    private Vector3[] rightFingerOriginalScales = new Vector3[5];
    private Vector3[] leftFingerOriginalScales = new Vector3[5];
    
    private bool initialized = false;
    private float avatarScale = 1f;
    
    void Start()
    {
        localPlayer = Networking.LocalPlayer;
        
        if (!Utilities.IsValid(localPlayer))
        {
            Debug.LogError("[TKO] Local player is not valid!");
            return;
        }
        
        isVRUser = localPlayer.IsUserInVR();
        Debug.Log("[TKO] User is in " + (isVRUser ? "VR" : "Desktop") + " mode");
        
        if (rightHandCollider == null || leftHandCollider == null)
        {
            Debug.LogError("[TKO] Hand collider objects not assigned!");
            return;
        }
        
        if (!isVRUser && vrOnlyHands)
        {
            rightHandCollider.SetActive(false);
            leftHandCollider.SetActive(false);
            Debug.Log("[TKO] Hand colliders disabled for desktop user");
            return;
        }
        
        rightHandRigidbody = rightHandCollider.GetComponent<Rigidbody>();
        leftHandRigidbody = leftHandCollider.GetComponent<Rigidbody>();
        
        if (rightHandRigidbody == null || leftHandRigidbody == null)
        {
            Debug.LogError("[TKO] Hand colliders must have Rigidbody components!");
            return;
        }
        
        if (handCollidersLayer > 0)
        {
            SetLayerRecursively(rightHandCollider, handCollidersLayer);
            SetLayerRecursively(leftHandCollider, handCollidersLayer);
            Debug.Log("[TKO] Set hand colliders to layer " + handCollidersLayer);
        }
        else
        {
            Debug.LogWarning("[TKO] WARNING: handCollidersLayer not set! Hand colliders may collide with player. See Inspector tooltip for setup instructions.");
        }
        
        rightHandOriginalScale = rightHandCollider.transform.localScale;
        leftHandOriginalScale = leftHandCollider.transform.localScale;
        
        bool enableFingers = isVRUser || !vrOnlyFingers;
        
        SetupFingerCollider(rightThumbCollider, ref rightThumbRb, ref rightFingerOriginalScales[0], 
                           enableFingers, "Right Thumb");
        SetupFingerCollider(rightIndexCollider, ref rightIndexRb, ref rightFingerOriginalScales[1], 
                           enableFingers, "Right Index");
        SetupFingerCollider(rightMiddleCollider, ref rightMiddleRb, ref rightFingerOriginalScales[2], 
                           enableFingers, "Right Middle");
        SetupFingerCollider(rightRingCollider, ref rightRingRb, ref rightFingerOriginalScales[3], 
                           enableFingers, "Right Ring");
        SetupFingerCollider(rightPinkyCollider, ref rightPinkyRb, ref rightFingerOriginalScales[4], 
                           enableFingers, "Right Pinky");
        
        SetupFingerCollider(leftThumbCollider, ref leftThumbRb, ref leftFingerOriginalScales[0], 
                           enableFingers, "Left Thumb");
        SetupFingerCollider(leftIndexCollider, ref leftIndexRb, ref leftFingerOriginalScales[1], 
                           enableFingers, "Left Index");
        SetupFingerCollider(leftMiddleCollider, ref leftMiddleRb, ref leftFingerOriginalScales[2], 
                           enableFingers, "Left Middle");
        SetupFingerCollider(leftRingCollider, ref leftRingRb, ref leftFingerOriginalScales[3], 
                           enableFingers, "Left Ring");
        SetupFingerCollider(leftPinkyCollider, ref leftPinkyRb, ref leftFingerOriginalScales[4], 
                           enableFingers, "Left Pinky");
        
        UpdateAvatarScale();
        
        initialized = true;
        Debug.Log("[TKO] Initialized successfully! Avatar scale: " + avatarScale);
    }
    
    private void SetupFingerCollider(GameObject fingerObj, ref Rigidbody rb, ref Vector3 originalScale, 
                                     bool enable, string fingerName)
    {
        if (fingerObj != null)
        {
            rb = fingerObj.GetComponent<Rigidbody>();
            originalScale = fingerObj.transform.localScale;
            
            if (!enable)
            {
                fingerObj.SetActive(false);
                Debug.Log("[TKO] " + fingerName + " disabled for desktop mode");
            }
            else if (handCollidersLayer > 0)
            {
                SetLayerRecursively(fingerObj, handCollidersLayer);
            }
        }
    }
    
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
    
    void FixedUpdate()
    {
        if (!initialized) return;
        
        if (!Utilities.IsValid(localPlayer)) return;
        
        Vector3 rightHandPos = localPlayer.GetBonePosition(HumanBodyBones.RightHand);
        Vector3 leftHandPos = localPlayer.GetBonePosition(HumanBodyBones.LeftHand);
        Quaternion rightHandRot = localPlayer.GetBoneRotation(HumanBodyBones.RightHand);
        Quaternion leftHandRot = localPlayer.GetBoneRotation(HumanBodyBones.LeftHand);
        
        if (rightHandPos != Vector3.zero)
            MoveHandCollider(rightHandRigidbody, rightHandPos, rightHandRot);
        if (leftHandPos != Vector3.zero)
            MoveHandCollider(leftHandRigidbody, leftHandPos, leftHandRot);
        
        if (isVRUser || !vrOnlyFingers)
        {
            UpdateFingerCollider(rightThumbRb, HumanBodyBones.RightThumbDistal);
            UpdateFingerCollider(rightIndexRb, HumanBodyBones.RightIndexDistal);
            UpdateFingerCollider(rightMiddleRb, HumanBodyBones.RightMiddleDistal);
            UpdateFingerCollider(rightRingRb, HumanBodyBones.RightRingDistal);
            UpdateFingerCollider(rightPinkyRb, HumanBodyBones.RightLittleDistal);
            
            UpdateFingerCollider(leftThumbRb, HumanBodyBones.LeftThumbDistal);
            UpdateFingerCollider(leftIndexRb, HumanBodyBones.LeftIndexDistal);
            UpdateFingerCollider(leftMiddleRb, HumanBodyBones.LeftMiddleDistal);
            UpdateFingerCollider(leftRingRb, HumanBodyBones.LeftRingDistal);
            UpdateFingerCollider(leftPinkyRb, HumanBodyBones.LeftLittleDistal);
        }
    }
    
    private void MoveHandCollider(Rigidbody handRb, Vector3 targetPosition, Quaternion targetRotation)
    {
        if (handRb == null) return;
        
        Vector3 newPosition = Vector3.Lerp(handRb.position, targetPosition, followSpeed * Time.fixedDeltaTime);
        handRb.MovePosition(newPosition);
        handRb.MoveRotation(targetRotation);
    }
    
    private void UpdateFingerCollider(Rigidbody fingerRb, HumanBodyBones bone)
    {
        if (fingerRb == null) return;
        
        Vector3 fingerPos = localPlayer.GetBonePosition(bone);
        
        if (fingerPos == Vector3.zero) return;
        
        Quaternion fingerRot = localPlayer.GetBoneRotation(bone);
        
        Vector3 newPosition = Vector3.Lerp(fingerRb.position, fingerPos, followSpeed * Time.fixedDeltaTime);
        fingerRb.MovePosition(newPosition);
        fingerRb.MoveRotation(fingerRot);
    }
    
    public override void OnAvatarChanged(VRCPlayerApi player)
    {
        if (!Utilities.IsValid(player)) return;
        if (player != localPlayer) return;
        
        Debug.Log("[TKO] Avatar changed, recalculating scale");
        UpdateAvatarScale();
    }
    
    private void UpdateAvatarScale()
    {
        if (!Utilities.IsValid(localPlayer)) return;
        
        if (scaleWithAvatar)
        {
            CalculateAvatarScale();
            ApplyScaleToColliders();
        }
    }
    
    private void CalculateAvatarScale()
    {
        float eyeHeight = localPlayer.GetAvatarEyeHeightAsMeters();
        
        avatarScale = eyeHeight / 1.6f;
        
        avatarScale = Mathf.Clamp(avatarScale, 0.1f, 10f);
    }
    
    private void ApplyScaleToColliders()
    {
        if (rightHandCollider != null)
            rightHandCollider.transform.localScale = rightHandOriginalScale * avatarScale;
        if (leftHandCollider != null)
            leftHandCollider.transform.localScale = leftHandOriginalScale * avatarScale;
        
        if (rightThumbCollider != null && rightThumbCollider.activeSelf)
            rightThumbCollider.transform.localScale = rightFingerOriginalScales[0] * avatarScale;
        if (rightIndexCollider != null && rightIndexCollider.activeSelf)
            rightIndexCollider.transform.localScale = rightFingerOriginalScales[1] * avatarScale;
        if (rightMiddleCollider != null && rightMiddleCollider.activeSelf)
            rightMiddleCollider.transform.localScale = rightFingerOriginalScales[2] * avatarScale;
        if (rightRingCollider != null && rightRingCollider.activeSelf)
            rightRingCollider.transform.localScale = rightFingerOriginalScales[3] * avatarScale;
        if (rightPinkyCollider != null && rightPinkyCollider.activeSelf)
            rightPinkyCollider.transform.localScale = rightFingerOriginalScales[4] * avatarScale;
        
        if (leftThumbCollider != null && leftThumbCollider.activeSelf)
            leftThumbCollider.transform.localScale = leftFingerOriginalScales[0] * avatarScale;
        if (leftIndexCollider != null && leftIndexCollider.activeSelf)
            leftIndexCollider.transform.localScale = leftFingerOriginalScales[1] * avatarScale;
        if (leftMiddleCollider != null && leftMiddleCollider.activeSelf)
            leftMiddleCollider.transform.localScale = leftFingerOriginalScales[2] * avatarScale;
        if (leftRingCollider != null && leftRingCollider.activeSelf)
            leftRingCollider.transform.localScale = leftFingerOriginalScales[3] * avatarScale;
        if (leftPinkyCollider != null && leftPinkyCollider.activeSelf)
            leftPinkyCollider.transform.localScale = leftFingerOriginalScales[4] * avatarScale;
    }
}