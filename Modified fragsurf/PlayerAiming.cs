using UnityEngine;

public class PlayerAiming : MonoBehaviour
{
	[Header("References")]
	public Transform bodyTransform;

	[Header("Sensitivity")]
	public float sensitivityMultiplier = 1f;
	public float horizontalSensitivity = 1f;
	public float verticalSensitivity   = 1f;

	[Header("Restrictions")]
	public float minYRotation = -90f;
	public float maxYRotation = 90f;

	//The real rotation of the camera without recoil
	private Vector3 real_rotation;

	[Header("Aimpunch")]
	[Tooltip("bigger number makes the response more damped, smaller is less damped, currently the system will overshoot, with larger damping values it won't")]
	public float punchDamping = 9.0f;

	[Tooltip("bigger number increases the speed at which the view corrects")]
	public float punchSpringConstant = 65.0f;

	[HideInInspector]
	public Vector2 punchAngle;

	[HideInInspector]
	public Vector2 punchAngleVel;

	private void Start()
	{
		// Lock the mouse
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible   = false;
	}

	private void Update()
	{
		// Fix pausing
		if (Mathf.Abs(Time.timeScale) <= 0)
			return;

		DecayPunchAngle();

		// Input
		float x_movement = Input.GetAxisRaw("Mouse X") * horizontalSensitivity * sensitivityMultiplier;
		float y_movement = -Input.GetAxisRaw("Mouse Y") * verticalSensitivity  * sensitivityMultiplier;

		// Calculate real rotation from input
		real_rotation   = new Vector3(Mathf.Clamp(real_rotation.x + y_movement, minYRotation, maxYRotation), real_rotation.y + x_movement, real_rotation.z);
		real_rotation.z = Mathf.Lerp(real_rotation.z, 0f, Time.deltaTime * 3f);

		//Apply real rotation to body
		bodyTransform.eulerAngles = Vector3.Scale(real_rotation, new Vector3(0f, 1f, 0f));

		//Apply rotation and recoil
		Vector3 camera_euler_punch_applied = real_rotation;
		camera_euler_punch_applied.x += punchAngle.x;
		camera_euler_punch_applied.y += punchAngle.y;

		transform.eulerAngles = camera_euler_punch_applied;
	}

	public void ViewPunch(Vector2 punch_amount)
	{
		//Remove previous recoil
		punchAngle = Vector2.zero;

		//Recoil go up
		punchAngleVel -= punch_amount * 20;
	}

	private void DecayPunchAngle()
	{
		if (punchAngle.sqrMagnitude > 0.001 || punchAngleVel.sqrMagnitude > 0.001)
		{
			punchAngle += punchAngleVel * Time.deltaTime;
			float damping = 1 - (punchDamping * Time.deltaTime);

			if (damping < 0)
				damping = 0;

			punchAngleVel *= damping;

			float spring_force_magnitude = punchSpringConstant * Time.deltaTime;
			punchAngleVel -= punchAngle * spring_force_magnitude;
		}
		else
		{
			punchAngle    = Vector2.zero;
			punchAngleVel = Vector2.zero;
		}
	}
}