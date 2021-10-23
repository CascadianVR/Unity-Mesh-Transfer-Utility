using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Configuration;

#if UNITY_EDITOR
class SkinnedMeshBoneRemapperEditor : EditorWindow
{

    public SkinnedMeshRenderer OldBones;
    public SkinnedMeshRenderer[] Mesh_Renderers;

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
        for (int i = 0; i < Mesh_Renderers.Length; i++)
        {
            if (Mesh_Renderers[i] == null) { continue; }

            Dictionary<string, Transform> boneMap = new Dictionary<string, Transform>();
            foreach (Transform bone in OldBones.bones)
                boneMap[bone.gameObject.name] = bone;

            Transform[] newBonesList = new Transform[Mesh_Renderers[i].bones.Length];
            for (int j = 0; j < Mesh_Renderers[i].bones.Length; ++j)
            {
                GameObject bone = Mesh_Renderers[i].bones[j].gameObject;
                if (!boneMap.TryGetValue(bone.name, out newBonesList[j]))
                {
                    Debug.Log("Unable to map bone \"" + bone.name + "\" to target skeleton.");
                    break;
                }
            }

            Mesh_Renderers[i].bones = newBonesList;
        }
    }

    void TransferMeshes()
    {
        if (OldBones.gameObject.transform.parent.gameObject.GetComponent<Animator>() == null) { Debug.LogError("Base Armature not Humanoid. Please set rig type to humanoid to proceed."); }
        for (int i = 0; i < Mesh_Renderers.Length; i++)
        {
            if (Mesh_Renderers[i] == null) { continue; }
            if (PrefabUtility.GetPrefabInstanceStatus(Mesh_Renderers[i].transform.parent.gameObject) == PrefabInstanceStatus.Connected)
            PrefabUtility.UnpackPrefabInstance(Mesh_Renderers[i].transform.parent.gameObject, unpackMode: PrefabUnpackMode.Completely,  action: InteractionMode.AutomatedAction);
            Mesh_Renderers[i].gameObject.transform.SetParent(OldBones.gameObject.transform.parent);
            Mesh_Renderers[i].rootBone = OldBones.gameObject.transform.parent.gameObject.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Hips);
        }
    }

    public void OnGUI()
    {
        GUILayout.Label("Remap Meshes", EditorStyles.boldLabel);

        GUILayout.Space(10f);

        GUILayout.Label("Body Mesh From Target Skeleton:", EditorStyles.boldLabel);
        OldBones = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(OldBones, typeof(SkinnedMeshRenderer), true);

        GUILayout.Space(10f);

        ScriptableObject target = this;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty bonesProperty = so.FindProperty("Mesh_Renderers");

        GUILayout.Label("Meshes To Bind to Target Skeleton:", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(bonesProperty, true);

        GUILayout.Space(10f);

        if (GUILayout.Button("Remap Meshes"))
        {
            BoneRemap();
            TransferMeshes();
        }

        so.ApplyModifiedProperties();

    }
}
#endif
