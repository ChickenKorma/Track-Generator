using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrivingController : MonoBehaviour
{
    public static DrivingController Instance;

    [SerializeField] private float maxMotorTorque;
    [SerializeField] private float maxSteeringAngle;

    private Rigidbody rb;

    private PlayerInputActions playerInputActions;

    [SerializeField] private List<Axel> axels;

    [SerializeField] private Transform coM;

    private bool handbrake;

    public bool Handbrake { set { handbrake = value; } }

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
        playerInputActions = new();
        playerInputActions.Enable();
    }

    private void OnDisable()
    {
        playerInputActions.Disable();
    }

    private void FixedUpdate()
    {
        if (handbrake)
        {
            UpdateCar(0, 0, 100);
        }
        else
        {
            UpdateCar(playerInputActions.Game.Throttle.ReadValue<float>(), playerInputActions.Game.Steering.ReadValue<float>(), 0);
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
