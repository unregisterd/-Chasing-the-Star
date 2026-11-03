using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MenuFontSetup
{
    private const string FontFolder = "Assets/Art/Fonts";
    private const string ChineseFontPath = FontFolder + "/ZaoziGongfang.ttf";
    private const string LatinFontPath = FontFolder + "/AG_Benguiat.ttf";
    private const string ChineseAssetPath = FontFolder + "/ZaoziGongfang SDF.asset";
    private const string LatinAssetPath = FontFolder + "/AG_Benguiat SDF.asset";
    private const string FallbackFontPath = "Assets/Resources/Fonts/SourceHanSansCN-VF-2 SDF.asset";
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";

    [MenuItem("Tools/Menu/Setup Title Fonts")]
    public static void Setup()
    {
        Font chineseFont = AssetDatabase.LoadAssetAtPath<Font>(ChineseFontPath);
        Font latinFont = AssetDatabase.LoadAssetAtPath<Font>(LatinFontPath);

        if (chineseFont == null || latinFont == null)
        {
            Debug.LogError("[MenuFont] 找不到字体文件：" + ChineseFontPath + " / " + LatinFontPath);
            return;
        }

        TMP_FontAsset fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontPath);
        TMP_FontAsset chinese = CreateOrLoad(chineseFont, ChineseAssetPath, fallback);
        TMP_FontAsset latin = CreateOrLoad(latinFont, LatinAssetPath, fallback);

        AssignToMenu(chinese, latin);

        Debug.Log("[MenuFont] 标题字体已生成：\n  " + ChineseAssetPath + "\n  " + LatinAssetPath
                  + "\n已自动挂到主菜单标题（中文用造字工房，英文用 AG_Benguiat）。");
    }

    private static TMP_FontAsset CreateOrLoad(Font font, string assetPath, TMP_FontAsset fallback)
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);

        if (existing != null)
        {
            ApplyFallback(existing, fallback);
            return existing;
        }

        TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(
            font,
            90,
            9,
            UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
            1024,
            1024,
            AtlasPopulationMode.Dynamic,
            true);

        asset.name = Path.GetFileNameWithoutExtension(assetPath);
        AssetDatabase.CreateAsset(asset, assetPath);

        if (asset.material != null)
        {
            asset.material.name = asset.name + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
        }

        if (asset.atlasTextures != null)
        {
            for (int i = 0; i < asset.atlasTextures.Length; i++)
            {
                if (asset.atlasTextures[i] == null)
                {
                    continue;
                }

                asset.atlasTextures[i].name = asset.name + " Atlas " + i;
                AssetDatabase.AddObjectToAsset(asset.atlasTextures[i], asset);
            }
        }

        ApplyFallback(asset, fallback);
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        return asset;
    }

    private static void ApplyFallback(TMP_FontAsset asset, TMP_FontAsset fallback)
    {
        if (fallback == null)
        {
            return;
        }

        if (asset.fallbackFontAssetTable == null)
        {
            asset.fallbackFontAssetTable = new List<TMP_FontAsset>();
        }

        if (!asset.fallbackFontAssetTable.Contains(fallback))
        {
            asset.fallbackFontAssetTable.Add(fallback);
        }

        EditorUtility.SetDirty(asset);
    }

    private static void AssignToMenu(TMP_FontAsset chinese, TMP_FontAsset latin)
    {
        Scene scene = EditorSceneManager.GetActiveScene();

        if (scene.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        GameObject canvas = GameObject.Find("Menu Canvas");

        if (canvas == null)
        {
            Debug.LogWarning("[MenuFont] 找不到 Menu Canvas，字体已生成但未挂载。");
            return;
        }

        MainMenuManager manager = canvas.GetComponent<MainMenuManager>();

        if (manager == null)
        {
            return;
        }

        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("titleFontZh").objectReferenceValue = chinese;
        so.FindProperty("titleFontEn").objectReferenceValue = latin;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
    }
}
