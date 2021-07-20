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

            VehicleDoorPosition doorPos = VehicleDoorPosition.LF;
            switch (seat.Alignment)
            {
                case Vehicle.SeatAlignment.BackLeft:
                    doorPos = VehicleDoorPosition.LR;
                    break;
                case Vehicle.SeatAlignment.BackRight:
                    doorPos = VehicleDoorPosition.RR;
                    break;
                case Vehicle.SeatAlignment.FrontLeft:
                    doorPos = VehicleDoorPosition.LF;
                    break;
                case Vehicle.SeatAlignment.FrontRight:
                    doorPos = VehicleDoorPosition.RF;
                    break;
            }

            AnimationState animState;
            AnimId? anim = VehiclesAnimation.GetAnim(this.CurrentVehicle.Definition.Id, VehiclesAnimation.Action.GetIn, seat.IsLeftHand ? VehiclesAnimation.Side.Left : VehiclesAnimation.Side.Right);
            Vector3 getInVector = Vector3.Scale(m_model.GetAnim(anim.GetValueOrDefault().AnimGroup, anim.GetValueOrDefault().AnimIndex).RootEnd, new Vector3(-1, -1, -1));

            anim = VehiclesAnimation.GetAnim(this.CurrentVehicle.Definition.Id, VehiclesAnimation.Action.Align, seat.IsLeftHand ? VehiclesAnimation.Side.Left : VehiclesAnimation.Side.Right);
            if (anim != null)
            {
                m_model.VehicleParentOffset = getInVector;
                animState = m_model.PlayAnim(anim.Value);
                animState.wrapMode = WrapMode.Once;
                while (animState != null && animState.enabled && this.CurrentVehicle != null)
                {
                    yield return new WaitForEndOfFrame();
                }
            }
            else
                Debug.Log("Align animation is null");

            if(this.CurrentVehicle._doors.Count > 0)
            {
                anim = VehiclesAnimation.GetAnim(this.CurrentVehicle.Definition.Id, VehiclesAnimation.Action.Open, seat.IsLeftHand ? VehiclesAnimation.Side.Left : VehiclesAnimation.Side.Right);
                if (anim != null)
                {
                    double openDelay = VehiclesAnimation.GetOpeningDelay(this.CurrentVehicle.Definition.Id);
                    VehicleDoor door = this.CurrentVehicle._doors.Find(x => x.Position == doorPos);
                    m_model.VehicleParentOffset = getInVector;
                    animState = m_model.PlayAnim(anim.Value);
                    animState.wrapMode = WrapMode.Once;
                    while (animState != null && animState.enabled && this.CurrentVehicle != null)
                    {
                        if (animState.time >= openDelay && door != null) door.Open();
                        yield return new WaitForEndOfFrame();
                    }
                }
                else
                    Debug.Log("Open animation is null");
            }

            if (seat.IsTaken)
            {
                anim = VehiclesAnimation.GetAnim(this.CurrentVehicle.Definition.Id, VehiclesAnimation.Action.PullOut, seat.IsLeftHand ? VehiclesAnimation.Side.Left : VehiclesAnimation.Side.Right);
                if (anim != null)
                {
                    m_model.VehicleParentOffset = getInVector;
                    animState = m_model.PlayAnim(anim.Value);
                    animState.wrapMode = WrapMode.Once;
                    StartCoroutine(PullOutOccupant(seat));
                    while (animState != null && animState.enabled && this.CurrentVehicle != null)
                    {
                        yield return new WaitForEndOfFrame();
                    }
                }
            }
            else
                Debug.Log("Seat is not taken");

            anim = VehiclesAnimation.GetAnim(this.CurrentVehicle.Definition.Id, VehiclesAnimation.Action.GetIn, seat.IsLeftHand ? VehiclesAnimation.Side.Left : VehiclesAnimation.Side.Right);
            if (anim != null)
            {
                m_model.VehicleParentOffset = Vector3.Scale(m_model.GetAnim(anim.Value.AnimGroup, anim.Value.AnimIndex).RootEnd, new Vector3(-1, -1, -1));
                animState = m_model.PlayAnim(anim.Value);
                animState.wrapMode = WrapMode.Once;
                while (animState != null && animState.enabled && this.CurrentVehicle != null)
                {
                    yield return new WaitForEndOfFrame();
                }
            }
            else
                Debug.Log("GetIn animation is null");

            if (this.CurrentVehicle._doors.Count > 0)
            {
                anim = VehiclesAnimation.GetAnim(this.CurrentVehicle.Definition.Id, VehiclesAnimation.Action.CloseIn, seat.IsLeftHand ? VehiclesAnimation.Side.Left : VehiclesAnimation.Side.Right);
                if (anim != null)
                {
                    double closeDelay = VehiclesAnimation.GetCloseInDelay(this.CurrentVehicle.Definition.Id);
                    VehicleDoor door = this.CurrentVehicle._doors.Find(x => x.Position == doorPos);

                    m_model.VehicleParentOffset = Vector3.Scale(m_model.GetAnim(anim.Value.AnimGroup, anim.Value.AnimIndex).RootEnd, new Vector3(-1, -1, -1));
                    animState = m_model.PlayAnim(anim.Value);
                    animState.wrapMode = WrapMode.Once;
                    while (animState != null && animState.enabled && this.CurrentVehicle != null)
                    {
                        if (animState.time >= closeDelay && door != null) door.Close();
                        yield return new WaitForEndOfFrame();
                    }
                }
                else
                    Debug.Log("CloseIn animation is null");
            }
            /*

            if (this.CurrentVehicle.animGroup == AnimGroup.Tank)
                m_model.VehicleParentOffset = new Vector3(-2.0f, 0.1f, 0.4f);
            else
                m_model.VehicleParentOffset = Vector3.Scale(m_model.GetAnim(AnimGroup.Car, openAnimIndex).RootEnd, new Vector3(-1, -1, -1));

            if (!immediate)
			{
                if (this.CurrentVehicle.animGroup == AnimGroup.Tank)
                {
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
                    VehicleDoor door = this.CurrentVehicle._doors.First();
                    Debug.Log("door = " + door.name);
                    // wait until anim is finished or vehicle is destroyed
                    while (animState != null && animState.enabled && this.CurrentVehicle != null)
                    {
                        if (animState.time > 0.08 && door.Status == VehicleDoorStatus.Closed)
                        {
                            Debug.Log("Door needs to be opened");
                            door.Open();
                        }
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
                    door.Close();
                    Debug.Log("calling closure");
                    // wait until anim is finished or vehicle is destroyed
                    while (animState != null && animState.enabled && this.CurrentVehicle != null)
                    {
                        yield return new WaitForEndOfFrame();
                    }
                }
                else
                {
                    VehicleDoor door = this.CurrentVehicle._doors.Find((d) => (int)d.Position == (int)seat.Alignment);
                    animState = m_model.PlayAnim(this.CurrentVehicle.animGroup, openAnimIndex, PlayMode.StopAll);
                    animState.wrapMode = WrapMode.Once;
                    // wait until anim is finished or vehicle is destroyed
                    while (animState != null && animState.enabled && this.CurrentVehicle != null)
                    {
                        if(null != door)
                            if (animState.time > 0.5 && door.Status == VehicleDoorStatus.Closed) door.Open();
                        yield return new WaitForEndOfFrame();
                    }
                }

                // wait until anim is finished or vehicle is destroyed
                while (animState != null && animState.enabled && this.CurrentVehicle != null)
                {
                    yield return new WaitForEndOfFrame();
                }
			}*/

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

        private System.Collections.IEnumerator PullOutOccupant(Vehicle.Seat seat)
        {
            AnimationState animState;
            AnimId? anim = VehiclesAnimation.GetAnim(this.CurrentVehicle.Definition.Id, VehiclesAnimation.Action.Jacked, seat.IsLeftHand ? VehiclesAnimation.Side.Left : VehiclesAnimation.Side.Right);
            if (anim != null)
            {
                animState = seat.OccupyingPed.PlayerModel.PlayAnim(anim.Value);
                animState.wrapMode = WrapMode.Once;
                while (animState != null && animState.enabled && this.CurrentVehicle != null)
                {
                    yield return new WaitForEndOfFrame();
                }
            }
        }
    }

}
