using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAiming : MonoBehaviour {

    [Header ("References")]
    public Transform bodyTransform = null;

    [Header ("Sensitivity")]
    public float sensitivityMultiplier = 1f;
    public float horizontalSensitivity = 1f;
    public float verticalSensitivity = 1f;

    [Header ("Restrictions")]
    public float minYRotation = -90f;
    public float maxYRotation = 90f;
    
    // Rotation values
    [HideInInspector] public float bodyRotation = 0f;
    [HideInInspector] public Vector3 cameraRotation = Vector3.zero;

    private float bodyRotationTemp = 0f;
    private Vector3 cameraRotationTemp = Vector3.zero;

    // Leaning
    [HideInInspector] public float leanInput = 0f;

    // Sway
    [HideInInspector] public float sway = 0f;

    void Start () {
        
        // Lock the mouse
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

    }

    void Update () {
        
        Vector3 eulerAngles = transform.localEulerAngles;

        // Remove previous rotation
        eulerAngles = new Vector3 (eulerAngles.x - cameraRotationTemp.x, eulerAngles.y, eulerAngles.z - cameraRotationTemp.z);
        bodyTransform.eulerAngles -= cameraRotationTemp.y * Vector3.up;
        
        // Fix pausing
        if (Time.timeScale == 0f)
            return;

        // Input
        float xMovement = Input.GetAxis ("Mouse X") * horizontalSensitivity * sensitivityMultiplier;
        float yMovement = -Input.GetAxis ("Mouse Y") * verticalSensitivity * sensitivityMultiplier;
        
        // Rotate camera
        cameraRotation = new Vector3 (Mathf.Clamp (cameraRotation.x + yMovement, minYRotation, maxYRotation),
                                      cameraRotation.y + xMovement,
                                      cameraRotation.z + sway);

        cameraRotation.z = Mathf.Lerp (cameraRotation.z, 0f, Time.deltaTime * 3f);

        // Apply rotation
        Vector3 clampedRotation = new Vector3 (Mathf.Clamp (cameraRotation.x, minYRotation, maxYRotation),
                                               cameraRotation.y,
                                               cameraRotation.z);

        eulerAngles = new Vector3 (eulerAngles.x + clampedRotation.x, eulerAngles.y, eulerAngles.z + clampedRotation.z);
        bodyTransform.eulerAngles += Vector3.Scale (clampedRotation, new Vector3 (0f, 1f, 0f));
        cameraRotationTemp = clampedRotation;
        
        // Remove recoil
        transform.localEulerAngles = eulerAngles;

    }

}
