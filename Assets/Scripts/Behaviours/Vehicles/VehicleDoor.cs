using UnityEngine;
using UnityEditor;
using System;

namespace SanAndreasUnity.Behaviours.Vehicles
{
    public enum VehicleDoorStatus
    {
        Opened,
        Opening,
        Closed,
        Closing
    }
    public enum VehicleDoorPosition
    {
        None,
        LF,
        RF,
        LR,
        RR
    }
    public enum VehicleDoorType
    {
        Side, // Most common (cars, some airplanes, some helicopters, ...)
        Top, // Hydra, Hunter, Rustler, Cropduster, Stuntplane
        Descending // Shamal
        // TODO: There is nothing for AT400 and Andromada
    }
    public class VehicleDoor : DetachablePart
    {
        [SerializeField] private bool m_opened;
        public bool Opened { get { return m_opened; } }

        [SerializeField] private Vector3 m_openingVector;
        public Vector3 OpeningVector { get { return m_openingVector; } set { m_openingVector = value; } }

        [SerializeField] private float m_speed = 100f;
        public float Speed { get { return m_speed; } set { m_speed = value; } }

        [SerializeField] private VehicleDoorStatus m_status;
        public VehicleDoorStatus Status { get { return m_status; } }

        [SerializeField] private VehicleDoorPosition m_position;
        public VehicleDoorPosition Position { get { return m_position; } set { m_position = value; } }
        
        private float actualRotation;

        [SerializeField] private VehicleDoorType m_type;
        public VehicleDoorType Type {
            get { return m_type; }
            set {
                m_type = value;
                switch(m_type)
                {
                    case VehicleDoorType.Side:
                        if (m_position == VehicleDoorPosition.LF || m_position == VehicleDoorPosition.LR)
                            m_openingVector = new Vector3(0, 60, 0);
                        else if (m_position == VehicleDoorPosition.RF || m_position == VehicleDoorPosition.RR)
                            m_openingVector = new Vector3(0, -60, 0);
                        break;
                    case VehicleDoorType.Top:
                        m_openingVector = new Vector3(0, 0, 60);
                        break;
                    case VehicleDoorType.Descending:
                        m_openingVector = new Vector3(0, 0, -60);
                        break;
                }
            }
        }

        public void Awake()
        {
            m_status = VehicleDoorStatus.Closed;
            /*
            var hinge = this.gameObject.AddComponent<HingeJoint>();
            hinge.axis = Vector3.up;
            hinge.useLimits = true;

            var limit = 90.0f * ((m_position == VehicleDoorPosition.LF || m_position == VehicleDoorPosition.LR) ? 1.0f : -1.0f);
            hinge.limits = new JointLimits { min = Mathf.Min(0, limit), max = Mathf.Max(0, limit), };
            //hinge.connectedBody = gameObject.GetComponentInParent<Rigidbody>();
            */
        }

        public void Update()
        {
            if(m_opened && m_status == VehicleDoorStatus.Closed)
            {
                Open();
                Debug.Log("Requesting Opening door");
            }
            if(!m_opened && m_status == VehicleDoorStatus.Opened)
            {
                Close();
                Debug.Log("Requesting Closing door");
            }

            if (m_status == VehicleDoorStatus.Opening)
            {
                switch (m_type)
                {
                    case VehicleDoorType.Side:
                        if (m_position == VehicleDoorPosition.LF || m_position == VehicleDoorPosition.LR)
                            actualRotation += Time.deltaTime * m_speed;
                        else if (m_position == VehicleDoorPosition.RF || m_position == VehicleDoorPosition.RR)
                            actualRotation -= Time.deltaTime * m_speed;

                        this.transform.localEulerAngles = new Vector3(0, actualRotation, 0);

                        if (Vector3.Distance(this.transform.localEulerAngles, m_openingVector) < 2.0f)
                        {
                            this.transform.localEulerAngles = m_openingVector;
                            m_status = VehicleDoorStatus.Opened;
                        }
                        break;
                    case VehicleDoorType.Top:
                        actualRotation += Time.deltaTime * m_speed;

                        this.transform.localEulerAngles = new Vector3(0, 0, actualRotation);

                        if (Vector3.Distance(this.transform.localEulerAngles, m_openingVector) < 2.0f)
                        {
                            this.transform.localEulerAngles = m_openingVector;
                            m_status = VehicleDoorStatus.Opened;
                        }
                        break;
                }
            }

            if (m_status == VehicleDoorStatus.Closing)
            {
                switch (m_type)
                {
                    case VehicleDoorType.Side:
                        if (m_position == VehicleDoorPosition.LF || m_position == VehicleDoorPosition.LR)
                            actualRotation -= Time.deltaTime * m_speed * 1.1f;
                        else if (m_position == VehicleDoorPosition.RF || m_position == VehicleDoorPosition.RR)
                            actualRotation += Time.deltaTime * m_speed * 1.1f;

                        this.transform.localEulerAngles = new Vector3(0, actualRotation, 0);

                        if (Vector3.Distance(this.transform.localEulerAngles,Vector3.zero) < 2.0f)
                        {
                            this.transform.localEulerAngles = Vector3.zero;
                            m_status = VehicleDoorStatus.Closed;
                        }
                        break;
                    case VehicleDoorType.Top:
                        actualRotation -= Time.deltaTime * m_speed;

                        this.transform.localEulerAngles = new Vector3(0, 0, actualRotation);

                        if (Vector3.Distance(this.transform.localEulerAngles, Vector3.zero) < 2.0f)
                        {
                            this.transform.localEulerAngles = Vector3.zero;
                            m_status = VehicleDoorStatus.Closed;
                        }
                        break;
                }
            }
        }

