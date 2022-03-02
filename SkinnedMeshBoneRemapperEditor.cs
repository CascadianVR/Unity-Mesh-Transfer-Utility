using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Configuration;

#if UNITY_EDITOR
class SkinnedMeshBoneRemapperEditor : EditorWindow
{

    public GameObject Armature;
    public SkinnedMeshRenderer[] Mesh_Renderers;

    public Dictionary<string, Transform> boneMap = new Dictionary<string, Transform>();

    [MenuItem("Tools/Cascadian/SkinnedMeshBoneRemapper")]

    static void Init()
    {
        // Get existing open window or if none, make a new one:
        SkinnedMeshBoneRemapperEditor window = (SkinnedMeshBoneRemapperEditor)EditorWindow.GetWindow(typeof(SkinnedMeshBoneRemapperEditor));
        window.Show();
    }

    /**
     * Put in a map every bones until there is no more children
     */
    void GetBones(Transform pBone)
    {
        foreach (Transform bone in pBone){
            if (bone.name.Equals("Armature")) { continue; } // skip sub-avatar in main avatar
            boneMap[bone.gameObject.name] = bone;
            GetBones(bone);
        }
    }

    //https://answers.unity.com/questions/44355/shared-skeleton-and-animation-state.html
    void BoneRemap(SkinnedMeshRenderer Mesh)
    {
        Transform[] newBonesList = new Transform[Mesh.bones.Length];
        for (int j = 0; j < Mesh.bones.Length; ++j)
        {
            Transform tempBone = Mesh.bones[j];
            if (tempBone == null) { continue; }
            GameObject bone = tempBone.gameObject;
            if (!boneMap.TryGetValue(bone.name, out newBonesList[j]))
            {
                if (boneMap.TryGetValue(bone.transform.parent.name, out Transform pBone))// try to find the parent reference in the target armature
                {
                    //add the new bones to the target armature
                    bone.transform.SetParent(pBone);
                    GetBones(pBone.transform);
                    boneMap.TryGetValue(bone.name, out newBonesList[j]);
                }
                else
                {
                    Debug.Log("Unable to map bone \"" + bone.name + "\" to target skeleton.");
                }
            }
        }

        Mesh.bones = newBonesList;
    }

    void TransferMeshes(SkinnedMeshRenderer Mesh)
    {
        Mesh.gameObject.transform.SetParent(Armature.gameObject.transform.parent);
        Mesh.rootBone = Armature.gameObject.transform.parent.gameObject.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Hips);
    }

    public void OnGUI()
    {
        GUILayout.Label("Remap Meshes", EditorStyles.boldLabel);

        GUILayout.Space(10f);

        GUILayout.Label("Armature From Target Skeleton:", EditorStyles.boldLabel);
        Armature = (GameObject)EditorGUILayout.ObjectField(Armature, typeof(GameObject), true);

        GUILayout.Space(10f);

        ScriptableObject target = this;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty bonesProperty = so.FindProperty("Mesh_Renderers");

        GUILayout.Label("Meshes To Bind to Target Skeleton:", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(bonesProperty, true);

        GUILayout.Space(10f);

        if (GUILayout.Button("Remap Meshes"))
        {
            if (Armature.gameObject.transform.parent.gameObject.GetComponent<Animator>() == null) { Debug.LogError("Base Armature not Humanoid. Please set rig type to humanoid to proceed."); }
            if (Armature.name.Equals("Armature"))//the gameobject must be the armature of target avatar
            {
                GetBones(Armature.transform);

                for (int i = 0; i < Mesh_Renderers.Length; i++)
                {
                    SkinnedMeshRenderer Mesh = Mesh_Renderers[i];

                    if (Mesh == null) { continue; }
                    if (PrefabUtility.GetPrefabInstanceStatus(Mesh.transform.parent.gameObject) == PrefabInstanceStatus.Connected) //unpack prefab to move mesh and bones to the target avatar
                    {
                        PrefabUtility.UnpackPrefabInstance(Mesh.transform.parent.gameObject, unpackMode: PrefabUnpackMode.Completely, action: InteractionMode.AutomatedAction);
                    }

                    BoneRemap(Mesh);
                    TransferMeshes(Mesh);
                }
            }
            else
            {
                Debug.LogError("Please select the \"Armature\" of target skeleton.");
            }
        }

        so.ApplyModifiedProperties();

    }
}
#endif
