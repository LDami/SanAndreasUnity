using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace SanAndreasUnity.Importing.Vehicles
{
	public class Sounds
	{
		[System.Serializable]
		public struct SoundData
		{
			public int Bank;
			public int Sfx;
		}
		[System.Serializable]
		public struct VehicleSound
		{
			public List<int> Vehicles;
			public SoundData Idle;
			public SoundData Accelerate;
			public SoundData Decelerate;
		}

		public static List<VehicleSound> vehicleSounds;

		public static void Load(TextAsset file)
		{
			Debug.Log("Loading Vehicle sounds");
			vehicleSounds = JsonConvert.DeserializeObject<List<VehicleSound>>(file.text);
			Debug.Log(vehicleSounds.Count + " set of sounds loaded");
		}

		public static VehicleSound GetVehicleSound(int vehicleid)
		{
			if (vehicleSounds.Where(vs => vs.Vehicles.Contains(vehicleid)).Count() == 0)
			{
				Debug.LogWarning("Unable to fetch vehicle sound for id " + vehicleid);
				return new VehicleSound();
			}
			else
				return vehicleSounds.Find(vs => vs.Vehicles.Contains(vehicleid));
		}
	}
}