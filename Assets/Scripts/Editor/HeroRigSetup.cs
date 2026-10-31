using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class HeroRigSetup
{
    private const string ModelPath = "Assets/Art/Hero/Leading/SM_leading_Rigged.fbx";
    private const string MaterialPath = "Assets/Materials/M_Leading.mat";

    private static readonly string[] RequiredLeftRightHint =
    {
        "Hand", "UpperArm", "LowerArm", "UpperLeg", "LowerLeg", "Foot",
    };

    [MenuItem("Tools/Hero/Setup Rigged Hero")]
    public static void Setup()
    {
        ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;

        if (importer == null)
        {
            Debug.LogError("[Hero] 找不到 " + ModelPath + "，先把 2.fbx 放进项目。");
            return;
        }

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.optimizeGameObjects = false;
        importer.SaveAndReimport();

        PatchHands(importer);
        ConfigureClips(importer);
        importer.SaveAndReimport();

        RemapMaterials(importer);
        importer.SaveAndReimport();

        Report();

        Debug.Log("[Hero] Rig 已切到 Humanoid，并把 forearm.*_end 顶替成 Hand。"
                  + "\n下一步：选中 FBX → Rig → Configure，检查 T-pose（手臂是否水平），不对就手动拖一下再 Done。");
    }

    private static void PatchHands(ModelImporter importer)
    {
        HumanDescription description = importer.humanDescription;
        List<HumanBone> bones = description.human != null
            ? new List<HumanBone>(description.human)
            : new List<HumanBone>();

        SetBone(bones, "LeftHand", "forearm.L_end");
        SetBone(bones, "RightHand", "forearm.R_end");

        description.human = bones.ToArray();
        description.hasTranslationDoF = false;
        importer.humanDescription = description;
    }

    private static void SetBone(List<HumanBone> bones, string humanName, string boneName)
    {
        HumanBone existing = bones.FirstOrDefault(b => b.humanName == humanName);

        if (existing.humanName != null)
        {
            existing.boneName = boneName;
            return;
        }

        bones.Add(new HumanBone
        {
            humanName = humanName,
            boneName = boneName,
            limit = new HumanLimit { useDefaultValues = true },
        });
    }

    private static void ConfigureClips(ModelImporter importer)
    {
        ModelImporterClipAnimation[] clips = importer.clipAnimations;

        if (clips == null || clips.Length == 0)
        {
            clips = importer.defaultClipAnimations;
        }

        if (clips == null)
        {
            return;
        }

        for (int i = 0; i < clips.Length; i++)
        {
            bool isRun = clips[i].name == "Run";
            clips[i].loopTime = isRun;
            clips[i].loopPose = isRun;
        }

        importer.clipAnimations = clips;
        Debug.Log("[Hero] 动画循环设置：Run 循环开启，其余保持单次。");
    }

    private static void RemapMaterials(ModelImporter importer)
    {
        Material target = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

        if (target == null)
        {
            Debug.LogWarning("[Hero] 没有 M_Leading，先跑 Tools/Hero/Setup Leading 生成材质。");
            return;
        }

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(ModelPath);
        Material[] imported = assets
            .OfType<Material>()
            .Where(m => AssetDatabase.GetAssetPath(m) == ModelPath)
            .ToArray();

        Debug.Log("[Hero] FBX 自带材质: " + (imported.Length == 0 ? "无" : string.Join(", ", imported.Select(m => m.name))));

        foreach (Material material in imported)
        {
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), material.name), target);
        }
    }

    private static void Report()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(ModelPath);
        Avatar avatar = assets.OfType<Avatar>().FirstOrDefault();
        var clips = assets.OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();

        if (avatar == null)
        {
            Debug.LogError("[Hero] 没有生成 Avatar。");
            return;
        }

        Debug.Log($"[Hero] Avatar: valid={avatar.isValid}, isHuman={avatar.isHuman}");
        Debug.Log("[Hero] 动画 clip: " + string.Join(", ", clips.Select(c => $"{c.name}({c.length:0.00}s)")));

        HumanDescription description = new HumanDescription();
        var importerDescription = AssetImporter.GetAtPath(ModelPath) as ModelImporter;

        if (importerDescription != null)
        {
            description = importerDescription.humanDescription;
        }

        var mapped = description.human.ToDictionary(h => h.humanName, h => h.boneName);
        var missing = new List<string>();

        foreach (string side in new[] { "Left", "Right" })
        {
            foreach (string part in RequiredLeftRightHint)
            {
                string name = side + part;

                if (!mapped.ContainsKey(name) || string.IsNullOrEmpty(mapped[name]))
                {
                    missing.Add(name);
                }
            }
        }

        foreach (string core in new[] { "Hips", "Spine", "Head" })
        {
            if (!mapped.ContainsKey(core) || string.IsNullOrEmpty(mapped[core]))
            {
                missing.Add(core);
            }
        }

        Debug.Log(missing.Count == 0
            ? "[Hero] 必需骨骼映射齐全 ✔"
            : "[Hero] 缺少映射: " + string.Join(", ", missing));

        Debug.Log("[Hero] 手部映射: LeftHand=" + mapped.GetValueOr("LeftHand", "-")
                  + ", RightHand=" + mapped.GetValueOr("RightHand", "-"));
    }

    private static string GetValueOr(this Dictionary<string, string> map, string key, string fallback)
    {
        return map.TryGetValue(key, out string value) && !string.IsNullOrEmpty(value) ? value : fallback;
    }
}
