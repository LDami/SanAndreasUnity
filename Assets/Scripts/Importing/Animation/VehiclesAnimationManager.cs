using UnityEngine;
using System.Collections;

namespace SanAndreasUnity.Importing.Animation
{
    public class VehiclesAnimationManager : MonoBehaviour
    {
        [SerializeField] private TextAsset file;
        // Use this for initialization
        void Awake()
        {
            VehiclesAnimation.Load(file);
        }
    }
}