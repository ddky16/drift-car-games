using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace DriftCar
{
    public class CarController : MonoBehaviour
    {
        [SerializeField] private WheelColliders wheelColliders;
        [SerializeField] private WheelMeshes wheelMeshes;
        [SerializeField] private WheelParticles wheelParticles;
        [SerializeField] private WheelTrails wheelTrails;

        [Header("Property")] [SerializeField] private Rigidbody carRb;

        [SerializeField] private TextMeshProUGUI rpmTmp, gearTmp;
        public AnimationCurve steeringCurve;

        public float motorPower, brakePower, maxSpeed;
        private float _gasInput, _steeringInput, _brakeInput, _speed, _slipAngle, _speedClamped;
        public float rpm, redLine, idleRpm;
        public int currentGear;

        public float[] gearRatios;
        public float differentialRatio;
        private float _currentTorque,_clutch,_wheelRpm;
        public AnimationCurve horsePowerToRpmCurve;
        
        private GearState _gearState;
        public float increaseGearRpm, decreaseGearRpm;
        public float changeGearTime = 0.5f;

        [FormerlySerializedAs("smokeWheel")]
        [FormerlySerializedAs("smokePrefab")]
        [Header("Particle System")] 
        [SerializeField] private GameObject smokeTrail;
        [SerializeField] private GameObject skidmarkTrail;

        private void Start()
        {
            SetUpEffect();
        }

        private void FixedUpdate()
        {
            var gear = (_gearState == GearState.Neutral) ? "N" : (currentGear + 1).ToString();
            rpmTmp.text = $"RPM: {rpm:0}";
            gearTmp.text = $"GEAR: {gear}";
            _speed = wheelColliders.rearLeft.rpm * wheelColliders.rearLeft.radius * 2f * Mathf.PI / 10f;
            _speedClamped = Mathf.Lerp(_speedClamped, _speed, Time.fixedDeltaTime);
            CheckInput();
            ApplyMotor();
            ApplySteering();
            ApplyBrake();
            CheckParticles();
            UpdateWheel();
        }

        private void SetUpEffect()
        {
            var frontLeftTransform = wheelColliders.frontLeft.transform;
            var frontRightTransform = wheelColliders.frontRight.transform;
            var rearLeftTransform = wheelColliders.rearLeft.transform;
            var rearRightTransform = wheelColliders.rearRight.transform;
                
            if (smokeTrail)
            {
                wheelParticles.frontLeft =
                    Instantiate(smokeTrail,
                        frontLeftTransform.position - Vector3.up * wheelColliders.frontLeft.radius,
                        Quaternion.identity, frontLeftTransform).GetComponent<ParticleSystem>();
                wheelParticles.frontRight =
                    Instantiate(smokeTrail,
                        frontRightTransform.position - Vector3.up * wheelColliders.frontRight.radius,
                        Quaternion.identity, frontRightTransform).GetComponent<ParticleSystem>();
                wheelParticles.rearLeft =
                    Instantiate(smokeTrail,
                        rearLeftTransform.position - Vector3.up * wheelColliders.rearLeft.radius,
                        Quaternion.identity, rearLeftTransform).GetComponent<ParticleSystem>();
                wheelParticles.rearRight =
                    Instantiate(smokeTrail,
                        rearRightTransform.position - Vector3.up * wheelColliders.rearRight.radius,
                        Quaternion.identity, rearRightTransform).GetComponent<ParticleSystem>();
            }

            if (skidmarkTrail)
            {
                wheelTrails.frontLeft =
                    Instantiate(skidmarkTrail,
                        frontLeftTransform.position - Vector3.up * wheelColliders.frontLeft.radius,
                        Quaternion.identity, frontLeftTransform).GetComponent<TrailRenderer>();
                wheelTrails.frontRight =
                    Instantiate(skidmarkTrail,
                        frontRightTransform.position - Vector3.up * wheelColliders.frontRight.radius,
                        Quaternion.identity, frontRightTransform).GetComponent<TrailRenderer>();
                wheelTrails.rearLeft =
                    Instantiate(skidmarkTrail,
                        rearLeftTransform.position - Vector3.up * wheelColliders.rearLeft.radius,
                        Quaternion.identity, rearLeftTransform).GetComponent<TrailRenderer>();
                wheelTrails.rearRight =
                    Instantiate(skidmarkTrail,
                        rearRightTransform.position - Vector3.up * wheelColliders.rearRight.radius,
                        Quaternion.identity, rearRightTransform).GetComponent<TrailRenderer>();
            }
        }

        private void CheckParticles()
        {
            WheelHit[] wheelHits = new WheelHit[4];

            wheelColliders.frontLeft.GetGroundHit(out wheelHits[0]);
            wheelColliders.frontRight.GetGroundHit(out wheelHits[1]);

            wheelColliders.rearLeft.GetGroundHit(out wheelHits[2]);
            wheelColliders.rearRight.GetGroundHit(out wheelHits[3]);

            float slipAllowance = 0.2f;

            if ((Mathf.Abs(wheelHits[0].sidewaysSlip) + Mathf.Abs(wheelHits[0].forwardSlip) > slipAllowance))
            {
                wheelParticles.frontLeft.Play();
                wheelTrails.frontLeft.emitting = true;
            }
            else
            {
                wheelParticles.frontLeft.Stop();
                wheelTrails.frontLeft.emitting = false;
            }

            if ((Mathf.Abs(wheelHits[1].sidewaysSlip) + Mathf.Abs(wheelHits[1].forwardSlip) > slipAllowance))
            {
                wheelParticles.frontRight.Play();
                wheelTrails.frontRight.emitting = true;
            }
            else
            {
                wheelParticles.frontRight.Stop();
                wheelTrails.frontRight.emitting = false;
            }

            if ((Mathf.Abs(wheelHits[2].sidewaysSlip) + Mathf.Abs(wheelHits[2].forwardSlip) > slipAllowance))
            {
                wheelParticles.rearLeft.Play();
                wheelTrails.rearLeft.emitting = true;
            }
            else
            {
                wheelParticles.rearLeft.Stop();
                wheelTrails.rearLeft.emitting = false;
            }

            if ((Mathf.Abs(wheelHits[3].sidewaysSlip) + Mathf.Abs(wheelHits[3].forwardSlip) > slipAllowance))
            {
                wheelParticles.rearRight.Play();
                wheelTrails.rearRight.emitting = true;
            }
            else
            {
                wheelParticles.rearRight.Stop();
                wheelTrails.rearRight.emitting = false;
            }
        }

        private void CheckInput()
        {
            _gasInput = Input.GetAxis("Vertical");
            _steeringInput = Input.GetAxis("Horizontal");

            var forward = transform.forward;
            _slipAngle = Vector3.Angle(forward, carRb.velocity-forward);
            
            if (_gearState != GearState.Changing)
            {
                if (_gearState == GearState.Neutral)
                {
                    _clutch = 0;
                    if (Mathf.Abs(_gasInput) > 0) _gearState = GearState.Running;
                }
                else
                {
                    _clutch = Input.GetKey(KeyCode.LeftShift) ? 0 : Mathf.Lerp(_clutch, 1, Time.deltaTime);
                }
            }
            else
            {
                _clutch = 0;
            }
            
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
            wheelColliders.frontLeft.brakeTorque = _brakeInput * brakePower * 0.7f;
            wheelColliders.frontRight.brakeTorque = _brakeInput * brakePower * 0.7f;

            wheelColliders.rearLeft.brakeTorque = _brakeInput * brakePower * 0.3f;
            wheelColliders.rearRight.brakeTorque = _brakeInput * brakePower * 0.3f;
        }

        private void ApplySteering()
        {
            var forward = transform.forward;
            float steeringAngle = _steeringInput * steeringCurve.Evaluate(_speed);
            steeringAngle += Vector3.SignedAngle(forward, carRb.velocity + forward, Vector3.up);
            steeringAngle = Mathf.Clamp(steeringAngle, -90f, 90f);
            wheelColliders.frontLeft.steerAngle = steeringAngle;
            wheelColliders.frontRight.steerAngle = steeringAngle;
        }

        private void ApplyMotor()
        {
            _currentTorque = CalculateTorque();
            wheelColliders.rearLeft.motorTorque = _currentTorque * _gasInput;
            wheelColliders.rearRight.motorTorque = _currentTorque * _gasInput;
        }

        private float CalculateTorque()
        {
            float torque = 0;
            
            if (rpm < idleRpm + 200 && _gasInput == 0 && currentGear == 0)
            {
                _gearState = GearState.Neutral;
            }
            
            if (_gearState == GearState.Running && _clutch > 0)
            {
                if (rpm > increaseGearRpm)
                    StartCoroutine(ChangeGear(1));
                else if (rpm < decreaseGearRpm)
                    StartCoroutine(ChangeGear(-1));
            }
            
            if (Mathf.Abs(_gasInput) > 0)
            {
                if (_clutch < 0.1f)
                {
                    rpm = Mathf.Lerp(rpm, Mathf.Max(idleRpm, redLine * _gasInput) + UnityEngine.Random.Range(-50, 50),
                        Time.deltaTime);
                }
                else
                {
                    _wheelRpm = Mathf.Abs((wheelColliders.rearRight.rpm + wheelColliders.rearLeft.rpm) / 2f) * gearRatios[currentGear] * differentialRatio;
                    rpm = Mathf.Lerp(rpm, Mathf.Max(idleRpm - 100, _wheelRpm), Time.deltaTime * 3f);
                    torque = (horsePowerToRpmCurve.Evaluate(rpm / redLine) * motorPower / rpm) * gearRatios[currentGear] * differentialRatio * 5252f * _clutch;
                }
            }

            return torque;
        }

        private void UpdateWheel()
        {
            SetWheel(wheelColliders.frontLeft, wheelMeshes.frontLeft);
            SetWheel(wheelColliders.frontRight, wheelMeshes.frontRight);
            SetWheel(wheelColliders.rearLeft, wheelMeshes.rearLeft);
            SetWheel(wheelColliders.rearRight, wheelMeshes.rearRight);
        }

        private void SetWheel(WheelCollider wheelColl, MeshRenderer wheelMesh)
        {
            wheelColl.GetWorldPose(out Vector3 pos, out Quaternion quat);
            var wheelTransform = wheelMesh.transform;
            wheelTransform.position = pos;
            wheelTransform.rotation = quat;
        }

        public float GetSpeedRatio()
        {
            var gas = Mathf.Clamp(_gasInput, 0.5f, 1f);
            return _speedClamped * gas / maxSpeed;
        }

        private IEnumerator ChangeGear(int gearChange)
        {
            _gearState = GearState.CheckingChange;

            if (currentGear + gearChange >= 0)
            {
                if (gearChange > 0)
                {
                    yield return new WaitForSeconds(0.7f);
                    if (rpm < increaseGearRpm || currentGear >= gearRatios.Length - 1)
                    {
                        _gearState = GearState.Running;
                        yield break;
                    }
                }
                if (gearChange < 0)
                {
                    yield return new WaitForSeconds(0.1f);
                    if (rpm > decreaseGearRpm || currentGear <= 0)
                    {
                        _gearState = GearState.Running;
                        yield break;
                    }
                }

                _gearState = GearState.Changing;
                yield return new WaitForSeconds(changeGearTime);
                currentGear += gearChange;
            }

            if (_gearState != GearState.Neutral)
                _gearState = GearState.Running;
        }
    }

    [Serializable]
    public class WheelColliders
    {
        public WheelCollider frontLeft;
        public WheelCollider frontRight;
        public WheelCollider rearLeft;
        public WheelCollider rearRight;
    }

    [Serializable]
    public class WheelMeshes
    {
        public MeshRenderer frontLeft;
        public MeshRenderer frontRight;
        public MeshRenderer rearLeft;
        public MeshRenderer rearRight;
    }

    [Serializable]
    public class WheelParticles
    {
        public ParticleSystem frontLeft;
        public ParticleSystem frontRight;
        public ParticleSystem rearLeft;
        public ParticleSystem rearRight;
    }

    [Serializable]
    public class WheelTrails
    {
        public TrailRenderer frontLeft;
        public TrailRenderer frontRight;
        public TrailRenderer rearLeft;
        public TrailRenderer rearRight;
    }

    public enum GearState
    {
        Neutral,
        Running,
        CheckingChange,
        Changing
    }
}