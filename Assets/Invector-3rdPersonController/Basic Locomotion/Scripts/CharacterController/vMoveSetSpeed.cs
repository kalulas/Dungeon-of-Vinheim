using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Invector.vCharacterController
{
    [vClassHeader("MoveSet Speed", "Use this to add extra speed into a specific MoveSet")]
    public class vMoveSetSpeed : vMonoBehaviour
    {
        vThirdPersonMotor cc;
        private vMoveSetControlSpeed defaultFree = new vMoveSetControlSpeed();
        private vMoveSetControlSpeed defaultStrafe = new vMoveSetControlSpeed();

        public List<vMoveSetControlSpeed> listFree;
        public List<vMoveSetControlSpeed> listStrafe;

        private int currentMoveSet;

        void Start()
        {
            cc = GetComponent<vThirdPersonMotor>();

            defaultFree.walkSpeed = cc.freeSpeed.walkSpeed;
            defaultFree.runningSpeed = cc.freeSpeed.runningSpeed;
            defaultFree.sprintSpeed = cc.freeSpeed.sprintSpeed;

            defaultStrafe.walkSpeed = cc.strafeSpeed.walkSpeed;
            defaultStrafe.runningSpeed = cc.strafeSpeed.runningSpeed;
            defaultStrafe.sprintSpeed = cc.strafeSpeed.sprintSpeed;

            StartCoroutine(UpdateMoveSetSpeed());
        }

        IEnumerator UpdateMoveSetSpeed()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.1f);
                ChangeSpeed();
            }
        }

        void ChangeSpeed()
        {
            currentMoveSet = (int)Mathf.Round(cc.animator.GetFloat("MoveSet_ID"));
            var strafing = cc.isStrafing;
            if (strafing)
            {
                var extraSpeed = listStrafe.Find(l => l.moveset == currentMoveSet);
                if (extraSpeed != null)
                {
                    cc.freeSpeed.walkSpeed = extraSpeed.walkSpeed;
                    cc.freeSpeed.runningSpeed = extraSpeed.runningSpeed;
                    cc.freeSpeed.sprintSpeed = extraSpeed.sprintSpeed;
                    cc.freeSpeed.crouchSpeed = extraSpeed.crouchSpeed;
                }
                else
                {
                    cc.strafeSpeed.walkSpeed = defaultStrafe.walkSpeed;
                    cc.strafeSpeed.runningSpeed = defaultStrafe.runningSpeed;
                    cc.strafeSpeed.sprintSpeed = defaultStrafe.sprintSpeed;
                    cc.strafeSpeed.crouchSpeed = defaultStrafe.crouchSpeed;
                }
            }
            else
            {
                var extraSpeed = listFree.Find(l => l.moveset == currentMoveSet);
                if (extraSpeed != null)
                {
                    cc.freeSpeed.walkSpeed = extraSpeed.walkSpeed;
                    cc.freeSpeed.runningSpeed = extraSpeed.runningSpeed;
                    cc.freeSpeed.sprintSpeed = extraSpeed.sprintSpeed;
                    cc.freeSpeed.crouchSpeed = extraSpeed.crouchSpeed;
                }
                else
                {
                    cc.strafeSpeed.walkSpeed = defaultFree.walkSpeed;
                    cc.strafeSpeed.runningSpeed = defaultFree.runningSpeed;
                    cc.strafeSpeed.sprintSpeed = defaultFree.sprintSpeed;
                    cc.strafeSpeed.crouchSpeed = defaultFree.crouchSpeed;
                }
            }
        }

        [System.Serializable]
        public class vMoveSetControlSpeed
        {
            public int moveset;
            public float walkSpeed = 1.5f;
            public float runningSpeed = 1.5f;
            public float sprintSpeed = 1.5f;
            public float crouchSpeed = 1.5f;
        }
    }
}

