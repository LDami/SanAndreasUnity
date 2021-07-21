using UnityEngine;
using SanAndreasUnity.Utilities;
using SanAndreasUnity.Behaviours.Vehicles;
using SanAndreasUnity.Importing.Animation;
using SanAndreasUnity.Importing.Vehicles;
using System.Linq;

namespace SanAndreasUnity.Behaviours.Peds.States
{

	public class VehicleExitingState : BaseVehicleState
	{
		Coroutine m_coroutine;
		bool m_isExitingImmediately = false;


		public override void OnBecameActive()
		{
			base.OnBecameActive();
			if (m_isServer)	// clients will do this when vehicle gets assigned
				this.ExitVehicleInternal();
		}

		public override void OnBecameInactive()
		{
			this.Cleanup();

			m_isExitingImmediately = false;

			if (m_coroutine != null)
				StopCoroutine(m_coroutine);
			m_coroutine = null;

			base.OnBecameInactive();
		}

		protected override void OnVehicleAssigned()
		{
			this.ExitVehicleInternal();
		}

		public void ExitVehicle(bool immediate = false)
		{
			if (!m_ped.IsInVehicle || !m_ped.IsInVehicleSeat)
				return;
			
			// obtain current vehicle from Ped
			this.CurrentVehicle = m_ped.CurrentVehicle;
			this.CurrentVehicleSeatAlignment = m_ped.CurrentVehicleSeatAlignment;

			m_isExitingImmediately = immediate;

			// after obtaining parameters, switch to this state
			m_ped.SwitchState<VehicleExitingState> ();
			
			// we'll do the rest of the work when our state gets activated

		}

		void ExitVehicleInternal()
		{
			BaseVehicleState.PreparePedForVehicle(m_ped, this.CurrentVehicle, this.CurrentVehicleSeat);

			// TODO: no need for this, vehicle should know when there is no driver
			// TODO: but, right now, this should be included in cleanup
			if (this.CurrentVehicleSeat.IsDriver)
				this.CurrentVehicle.StopControlling();
			
			if (m_isServer && this.CurrentVehicleSeat.IsDriver)
			{
				// remove authority
			//	Net.NetManager.RemoveAuthority(this.CurrentVehicle.gameObject);
			}

			m_coroutine = StartCoroutine (ExitVehicleAnimation (m_isExitingImmediately));

		}
        
		private System.Collections.IEnumerator ExitVehicleAnimation(bool immediate)
		{
            Door.Position doorPos = Door.Position.LF;
            switch (this.CurrentVehicleSeatAlignment)
            {
                case Vehicle.SeatAlignment.BackLeft:
                    doorPos = Door.Position.LR;
                    break;
                case Vehicle.SeatAlignment.BackRight:
                    doorPos = Door.Position.RR;
                    break;
                case Vehicle.SeatAlignment.FrontLeft:
                    doorPos = Door.Position.LF;
                    break;
                case Vehicle.SeatAlignment.FrontRight:
                    doorPos = Door.Position.RF;
                    break;
            }

            AnimationState animState;
            AnimId? anim = VehiclesAnimation.GetAnim(this.CurrentVehicle.Definition.Id, VehiclesAnimation.Action.GetIn, this.CurrentVehicleSeat.IsLeftHand ? VehiclesAnimation.Side.Left : VehiclesAnimation.Side.Right);
            Vector3 getInVector = Vector3.Scale(m_model.GetAnim(anim.GetValueOrDefault().AnimGroup, anim.GetValueOrDefault().AnimIndex).RootEnd, new Vector3(-1, -1, -1));

            anim = VehiclesAnimation.GetAnim(this.CurrentVehicle.Definition.Id, VehiclesAnimation.Action.CloseIn, this.CurrentVehicleSeat.IsLeftHand ? VehiclesAnimation.Side.Left : VehiclesAnimation.Side.Right);
            if (anim != null)
            {
                m_model.VehicleParentOffset = Vector3.Scale(m_model.GetAnim(anim.Value.AnimGroup, anim.Value.AnimIndex).RootEnd, new Vector3(-1, -1, -1));
                animState = m_model.PlayAnim(anim.Value, PlayMode.StopAll);
                animState.wrapMode = WrapMode.Once;
                animState.speed = -1;
                while (animState != null && animState.enabled && this.CurrentVehicle != null)
                {
                    yield return new WaitForEndOfFrame();
                }
            }
            else
                Debug.Log("Align animation is null");

            if (this.CurrentVehicle._doors.Count > 0)
            {
                Debug.Log("This vehicle has " + this.CurrentVehicle._doors.Count + " doors");

                anim = VehiclesAnimation.GetAnim(this.CurrentVehicle.Definition.Id, VehiclesAnimation.Action.GetOut, this.CurrentVehicleSeat.IsLeftHand ? VehiclesAnimation.Side.Left : VehiclesAnimation.Side.Right);
                if (anim != null)
                {
                    double openDelay = VehiclesAnimation.GetOpeningDelay(this.CurrentVehicle.Definition.Id);
                    VehicleDoor door = this.CurrentVehicle._doors.Find(x => x.Position == doorPos);
                    m_model.VehicleParentOffset = Vector3.zero;
                    m_model.VehicleParentOffset = Vector3.Scale(m_model.GetAnim(anim.Value.AnimGroup, anim.Value.AnimIndex).RootEnd, new Vector3(-1, -1, -1));
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

            if (this.CurrentVehicle._doors.Count > 0)
            {
                anim = VehiclesAnimation.GetAnim(this.CurrentVehicle.Definition.Id, VehiclesAnimation.Action.CloseOut, this.CurrentVehicleSeat.IsLeftHand ? VehiclesAnimation.Side.Left : VehiclesAnimation.Side.Right);
                if (anim != null)
                {
                    double closeDelay = VehiclesAnimation.GetCloseOutDelay(this.CurrentVehicle.Definition.Id);
                    VehicleDoor door = this.CurrentVehicle._doors.Find(x => x.Position == doorPos);

                    m_model.VehicleParentOffset = getInVector;
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

			// ped now completely exited the vehicle

			m_ped.transform.localPosition = m_model.VehicleParentOffset;
			m_ped.transform.localRotation = Quaternion.identity;

			m_model.VehicleParentOffset = Vector3.zero;
			
			// now switch to other state
			// when our state gets deactivated, it will cleanup everything
			
			if (m_isServer)
				m_ped.SwitchState<StandState> ();

		}
    
	}

}
