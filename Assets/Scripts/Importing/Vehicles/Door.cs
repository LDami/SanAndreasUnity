using UnityEngine;
using System.Collections;

namespace SanAndreasUnity.Importing.Vehicles
{
    public class Door
    {
        public enum Status
        {
            Opened,
            Opening,
            Closed,
            Closing
        }
        public enum Position
        {
            None,
            LF,
            RF,
            LR,
            RR
        }
        public enum Type
        {
            Side, // Most common (cars, some airplanes, some helicopters, ...)
            Top, // Hydra, Hunter, Rustler, Cropduster, Stuntplane
            Descending // Shamal
            // TODO: Make custom animation for AT400 and Andromada
        }
    }
}