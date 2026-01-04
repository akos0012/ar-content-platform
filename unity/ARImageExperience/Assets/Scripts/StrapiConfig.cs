using UnityEngine;

[CreateAssetMenu(fileName = "StrapiConfig", menuName = "AR/Strapi Config")]
public class StrapiConfig : ScriptableObject
{
    [Header("Strapi API Configuration")]
    [Tooltip("Base URL of your Strapi API (e.g., http://localhost:1337)")]
    public string apiBaseUrl = "http://localhost:1337";

    [Tooltip("API Token for authentication (optional)")]
    public string apiToken = "";

    [Header("Endpoints")]
    public string arExperiencesEndpoint = "/api/ar-experiences";

    public string GetFullUrl(string endpoint)
    {
        return apiBaseUrl.TrimEnd('/') + endpoint;
    }
}
