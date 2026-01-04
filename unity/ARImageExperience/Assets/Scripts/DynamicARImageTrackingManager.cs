using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class DynamicARImageTrackingManager : MonoBehaviour
{
    [Header("AR Components")]
    [SerializeField] private ARTrackedImageManager trackedImageManager;
    [SerializeField] private XRReferenceImageLibrary referenceImageLibrary;

    [Header("Strapi Configuration")]
    [SerializeField] private StrapiAPIClient strapiClient;

    [Header("Prefab Mapping")]
    [SerializeField] private List<PrefabMapping> prefabMappings = new List<PrefabMapping>();

    private Dictionary<string, GameObject> arObjects = new Dictionary<string, GameObject>();
    private Dictionary<string, ARExperienceData> experienceData = new Dictionary<string, ARExperienceData>();
    private List<Texture2D> downloadedTextures = new List<Texture2D>();

    private bool isInitialized = false;

    [System.Serializable]
    public class PrefabMapping
    {
        public string prefabName;
        public GameObject prefab;
    }

    private void Start()
    {
        if (trackedImageManager == null)
        {
            trackedImageManager = GetComponent<ARTrackedImageManager>();
        }

        if (trackedImageManager != null)
        {
            trackedImageManager.enabled = false;
        }

        StartCoroutine(InitializeFromStrapi());
    }

    private IEnumerator InitializeFromStrapi()
    {
        Debug.Log("Loading AR Experiences from Strapi...");

        bool requestComplete = false;
        ARExperiencesResponse response = null;
        string errorMessage = null;

        yield return strapiClient.GetARExperiences(
            (res) =>
            {
                response = res;
                requestComplete = true;
            },
            (error) =>
            {
                errorMessage = error;
                requestComplete = true;
            }
        );

        yield return new WaitUntil(() => requestComplete);

        if (!string.IsNullOrEmpty(errorMessage))
        {
            Debug.LogError($"Failed to load AR Experiences: {errorMessage}");
            yield break;
        }

        if (response == null || response.data == null || response.data.Count == 0)
        {
            Debug.LogWarning("No AR Experiences found in Strapi");
            yield break;
        }

        Debug.Log($"Found {response.data.Count} AR Experiences");

        yield return LoadARExperiences(response.data);

        if (trackedImageManager != null)
        {
            trackedImageManager.trackablesChanged.AddListener(OnImagesTrackedChanged);
            trackedImageManager.enabled = true;
        }

        isInitialized = true;
        Debug.Log("AR Image Tracking initialized successfully");
    }

    private IEnumerator LoadARExperiences(List<ARExperienceData> experiences)
    {
        var imageLibrary = trackedImageManager.CreateRuntimeLibrary() as MutableRuntimeReferenceImageLibrary;

        if (imageLibrary == null)
        {
            Debug.LogError("Failed to create runtime image library");
            yield break;
        }

        foreach (var experience in experiences)
        {
            if (experience.attributes == null || !experience.attributes.isActive)
                continue;

            var attrs = experience.attributes;

            if (attrs.targetImage?.data?.attributes?.url == null)
            {
                Debug.LogWarning($"Experience '{attrs.name}' has no target image");
                continue;
            }

            string imageUrl = attrs.targetImage.data.attributes.url;
            Texture2D texture = null;
            bool imageLoaded = false;

            yield return strapiClient.DownloadImage(
                imageUrl,
                (tex) =>
                {
                    texture = tex;
                    imageLoaded = true;
                },
                (error) =>
                {
                    Debug.LogError($"Failed to download image for '{attrs.name}': {error}");
                    imageLoaded = true;
                }
            );

            yield return new WaitUntil(() => imageLoaded);

            if (texture == null)
                continue;

            downloadedTextures.Add(texture);

            float physicalSize = attrs.physicalSize > 0 ? attrs.physicalSize : 0.1f;

            var imageJobState = imageLibrary.ScheduleAddImageWithValidationJob(
                texture,
                attrs.name,
                physicalSize
            );

            while (!imageJobState.jobHandle.IsCompleted)
            {
                yield return null;
            }

            imageJobState.jobHandle.Complete();

            if (imageJobState.status == AddReferenceImageJobStatus.Success)
            {
                Debug.Log($"Successfully added image '{attrs.name}' to library");
                experienceData[attrs.name] = experience;

                GameObject prefab = GetPrefabByName(attrs.prefabName);
                if (prefab != null)
                {
                    var arObject = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                    arObject.name = attrs.name;

                    if (attrs.scale != null)
                    {
                        arObject.transform.localScale = attrs.scale.ToVector3();
                    }

                    arObject.SetActive(false);
                    arObjects[attrs.name] = arObject;
                }
                else
                {
                    Debug.LogWarning($"Prefab '{attrs.prefabName}' not found for experience '{attrs.name}'");
                }
            }
            else
            {
                Debug.LogError($"Failed to add image '{attrs.name}': {imageJobState.status}");
            }
        }

        trackedImageManager.referenceLibrary = imageLibrary;
    }

    private GameObject GetPrefabByName(string prefabName)
    {
        foreach (var mapping in prefabMappings)
        {
            if (mapping.prefabName == prefabName)
            {
                return mapping.prefab;
            }
        }
        return null;
    }

    private void OnImagesTrackedChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        foreach (var trackedImage in eventArgs.added)
        {
            UpdateTrackedImage(trackedImage);
        }

        foreach (var trackedImage in eventArgs.updated)
        {
            UpdateTrackedImage(trackedImage);
        }

        foreach (var trackedImage in eventArgs.removed)
        {
            UpdateTrackedImage(trackedImage.Value);
        }
    }

    private void UpdateTrackedImage(ARTrackedImage trackedImage)
    {
        if (trackedImage == null || string.IsNullOrEmpty(trackedImage.referenceImage.name))
            return;

        string imageName = trackedImage.referenceImage.name;

        if (!arObjects.ContainsKey(imageName))
            return;

        GameObject arObject = arObjects[imageName];

        if (trackedImage.trackingState == TrackingState.Limited || trackedImage.trackingState == TrackingState.None)
        {
            arObject.SetActive(false);
            return;
        }

        arObject.SetActive(true);
        arObject.transform.position = trackedImage.transform.position;
        arObject.transform.rotation = trackedImage.transform.rotation;

        if (experienceData.ContainsKey(imageName))
        {
            var experience = experienceData[imageName];
            if (experience.attributes.position != null)
            {
                arObject.transform.localPosition += experience.attributes.position.ToVector3();
            }
            if (experience.attributes.rotation != null)
            {
                arObject.transform.localRotation *= Quaternion.Euler(experience.attributes.rotation.ToVector3());
            }
        }
    }

    private void OnDestroy()
    {
        if (trackedImageManager != null)
        {
            trackedImageManager.trackablesChanged.RemoveListener(OnImagesTrackedChanged);
        }

        foreach (var texture in downloadedTextures)
        {
            if (texture != null)
                Destroy(texture);
        }
        downloadedTextures.Clear();
    }
}
