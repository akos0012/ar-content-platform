using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class StrapiAPIClient : MonoBehaviour
{
    [SerializeField] private StrapiConfig config;

    private static StrapiAPIClient _instance;
    public static StrapiAPIClient Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<StrapiAPIClient>();
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    public IEnumerator GetARExperiences(Action<ARExperiencesResponse> onSuccess, Action<string> onError)
    {
        string url = config.GetFullUrl(config.arExperiencesEndpoint) + "?populate=*";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            SetHeaders(request);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string jsonResponse = request.downloadHandler.text;
                    Debug.Log($"Strapi Response: {jsonResponse}");

                    ARExperiencesResponse response = JsonUtility.FromJson<ARExperiencesResponse>(jsonResponse);
                    onSuccess?.Invoke(response);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to parse JSON: {e.Message}");
                    onError?.Invoke($"JSON Parse Error: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"Request failed: {request.error}");
                onError?.Invoke(request.error);
            }
        }
    }

    public IEnumerator GetARExperience(int id, Action<SingleARExperienceResponse> onSuccess, Action<string> onError)
    {
        string url = config.GetFullUrl(config.arExperiencesEndpoint) + $"/{id}?populate=*";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            SetHeaders(request);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string jsonResponse = request.downloadHandler.text;
                    SingleARExperienceResponse response = JsonUtility.FromJson<SingleARExperienceResponse>(jsonResponse);
                    onSuccess?.Invoke(response);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to parse JSON: {e.Message}");
                    onError?.Invoke($"JSON Parse Error: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"Request failed: {request.error}");
                onError?.Invoke(request.error);
            }
        }
    }

    public IEnumerator DownloadImage(string imageUrl, Action<Texture2D> onSuccess, Action<string> onError)
    {
        string fullUrl = GetFullMediaUrl(imageUrl);

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(fullUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                onSuccess?.Invoke(texture);
            }
            else
            {
                Debug.LogError($"Failed to download image: {request.error}");
                onError?.Invoke(request.error);
            }
        }
    }

    public IEnumerator DownloadAssetBundle(string bundleUrl, Action<AssetBundle> onSuccess, Action<string> onError)
    {
        string fullUrl = GetFullMediaUrl(bundleUrl);

        using (UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(fullUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(request);
                onSuccess?.Invoke(bundle);
            }
            else
            {
                Debug.LogError($"Failed to download asset bundle: {request.error}");
                onError?.Invoke(request.error);
            }
        }
    }

    private void SetHeaders(UnityWebRequest request)
    {
        request.SetRequestHeader("Content-Type", "application/json");

        if (!string.IsNullOrEmpty(config.apiToken))
        {
            request.SetRequestHeader("Authorization", $"Bearer {config.apiToken}");
        }
    }

    private string GetFullMediaUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return "";

        if (url.StartsWith("http://") || url.StartsWith("https://"))
            return url;

        return config.apiBaseUrl.TrimEnd('/') + url;
    }
}
