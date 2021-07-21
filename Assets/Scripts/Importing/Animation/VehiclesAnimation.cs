using UnityEngine;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System;

namespace SanAndreasUnity.Importing.Animation
{
    public class VehiclesAnimation
    {
        public enum Action
        {
            Align,
            Open,
            PullOut,
            GetIn,
            Jacked,
            GetOut,
            CloseIn,    // Close door when the player is inside the vehicle
            CloseOut   // Close door when the player is outside the vehicle
        }

        public enum Side
        {
            Left,
            Right
        }

        private struct Anim
        {
            public Action index;
            public string file;
            public AnimIndex left;
            public AnimIndex right;
        }

        private struct VehicleAnim
        {
            public string label;
            public List<int> vehicles;
            public Vehicles.Door.Type doorType;
            public AnimGroup animGroup;
            public List<Anim> animList;
            public double delayOpenDoor;
            public double delayCloseInDoor;
            public double delayCloseOutDoor;
        }

        private static List<VehicleAnim> vehAnims;

        public static void Load(TextAsset file)
        {
            Debug.Log("Loading Vehicle animations");
            vehAnims = JsonConvert.DeserializeObject<List<VehicleAnim>>(file.text);
            Debug.Log(vehAnims.Count + " vehicles animations groups loaded");
        }

        public static AnimId GetAnim(int vehicleid, Action action, Side side)
        {
            try
            {
                VehicleAnim vehicleAnim = vehAnims.Find(x => x.vehicles.Contains(vehicleid));
                Anim anim = vehicleAnim.animList.Find(x => x.index == action);
                return new AnimId(vehicleAnim.animGroup, side == Side.Left ? anim.left : anim.right);
            }
            
            catch(Exception e)
            {
                Debug.LogError("VehiclesAnimation.GetAnim called but animations data are not loaded yet.");
                return new AnimId();
            }
        }

        public static double GetOpeningDelay(int vehicleid)
        {
            try
            {
                return vehAnims.Find(x => x.vehicles.Contains(vehicleid)).delayOpenDoor;
            }
            catch (Exception e)
            {
                Debug.LogError("VehiclesAnimation.GetOpeningDelay called but animations data are not loaded yet.");
                return 0;
            }
        }

        public static double GetCloseInDelay(int vehicleid)
        {
            try
            {
                return vehAnims.Find(x => x.vehicles.Contains(vehicleid)).delayCloseInDoor;
            }
            catch (Exception e)
            {
                Debug.LogError("VehiclesAnimation.GetCloseInDelay called but animations data are not loaded yet.");
                return 0;
            }
        }

        public static double GetCloseOutDelay(int vehicleid)
        {
            try
            {
                return vehAnims.Find(x => x.vehicles.Contains(vehicleid)).delayCloseOutDoor;
            }
            catch (Exception e)
            {
                Debug.LogError("VehiclesAnimation.GetCloseOutDelay called but animations data are not loaded yet.");
                return 0;
            }
        }
    }
}
