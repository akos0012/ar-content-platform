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
            if (!experience.isActive)
            {
                Debug.Log($"Skipping inactive experience: {experience.name}");
                continue;
            }

            if (experience.targetImage == null || string.IsNullOrEmpty(experience.targetImage.url))
            {
                Debug.LogWarning($"Experience '{experience.name}' has no target image");
                continue;
            }

            string imageUrl = experience.targetImage.url;
            Debug.Log($"Loading image for '{experience.name}' from: {imageUrl}");

            Texture2D texture = null;
            bool imageLoaded = false;

            yield return strapiClient.DownloadImage(
                imageUrl,
                (tex) =>
                {
                    texture = tex;
                    imageLoaded = true;
                    Debug.Log($"Image downloaded successfully for '{experience.name}': {tex.width}x{tex.height}");
                },
                (error) =>
                {
                    Debug.LogError($"Failed to download image for '{experience.name}': {error}");
                    imageLoaded = true;
                }
            );

            yield return new WaitUntil(() => imageLoaded);

            if (texture == null)
            {
                Debug.LogError($"Texture is null for '{experience.name}'");
                continue;
            }

            downloadedTextures.Add(texture);

            float physicalSize = experience.physicalSize > 0 ? experience.physicalSize : 0.1f;
            Debug.Log($"Adding image '{experience.name}' to library with physical size: {physicalSize}");

            var imageJobState = imageLibrary.ScheduleAddImageWithValidationJob(
                texture,
                experience.name,
                physicalSize
            );

            while (!imageJobState.jobHandle.IsCompleted)
            {
                yield return null;
            }

            imageJobState.jobHandle.Complete();

            if (imageJobState.status == AddReferenceImageJobStatus.Success)
            {
                Debug.Log($"Successfully added image '{experience.name}' to library");
                experienceData[experience.name] = experience;

                GameObject prefab = GetPrefabByName(experience.prefabName);
                if (prefab != null)
                {
                    var arObject = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                    arObject.name = experience.name;

                    if (experience.scale != null)
                    {
                        arObject.transform.localScale = experience.scale.ToVector3();
                        Debug.Log($"Set scale for '{experience.name}': {experience.scale.ToVector3()}");
                    }

                    arObject.SetActive(false);
                    arObjects[experience.name] = arObject;
                    Debug.Log($"Created AR object for '{experience.name}' with prefab '{experience.prefabName}'");
                }
                else
                {
                    Debug.LogWarning($"Prefab '{experience.prefabName}' not found for experience '{experience.name}'");
                }
            }
            else
            {
                Debug.LogError($"Failed to add image '{experience.name}': {imageJobState.status}");
            }
        }

        trackedImageManager.referenceLibrary = imageLibrary;
        Debug.Log($"Image library updated with {experienceData.Count} images");
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
        {
            Debug.LogWarning($"No AR object found for tracked image: {imageName}");
            return;
        }

        GameObject arObject = arObjects[imageName];

        if (trackedImage.trackingState == TrackingState.Limited || trackedImage.trackingState == TrackingState.None)
        {
            Debug.Log($"Image '{imageName}' tracking state: {trackedImage.trackingState} - hiding object");
            arObject.SetActive(false);
            return;
        }

        Debug.Log($"Image '{imageName}' tracked! State: {trackedImage.trackingState}");
        arObject.SetActive(true);
        arObject.transform.position = trackedImage.transform.position;
        arObject.transform.rotation = trackedImage.transform.rotation;

        if (experienceData.ContainsKey(imageName))
        {
            var experience = experienceData[imageName];
            if (experience.position != null)
            {
                arObject.transform.localPosition += experience.position.ToVector3();
            }
            if (experience.rotation != null)
            {
                arObject.transform.localRotation *= Quaternion.Euler(experience.rotation.ToVector3());
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
