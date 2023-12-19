using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour
{
    [SerializeField] private WheelColliders wheelColls;
    [SerializeField] private WheelMeshes wheelMeshes;
    [SerializeField] private WheelParticles wheelParticles;

    [Header("Property")]
    [SerializeField] private Rigidbody carRb;
    public float motorPower, brakePower;
    private float gasInput, steeringInput, brakeInput, speed, slipAngle;
    public AnimationCurve steeringCurve;

    [Header("Particle System")]
    [SerializeField] private GameObject smokePrefab;

    private void Start()
    {
        InstantiateSmoke();
    }

    private void Update()
    {
        speed = carRb.velocity.magnitude;
        CheckInput();
        ApplyMotor();
        ApplySteering();
        ApplyBrake();
        CheckParticles();
        UpdateWheel();
    }

    private void InstantiateSmoke()
    {
        wheelParticles.WheelFL = Instantiate(smokePrefab, wheelColls.WheelFL.transform.position - Vector3.up * wheelColls.WheelFL.radius, Quaternion.identity, wheelColls.WheelFL.transform).GetComponent<ParticleSystem>();
        wheelParticles.WheelFR = Instantiate(smokePrefab, wheelColls.WheelFR.transform.position - Vector3.up * wheelColls.WheelFR.radius, Quaternion.identity, wheelColls.WheelFR.transform).GetComponent<ParticleSystem>();
        wheelParticles.WheelRL = Instantiate(smokePrefab, wheelColls.WheelRL.transform.position - Vector3.up * wheelColls.WheelRL.radius, Quaternion.identity, wheelColls.WheelRL.transform).GetComponent<ParticleSystem>();
        wheelParticles.WheelRR = Instantiate(smokePrefab, wheelColls.WheelRR.transform.position - Vector3.up * wheelColls.WheelRR.radius, Quaternion.identity, wheelColls.WheelRR.transform).GetComponent<ParticleSystem>();
    }

    private void CheckParticles()
    {
        WheelHit[] wheelHits = new WheelHit[4];

        wheelColls.WheelFL.GetGroundHit(out wheelHits[0]);
        wheelColls.WheelFR.GetGroundHit(out wheelHits[1]);

        wheelColls.WheelRL.GetGroundHit(out wheelHits[2]);
        wheelColls.WheelRR.GetGroundHit(out wheelHits[3]);

        float slipAllowance = 0.3f;

        if ((Mathf.Abs(wheelHits[0].sidewaysSlip) + Mathf.Abs(wheelHits[0].forwardSlip) > slipAllowance))
        {
            wheelParticles.WheelFL.Play();
        }
        else
        {
            wheelParticles.WheelFL.Stop();
        }
        if ((Mathf.Abs(wheelHits[1].sidewaysSlip) + Mathf.Abs(wheelHits[1].forwardSlip) > slipAllowance))
        {
            wheelParticles.WheelFR.Play();
        }
        else
        {
            wheelParticles.WheelFR.Stop();
        }
        if (Mathf.Abs(wheelHits[2].sidewaysSlip) + Mathf.Abs(wheelHits[2].forwardSlip) > slipAllowance)
        {
            wheelParticles.WheelRL.Play();
        }
        else
        {
            wheelParticles.WheelRL.Stop();
        }
        if ((Mathf.Abs(wheelHits[3].sidewaysSlip) + Mathf.Abs(wheelHits[3].forwardSlip) > slipAllowance))
        {
            wheelParticles.WheelRR.Play();
        }
        else
        {
            wheelParticles.WheelRR.Stop();
        }
    }

    private void CheckInput()
    {
        gasInput = Input.GetAxis("Vertical");
        steeringInput = Input.GetAxis("Horizontal");
        slipAngle = Vector3.Angle(transform.forward, carRb.velocity - transform.forward);

        if (slipAngle < 120f)
        {
            if (gasInput < 0)
            {
                brakeInput = Mathf.Abs(gasInput);
                gasInput = 0;
            }
            else
            {
                brakeInput = 0;
            }
        }
        else
        {
            brakeInput = 0;
        }
    }

    private void ApplyBrake()
    {
        wheelColls.WheelFL.brakeTorque = brakeInput * brakePower * 0.7f;
        wheelColls.WheelFR.brakeTorque = brakeInput * brakePower * 0.7f;

        wheelColls.WheelRL.brakeTorque = brakeInput * brakePower * 0.3f;
        wheelColls.WheelRR.brakeTorque = brakeInput * brakePower * 0.3f;
    }

    private void ApplySteering()
    {
        float steeringAngle = steeringInput * steeringCurve.Evaluate(speed);
        steeringAngle += Vector3.SignedAngle(transform.forward, carRb.velocity + transform.forward, Vector3.up);
        steeringAngle = Mathf.Clamp(steeringAngle, -90f, 90f);
        wheelColls.WheelFL.steerAngle = steeringAngle;
        wheelColls.WheelFR.steerAngle = steeringAngle;
    }

    private void ApplyMotor()
    {
        wheelColls.WheelRL.motorTorque = motorPower * gasInput;
        wheelColls.WheelRR.motorTorque = motorPower * gasInput;
    }

    private void UpdateWheel()
    {
        SetWheel(wheelColls.WheelFL, wheelMeshes.WheelFL);
        SetWheel(wheelColls.WheelFR, wheelMeshes.WheelFR);
        SetWheel(wheelColls.WheelRL, wheelMeshes.WheelRL);
        SetWheel(wheelColls.WheelRR, wheelMeshes.WheelRR);
    }

    private void SetWheel(WheelCollider wheelColl, MeshRenderer wheelMesh)
    {
        Quaternion quat;
        Vector3 pos;

        wheelColl.GetWorldPose(out pos, out quat);
        wheelMesh.transform.position = pos;
        wheelMesh.transform.rotation = quat;
    }
}

[Serializable]
public class WheelColliders
{
    public WheelCollider WheelFL;
    public WheelCollider WheelFR;
    public WheelCollider WheelRL;
    public WheelCollider WheelRR;
}

[Serializable]
public class WheelMeshes
{
    public MeshRenderer WheelFL;
    public MeshRenderer WheelFR;
    public MeshRenderer WheelRL;
    public MeshRenderer WheelRR;
}

[Serializable]
public class WheelParticles
{
    public ParticleSystem WheelFL;
    public ParticleSystem WheelFR;
    public ParticleSystem WheelRL;
    public ParticleSystem WheelRR;
}