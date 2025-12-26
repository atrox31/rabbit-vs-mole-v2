using RabbitVsMole;
using RabbitVsMole.InteractableGameObject.Enums;
using UnityEngine;
using Extensions;

public class AvatarAddon : MonoBehaviour
{
    public enum ModelAnchor
    {
        LeftFoot, RightFoot, LeftHand, RightHand
    }
    [SerializeField] public ActionType actionType;
    [SerializeField] public GameObject addonObject;
    [SerializeField] public ModelAnchor modelAnchor;

    private Transform _modelAnchor;
    private GameObject _model;

    public void Setup(PlayerAvatar playerAvatar)
    {
        _modelAnchor = playerAvatar.gameObject.FindChildByNameRecursive($"!{modelAnchor}").transform;
        if (_modelAnchor == null)
        {
            DebugHelper.LogError(playerAvatar, $"Can not find model anchor for {modelAnchor}");
            return;
        }

        _model = GameObject.Instantiate(addonObject, _modelAnchor);
        Hide();
        return;
    }

    public void Show() =>
        _model?.SetActive(true);
    
    public void Hide() =>
        _model?.SetActive(false);

    public bool IsVisible =>
        _model.activeSelf;
}