using UnityEngine;
using System.Collections;

public class AvatarLayerManager : MonoBehaviour
{
    private void Awake()
    {
        // 检查Avatar层是否存在
        int avatarLayer = LayerMask.NameToLayer("Avatar");
        if (avatarLayer == -1)
        {
            Debug.LogError("Avatar层不存在！请在Unity编辑器中添加Avatar层。");
        }
        else
        {
            Debug.Log("Avatar层已存在，索引为: " + avatarLayer);
        }
    }
    
    // 在运行时设置GameObject到Avatar层
    public static void SetGameObjectToAvatarLayer(GameObject obj)
    {
        int avatarLayer = LayerMask.NameToLayer("Avatar");
        if (avatarLayer != -1)
        {
            SetLayerRecursively(obj, avatarLayer);
        }
    }
    
    // 递归设置物体及其子物体的Layer
    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
} 