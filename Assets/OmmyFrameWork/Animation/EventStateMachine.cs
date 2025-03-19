//Ommy
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Ommy.Animation
{
    public class EventStateMachine : StateMachineBehaviour
    {
        [Serializable]
        public class EventInfo
        {
            public bool invokeInUpdate;
            public bool invokeOnceIfLoop;
            public StateMachineEventType eventType;
            [Min(0)]
            public float invokeNormalizedTime;
            [Header("debugging")]
            public bool isInvoked;
            public bool logs;
        }
        public Vector2 statRandomSpeed = Vector2.one;
        public EventInfo[] eventInfos;
        public void DrawValue(AnimatorStateInfo stateInfo, Animator animator)
        {
            //Debug.Log("normalizedTime "+ stateInfo.normalizedTime);
            for (int i = 0; i < eventInfos.Length; i++)
            {

                float normalizedTime;
                if (!eventInfos[i].invokeOnceIfLoop) normalizedTime = stateInfo.normalizedTime % 1;
                else normalizedTime = stateInfo.normalizedTime;
                if (normalizedTime >= eventInfos[i].invokeNormalizedTime)
                {
                    if (!eventInfos[i].invokeInUpdate && eventInfos[i].isInvoked) continue;
                    animator.GetComponent<StateMachineEventListner>().OnEventInvoke.Invoke(eventInfos[i].eventType);
                    if (eventInfos[i].logs) Debug.Log("Event Invoked Type " + eventInfos[i].eventType);
                    eventInfos[i].isInvoked = true;
                }

            }
        }
        //OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            animator.speed = UnityEngine.Random.Range(statRandomSpeed.x, statRandomSpeed.y);
        }

        //OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
        override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            DrawValue(stateInfo, animator);
        }

        //OnStateExit is called when a transition ends and the state machine finishes evaluating this state
        override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            animator.speed = 1;
            foreach (var item in eventInfos)
            {
                item.isInvoked = false;
            }
        }

        //OnStateMove is called right after Animator.OnAnimatorMove()
        override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // Implement code that processes and affects root motion
        }

        //OnStateIK is called right after Animator.OnAnimatorIK()
        override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // Implement code that sets up animation IK (inverse kinematics)
        }
    }
}