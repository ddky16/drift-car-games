using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CarController : MonoBehaviour
{
    [SerializeField] private WheelColliders wheelColls;
    [SerializeField] private WheelMeshes wheelMeshes;
    [SerializeField] private WheelParticles wheelParticles;

    [Header("Property")]
    [SerializeField] private Rigidbody carRb;

    [SerializeField] private TextMeshProUGUI rpmTmp, gearTmp;
    public AnimationCurve steeringCurve;
    
    public float motorPower, brakePower, maxSpeed;
    private float _gasInput, _steeringInput, _brakeInput, _speed, _slipAngle, _speedClamped;
    public float rpm, redLine, idleRpm;
    public int currentGear;

    [Header("Particle System")]
    [SerializeField] private GameObject smokePrefab;

    private void Start()
    {
        InstantiateSmoke();
    }

    private void FixedUpdate()
    {
        rpmTmp.text = $"RPM: {rpm}";
        gearTmp.text = $"{currentGear+1}";
        _speed = wheelColls.WheelRL.rpm*wheelColls.WheelRL.radius * 2f * Mathf.PI / 10f;
        _speedClamped = Mathf.Lerp(_speedClamped, _speed,Time.fixedDeltaTime);
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
        _gasInput = Input.GetAxis("Vertical");
        _steeringInput = Input.GetAxis("Horizontal");
        _slipAngle = Vector3.Angle(transform.forward, carRb.velocity - transform.forward);

        if (_slipAngle < 120f)
        {
            if (_gasInput < 0)
            {
                _brakeInput = Mathf.Abs(_gasInput);
                _gasInput = 0;
            }
            else
            {
                _brakeInput = 0;
            }
        }
        else
        {
            _brakeInput = 0;
        }
    }

    private void ApplyBrake()
    {
        wheelColls.WheelFL.brakeTorque = _brakeInput * brakePower * 0.7f;
        wheelColls.WheelFR.brakeTorque = _brakeInput * brakePower * 0.7f;

        wheelColls.WheelRL.brakeTorque = _brakeInput * brakePower * 0.3f;
        wheelColls.WheelRR.brakeTorque = _brakeInput * brakePower * 0.3f;
    }

    private void ApplySteering()
    {
        float steeringAngle = _steeringInput * steeringCurve.Evaluate(_speed);
        steeringAngle += Vector3.SignedAngle(transform.forward, carRb.velocity + transform.forward, Vector3.up);
        steeringAngle = Mathf.Clamp(steeringAngle, -90f, 90f);
        wheelColls.WheelFL.steerAngle = steeringAngle;
        wheelColls.WheelFR.steerAngle = steeringAngle;
    }

    private void ApplyMotor()
    {
        if (_speed < maxSpeed)
        {
            wheelColls.WheelRL.motorTorque = motorPower * _gasInput;
            wheelColls.WheelRR.motorTorque = motorPower * _gasInput;
        }
        else
        {
            wheelColls.WheelRL.motorTorque = 0;
            wheelColls.WheelRR.motorTorque = 0;
        }
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

    public float GetSpeedRatio()
    {
        var gas = Mathf.Clamp(_gasInput,0.5f, 1f);
        return _speedClamped * gas / maxSpeed;
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