        public void Open()
        {
            if(!m_opened)
            {
                Debug.Log("Opening door");
                m_opened = true;
                m_status = VehicleDoorStatus.Opening;
                this.transform.localEulerAngles = Vector3.zero;
            }
        }

        public void Close()
        {
            if(m_opened)
            {
                Debug.Log("Closing door");
                m_opened = false;
                m_status = VehicleDoorStatus.Closing;
                this.transform.localEulerAngles = m_openingVector;
            }
        }

        private System.Collections.IEnumerator OpenDoorRoutine(Transform doorTransform, Vector3 finalVector)
        {
            Vector3 actualAngle;
            actualAngle = doorTransform.rotation.eulerAngles;
            if (actualAngle.x > 180) actualAngle.x -= 360;
            if (actualAngle.y > 180) actualAngle.y -= 360;
            if (actualAngle.z > 180) actualAngle.z -= 360;

            Debug.Log("distance: " + Vector3.Distance(actualAngle, finalVector));
            Debug.Log("actualAngle: " + actualAngle);
            while ((actualAngle.x - finalVector.x) > 5 || (actualAngle.x - finalVector.x) < -5 ||
                    (actualAngle.y - finalVector.y) > 5 || (actualAngle.y - finalVector.y) < -5 ||
                    (actualAngle.z - finalVector.z) > 5 || (actualAngle.z - finalVector.z) < -5
                )
            {
                if ((actualAngle.x - finalVector.x) > 5) doorTransform.Rotate(-4, 0, 0);
                if ((actualAngle.x - finalVector.x) < -5) doorTransform.Rotate(4, 0, 0);
                if ((actualAngle.y - finalVector.y) > 5) doorTransform.Rotate(0, -4, 0);
                if ((actualAngle.y - finalVector.y) < -5) doorTransform.Rotate(0, 4, 0);
                if ((actualAngle.z - finalVector.z) > 5) doorTransform.Rotate(0, 0, -4);
                if ((actualAngle.z - finalVector.z) < -5) doorTransform.Rotate(0, 0, 4);

                actualAngle = doorTransform.rotation.eulerAngles;
                if (actualAngle.x > 180) actualAngle.x -= 360;
                if (actualAngle.y > 180) actualAngle.y -= 360;
                if (actualAngle.z > 180) actualAngle.z -= 360;
                Debug.Log("distance: " + Vector3.Distance(actualAngle, finalVector));
                Debug.Log("actualAngle: " + actualAngle);
                yield return new WaitForEndOfFrame();
            }
            Debug.Log("Door opened");
            m_status = VehicleDoorStatus.Opened;
        }

        private System.Collections.IEnumerator CloseDoorRoutine(Transform doorTransform)
        {
            Vector3 finalVector = Vector3.zero;
            Vector3 actualAngle;

            actualAngle = doorTransform.rotation.eulerAngles;
            if (actualAngle.x > 180) actualAngle.x -= 360;
            if (actualAngle.y > 180) actualAngle.y -= 360;
            if (actualAngle.z > 180) actualAngle.z -= 360;


            while (!actualAngle.Equals(finalVector))
            {
            }



            actualAngle = doorTransform.rotation.eulerAngles;
            if (actualAngle.x > 180) actualAngle.x -= 360;
            if (actualAngle.y > 180) actualAngle.y -= 360;
            if (actualAngle.z > 180) actualAngle.z -= 360;

            Debug.Log("distance: " + Vector3.Distance(actualAngle, finalVector));
            Debug.Log("actualAngle: " + actualAngle);
            while ((actualAngle.x - finalVector.x) > 2 || (actualAngle.x - finalVector.x) < -2 ||
                    (actualAngle.y - finalVector.y) > 2 || (actualAngle.y - finalVector.y) < -2 ||
                    (actualAngle.z - finalVector.z) > 2 || (actualAngle.z - finalVector.z) < -2
                )
            {
                if ((actualAngle.x - finalVector.x) > 2) doorTransform.Rotate(-5, 0, 0);
                if ((actualAngle.x - finalVector.x) < -2) doorTransform.Rotate(5, 0, 0);
                if ((actualAngle.y - finalVector.y) > 2) doorTransform.Rotate(0, -5, 0);
                if ((actualAngle.y - finalVector.y) < -2) doorTransform.Rotate(0, 5, 0);
                if ((actualAngle.z - finalVector.z) > 2) doorTransform.Rotate(0, 0, -5);
                if ((actualAngle.z - finalVector.z) < -2) doorTransform.Rotate(0, 0, 5);

                actualAngle = doorTransform.rotation.eulerAngles;
                if (actualAngle.x > 180) actualAngle.x -= 360;
                if (actualAngle.y > 180) actualAngle.y -= 360;
                if (actualAngle.z > 180) actualAngle.z -= 360;
                Debug.Log("distance: " + Vector3.Distance(actualAngle, finalVector));
                Debug.Log("actualAngle: " + actualAngle);
                yield return new WaitForEndOfFrame();
            }
            Debug.Log("Door closed");
            m_status = VehicleDoorStatus.Closed;
        }
    }
}