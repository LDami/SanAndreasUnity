using UnityEngine;
using SanAndreasUnity.Utilities;
using SanAndreasUnity.Behaviours.Vehicles;
using SanAndreasUnity.Behaviours.World;
using SanAndreasUnity.Importing.Animation;
using System.Linq;
using System;

namespace SanAndreasUnity.Behaviours.Peds.States
{

	public class VehicleEnteringState : BaseVehicleState
	{
		Coroutine m_coroutine;
		bool m_immediate = false;


		public override void OnBecameActive()
		{
			base.OnBecameActive();
			if (m_isServer)	// clients will do this when vehicle gets assigned
				this.EnterVehicleInternal();
		}

		public override void OnBecameInactive()
		{
			// restore everything

			m_immediate = false;

			this.Cleanup();

			if (m_coroutine != null)
				StopCoroutine(m_coroutine);
			m_coroutine = null;

			base.OnBecameInactive();
		}

		protected override void OnVehicleAssigned()
		{
			this.EnterVehicleInternal();
		}

		public bool TryEnterVehicle(Vehicle vehicle, Vehicle.SeatAlignment seatAlignment, bool immediate = false)
		{
			Net.NetStatus.ThrowIfNotOnServer();

			if (!this.CanEnterVehicle (vehicle, seatAlignment))
				return false;

			this.EnterVehicle(vehicle, seatAlignment, immediate);
			
			return true;
		}

		internal void EnterVehicle(Vehicle vehicle, Vehicle.SeatAlignment seatAlignment, bool immediate)
		{
            // first assign params
			this.CurrentVehicle = vehicle;
			this.CurrentVehicleSeatAlignment = seatAlignment;
			m_immediate = immediate;

			// switch state
			m_ped.SwitchState<VehicleEnteringState>();
		}

		void EnterVehicleInternal()
		{
			
			Vehicle vehicle = this.CurrentVehicle;
			Vehicle.Seat seat = this.CurrentVehicleSeat;
			bool immediate = m_immediate;
			

			BaseVehicleState.PreparePedForVehicle(m_ped, vehicle, seat);

			if (seat.IsDriver)
			{
				// TODO: this should be done when ped enters the car - or, it should be removed, because
				// vehicle should know if it has a driver
				vehicle.StartControlling();

				// if (m_isServer) {
				// 	var p = Net.Player.GetOwningPlayer(m_ped);
				// 	if (p != null)
				// 		Net.NetManager.AssignAuthority(vehicle.gameObject, p);
				// }
			}


			m_coroutine = StartCoroutine (EnterVehicleAnimation (seat, immediate));

		}

		private System.Collections.IEnumerator EnterVehicleAnimation(Vehicle.Seat seat, bool immediate)
		{
			var animIndex = seat.IsLeftHand ? AnimIndex.GetInLeft : AnimIndex.GetInRight;

            if (this.CurrentVehicle.animGroup == AnimGroup.Tank)
                m_model.VehicleParentOffset = new Vector3(-2.0f, 0.1f, 0.4f);
            else
                m_model.VehicleParentOffset = Vector3.Scale(m_model.GetAnim(AnimGroup.Car, animIndex).RootEnd, new Vector3(-1, -1, -1));

            if (!immediate)
			{
                AnimationState animState;
                if (this.CurrentVehicle.animGroup == AnimGroup.Tank)
                {
                    Transform door = this.CurrentVehicle.transform.FindChildRecursive("door_lf_dummy").transform;

                    animState = m_model.PlayAnim("tank", "TANK_align_LHS", PlayMode.StopAll);
                    animState.wrapMode = WrapMode.Once;
                    // wait until anim is finished or vehicle is destroyed
                    while (animState != null && animState.enabled && this.CurrentVehicle != null)
                    {
                        yield return new WaitForEndOfFrame();
                    }

                    m_model.VehicleParentOffset = new Vector3(-3.1f, -1.1f, -0.14f);
                    animState = m_model.PlayAnim("tank", "TANK_open_LHS", PlayMode.StopAll);
                    animState.wrapMode = WrapMode.Once;
                    //StartCoroutine(OpenTankDoor(door));
                    // wait until anim is finished or vehicle is destroyed
                    while (animState != null && animState.enabled && this.CurrentVehicle != null)
                    {
                        yield return new WaitForEndOfFrame();
                    }

                    m_model.VehicleParentOffset = new Vector3(-4.24f, -2.5f, -0.14f);
                    animState = m_model.PlayAnim("tank", "TANK_getin_LHS", PlayMode.StopAll);
                    animState.wrapMode = WrapMode.Once;
                    // wait until anim is finished or vehicle is destroyed
                    while (animState != null && animState.enabled && this.CurrentVehicle != null)
                    {
                        yield return new WaitForEndOfFrame();
                    }

                    m_model.VehicleParentOffset = Vector3.zero;
                    animState = m_model.PlayAnim("tank", "TANK_close_LHS", PlayMode.StopAll);
                    animState.wrapMode = WrapMode.Once;
                    //StartCoroutine(CloseTankDoor(door));
                    // wait until anim is finished or vehicle is destroyed
                    while (animState != null && animState.enabled && this.CurrentVehicle != null)
                    {
                        yield return new WaitForEndOfFrame();
                    }
                }
                else
                {
                    animState = m_model.PlayAnim(this.CurrentVehicle.animGroup, animIndex, PlayMode.StopAll);
                    animState.wrapMode = WrapMode.Once;
                }

                // wait until anim is finished or vehicle is destroyed
                while (animState != null && animState.enabled && this.CurrentVehicle != null)
                {
                    yield return new WaitForEndOfFrame();
                }
			}
            
            // check if vehicle is alive
            if (null == this.CurrentVehicle)
			{
				// vehicle destroyed in the meantime ? hmm... ped is a child of vehicle, so it should be
				// destroyed as well ?
				// anyway, switch to stand state
				if (m_isServer)
					m_ped.SwitchState<StandState>();
				yield break;
			}


			// ped now completely entered the vehicle

			// call method from VehicleSittingState - he will switch state
			if (m_isServer)
				m_ped.GetStateOrLogError<VehicleSittingState> ().EnterVehicle(this.CurrentVehicle, seat);

        }
    }

}
