using UnityEngine;
using System.Collections;
using SanAndreasUnity.Behaviours.Audio;
using SanAndreasUnity.Importing.Vehicles;

namespace SanAndreasUnity.Behaviours.Vehicles
{
	public partial class Vehicle
	{
		public AudioSource EngineAudioSource { get; private set; }

		AudioClip startingEngineSound;
		AudioClip idleEngineSound;
		AudioClip accelEngineSound;
		AudioClip decelEngineSound;

		float[] gearsRatio =
		{
			.1f,
			.25f,
			.4f,
			.55f,
			1f
		};

		void Awake_SFX()
		{
			EngineAudioSource = this.gameObject.AddComponent<AudioSource>();
		}

		void Start_SFX()
		{

			Sounds.VehicleSound vehicleSound = Sounds.GetVehicleSound(this.Definition.Id);

			startingEngineSound = AudioManager.CreateAudioClipFromSfx("GENRL", 132, 38);
			idleEngineSound = AudioManager.CreateAudioClipFromSfx("GENRL", vehicleSound.Idle.Bank, vehicleSound.Idle.Sfx);
			accelEngineSound = AudioManager.CreateAudioClipFromSfx("GENRL", vehicleSound.Accelerate.Bank, vehicleSound.Accelerate.Sfx);
			decelEngineSound = AudioManager.CreateAudioClipFromSfx("GENRL", vehicleSound.Decelerate.Bank, vehicleSound.Decelerate.Sfx);

			EngineAudioSource.PlayOneShot(startingEngineSound);
			EngineAudioSource.loop = true;
			EngineAudioSource.volume = 0.7f;
			EngineAudioSource.spatialBlend = 1;
			EngineAudioSource.maxDistance = 20f;
			EngineAudioSource.rolloffMode = AudioRolloffMode.Custom;
			AnimationCurve curve = new AnimationCurve();
			curve.AddKey(0.04f, 0.8f);
			curve.AddKey(0.4f, 0.1f);
			curve.AddKey(0.6f, 0f);
			EngineAudioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curve);
		}
		
		void Update_SFX()
		{
			if(Net.NetStatus.IsServer)
			{
				if(this.Velocity.magnitude < 1) // Idle
				{
					if (EngineAudioSource.clip != idleEngineSound)
					{
						EngineAudioSource.clip = idleEngineSound;
						EngineAudioSource.pitch = 1;
						EngineAudioSource.Play();
					}
				}
				else
				{
					float velocityInGear = this.Velocity.magnitude % ((this.HandlingData.TransmissionMaxVel / 3) / this.HandlingData.TransmissionGears);
					float rpmPercentage = velocityInGear / ((this.HandlingData.TransmissionMaxVel / 3) / this.HandlingData.TransmissionGears);
					AudioClip nextClip = (this.Accelerator > 0) ? accelEngineSound : decelEngineSound;
					if (EngineAudioSource.clip != nextClip)
					{
						EngineAudioSource.clip = nextClip;
						EngineAudioSource.Play();
					}
					EngineAudioSource.pitch = Map(rpmPercentage, 0, 1, this.Gear == 1 ? 0.25f : 0.5f, 0.75f);
				}
			}
		}

		float Map(float x, float inLow, float inHigh, float outLow, float outHigh)
		{
			return (x - inLow) * (outHigh - outLow) / (inHigh - inLow) + outLow;
		}
	}
}
