using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Vector3Data
{
    public float x;
    public float y;
    public float z;

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

[Serializable]
public class ImageFormat
{
    public string name;
    public string hash;
    public string ext;
    public string mime;
    public string path;
    public int width;
    public int height;
    public int size;
    public string url;
}

[Serializable]
public class ImageFormats
{
    public ImageFormat thumbnail;
    public ImageFormat small;
    public ImageFormat medium;
    public ImageFormat large;
}

[Serializable]
public class MediaFile
{
    public int id;
    public string name;
    public string alternativeText;
    public string caption;
    public int width;
    public int height;
    public ImageFormats formats;
    public string hash;
    public string ext;
    public string mime;
    public float size;
    public string url;
    public string previewUrl;
    public string provider;
    public string createdAt;
    public string updatedAt;
}

[Serializable]
public class MediaData
{
    public int id;
    public MediaFile attributes;
}

[Serializable]
public class MediaWrapper
{
    public MediaData data;
}

[Serializable]
public class ARExperienceAttributes
{
    public string name;
    public string description;
    public MediaWrapper targetImage;
    public MediaWrapper model3D;
    public string prefabName;
    public float physicalSize;
    public Vector3Data scale;
    public Vector3Data position;
    public Vector3Data rotation;
    public bool isActive;
    public string createdAt;
    public string updatedAt;
    public string publishedAt;
}

[Serializable]
public class ARExperienceData
{
    public int id;
    public ARExperienceAttributes attributes;
}

[Serializable]
public class ARExperiencesResponse
{
    public List<ARExperienceData> data;
    public MetaData meta;
}

[Serializable]
public class SingleARExperienceResponse
{
    public ARExperienceData data;
    public MetaData meta;
}

[Serializable]
public class MetaData
{
    public Pagination pagination;
}

[Serializable]
public class Pagination
{
    public int page;
    public int pageSize;
    public int pageCount;
    public int total;
}
