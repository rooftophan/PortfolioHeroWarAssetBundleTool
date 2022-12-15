using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;

public class AssetResSelectUploadInfo
{
    public bool isSelect = true;
    public string bundlename;

}

public class AssetResSelectUploadWindow : EditorWindow
{
    public static EditorWindow ShowWindow()
    {
        return EditorWindow.GetWindow(typeof(AssetResSelectUploadWindow));
    }

    #region Variables

    public List<AssetResSelectUploadInfo> uploadResList = new List<AssetResSelectUploadInfo>();

    public Action onUpload = null;

    Vector2 _scroll;

    #endregion

    #region Methods

    private void Awake()
    {
        InitAssetResSelectUpload();
    }

    void InitAssetResSelectUpload()
    {
        
    }

    public void SetPatchList(List<string> patchList)
    {
        uploadResList.Clear();
        if (patchList == null || patchList.Count == 0)
            return;

        for(int i = 0;i< patchList.Count; i++) {
            AddUploadResList(patchList[i]);
        }
    }

    public void AddUploadResList(string bundlename)
    {
        AssetResSelectUploadInfo inputResUpload = new AssetResSelectUploadInfo();
        inputResUpload.bundlename = bundlename;
        uploadResList.Add(inputResUpload);
    }

    public List<string> GetSelectPatchList()
    {
        List<string> retValue = new List<string>();

        for(int i = 0;i< uploadResList.Count; i++) {
            if (uploadResList[i].isSelect) {
                Debug.Log(string.Format("GetSelectPatchList name : {0}", uploadResList[i].bundlename));
                retValue.Add(uploadResList[i].bundlename);
            }
        }

        return retValue;
    }

    void OnGUI()
    {

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        for (int i = 0;i< uploadResList.Count; i++) {
            EditorGUILayout.BeginHorizontal();

            uploadResList[i].isSelect = EditorGUILayout.Toggle(uploadResList[i].isSelect);
            GUILayout.Label(uploadResList[i].bundlename, EditorStyles.boldLabel);

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Upload")) {
            if(onUpload != null) {
                onUpload();
            }
            this.Close();
        }
    }

    #endregion
}

#endif
