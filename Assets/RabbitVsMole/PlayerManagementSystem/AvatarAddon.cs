using RabbitVsMole;
using RabbitVsMole.InteractableGameObject.Enums;
using UnityEngine;
using Extensions;
using System.Collections;

public class AvatarAddon : MonoBehaviour
{
    public enum ModelAnchor
    {
        LeftFoot, RightFoot, LeftHand, RightHand
    }

    [SerializeField] public ActionType actionType;
    [SerializeField] public GameObject addonObject;
    [SerializeField] public ModelAnchor modelAnchor;
    [SerializeField] public ParticleSystem showUpParticles;
    [SerializeField] private float scaleUpDuration = 0.5f;

    private Vector3 originalScale;
    private Coroutine _scaleCorutine;

    public void Setup(PlayerAvatar playerAvatar)
    {
        var goModelAnchor = playerAvatar.gameObject.FindChildByNameRecursive($"!{modelAnchor}");
        if (goModelAnchor == null)
        {
            DebugHelper.LogError(playerAvatar, $"Can not find model anchor for {modelAnchor}");
            return;
        }

        transform.SetParent(goModelAnchor.transform);
        transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        transform.localScale = Vector3.one;

        originalScale = addonObject.transform.localScale;

        addonObject.SetActive(false);
        return;
    }

    private IEnumerator ScaleOverTime(bool show)
    {
        float elapsedTime = 0f;

        var fromVector = show
            ? Vector3.zero
            : originalScale;

        var toVector = show
            ? originalScale
            : Vector3.zero;

        addonObject.SetActive(true);
        addonObject.transform.localScale = fromVector;
        showUpParticles.SafePlay();

        while (elapsedTime < scaleUpDuration)
        {
            float scaleProgress = elapsedTime / scaleUpDuration;
            addonObject.transform.localScale = Vector3.Lerp(fromVector, toVector, scaleProgress);

            elapsedTime += Time.deltaTime;
            yield return null; 
        }

        addonObject.transform.localScale = toVector;
        addonObject.SetActive(show);
    }

    public void Show() 
    {
        if (IsVisible)
            return;

        if (_scaleCorutine != null)
            StopCoroutine( _scaleCorutine );

        _scaleCorutine = StartCoroutine(ScaleOverTime(true));
    }

    public void Hide() 
    {
        if(!IsVisible) 
            return;

        if (_scaleCorutine != null)
            StopCoroutine(_scaleCorutine);

        _scaleCorutine = StartCoroutine(ScaleOverTime(false));
    }

    public bool IsVisible =>
        addonObject.activeSelf;
}