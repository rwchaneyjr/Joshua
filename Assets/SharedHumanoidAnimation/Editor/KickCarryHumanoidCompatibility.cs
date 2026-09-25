#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SharedHumanoidAnimation.Editor
{
    /// <summary>Converts the two skinned action sources to validated Humanoid imports for existing controller states.</summary>
    public static class KickCarryHumanoidCompatibility
    {
        private static readonly string[] SourcePaths =
        {
            "Assets/CHARACTERS/Meshy_AI_Ancient_Levantine_War_biped_Animation_High_Kick_withSkin.fbx",
            "Assets/CHARACTERS/Meshy_AI_Desert_Sentinel_biped_Animation_Carry_Heavy_Cannon_Forward_withSkin.fbx"
        };

        [MenuItem("Tools/Shared Humanoid Animation/Resolve Kick and Carry Humanoid Compatibility")]
        public static void ResolveAndValidate()
        {
            foreach (string sourcePath in SourcePaths)
            {
                ModelImporter importer = AssetImporter.GetAtPath(sourcePath) as ModelImporter;
                if (importer == null)
                {
                    throw new InvalidOperationException("Missing action source: " + sourcePath);
                }

                // The models carry the Mixamo skeleton, so create and validate their own Humanoid Avatars.
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();

                importer = AssetImporter.GetAtPath(sourcePath) as ModelImporter;
                Avatar avatar = LoadHumanoidAvatar(sourcePath);
                if (importer == null || importer.animationType != ModelImporterAnimationType.Human || !avatar.isHuman)
                {
                    throw new InvalidOperationException(sourcePath + " could not be validated as a Humanoid animation source.");
                }

                Debug.Log("Validated Humanoid action source: " + sourcePath, avatar);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Kick and Carry Humanoid compatibility resolved.");
        }

        private static Avatar LoadHumanoidAvatar(string sourcePath)
        {
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isHuman)
            {
                throw new InvalidOperationException(sourcePath + " did not produce a valid Humanoid Avatar.");
            }

            return avatar;
        }
    }
}
#endif