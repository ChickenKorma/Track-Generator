using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrivingController : MonoBehaviour
{
    public static DrivingController Instance;

    [SerializeField] private float maxMotorTorque;
    [SerializeField] private float maxSteeringAngle;

    private float throttleInput;
    private float steerInput;

    private Rigidbody rb;

    [SerializeField] private List<Axel> axels;

    [SerializeField] private Transform coM;

    private bool handbrake = true;

    public bool Handbrake { get { return handbrake; } set { handbrake = value; } }

    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }

        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        rb.centerOfMass = coM.localPosition;
    }

    private void OnEnable()
    {
        InputManager.throttleEvent += ThrottleInput;
        InputManager.steerEvent += SteerInput;
        InputManager.handbrakeEvent += HandbrakeToggle;
    }

    private void OnDisable()
    {
        InputManager.throttleEvent -= ThrottleInput;
        InputManager.steerEvent -= SteerInput;
        InputManager.handbrakeEvent -= HandbrakeToggle;
    }

    private void FixedUpdate()
    {
        if (handbrake)
        {
            if(Mathf.Abs(throttleInput) > 0.01f)
            {
                handbrake = false;

                UpdateCar(throttleInput, steerInput, 0);
            }
            else
            {
                UpdateCar(0, 0, 100);
            }
        }
        else
        {
            UpdateCar(throttleInput, steerInput, 0);
        }
    }

    public void UpdateTransform(Vector3 position, Quaternion rotation)
    {
        rb.position = position;
        rb.rotation = rotation;

        rb.velocity = Vector3.zero;
    }

    private void UpdateCar(float throttleInput, float steeringInput, float brakeTorque)
    {
        float motorTorque = maxMotorTorque * throttleInput;

        float steeringAngle = maxSteeringAngle * steeringInput;

        foreach(Axel axel in axels)
        {
            axel.Update(motorTorque, steeringAngle, brakeTorque);
        }
    }

    private void ThrottleInput(float input) => throttleInput = input;

    private void SteerInput(float input) => steerInput = input;

    private void HandbrakeToggle() => handbrake = !handbrake;
}

[System.Serializable]
class Axel
{
    [SerializeField] private Wheel[] wheels;

    [SerializeField] private bool motor;
    [SerializeField] private bool steering;

    public void Update(float motorTorque, float steeringAngle, float brakeTorque)
    {
        foreach(Wheel wheel in wheels)
        {
            if (motor)
            {
                wheel.MotorTorque(motorTorque);
            }

            if (steering)
            {
                wheel.SteerAngle(steeringAngle);
            }

            wheel.BrakeTorque(brakeTorque);

            wheel.UpdateModel();
        }
    }
}

[System.Serializable]
class Wheel
{
    [SerializeField] private WheelCollider wheelCollider;

    [SerializeField] private Transform wheelModel;

    private static readonly float steeringAngleCorrection = -0.00091f;

    public void MotorTorque(float motorTorque)
    {
        wheelCollider.motorTorque = motorTorque;
    }

    public void SteerAngle(float steerAngle)
    {
        wheelCollider.steerAngle = steerAngle + (wheelCollider.rpm * steeringAngleCorrection) ;
    }

    public void BrakeTorque(float brakeTorque)
    {
        wheelCollider.brakeTorque = brakeTorque;
    }

    public void UpdateModel()
    {
        Vector3 position;
        Quaternion rotation;

        wheelCollider.GetWorldPose(out position, out rotation);

        wheelModel.position = position;
        wheelModel.rotation = rotation;
    }
}
