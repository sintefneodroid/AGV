using UnityEngine;
using System.Collections;

public class LightFlicker : MonoBehaviour {

	public float MinLightIntensity = 0.6F;
	public float MaxLightIntensity = 1.0F;

	public float AccelerateTime = 0.15f;

	private float _targetIntensity = 1.0f;
	private float _lastIntensity = 1.0f;

	private float _timePassed = 0.0f;

	private Light _lt;
	private const double Tolerance = 0.0001;

	private void Start() {
		_lt = GetComponent<Light>();
		_lastIntensity = _lt.intensity;
		FixedUpdate();
	}

	private void FixedUpdate() {
		_timePassed += Time.deltaTime;
		_lt.intensity = Mathf.Lerp(_lastIntensity, _targetIntensity, _timePassed/AccelerateTime);

		if (Mathf.Abs(_lt.intensity - _targetIntensity) < Tolerance) {
			_lastIntensity = _lt.intensity;
			_targetIntensity = Random.Range(MinLightIntensity, MaxLightIntensity);
			_timePassed = 0.0f;
		}
	}
}

