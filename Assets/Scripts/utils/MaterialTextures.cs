using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialTextures
{
    private Dictionary<MapTypes, RenderTexture> textures = new Dictionary<MapTypes, RenderTexture>();
    private Dictionary<MapTypes, bool> textureDisabled = new Dictionary<MapTypes, bool>();
    private static RenderTexture resampleLocations;
    public Renderer rend { get; private set; }
    public int materialIndex { get; private set; }
    public FalseColor falseColor { get; set; }

    public MaterialPropertyBlock newProperties { get; private set; } = new MaterialPropertyBlock();

    public int resolutionX { get; private set; }
    public int resolutionY { get; private set; }

    ~MaterialTextures()
    {
        foreach (var texture in textures)
            UnityEngine.Object.Destroy(texture.Value);
        if (resampleLocations != null)
            UnityEngine.Object.Destroy(resampleLocations);
        resampleLocations = null;
    }

    public MaterialTextures(Vector2Int resolution, Renderer rend, int materialIndex)
    {
        resolutionX = Math.Max(0, resolution.x);
        resolutionY = Math.Max(0, resolution.y);
        UpdateLinkedRenderer(rend, materialIndex);
    }
    public void UpdateLinkedRenderer(Renderer rend, int materialIndex)
    {
        foreach (var keyValue in this.textures)
        {
            if (keyValue.Value == null)
                continue;
            if(!textureDisabled.ContainsKey(keyValue.Key))
                textureDisabled.Add(keyValue.Key, true);
        }

        this.rend = rend;
        this.materialIndex = materialIndex;
        this.falseColor = null;

        rend.GetPropertyBlock(newProperties, materialIndex);
        newProperties.Clear();
    }

    public enum MapTypes
    {
        colorMap,
        maskMap,
        detailMap,
        normalMap,
        defectMap,
        //resampleLocationMap// this is a static map to save ram memory.
    }
    public RenderTexture set(MapTypes type, Texture baseTexture, Color backupColor)
    {
        if (!textures.ContainsKey(type))
            textures.Add(type, null);
        if (textureDisabled.ContainsKey(type))
            textureDisabled.Remove(type);

        RenderTexture newTexture = textures[type];
        setTexture(baseTexture, backupColor, ref newTexture, type != MapTypes.colorMap);
        textures[type] = newTexture;
        return newTexture;
    }
    public RenderTexture get(MapTypes type)
    {
        if (textures.ContainsKey(type) && !textureDisabled.ContainsKey(type))
        {
            if (!textures[type].IsCreated())
                textures[type].Create();
            return textures[type];
        }
        else
            return null;
    }

    public RenderTexture getResamplelocations()
    {
        //return get(MapTypes.resampleLocationMap);
        if(resampleLocations == null || resampleLocations.width != this.resolutionX || resampleLocations.height != this.resolutionY)
        {
            if (resampleLocations != null)
                UnityEngine.Object.Destroy(resampleLocations);
            resampleLocations = new RenderTexture(resolutionX, resolutionY, 0, UnityEngine.Experimental.Rendering.DefaultFormat.LDR);
            resampleLocations.enableRandomWrite = true;
            resampleLocations.wrapMode = TextureWrapMode.Mirror;
            resampleLocations.Create();
        }
        return resampleLocations;
    }

    public string getTextureName(MapTypes type)
    {
        switch (type)
        {
            case MapTypes.colorMap:
                return "_BaseColorMap";
            case MapTypes.maskMap:
                return "_MaskMap";
            case MapTypes.detailMap:
                return "_DetailMap";
            case MapTypes.normalMap:
                return "_NormalMap";
            case MapTypes.defectMap:
                return "_FalseColorTex";
            default:
                return "";
        }
    }

    public void linkpropertyBlock()
    {
        //texture asseignment
        foreach(var keyValue in this.textures)
        {
            if (keyValue.Value == null || textureDisabled.ContainsKey(keyValue.Key))
                continue;
            string textureName = getTextureName(keyValue.Key);
            newProperties.SetTexture(textureName, keyValue.Value);
        }

        if (falseColor != null)
        {
            falseColor.falseColorTex = get(MapTypes.defectMap);
            Vector4 scaleOffsetVector = GetCurrentLinkedVector("_BaseColorMap_ST", "baseColorTexture_ST");
            if (scaleOffsetVector == new Vector4(0, 0, 0, 0))
                scaleOffsetVector = new Vector4(1, 1, 0, 0);
            falseColor.scaleOffset = scaleOffsetVector;
            falseColor.ApplyFalseColorProperties(newProperties);
        }

        rend.SetPropertyBlock(newProperties, materialIndex);
    }

    public bool linkTexture(MapTypes type)
    {
        string textureName = getTextureName(type);
        if (textureName == "" || !textures.ContainsKey(type) || textureDisabled.ContainsKey(type))
            return false;

        newProperties.SetTexture(textureName, textures[type]);
        return true;
    }


    private void setTexture(Texture source, Color backupColor, ref RenderTexture destination, bool liniearColorSpace = true)
    {
        if (destination == null)
        {
            if (resolutionX == 0)
                resolutionX = (source != null ? source.width : 2048);
            if (resolutionY == 0)
                resolutionY = (source != null ? source.height : 2048);

            if (liniearColorSpace)
                destination = new RenderTexture(resolutionX, resolutionY, 0, UnityEngine.Experimental.Rendering.DefaultFormat.LDR);
            else
                destination = new RenderTexture(resolutionX, resolutionY, 0, UnityEngine.Experimental.Rendering.DefaultFormat.HDR);
            destination.enableRandomWrite = true;
            destination.wrapMode = TextureWrapMode.Mirror;
            destination.Create();
        }

        if (source != null)
        {
            //Graphics.Blit(source, destination, new Vector2(((float)resolutionX) / source.width, ((float)resolutionY) / source.height), new Vector2(0, 0)); scale works difrent then expected.  source is automaticly scaled to output texture, and then the scale is applied
            Graphics.Blit(source, destination);
        }
        else
        {
            if (backupColor == null)
                backupColor = new Color(0, 0, 0);
            RenderTexture rt = RenderTexture.active;
            RenderTexture.active = destination;
            GL.Clear(true, true, backupColor);
            RenderTexture.active = rt;
        }
    }

    public Vector4 GetCurrentLinkedVector(string propertyName, string altPropertyName = null)
    {
        if (newProperties.HasVector(propertyName))
            return newProperties.GetVector(propertyName);

        if (rend.materials[materialIndex].HasVector(propertyName))
            return rend.materials[materialIndex].GetVector(propertyName);
        
        if (altPropertyName != null)
        {
            if (newProperties.HasVector(altPropertyName))
                return newProperties.GetVector(altPropertyName);

            if (rend.materials[materialIndex].HasVector(altPropertyName))
                return rend.materials[materialIndex].GetVector(altPropertyName);
        }

        Debug.LogWarning("Error occured while requesting a property of a material. Probably an unsuported material shader is used. <br>Shader: <b>" + rend.material.shader.name + "</b> has no attribute: <b>" + propertyName + "</b>");
        return new Vector4(0, 0, 0, 0);

    }
    public Color GetCurrentLinkedColor(string propertyName, string altPropertyName = null)
    {
        if (newProperties.HasColor(propertyName))
            return newProperties.GetColor(propertyName);

        if (rend.materials[materialIndex].HasColor(propertyName))
            return rend.materials[materialIndex].GetColor(propertyName);
        
        if (altPropertyName != null)
        {
            if (newProperties.HasColor(altPropertyName))
                return newProperties.GetColor(altPropertyName);

            if (rend.materials[materialIndex].HasColor(altPropertyName))
                return rend.materials[materialIndex].GetColor(altPropertyName);
        }

        Debug.LogWarning("Error occured while requesting a property of a material. Probably an unsuported material shader is used. <br>Shader: <b>" + rend.material.shader.name + "</b> has no attribute: <b>" + propertyName + "</b>");
        return new Color(0, 0, 0, 0);
    }
    public Texture GetCurrentLinkedTexture(string propertyName, string altPropertyName = null)
    {
        if (newProperties.HasTexture(propertyName))
            return newProperties.GetTexture(propertyName);

        if (rend.materials[materialIndex].HasTexture(propertyName))
            return rend.materials[materialIndex].GetTexture(propertyName);

        if (altPropertyName != null)
        {
            if (newProperties.HasTexture(altPropertyName))
                return newProperties.GetTexture(altPropertyName);

            if (rend.materials[materialIndex].HasTexture(altPropertyName))
                return rend.materials[materialIndex].GetTexture(altPropertyName);
        }

        Debug.LogWarning("Error occured while requesting a property of a material. Probably an unsuported material shader is used. <br>Shader: <b>" + rend.material.shader.name + "</b> has no attribute: <b>" + propertyName + "</b>");
        return null;
        
    }

    public float GetCurrentLinkedFloat(string propertyName, string altPropertyName = null)
    {
        if (newProperties.HasFloat(propertyName))
            return newProperties.GetFloat(propertyName);

        if (rend.materials[materialIndex].HasFloat(propertyName))
            return rend.materials[materialIndex].GetFloat(propertyName);
        
        if (altPropertyName != null)
        {
            if (newProperties.HasFloat(altPropertyName))
                return newProperties.GetFloat(altPropertyName);

            if (rend.materials[materialIndex].HasFloat(altPropertyName))
                return rend.materials[materialIndex].GetFloat(altPropertyName);
        }

        Debug.LogWarning("Error occured while requesting a property of a material. Probably an unsuported material shader is used. <br>Shader: <b>" + rend.material.shader.name + "</b> has no attribute: <b>" + propertyName + "</b>");
        return 0.0f;
        
    }
}
