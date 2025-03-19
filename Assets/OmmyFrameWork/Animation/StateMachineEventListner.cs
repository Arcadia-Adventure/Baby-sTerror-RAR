//Ommy
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace Ommy.Animation
{
    public class StateMachineEventListner : MonoBehaviour
    {
        [Header("You can subscribe OnEventInvoke in your target script")]
        public UnityEvent<StateMachineEventType> OnEventInvoke;

        // Demo Code
        // public void OnEventInvoke(StateMachineEventType stateMachineEventType)
        // {
        //     switch (stateMachineEventType)
        //     {
        //         case StateMachineEventType.Release:
        //             break;
        //         case StateMachineEventType.Charging:
        //             break;
        //         case StateMachineEventType.Pick:
        //             break;
        //     }
        // }
    }
    public enum StateMachineEventType
    {
        Release, Charging, Pick
    }
}