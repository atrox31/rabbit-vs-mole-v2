using RabbitVsMole.InteractableGameObject.Base;
using Unity.VisualScripting;
using UnityEngine;

namespace RabbitVsMole.InteractableGameObject.Visuals
{
    public class WallVisual : VisualBase
    {
        protected override void Awake()
        {
            base.Awake();
            Quaternion blockerRotation = Quaternion.Euler(0f, Random.Range(0, 4) * 90f, 0f);
            _model.transform.rotation = blockerRotation;
        }
    }
}