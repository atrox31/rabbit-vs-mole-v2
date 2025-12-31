using RabbitVsMole.InteractableGameObject.Enums;
using UnityEngine;

namespace RabbitVsMole.Events
{
    public struct TravelEvent { public Vector3 NewLocation; public ActionType actionTypeAfterTravel; }
}