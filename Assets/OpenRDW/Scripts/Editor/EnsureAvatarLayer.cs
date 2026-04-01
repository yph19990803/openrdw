using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class EnsureAvatarLayer
{
    static EnsureAvatarLayer()
    {
        // 检查是否存在Avatar层，如果不存在则创建
        CheckAvatarLayer();
    }

    static void CheckAvatarLayer()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        bool avatarLayerExists = false;
        
        // 检查Avatar层是否已存在
        for (int i = 8; i < 32; i++) // 用户自定义层从8开始
        {
            SerializedProperty layerSP = layers.GetArrayElementAtIndex(i);
            if (layerSP.stringValue == "Avatar")
            {
                avatarLayerExists = true;
                Debug.Log("Avatar层已存在，索引为: " + i);
                break;
            }
        }

        // 如果不存在，找一个空位创建
        if (!avatarLayerExists)
        {
            for (int i = 8; i < 32; i++)
            {
                SerializedProperty layerSP = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layerSP.stringValue))
                {
                    layerSP.stringValue = "Avatar";
                    tagManager.ApplyModifiedProperties();
                    Debug.Log("已创建Avatar层，索引为: " + i);
                    break;
                }
            }
        }
    }
} 