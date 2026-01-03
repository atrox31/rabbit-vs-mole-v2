using Extensions;
using Steamworks;
using System.Collections;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class SteamAvatar : MonoBehaviour
{
    [SerializeField] private RawImage displayImage; // Assign a RawImage in the Inspector to see the result
    [SerializeField] private TMPro.TextMeshProUGUI playerNick;
    [SerializeField] private GameObject panel;

    const float startX = -420f;
    const float endX = 32f;
    const float animationTime = 0.33f;
    public bool IsReady { get; private set; } = false;
    public bool IsVisible { get; private set; } = false;

    IEnumerator Start()
    {
        panel.SetActive(false);
        yield return null;

        if (SteamManager.Initialized)
        {
            CSteamID userID = SteamUser.GetSteamID();
            string name = SteamFriends.GetPersonaName();
            Debug.Log($"Steam welcome: {name}");

            Texture2D avatar = GetSteamUserAvatar(userID);
            if (avatar != null && displayImage != null)
            {
                displayImage.texture = avatar;
            }
            playerNick.text = name;
            IsReady = true;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool Show()
    {
        if (!IsReady || IsVisible)
            return false;

        IsVisible = true;
        StartCoroutine(ShowCorutine());
        return IsVisible;
    }

    IEnumerator ShowCorutine()
    {
        panel.SetActive(true);
        var startposition = new Vector3(startX, panel.transform.position.y, panel.transform.position.z);
        var endposition = new Vector3(endX, panel.transform.position.y, panel.transform.position.z);

        var elapsedTime = 0f;
        panel.transform.position = startposition;
        while (elapsedTime < animationTime)
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(elapsedTime / animationTime);
            float sineProgress = (1f - Mathf.Cos(t * Mathf.PI)) / 2f;

            panel.transform.position = Vector3.Lerp(startposition, endposition, sineProgress);

            yield return null;
        }
        panel.transform.position = endposition;
    }

    Texture2D GetSteamUserAvatar(CSteamID sID)
    {
        // Get the handle to the medium-sized avatar (32x32: Small, 64x64: Medium, 128x128: Large)
        int handler = SteamFriends.GetMediumFriendAvatar(sID);

        // If the avatar is not yet loaded, Steam returns 0
        if (handler == 0 || handler == -1)
        {
            Debug.LogWarning("Avatar not loaded yet or doesn't exist.");
            return null;
        }

        // Get the dimensions of the image
        uint width, height;
        bool success = SteamUtils.GetImageSize(handler, out width, out height);

        if (success && width > 0 && height > 0)
        {
            // The image data is RGBA, so 4 bytes per pixel
            byte[] imageBuffer = new byte[width * height * 4];

            // Copy image data from Steam into our buffer
            bool gotImage = SteamUtils.GetImageRGBA(handler, imageBuffer, (int)(width * height * 4));

            if (gotImage)
            {
                // Create a new Texture2D
                Texture2D texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);

                // Load raw data. Note: Steam's image data is top-to-bottom, 
                // but Unity textures are bottom-to-top, so we flip it later if needed.
                texture.LoadRawTextureData(imageBuffer);
                texture.Apply();

                // To fix the "flipped image" issue common with Steam avatars:
                return FlipTexture(texture);
            }
        }

        return null;
    }
    Texture2D FlipTexture(Texture2D original)
    {
        Texture2D flipped = new Texture2D(original.width, original.height);
        int xN = original.width;
        int yN = original.height;

        for (int i = 0; i < xN; i++)
        {
            for (int j = 0; j < yN; j++)
            {
                flipped.SetPixel(i, yN - j - 1, original.GetPixel(i, j));
            }
        }
        flipped.Apply();
        return flipped;
    }
}