﻿namespace VRTK.Examples.Archery
{
    using UnityEngine;

    public class Follow : MonoBehaviour
    {
        public bool followPosition;
        public bool followRotation;
        public Transform target;
        public float yOffset = 0.2f;

        private void Update()
        {
            if (target != null)
            {
                if (followRotation)
                {
                    transform.rotation = target.rotation;
                }

                if (followPosition)
                {
                    transform.position = new Vector3(target.position.x, target.position.y + yOffset, target.position.z);
                }
            }
            else
            {
                VRTK_Logger.Error(VRTK_Logger.GetCommonMessage(VRTK_Logger.CommonMessageKeys.NOT_DEFINED, "target"));
            }
        }
    }
}