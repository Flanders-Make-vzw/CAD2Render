using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MapTypes = MaterialTextures.MapTypes;

public static class TextureNameResolver
{
    // Shader names used by glTFast ShaderGraphs
    private const string GltfPbrMetalRough = "Shader Graphs/glTF-pbrMetallicRoughness";
    private const string GltfPbrSpecGloss = "Shader Graphs/glTF-pbrSpecularGlossiness";
    private const string GltfUnlit = "Shader Graphs/glTF-unlit";

    public static string GetTextureName(Material mat, MapTypes type)
    {
        if (mat == null)
            return "";

        return getTextureName(mat.shader, type);
    }

    public static string getTextureName(Shader shader, MapTypes type)
    {
        string shaderName = shader?.name;

        return shaderName switch
        {
            // ---- GLTFAST: Metallic-Roughness ----------------------------------
            GltfPbrMetalRough => GetGltfMetalRoughName(type),
            // ---- GLTFAST: Specular-Glossiness ---------------------------------
            GltfPbrSpecGloss => GetGltfSpecGlossName(type),
            // ---- GLTFAST: Unlit ------------------------------------------------
            GltfUnlit => GetGltfUnlitName(type),
            // ---- FALLBACK: HDRP / default --------------------------------------
            _ => GetHdrpName(type)
        };
    }

    // ---------------------------------------------------------------------
    // glTF – Metallic/Roughness (KHR_materials_pbrMetallicRoughness)
    // ---------------------------------------------------------------------
    private static string GetGltfMetalRoughName(MapTypes type)
    {
        return type switch
        {
            MapTypes.colorMap => "baseColorTexture",
            MapTypes.normalMap => "normalTexture",
            MapTypes.detailMap => "occlusionTexture",
            MapTypes.maskMap => "metallicRoughnessTexture",
            MapTypes.defectMap => "emissiveTexture",
            _ => ""
        };
    }

    // ---------------------------------------------------------------------
    // glTF – Specular/Glossiness (KHR_materials_pbrSpecularGlossiness)
    // Property names inferred based on glTF specification and glTFast naming.
    // glTF spec says:
    // - diffuseTexture
    // - specularGlossinessTexture (RGB = specular, A = glossiness)
    // ---------------------------------------------------------------------
    private static string GetGltfSpecGlossName(MapTypes type)
    {
        return type switch
        {
            MapTypes.colorMap => "diffuseTexture",
            MapTypes.maskMap => "specularGlossinessTexture",
            MapTypes.normalMap => "normalTexture", // glTF normal is the same
            MapTypes.detailMap => "occlusionTexture",
            MapTypes.defectMap => "emissiveTexture",
            _ => ""
        };
    }

    // ---------------------------------------------------------------------
    // glTF – Unlit (KHR_materials_unlit)
    // ---------------------------------------------------------------------
    private static string GetGltfUnlitName(MapTypes type)
    {
        return type switch
        {
            MapTypes.colorMap => "baseColorTexture",
            _ => ""
        };
    }

    // ---------------------------------------------------------------------
    // HDRP / DEFAULT fallback
    // ---------------------------------------------------------------------
    private static string GetHdrpName(MapTypes type)
    {
        return type switch
        {
            MapTypes.colorMap => "_BaseColorMap",
            MapTypes.maskMap => "_MaskMap",
            MapTypes.detailMap => "_DetailMap",
            MapTypes.normalMap => "_NormalMap",
            MapTypes.defectMap => "_FalseColorTex",
            _ => ""
        };
    }
}