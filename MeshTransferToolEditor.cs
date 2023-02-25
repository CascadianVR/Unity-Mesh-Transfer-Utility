using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
class MeshTransferToolEditor : EditorWindow
{

    public static GameObject Armature;
    public static List<SkinnedMeshRenderer> Mesh_Renderers = new List<SkinnedMeshRenderer>();

    public static Dictionary<string, Transform> boneMap = new Dictionary<string, Transform>();

    private int meshNum = 0;

    [MenuItem("GameObject/TransferMeshes", false, 0)]
    public static void contextMenu(MenuCommand menuCommand)
    {
        Mesh_Renderers = new List<SkinnedMeshRenderer>();
        foreach (var gameObject in Selection.gameObjects)
        {
            if (gameObject.GetComponent<SkinnedMeshRenderer>() == true)
            {
                Mesh_Renderers.Add(gameObject.GetComponent<SkinnedMeshRenderer>());
                Debug.Log("MESH: " + gameObject.name);
            }
            else
            {
                Armature = gameObject.GetComponentsInChildren<Transform>()[1].gameObject;
                Debug.Log("ROOT: " + Armature.name);
            }
        }
        
        MeshRemap();
        
    }
    
    [MenuItem("Cascadian/MeshTransferTool")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        MeshTransferToolEditor window = (MeshTransferToolEditor)EditorWindow.GetWindow(typeof(MeshTransferToolEditor));
        window.Show();
    }
    
    // Put in a map every bones until there is no more children
    static void GetBones(Transform pBone)
    {
        foreach (Transform bone in pBone){
            if (bone.name.Equals("Armature")) { continue; } // skip sub-avatar in main avatar
            boneMap[bone.gameObject.name] = bone;
            GetBones(bone);
        }
    }

    //https://answers.unity.com/questions/44355/shared-skeleton-and-animation-state.html
    static void BoneRemap(SkinnedMeshRenderer Mesh)
    {
        Transform[] newBonesList = new Transform[Mesh.bones.Length];
        for (int j = 0; j < Mesh.bones.Length; ++j)
        {
            Transform tempBone = Mesh.bones[j];
            if (tempBone == null) { continue; }
            GameObject bone = tempBone.gameObject;
            if (!boneMap.TryGetValue(bone.name, out newBonesList[j])) // Check to see if bone exists in bone map
            {
                if (boneMap.TryGetValue(bone.transform.parent.name, out Transform pBone)) // try to find the parent reference in the target armature
                {
                    //add the new bones to the target armature
                    var components = bone.GetComponents(typeof(Component));
                    foreach (var component in components)
                    {
                        //Debug.Log(component.GetComponent(component));
                    }
                    bone.transform.position += pBone.position - bone.transform.parent.position;
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

    static void TransferMeshes(SkinnedMeshRenderer Mesh)
    {
        Mesh.gameObject.transform.SetParent(Armature.gameObject.transform.parent);
        Mesh.rootBone = Armature.gameObject.transform.parent.gameObject.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Hips);
    }

    public void OnGUI()
    {
        GUILayout.Label("Remap Meshes", EditorStyles.largeLabel);

        GUILayout.Space(10f);

        GUILayout.Label("Armature From Target Skeleton:", EditorStyles.boldLabel);
        Armature = (GameObject)EditorGUILayout.ObjectField(Armature, typeof(GameObject), true, GUILayout.Height(25f));

        GUILayout.Space(20f);
        
        { // Meshes
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Meshes to Transfer", EditorStyles.boldLabel);
            GUIStyle customButton = new GUIStyle("button");
            customButton.fontSize = 20;

            if (GUILayout.Button("-", customButton, GUILayout.Width(25f), GUILayout.Height(25f)))
            {
                if (meshNum <= 0) {return;}
                meshNum--;
                Mesh_Renderers.RemoveAt(Mesh_Renderers.Count - 1);
            }

            if (GUILayout.Button("+", customButton, GUILayout.Width(25f), GUILayout.Height(25f)))
            {
                meshNum++;
                Mesh_Renderers.Add(null);
            }

            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < meshNum; i++)
            {
                Mesh_Renderers[i] = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(Mesh_Renderers[i], typeof(SkinnedMeshRenderer), true, GUILayout.Height(30f));
            }
        }

        GUILayout.Space(10f);

        if (GUILayout.Button("Remap Meshes"))
        {
            MeshRemap();
        }
    }

    private static void MeshRemap()
    {
        if (Armature.gameObject.transform.parent.gameObject.GetComponent<Animator>() == null)
        {
            Debug.LogError("Base Armature not Humanoid. Please set rig type to humanoid to proceed.");
        }

        if (Armature.name.Equals("Armature")) //the gameobject must be the armature of target avatar
        {
            GetBones(Armature.transform);

            for (int i = 0; i < Mesh_Renderers.Count; i++)
            {
                SkinnedMeshRenderer Mesh = Mesh_Renderers[i];

                if (Mesh == null)
                {
                    continue;
                }

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
}
#endif
