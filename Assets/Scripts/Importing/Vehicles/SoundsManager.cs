using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SanAndreasUnity.Importing.Vehicles
{
	public class SoundsManager : MonoBehaviour
	{
		[SerializeField] private TextAsset file;
		[SerializeField] private List<Sounds.VehicleSound> list;

		void Awake()
		{
			Sounds.Load(file);
			list = Sounds.vehicleSounds;
		}
	}

}