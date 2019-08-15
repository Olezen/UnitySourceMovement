using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace Fragsurf.Movement {

    /// <summary>
    /// Easily add a surfable character to the scene
    /// </summary>
    [AddComponentMenu ("Fragsurf/Surf Character")]
    public class SurfCharacter : MonoBehaviour, ISurfControllable {

        public enum ColliderType {
            Capsule,
            Box
        }

        ///// Fields /////

        [Header("Physics Settings")]
        public Vector3 colliderSize = new Vector3 (1f, 2f, 1f);
        public ColliderType collisionType;
        public float weight = 75f;
        public float rigidbodyPushForce = 2f;
        public bool solidCollider = false;

        [Header("View Settings")]
        public Transform viewTransform;
        public Transform playerRotationTransform;

        [Header ("Crouching setup")]
        public float crouchingHeightMultiplier = 0.5f;
        public float crouchingSpeed = 10f;
        float defaultHeight;
        bool allowCrouch = true; // This is separate because you shouldn't be able to toggle crouching on and off during gameplay for various reasons

        [Header ("Features")]
        public bool crouchingEnabled = true;
        public bool slidingEnabled = false;

        [Header ("Step offset (can be buggy, enable at your own risk)")]
        public bool useStepOffset = false;
        public float stepOffset = 0.35f;

        [Header ("Movement Config")]
        [SerializeField]
        public MovementConfig movementConfig;
        
        private GameObject _groundObject;
        private Vector3 _baseVelocity;
        private Collider _collider;
        private Vector3 _angles;
        private Vector3 _startPosition;
        private GameObject _colliderObject;
        private GameObject _cameraWaterCheckObject;
        private CameraWaterCheck _cameraWaterCheck;

        private MoveData _moveData = new MoveData ();
        private SurfController _controller = new SurfController ();

        private Rigidbody rb;

        private List<Collider> triggers = new List<Collider> ();
        private int numberOfTriggers = 0;

        private bool underwater = false;

        ///// Properties /////

        public MoveType moveType { get { return MoveType.Walk; } }
        public MovementConfig moveConfig { get { return movementConfig; } }
        public MoveData moveData { get { return _moveData; } }
        public new Collider collider { get { return _collider; } }

        public GameObject groundObject {

            get { return _groundObject; }
            set { _groundObject = value; }

        }

        public Vector3 baseVelocity { get { return _baseVelocity; } }

        public Vector3 forward { get { return viewTransform.forward; } }
        public Vector3 right { get { return viewTransform.right; } }
        public Vector3 up { get { return viewTransform.up; } }

        Vector3 prevPosition;

        ///// Methods /////

        private void Awake () {
            
            _controller.playerTransform = playerRotationTransform;
            
            if (viewTransform != null) {

                _controller.camera = viewTransform;
                _controller.cameraYPos = viewTransform.localPosition.y;

            }

        }

        private void Start () {
            
            _colliderObject = new GameObject ("PlayerCollider");
            _colliderObject.layer = gameObject.layer;
            _colliderObject.transform.SetParent (transform);
            _colliderObject.transform.rotation = Quaternion.identity;
            _colliderObject.transform.localPosition = Vector3.zero;
            _colliderObject.transform.SetSiblingIndex (0);

            // Water check
            _cameraWaterCheckObject = new GameObject ("Camera water check");
            _cameraWaterCheckObject.layer = gameObject.layer;
            _cameraWaterCheckObject.transform.position = viewTransform.position;

            SphereCollider _cameraWaterCheckSphere = _cameraWaterCheckObject.AddComponent<SphereCollider> ();
            _cameraWaterCheckSphere.radius = 0.1f;
            _cameraWaterCheckSphere.isTrigger = true;

            Rigidbody _cameraWaterCheckRb = _cameraWaterCheckObject.AddComponent<Rigidbody> ();
            _cameraWaterCheckRb.useGravity = false;
            _cameraWaterCheckRb.isKinematic = true;

            _cameraWaterCheck = _cameraWaterCheckObject.AddComponent<CameraWaterCheck> ();

            prevPosition = transform.position;

            if (viewTransform == null)
                viewTransform = Camera.main.transform;

            if (playerRotationTransform == null && transform.childCount > 0)
                playerRotationTransform = transform.GetChild (0);

            _collider = gameObject.GetComponent<Collider> ();

            if (_collider != null)
                GameObject.Destroy (_collider);

            // rigidbody is required to collide with triggers
            rb = gameObject.GetComponent<Rigidbody> ();
            if (rb == null)
                rb = gameObject.AddComponent<Rigidbody> ();

            allowCrouch = crouchingEnabled;

            rb.isKinematic = true;
            rb.useGravity = false;
            rb.angularDrag = 0f;
            rb.drag = 0f;
            rb.mass = weight;


            switch (collisionType) {

                // Box collider
                case ColliderType.Box:

                _collider = _colliderObject.AddComponent<BoxCollider> ();

                var boxc = (BoxCollider)_collider;
                boxc.size = colliderSize;

                defaultHeight = boxc.size.y;

                break;

                // Capsule collider
                case ColliderType.Capsule:

                _collider = _colliderObject.AddComponent<CapsuleCollider> ();

                var capc = (CapsuleCollider)_collider;
                capc.height = colliderSize.y;
                capc.radius = colliderSize.x / 2f;

                defaultHeight = capc.height;

                break;

            }

            _moveData.slopeLimit = movementConfig.slopeLimit;

            _moveData.rigidbodyPushForce = rigidbodyPushForce;

            _moveData.slidingEnabled = slidingEnabled;

            _moveData.playerTransform = transform;
            _moveData.viewTransform = viewTransform;
            _moveData.viewTransformDefaultLocalPos = viewTransform.localPosition;

            _moveData.defaultHeight = defaultHeight;
            _moveData.crouchingHeight = crouchingHeightMultiplier;
            _moveData.crouchingSpeed = crouchingSpeed;
            
            _collider.isTrigger = !solidCollider;
            _moveData.origin = transform.position;
            _startPosition = transform.position;

            _moveData.useStepOffset = useStepOffset;
            _moveData.stepOffset = stepOffset;

        }

        private void Update () {

            _colliderObject.transform.rotation = Quaternion.identity;


            //UpdateTestBinds ();
            UpdateMoveData ();
            
            // Previous movement code
            Vector3 positionalMovement = transform.position - prevPosition;
            transform.position = prevPosition;
            moveData.origin += positionalMovement;

            // Triggers
            if (numberOfTriggers != triggers.Count) {
                numberOfTriggers = triggers.Count;

                underwater = false;
                triggers.RemoveAll (item => item == null);
                foreach (Collider trigger in triggers) {

                    if (trigger == null)
                        continue;

                    if (trigger.GetComponentInParent<Water> ())
                        underwater = true;

                }

            }

            _moveData.cameraUnderwater = _cameraWaterCheck.IsUnderwater ();
            _cameraWaterCheckObject.transform.position = viewTransform.position;
            moveData.underwater = underwater;
            
            if (allowCrouch)
                _controller.Crouch (this, movementConfig, Time.deltaTime);

            _controller.ProcessMovement (this, movementConfig, Time.deltaTime);

            transform.position = moveData.origin;
            prevPosition = transform.position;

            _colliderObject.transform.rotation = Quaternion.identity;

        }
        
        private void UpdateTestBinds () {

            if (Input.GetKeyDown (KeyCode.Backspace))
                ResetPosition ();

        }

        private void ResetPosition () {
            
            moveData.velocity = Vector3.zero;
            moveData.origin = _startPosition;

        }

        private void UpdateMoveData () {
            
            _moveData.verticalAxis = Input.GetAxisRaw ("Vertical");
            _moveData.horizontalAxis = Input.GetAxisRaw ("Horizontal");

            _moveData.sprinting = Input.GetButton ("Sprint");
            
            if (Input.GetButtonDown ("Crouch"))
                _moveData.crouching = true;

            if (!Input.GetButton ("Crouch"))
                _moveData.crouching = false;
            
            bool moveLeft = _moveData.horizontalAxis < 0f;
            bool moveRight = _moveData.horizontalAxis > 0f;
            bool moveFwd = _moveData.verticalAxis > 0f;
            bool moveBack = _moveData.verticalAxis < 0f;
            bool jump = Input.GetButton ("Jump");

            if (!moveLeft && !moveRight)
                _moveData.sideMove = 0f;
            else if (moveLeft)
                _moveData.sideMove = -moveConfig.acceleration;
            else if (moveRight)
                _moveData.sideMove = moveConfig.acceleration;

            if (!moveFwd && !moveBack)
                _moveData.forwardMove = 0f;
            else if (moveFwd)
                _moveData.forwardMove = moveConfig.acceleration;
            else if (moveBack)
                _moveData.forwardMove = -moveConfig.acceleration;
            
            if (Input.GetButtonDown ("Jump"))
                _moveData.wishJump = true;

            if (!Input.GetButton ("Jump"))
                _moveData.wishJump = false;
            
            _moveData.viewAngles = _angles;

        }

        private void DisableInput () {

            _moveData.verticalAxis = 0f;
            _moveData.horizontalAxis = 0f;
            _moveData.sideMove = 0f;
            _moveData.forwardMove = 0f;
            _moveData.wishJump = false;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static float ClampAngle (float angle, float from, float to) {

            if (angle < 0f)
                angle = 360 + angle;

            if (angle > 180f)
                return Mathf.Max (angle, 360 + from);

            return Mathf.Min (angle, to);

        }

        private void OnTriggerEnter (Collider other) {
            
            if (!triggers.Contains (other))
                triggers.Add (other);

        }

        private void OnTriggerExit (Collider other) {
            
            if (triggers.Contains (other))
                triggers.Remove (other);

        }

        private void OnCollisionStay (Collision collision) {

            if (collision.rigidbody == null)
                return;

            Vector3 relativeVelocity = collision.relativeVelocity * collision.rigidbody.mass / 50f;
            Vector3 impactVelocity = new Vector3 (relativeVelocity.x * 0.0025f, relativeVelocity.y * 0.00025f, relativeVelocity.z * 0.0025f);

            float maxYVel = Mathf.Max (moveData.velocity.y, 10f);
            Vector3 newVelocity = new Vector3 (moveData.velocity.x + impactVelocity.x, Mathf.Clamp (moveData.velocity.y + Mathf.Clamp (impactVelocity.y, -0.5f, 0.5f), -maxYVel, maxYVel), moveData.velocity.z + impactVelocity.z);

            newVelocity = Vector3.ClampMagnitude (newVelocity, Mathf.Max (moveData.velocity.magnitude, 30f));
            moveData.velocity = newVelocity;

        }

    }

}

