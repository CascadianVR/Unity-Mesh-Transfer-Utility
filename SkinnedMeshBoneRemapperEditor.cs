using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
class SkinnedMeshBoneRemapperEditor : EditorWindow
{

    public SkinnedMeshRenderer OldBones;
    public SkinnedMeshRenderer NewBones;

    [MenuItem("Tools/Cascadian/SkinnedMeshBoneRemapper")]

    static void Init()
    {
        // Get existing open window or if none, make a new one:
        SkinnedMeshBoneRemapperEditor window = (SkinnedMeshBoneRemapperEditor)EditorWindow.GetWindow(typeof(SkinnedMeshBoneRemapperEditor));
        window.Show();
    }

    //https://answers.unity.com/questions/44355/shared-skeleton-and-animation-state.html
    void BoneRemap()
    {

        Dictionary<string, Transform> boneMap = new Dictionary<string, Transform>();
        foreach (Transform bone in OldBones.bones)
            boneMap[bone.gameObject.name] = bone;

        Transform[] newBonesList = new Transform[NewBones.bones.Length];
        for (int i = 0; i < NewBones.bones.Length; ++i)
        {
            GameObject bone = NewBones.bones[i].gameObject;
            if (!boneMap.TryGetValue(bone.name, out newBonesList[i]))
            {
                Debug.Log("Unable to map bone \"" + bone.name + "\" to target skeleton.");
                break;
            }
        }

        NewBones.bones = newBonesList;

    }

    public void OnGUI()
    {
        GUILayout.Label("Remap Bones", EditorStyles.boldLabel);

        GUILayout.Space(10f);

        GUILayout.Label("Any mesh from target skeleton to copy from:", EditorStyles.boldLabel);
        OldBones = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(OldBones, typeof(SkinnedMeshRenderer), true);

        GUILayout.Label("Mesh to copy new bones to:", EditorStyles.boldLabel);
        NewBones = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(NewBones, typeof(SkinnedMeshRenderer), true);
;

        if (GUILayout.Button("Remap Bones"))
        {
            BoneRemap();
        }
    }
}
#endif
