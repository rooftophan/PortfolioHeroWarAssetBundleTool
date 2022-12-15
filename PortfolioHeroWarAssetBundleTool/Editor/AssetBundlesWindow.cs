using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Text;

#if UNITY_EDITOR
using UnityEditor;

public class AssetBundleWindowEvent
{
    Action _actionEvent;

    public AssetBundleWindowEvent(Action actionEvent)
    {
        _actionEvent = actionEvent;
    }

    public void Execute()
    {
        if (_actionEvent != null) {
            _actionEvent();
        }
    }
}

public class AssetBundlesWindow : EditorWindow
{
    #region Variables

    const string _assetBundlesSettingFile = "AssetBundlesSetting.dat";

    BuildAssetBundleOptions _assetBundleOptions =  BuildAssetBundleOptions.ChunkBasedCompression; // BuildAssetBundleOptions.ChunkBasedCompression;

    string _assetBundlePath = null;

    string _assetBundleBranchesPath = "";

    string _ftpPW = "";

    AssetBundlesSettingInfo _assetBundlesSetting = null;

    AssetFTPUploaderManager _ftpUploaderManager = new AssetFTPUploaderManager();

    string _resVersionStr = "";

    string _sheetVerStr = "";

    string _addAppVersionStr = "";
    string _removeAppVersionStr = "";
    string _selectAppVersionStr = "";

    bool _isTotalResourceBuild = false;
    bool _isTotalSheetBuild = false;
    bool _isTotalAssetBundleBuild = false;

    bool _isProgressBarState = false;

    BundleToolTargetSetting _curTargetSettingInfo = null;
    AssetBundlePatchInfoData _bundlePatchInfoDataInfo = null;
    AssetBundlesAppVersionInfo _curBundleAppversionInfo = null;

    Queue<AssetBundleWindowEvent> _bundleEvents = new Queue<AssetBundleWindowEvent>();

    Dictionary<string, AssetBundlePatchData> _comparePatchInfoList = new Dictionary<string, AssetBundlePatchData>();
    Dictionary<string, AssetBundlePatchData> _compareEachSheetPatchInfoList = new Dictionary<string, AssetBundlePatchData>();
    List<string> _patchAssetBundleList = new List<string>();
    List<string> _patchEachAssetBundleList = new List<string>();

    string _resRemoveVersion = "";
    string _sheetRemoveVersion = "";

    bool _isCompressPatchInfo = true;

    Vector2 _scroll;

    #endregion

    #region Methods

    [MenuItem("Build/AssetBundlesWindow")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(AssetBundlesWindow));
    }

    private void Awake()
    {
        InitAssetBundles();
        AssetBundleBuildManager.Init();

        _ftpUploaderManager.OnFTPUploadState = OnFTPUploadState;
    }

    private void OnEnable()
    {
        if(_assetBundlesSetting == null) {
            InitAssetBundles();
            AssetBundleBuildManager.Init();

            _ftpUploaderManager.OnFTPUploadState = OnFTPUploadState;
        }

    }

    private void OnDestroy()
    {
        _assetBundlePath = null;
        _assetBundlesSetting = null;
        AssetBundleBuildManager.Release();
    }

    private void OnGUI()
    {
        if (_assetBundlesSetting == null)
            return;

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        // FGT Settings
        OnFGTSettingGUI();

        int saveAssetBundleTarget = _assetBundlesSetting.assetBundleTarget;
        int saveBuildKind = _assetBundlesSetting.buildKind;

        // Build Target
        GUILayout.Label("Build Target", EditorStyles.boldLabel);
        string[] targetOptions = new string[]
        {
            "Android", "IOS"
        };
        _assetBundlesSetting.assetBundleTarget = EditorGUILayout.Popup("Platform", _assetBundlesSetting.assetBundleTarget, targetOptions);

        string[] buildKindOptions = new string[]
        {
            "Daily", "Dev", "ENT", "FGT", "INAPP", "LIVE"
        };
        _assetBundlesSetting.buildKind = EditorGUILayout.Popup("BuildKind", _assetBundlesSetting.buildKind, buildKindOptions);

        OnAppVersionGUI();

        if (_assetBundlesSetting.assetBundleTarget != saveAssetBundleTarget ||
            _assetBundlesSetting.buildKind != saveBuildKind) {
            _curTargetSettingInfo = GetBundleTargetSettingInfo((AssetBundleTarget)_assetBundlesSetting.assetBundleTarget, (AssetBundleBuildKind)_assetBundlesSetting.buildKind);

            InitAssetVersionInfo();
        }

        if(_curBundleAppversionInfo == null)
            return;

        // Version Kind
        GUILayout.Label("Version Kind", EditorStyles.boldLabel);
        string[] versionKindOptions = new string[]
        {
            "StreamingAssetBuild", "Resource Or Sheet Patch Up"
        };
        _curBundleAppversionInfo.versionKind = EditorGUILayout.Popup("VersionKind", _curBundleAppversionInfo.versionKind, versionKindOptions);

        // Resource Build
        GUILayout.Label("Resource Build", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("StreamingAssetVer", _curBundleAppversionInfo.streamingAssetResVer);
        EditorGUILayout.LabelField("PatchUp Version List", _curBundleAppversionInfo.GetPatchUpResVerListStr());

        EditorGUILayout.BeginHorizontal();

        _resRemoveVersion = EditorGUILayout.TextField("Remove Version", _resRemoveVersion);
        if (GUILayout.Button("Remove Version")) {
            _curBundleAppversionInfo.RemoveResVerList(_resRemoveVersion);

            SaveAssetBundleSetting();
        }

        EditorGUILayout.EndHorizontal();

        _resVersionStr = EditorGUILayout.TextField("Resource Version", _resVersionStr);
        if (GUILayout.Button("Increase Resource Version")) {
            OnIncreaseResVerions();
        }

        if (GUILayout.Button("Build Resource AssetBundle")) {
            if(_curBundleAppversionInfo.versionKind == (int)AssetBundleVersionKind.StreamingAssetBuild) {
                if (EditorUtility.DisplayDialog("AssetBundle",
                        "리소스 \"통빌드\" 하시겠습니까?", "OK", "Cancel")) {
                    OnBuildResAssetBundle();
                }
            } else if(_curBundleAppversionInfo.versionKind == (int)AssetBundleVersionKind.ResOrSheetPatchUp) {
                if (EditorUtility.DisplayDialog("AssetBundle",
                        "리소스 \"패치 진행\" 하시겠습니까?", "OK", "Cancel")) {
                    OnBuildResAssetBundle();
                }
            }
        }

        GUILayout.Label("Sheet Build", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("StreamingAssetVer", _curBundleAppversionInfo.streamingAssetSheetVer);
        EditorGUILayout.LabelField("PatchUp Version List", _curBundleAppversionInfo.GetPatchUpSheetVerListStr());

        EditorGUILayout.BeginHorizontal();

        _sheetRemoveVersion = EditorGUILayout.TextField("Remove Version", _sheetRemoveVersion);
        if (GUILayout.Button("Remove Version")) {
            _curBundleAppversionInfo.RemoveSheetVerList(_sheetRemoveVersion);

            SaveAssetBundleSetting();
        }

        EditorGUILayout.EndHorizontal();

        _sheetVerStr = EditorGUILayout.TextField("Sheet Version", _sheetVerStr);
        if (GUILayout.Button("Increase Sheet Version")) {
            OnIncreaseSheetVersion();
        }

        if (GUILayout.Button("Build Sheet AssetBundle")) {
            if (_curBundleAppversionInfo.versionKind == (int)AssetBundleVersionKind.StreamingAssetBuild) {
                if (EditorUtility.DisplayDialog("AssetBundle",
                        "Sheet(기획데이터) \"통빌드\" 하시겠습니까?", "OK", "Cancel")) {
                    OnBuildSheetAssetBundle();
                }
            } else if (_curBundleAppversionInfo.versionKind == (int)AssetBundleVersionKind.ResOrSheetPatchUp) {
                if (EditorUtility.DisplayDialog("AssetBundle",
                        "Sheet(기획데이터) \"패치 진행\" 하시겠습니까?", "OK", "Cancel")) {
                    OnBuildSheetAssetBundle();
                }
            }
        }

        GUILayout.Label("Total AssetBundle Build & FTP Upload", EditorStyles.boldLabel);
        if (GUILayout.Button("Increase Resources & Sheet Version")) {
            OnIncreaseResAndSheetVersion();
        }

        if (GUILayout.Button("AssetBundle Resource & Sheet Build")) {
            if (_curBundleAppversionInfo.versionKind == (int)AssetBundleVersionKind.StreamingAssetBuild) {
                if (EditorUtility.DisplayDialog("AssetBundle",
                        "리소스, Sheet(기획데이터) \"통빌드\" 하시겠습니까?", "OK", "Cancel")) {
                    OnBuildAssetBundleResAndSheet();
                }
            } else if (_curBundleAppversionInfo.versionKind == (int)AssetBundleVersionKind.ResOrSheetPatchUp) {
                if (EditorUtility.DisplayDialog("AssetBundle",
                        "리소스, Sheet(기획데이터) \"패치 진행\" 하시겠습니까?", "OK", "Cancel")) {
                    OnBuildAssetBundleResAndSheet();
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    void OnFGTSettingGUI()
    {
        GUILayout.Label("FTP Settings", EditorStyles.boldLabel);
        _assetBundlesSetting.ftpURL = EditorGUILayout.TextField("FTP URL", _assetBundlesSetting.ftpURL);
        _assetBundlesSetting.ftpID = EditorGUILayout.TextField("FTP ID", _assetBundlesSetting.ftpID);
    }

    void OnAppVersionGUI()
    {
        GUILayout.Label("AppVersion", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        _addAppVersionStr = EditorGUILayout.TextField("Add App Version", _addAppVersionStr);
        if (GUILayout.Button("Add AppVersion")) {
            if (_curTargetSettingInfo != null) {
                AssetBundlesAppVersionInfo appVersionInfo = _curTargetSettingInfo.AddAppVersion(_addAppVersionStr);
                if (appVersionInfo != null) {
                    appVersionInfo.resourceVersion = _curTargetSettingInfo.GetLastResVersion();
                    appVersionInfo.sheetVersion = _curTargetSettingInfo.GetLastSheetVersion();
                    if (_curBundleAppversionInfo == null) {
                        InitAssetVersionInfo();
                    }

                    SaveAssetBundleSetting();
                } else {
                    if (EditorUtility.DisplayDialog("AssetBundle",
                        "Not Valid AppVersion", "OK")) {

                    }
                }
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        _removeAppVersionStr = EditorGUILayout.TextField("Remove App Version", _removeAppVersionStr);
        if (GUILayout.Button("Remove AppVersion")) {
            if(_curTargetSettingInfo != null) {
                if(_curTargetSettingInfo.RemoveAppVersionInfo(_removeAppVersionStr)) {
                    if (_curBundleAppversionInfo != null && _curBundleAppversionInfo.appVersion == _removeAppVersionStr) {
                        _curBundleAppversionInfo = null;
                    }
                }
            }

            SaveAssetBundleSetting();
        }

        EditorGUILayout.EndHorizontal();

        if (_curTargetSettingInfo != null && _curTargetSettingInfo.appVersionInfos != null && _curTargetSettingInfo.appVersionInfos.Count > 0) {
            string[] appVersions = _curTargetSettingInfo.GetAppVersionArray();

            _curTargetSettingInfo.curAppVersionIndex = EditorGUILayout.Popup("Appversion", _curTargetSettingInfo.curAppVersionIndex, appVersions);
            if (_curBundleAppversionInfo != _curTargetSettingInfo.GetCurAppVersionInfo()) {
                InitAssetVersionInfo();
            }
        }
    }

    void IncreaseResourcesVersion()
    {
        _curBundleAppversionInfo.resourceVersion = _curTargetSettingInfo.GetLastResVersion();
        _resVersionStr = string.Format("{0}", _curBundleAppversionInfo.resourceVersion);
    }

    void IncreaseSheetVersion()
    {
        _curBundleAppversionInfo.sheetVersion = _curTargetSettingInfo.GetLastSheetVersion();
        _sheetVerStr = string.Format("{0}", _curBundleAppversionInfo.sheetVersion);
    }

    void BuildResourceAssetBundles()
    {
        int resVersion = 0;
        bool isComplete = false;
        if (int.TryParse(_resVersionStr, out resVersion)) {
            _curBundleAppversionInfo.resourceVersion = resVersion;

            BuildTarget target = BuildTarget.Android;
            if (_assetBundlesSetting.assetBundleTarget == (int)AssetBundleTarget.Android) {
                target = BuildTarget.Android;
            } else if (_assetBundlesSetting.assetBundleTarget == (int)AssetBundleTarget.iOS) {
                target = BuildTarget.iOS;
            }

            isComplete = AssetBundleBuildManager.BuildResAssetBundles(AssetBundleUtil.originBundlePath, _assetBundleOptions, target, _assetBundlesSetting);
        } else {
            if (EditorUtility.DisplayDialog("AssetBundle",
               "Resource Version. Please enter numbers only.", "OK")) {

            }
            return;
        }

        if (isComplete) {
            int patchCount = 0;
            if (_curBundleAppversionInfo.versionKind == (int)AssetBundleVersionKind.ResOrSheetPatchUp) {
                patchCount = _curBundleAppversionInfo.GetResPatchCount(_resVersionStr);
            }
            EditorCoroutine.start(UpdateMakePatchInfo("Resources", _resVersionStr, patchCount, _comparePatchInfoList, _patchAssetBundleList, OnCompleteResBuildAssetBundle));
        } else {
            if (EditorUtility.DisplayDialog("AssetBundle",
                   "Fail Build Resource AssetBundles", "OK")) {

            }
        }   
    }

    void SetAssetBundleBranchesPath()
    {
        string rootProjectPath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/'));
        rootProjectPath = rootProjectPath.Substring(0, rootProjectPath.LastIndexOf('/'));
        _assetBundleBranchesPath = rootProjectPath.Substring(0, rootProjectPath.LastIndexOf('/') + 1) + "AssetBundleBranches/";
    }

    void LoadCompareResAssetBundlePatchInfo(string assetType, string compareVersionStr, Dictionary<string, AssetBundlePatchData> comparePatchInfoList)
    {
        string bundleTarget = ((AssetBundleTarget)_assetBundlesSetting.assetBundleTarget).ToString();
        string buildKind = ((AssetBundleBuildKind)_assetBundlesSetting.buildKind).ToString();
        string patchInfoFileName = string.Format(AssetBundleUtil.bundlePatchInfoFile, compareVersionStr);
        string patchInfoPath = _assetBundleBranchesPath + string.Format("{0}/{1}/AppVer_{2}/{3}/{4}/{5}", bundleTarget, buildKind, _curBundleAppversionInfo.appVersion, assetType, compareVersionStr, patchInfoFileName);

        comparePatchInfoList.Clear();
        AssetBundlePatchInfoData patchInfoData = null;
        if (assetType != "SheetEach") {
            patchInfoData = AssetBundleJson.LoadBundlePatchInfo(patchInfoPath, _isCompressPatchInfo);
        } else {
            patchInfoData = AssetBundleJson.LoadBundlePatchInfo(patchInfoPath, false);
        }
        if(patchInfoData != null) {
            if(patchInfoData.datas != null && patchInfoData.datas.Length > 0) {
                for(int i = 0;i< patchInfoData.datas.Length;i++) {
                    comparePatchInfoList.Add(patchInfoData.datas[i].assetName, patchInfoData.datas[i]);
                }
            }
        } else {
            if(assetType != "SheetEach") {
                string errorLog = string.Format("Not Exist PatchInfo : {0} !!", patchInfoPath);
                if (EditorUtility.DisplayDialog("AssetBundle",
                       errorLog, "OK")) {

                }
                return;
            }
        }

        if(_bundleEvents.Count > 0)
            _bundleEvents.Dequeue().Execute();
    }

    void CopyResBundlesToStreamingAsset()
    {
        string assetType = "Resources";
        string destPath = Application.dataPath + "/StreamingAssets/" + string.Format("AssetBundles/{0}/", assetType);
        BundleFile.DeleteFileAndDirectory(destPath);
        EditorCoroutine.start(UpdateCopyBundleToDestPath(assetType, _resVersionStr, destPath, OnCompleteResBundleStreamingCopy));
    }

    void CopyBundlesToAssetBundleBranches(string assetType, string versionStr)
    {
        string bundleTarget = ((AssetBundleTarget)_assetBundlesSetting.assetBundleTarget).ToString();
        string buildKind = ((AssetBundleBuildKind)_assetBundlesSetting.buildKind).ToString();

        string destPath = _assetBundleBranchesPath + string.Format("{0}/{1}/AppVer_{2}/{3}/{4}/", bundleTarget, buildKind, _curBundleAppversionInfo.appVersion, assetType, versionStr);
        BundleFile.DeleteFileAndDirectory(destPath);
        if (_curBundleAppversionInfo.versionKind == (int)AssetBundleVersionKind.StreamingAssetBuild) {
            EditorCoroutine.start(UpdateCopyBundleToDestPath(assetType, versionStr, destPath, OnCompleteResBundleCopy, true));
        } else if(_curBundleAppversionInfo.versionKind == (int)AssetBundleVersionKind.ResOrSheetPatchUp) {
            if(assetType != "SheetEach") {
                EditorCoroutine.start(UpdateCopyPatchBundleToDestPath(assetType, versionStr, destPath, _patchAssetBundleList, OnCompleteResBundleCopy));
            } else {
                EditorCoroutine.start(UpdateCopyPatchBundleToDestPath(assetType, versionStr, destPath, _patchEachAssetBundleList, OnCompleteResBundleCopy));
            }
        } else {
            OnCompleteResBundleCopy(true);
        }
    }

    void CopySheetBundlesToStreamingAsset()
    {
        string assetType = "Sheet";
        string destPath = Application.dataPath + "/StreamingAssets/" + string.Format("AssetBundles/{0}/", assetType);
        BundleFile.DeleteFileAndDirectory(destPath);
        EditorCoroutine.start(UpdateCopyBundleToDestPath(assetType, _sheetVerStr, destPath, OnCompleteSheetBundleStreamingCopy));
    }

    void OnCompleteResBundleStreamingCopy(bool isSuccess)
    {
        if (isSuccess) {
            _curBundleAppversionInfo.streamingAssetResVer = _resVersionStr;

            SaveAssetBundleSetting();

            if (_bundleEvents.Count > 0) {
                _bundleEvents.Dequeue().Execute();
            } else {
                if (EditorUtility.DisplayDialog("AssetBundle",
                       "Finish Build Resource AssetBundles", "OK")) {

                }
            }
        }
    }

    void OnCompleteResBundleCopy(bool isSuccess)
    {
        if(isSuccess) {
            if (_bundleEvents.Count > 0) {
                _bundleEvents.Dequeue().Execute();
            } else {
                if (EditorUtility.DisplayDialog("AssetBundle",
                       "Finish Build Resource AssetBundles", "OK")) {

                }
            }
        }
    }

    void OnCompleteSheetBundleStreamingCopy(bool isSuccess)
    {
        if (isSuccess) {
            _curBundleAppversionInfo.streamingAssetSheetVer = _sheetVerStr;

            SaveAssetBundleSetting();

            if (_bundleEvents.Count > 0) {
                _bundleEvents.Dequeue().Execute();
            } else {
                StringBuilder sb = new StringBuilder();
                sb.Append("Finish Build Sheet AssetBundles");
                for(int i = 0;i< _patchEachAssetBundleList.Count; i++) {
                    if(i == 0) {
                        sb.Append("\n");
                    } else {
                        sb.Append(", ");
                    }
                    sb.Append(_patchEachAssetBundleList[i]);
                }
                if (EditorUtility.DisplayDialog("AssetBundle",
                       sb.ToString(), "OK")) {

                }
            }
        }
    }

    void OnCompleteSheetBundleCopy(bool isSuccess)
    {
        if (isSuccess) {
            if (_bundleEvents.Count > 0) {
                _bundleEvents.Dequeue().Execute();
            } else {
                if (EditorUtility.DisplayDialog("AssetBundle",
                       "Finish Build Sheet AssetBundles", "OK")) {

                }
            }
        }
    }

    void BuildSheetAssetBundles()
    {
        int sheetVer = 0;
        bool isComplete = false;
        if (int.TryParse(_sheetVerStr, out sheetVer)) {
            _curBundleAppversionInfo.sheetVersion = sheetVer;

            BuildTarget target = BuildTarget.Android;
            if (_assetBundlesSetting.assetBundleTarget == (int)AssetBundleTarget.Android) {
                target = BuildTarget.Android;
            } else if (_assetBundlesSetting.assetBundleTarget == (int)AssetBundleTarget.iOS) {
                target = BuildTarget.iOS;
            }

            isComplete = AssetBundleBuildManager.BuildSheetAssetBundles(AssetBundleUtil.originBundlePath, _assetBundleOptions, target, _assetBundlesSetting);
        } else {
            if (EditorUtility.DisplayDialog("AssetBundle",
               "Sheet Version. Please enter numbers only.", "OK")) {

            }
            return;
        }

        if (isComplete) {
            int patchCount = 0;
            if (_curBundleAppversionInfo.versionKind == (int)AssetBundleVersionKind.ResOrSheetPatchUp) {
                patchCount = _curBundleAppversionInfo.GetSheetPatchCount(_sheetVerStr);
            }
            EditorCoroutine.start(UpdateMakePatchInfo("Sheet", _sheetVerStr, patchCount, _comparePatchInfoList, _patchAssetBundleList, OnCompleteSheetBuildAssetBundle));
        } else {
            if (EditorUtility.DisplayDialog("AssetBundle",
                    "Fail Build Sheet AssetBundles", "OK")) {

            }
        }
    }

    void BuildEachSheetAssetBundles()
    {
        int sheetVer = 0;
        bool isComplete = false;
        if (int.TryParse(_sheetVerStr, out sheetVer)) {
            _curBundleAppversionInfo.sheetVersion = sheetVer;

            BuildTarget target = BuildTarget.Android;
            if (_assetBundlesSetting.assetBundleTarget == (int)AssetBundleTarget.Android) {
                target = BuildTarget.Android;
            } else if (_assetBundlesSetting.assetBundleTarget == (int)AssetBundleTarget.iOS) {
                target = BuildTarget.iOS;
            }

            isComplete = AssetBundleBuildManager.BuildEachSheetAssetBundles(AssetBundleUtil.originBundlePath, _assetBundleOptions, target, _assetBundlesSetting);
        } else {
            if (EditorUtility.DisplayDialog("AssetBundle",
               "Sheet Version. Please enter numbers only.", "OK")) {

            }
            return;
        }

        if (isComplete) {
            int patchCount = 0;
            if (_curBundleAppversionInfo.versionKind == (int)AssetBundleVersionKind.ResOrSheetPatchUp) {
                patchCount = _curBundleAppversionInfo.GetSheetPatchCount(_sheetVerStr);
            }
            EditorCoroutine.start(UpdateMakePatchInfo("SheetEach", _sheetVerStr, patchCount, _compareEachSheetPatchInfoList, _patchEachAssetBundleList, OnCompleteSheetBuildAssetBundle));
        } else {
            if (EditorUtility.DisplayDialog("AssetBundle",
                    "Fail Build Sheet AssetBundles", "OK")) {

            }
        }
    }

    IEnumerator UpdateMakePatchInfo(string assetType, string version, int patchCount, Dictionary<string, AssetBundlePatchData> comparePatchInfoList, List<string> patchAssetBundleList, Action<bool> onCompleteBuildAssetBundle)
    {
        _bundlePatchInfoDataInfo = new AssetBundlePatchInfoData();
        _bundlePatchInfoDataInfo.version = version;
        _bundlePatchInfoDataInfo.patchCount = patchCount;

        AssetBundleTarget bundleTarget = (AssetBundleTarget)_assetBundlesSetting.assetBundleTarget;
        string bundleInfoPath = string.Format("AssetBundles/{0}/{1}/{1}", bundleTarget.ToString(), assetType);

        AssetBundle bundle = AssetBundle.LoadFromFile(bundleInfoPath);
        bool isMakePatchInfo = false;
        if (bundle == null) {
            isMakePatchInfo = false;
        } else {
            isMakePatchInfo = true;
        }

        patchAssetBundleList.Clear();
        if(isMakePatchInfo) {
            string progressTitle = "Make Patch Info File";
            AssetBundleManifest bundleManifest = bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            string[] assetBundles = bundleManifest.GetAllAssetBundles();
            bundle.Unload(false);

            _bundlePatchInfoDataInfo.datas = new AssetBundlePatchData[assetBundles.Length];
            
            for (int i = 0; i < assetBundles.Length; i++) {
                _bundlePatchInfoDataInfo.datas[i] = new AssetBundlePatchData();
                MakeIntegrityData(bundleManifest, _bundlePatchInfoDataInfo.datas[i], assetType, assetBundles[i]);

                if (_curBundleAppversionInfo.versionKind == (int)AssetBundleVersionKind.ResOrSheetPatchUp) {
                    if (comparePatchInfoList.ContainsKey(_bundlePatchInfoDataInfo.datas[i].assetName)) {
                        AssetBundlePatchData comparePatchData = comparePatchInfoList[_bundlePatchInfoDataInfo.datas[i].assetName];
                        if (comparePatchData.assetHash != _bundlePatchInfoDataInfo.datas[i].assetHash) {
                            patchAssetBundleList.Add(_bundlePatchInfoDataInfo.datas[i].assetName);
                        }
                    } else {
                        patchAssetBundleList.Add(_bundlePatchInfoDataInfo.datas[i].assetName);
                    }
                }

                EditorUtility.DisplayProgressBar(progressTitle, assetBundles[i], (float)i / (float)assetBundles.Length);
                yield return null;
            }

            

            if (_curBundleAppversionInfo.versionKind == (int)AssetBundleVersionKind.ResOrSheetPatchUp) {
                AssetResSelectUploadWindow selectUploadWindow = null;
                if((assetType == "Resources" || assetType == "Sheet") && patchAssetBundleList.Count > 0) {
                    selectUploadWindow = (AssetResSelectUploadWindow)AssetResSelectUploadWindow.ShowWindow();
                }
                
                if (selectUploadWindow != null) {
                    selectUploadWindow.SetPatchList(patchAssetBundleList);
                    selectUploadWindow.onUpload = OnUploadSelectRes;
                    _isSelectPatchList = true;

                    while (_isSelectPatchList) {
                        yield return null;
                    }

                    patchAssetBundleList.Clear();
                    List<string> selectPatchList = selectUploadWindow.GetSelectPatchList();
                    for(int i = 0;i< selectPatchList.Count; i++) {
                        patchAssetBundleList.Add(selectPatchList[i]);
                    }
                }

                _bundlePatchInfoDataInfo.patchList = patchAssetBundleList.ToArray();
            }

            string savePath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/') + 1) + string.Format("AssetBundles/{0}/{1}/", bundleTarget.ToString(), assetType);
            string fileName = string.Format(AssetBundleUtil.bundlePatchInfoFile, version);
            if(assetType != "SheetEach") {
                AssetBundleJson.WriteBundlePatchInfo(savePath, fileName, _bundlePatchInfoDataInfo, _isCompressPatchInfo);
            } else {
                AssetBundleJson.WriteBundlePatchInfo(savePath, fileName, _bundlePatchInfoDataInfo, false);
            }

            RemoveNotvalidAssetBundles(assetType, bundleTarget, assetBundles, fileName);

            EditorUtility.ClearProgressBar();

            if(onCompleteBuildAssetBundle != null)
                onCompleteBuildAssetBundle(true);
        } else {
            if (onCompleteBuildAssetBundle != null)
                onCompleteBuildAssetBundle(false);
        }
    }

    bool _isSelectPatchList = false;

    void OnUploadSelectRes()
    {
        _isSelectPatchList = false;
    }

    void RemoveNotvalidAssetBundles(string assetType, AssetBundleTarget bundleTarget, string[] assetBundles, string patchInfoFileName)
    {
        if(assetBundles == null || assetBundles.Length == 0)
            return;

        Dictionary<string, string> assetBundlesDic = new Dictionary<string, string>();

        assetBundlesDic.Add(assetType, "");
        assetBundlesDic.Add(assetType + ".manifest", "");

        assetBundlesDic.Add(patchInfoFileName, "");

        for (int i = 0;i< assetBundles.Length;i++) {
            if(assetBundlesDic.ContainsKey(assetBundles[i])) {
                Debug.Log(string.Format("RemoveNotvalidAssetBundles Exist AssetBundleName : {0}", assetBundles[i]));
                continue;
            }
            assetBundlesDic.Add(assetBundles[i], "");
            assetBundlesDic.Add(assetBundles[i] + ".manifest", "");
        }

        string comparePath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/') + 1) + string.Format("AssetBundles/{0}/{1}/", bundleTarget.ToString(), assetType);

        string[] compareFiles = Directory.GetFiles(comparePath, "*.*", SearchOption.AllDirectories);
        if(compareFiles != null && compareFiles.Length > 0) {
            for(int i = 0;i< compareFiles.Length;i++) {
                string compareFileName = compareFiles[i].Replace(comparePath, "");
                compareFileName = compareFileName.Replace('\\', '/');
                if (!assetBundlesDic.ContainsKey(compareFileName)) {
                    if (File.Exists(compareFiles[i])) {
                        File.Delete(compareFiles[i]);
                    }
                }
            }
        }
    }

    IEnumerator UpdateCopyBundleToDestPath(string assetType, string version, string destPath, Action<bool> onCompleteBundleCopy, bool isSvnFolder = false)
    {
        AssetBundleTarget bundleTarget = (AssetBundleTarget)_assetBundlesSetting.assetBundleTarget;
        string bundleInfoPath = string.Format("AssetBundles/{0}/{1}/{1}", bundleTarget.ToString(), assetType);

        AssetBundle bundle = AssetBundle.LoadFromFile(bundleInfoPath);
        bool isCopyBundle = false;
        if (bundle == null) {
            isCopyBundle = false;
        } else {
            isCopyBundle = true;
        }

        if (isCopyBundle) {
            string rootLocalPath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/') + 1) + string.Format("AssetBundles/{0}/{1}/", bundleTarget.ToString(), assetType);
            if (!Directory.Exists(destPath)) {
                Directory.CreateDirectory(destPath);
            }

            if(isSvnFolder) {
                string assetBundleLocalPath = rootLocalPath + string.Format("{0}", assetType);
                string assetBundleDestPath = destPath + string.Format("{0}", assetType);

                if (File.Exists(assetBundleDestPath)) {
                    File.Delete(assetBundleDestPath);
                }
                File.Copy(assetBundleLocalPath, assetBundleDestPath);

                string assetBundleManifestLocalPath = rootLocalPath + string.Format("{0}.manifest", assetType);
                string assetBundleManifestDestPath = destPath + string.Format("{0}.manifest", assetType);

                if (File.Exists(assetBundleManifestDestPath)) {
                    File.Delete(assetBundleManifestDestPath);
                }
                File.Copy(assetBundleManifestLocalPath, assetBundleManifestDestPath);
            }

            string patchInfoLocalPath = rootLocalPath + string.Format("patchinfo_{0}.bytes", version);
            string patchInfoDestPath = destPath + string.Format("patchinfo_{0}.bytes", version);

            if (File.Exists(patchInfoDestPath)) {
                File.Delete(patchInfoDestPath);
            }
            File.Copy(patchInfoLocalPath, patchInfoDestPath);

            string progressTitle = string.Format("Copy AssetBundles To {0}", destPath);
            AssetBundleManifest bundleManifest = bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            string[] assetBundles = bundleManifest.GetAllAssetBundles();
            bundle.Unload(false);
            for (int i = 0; i < assetBundles.Length; i++) {
                string localFile = rootLocalPath + assetBundles[i];
                string destFile = destPath + assetBundles[i];

                string destFilePath = destFile.Substring(0, destFile.LastIndexOf('/') + 1);
                if (!Directory.Exists(destFilePath)) {
                    Directory.CreateDirectory(destFilePath);
                }

                if (File.Exists(destFile)) {
                    File.Delete(destFile);
                }
                File.Copy(localFile, destFile);

                if(isSvnFolder) {
                    string localManifestFile = rootLocalPath + assetBundles[i] + ".manifest";
                    string destManifestFile = destPath + assetBundles[i] + ".manifest";

                    if (File.Exists(destManifestFile)) {
                        File.Delete(destManifestFile);
                    }
                    File.Copy(localManifestFile, destManifestFile);
                }

                EditorUtility.DisplayProgressBar(progressTitle, assetBundles[i], (float)i / (float)assetBundles.Length);
                yield return null;
            }

            EditorUtility.ClearProgressBar();

            if (onCompleteBundleCopy != null)
                onCompleteBundleCopy(true);
        } else {
            if (onCompleteBundleCopy != null)
                onCompleteBundleCopy(false);
        }

        yield return null;
    }

    IEnumerator UpdateCopyPatchBundleToDestPath(string assetType, string version, string destPath, List<string> patchAssetBundleList, Action<bool> onCompleteBundleCopy)
    {
        AssetBundleTarget bundleTarget = (AssetBundleTarget)_assetBundlesSetting.assetBundleTarget;

        string rootLocalPath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/') + 1) + string.Format("AssetBundles/{0}/{1}/", bundleTarget.ToString(), assetType);
        if (!Directory.Exists(destPath)) {
            Directory.CreateDirectory(destPath);
        }

        string assetBundleLocalPath = rootLocalPath + string.Format("{0}", assetType);
        string assetBundleDestPath = destPath + string.Format("{0}", assetType);

        if (File.Exists(assetBundleDestPath)) {
            File.Delete(assetBundleDestPath);
        }
        File.Copy(assetBundleLocalPath, assetBundleDestPath);

        string assetBundleManifestLocalPath = rootLocalPath + string.Format("{0}.manifest", assetType);
        string assetBundleManifestDestPath = destPath + string.Format("{0}.manifest", assetType);

        if (File.Exists(assetBundleManifestDestPath)) {
            File.Delete(assetBundleManifestDestPath);
        }
        File.Copy(assetBundleManifestLocalPath, assetBundleManifestDestPath);

        string patchInfoLocalPath = rootLocalPath + string.Format("patchinfo_{0}.bytes", version);
        string patchInfoDestPath = destPath + string.Format("patchinfo_{0}.bytes", version);

        if (File.Exists(patchInfoDestPath)) {
            File.Delete(patchInfoDestPath);
        }
        File.Copy(patchInfoLocalPath, patchInfoDestPath);

        string progressTitle = string.Format("Copy AssetBundles To {0}", destPath);
        for (int i = 0; i < patchAssetBundleList.Count; i++) {
            string bundleName = patchAssetBundleList[i];
            string localFile = rootLocalPath + bundleName;
            string destFile = destPath + bundleName;

            string destFilePath = destFile.Substring(0, destFile.LastIndexOf('/') + 1);
            if (!Directory.Exists(destFilePath)) {
                Directory.CreateDirectory(destFilePath);
            }

            if (File.Exists(destFile)) {
                File.Delete(destFile);
            }
            File.Copy(localFile, destFile);

            string localManifestFile = rootLocalPath + bundleName + ".manifest";
            string destManifestFile = destPath + bundleName + ".manifest";

            if (File.Exists(destManifestFile)) {
                File.Delete(destManifestFile);
            }
            File.Copy(localManifestFile, destManifestFile);

            EditorUtility.DisplayProgressBar(progressTitle, bundleName, (float)i / (float)patchAssetBundleList.Count);
            yield return null;
        }

        EditorUtility.ClearProgressBar();

        if (onCompleteBundleCopy != null)
            onCompleteBundleCopy(true);

        yield return null;
    }

    void MakeIntegrityData(AssetBundleManifest bundleManifest, AssetBundlePatchData data, string assetType, string bundleName)
    {
        //GetHash
        Hash128 hash = bundleManifest.GetAssetBundleHash(bundleName);

        AssetBundleTarget bundleTarget = (AssetBundleTarget)_assetBundlesSetting.assetBundleTarget;
        string bundlePath = string.Format("AssetBundles/{0}/{1}/{2}", bundleTarget.ToString(), assetType, bundleName);

        AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);

        if (bundle == null) {
            Debug.Log(string.Format("bundleName : {0} Bundle is Null!", bundleName));
            return;
        }

        string[] names = bundle.GetAllAssetNames();

        data.assetName = bundleName;
        data.assetHash = hash.ToString();
        data.fileNames = names;

        string[] dependencies = bundleManifest.GetAllDependencies(bundleName);
        if (dependencies.Length > 0) {
            data.dependencies = dependencies;
        }

        string assetPath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/') + 1) + string.Format("AssetBundles/{0}/{1}/", bundleTarget.ToString(), assetType);
        string result = StringEncription.MakeMD5FromFile(assetPath + bundleName);
        if (!string.IsNullOrEmpty(result)) {
            data.md5Hash = result;
        }

        data.size = IOUtil.GetFileSize(assetPath + bundleName);

        bundle.Unload(false);
    }

    void UploadFtpAssetBundleList(AssetFTPUploaderManager.FTPUploadKind uploadKind, string pathKind, string versionStr, bool isPatchAsset = false)
    {
        if (!CheckValidInputData())
            return;

        if (!CheckValidFTPInputData())
            return;

        AssetBundleTarget bundleTarget = (AssetBundleTarget)_assetBundlesSetting.assetBundleTarget;
        AssetBundleBuildKind buildKind = (AssetBundleBuildKind)_assetBundlesSetting.buildKind;

        string platformPath = string.Format("{0}_{1}_{2}_{3}", bundleTarget.ToString(), buildKind.ToString(), pathKind, versionStr);
        string uploadAssetPath = AssetBundleUtil.ftpRootPath + platformPath;

        string ftpURL = _assetBundlesSetting.ftpURL;
        string ftpID = _assetBundlesSetting.ftpID;
        string ftpPW = _ftpPW;

        string dataPath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/') + 1) + string.Format("AssetBundles/{0}/{1}/", bundleTarget.ToString(), uploadKind.ToString());

        _ftpUploaderManager.UploadKind = uploadKind;

        _ftpUploaderManager.RootLocalPath = dataPath;
        _ftpUploaderManager.BundleTarget = bundleTarget;
        _ftpUploaderManager.BuildKind = buildKind;
        _ftpUploaderManager.CurUploadVersion = versionStr;
        _ftpUploaderManager.InitAssetFTPUploader();

        if(isPatchAsset) {
            if(_patchAssetBundleList == null || _patchAssetBundleList.Count == 0) {
                string listLog = string.Format("No {0} patch list!!", uploadKind.ToString());
                Debug.Log(listLog);

                if (_bundleEvents.Count > 0) {
                    _bundleEvents.Dequeue().Execute();
                } else {
                    if (EditorUtility.DisplayDialog("AssetBundle",
                           listLog, "OK")) {

                    }
                }
                return;
            }

            _ftpUploaderManager.UploadFtpAssetBundlePatchList(uploadAssetPath, _patchAssetBundleList, uploadKind.ToString(), ftpURL, ftpID, ftpPW);
        } else {
            string bundleInfoPath = string.Format("AssetBundles/{0}/{1}/{1}", bundleTarget.ToString(), uploadKind.ToString());
            _ftpUploaderManager.UploadFtpAssetBundleList(uploadAssetPath, bundleInfoPath, uploadKind.ToString(), ftpURL, ftpID, ftpPW);
        }
    }

    void UploadSFtpAssetBundleList(AssetFTPUploaderManager.FTPUploadKind uploadKind, string pathKind, string versionStr, bool isPatchAsset = false)
    {
        if (!CheckValidInputData())
            return;

        if (!CheckValidFTPInputData())
            return;

        AssetBundleTarget bundleTarget = (AssetBundleTarget)_assetBundlesSetting.assetBundleTarget;
        AssetBundleBuildKind buildKind = (AssetBundleBuildKind)_assetBundlesSetting.buildKind;

        string platformPath = string.Format("{0}_{1}_{2}_{3}", bundleTarget.ToString(), buildKind.ToString(), pathKind, versionStr);
        string uploadAssetPath = AssetBundleUtil.ftpRootPath + platformPath;

        string ftpURL = _assetBundlesSetting.ftpURL;
        string ftpID = _assetBundlesSetting.ftpID;

        string dataPath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/') + 1) + string.Format("AssetBundles/{0}/{1}/", bundleTarget.ToString(), uploadKind.ToString());

        _ftpUploaderManager.UploadKind = uploadKind;

        _ftpUploaderManager.RootLocalPath = dataPath;
        _ftpUploaderManager.BundleTarget = bundleTarget;
        _ftpUploaderManager.BuildKind = buildKind;
        _ftpUploaderManager.CurUploadVersion = versionStr;
        _ftpUploaderManager.InitAssetFTPUploader();

        _ftpUploaderManager.SftpKeyFile = string.Format("{0}.pem", ftpID);

        if (isPatchAsset) {
            if (_patchAssetBundleList == null || _patchAssetBundleList.Count == 0) {
                string listLog = string.Format("No {0} patch list!!", uploadKind.ToString());
                Debug.Log(listLog);

                if (_bundleEvents.Count > 0) {
                    _bundleEvents.Dequeue().Execute();
                } else {
                    if (EditorUtility.DisplayDialog("AssetBundle",
                           listLog, "OK")) {

                    }
                }
                return;
            }

            _ftpUploaderManager.UploadSFtpAssetBundlePatchList(uploadAssetPath, _patchAssetBundleList, uploadKind.ToString(), ftpURL, ftpID);
        } else {
            string bundleInfoPath = string.Format("AssetBundles/{0}/{1}/{1}", bundleTarget.ToString(), uploadKind.ToString());
            _ftpUploaderManager.UploadSFtpAssetBundleList(uploadAssetPath, bundleInfoPath, uploadKind.ToString(), ftpURL, ftpID);
        }
    }

    bool CheckValidInputData()
    {
        int resVersion = 0;
        if (int.TryParse(_resVersionStr, out resVersion)) {
            _curBundleAppversionInfo.resourceVersion = resVersion;
        } else {
            if (EditorUtility.DisplayDialog("AssetBundle",
               "Resource Version. Please enter numbers only.", "OK")) {

            }
            return false;
        }

        int sheetVer = 0;
        if (int.TryParse(_sheetVerStr, out sheetVer)) {
            _curBundleAppversionInfo.sheetVersion = sheetVer;
        } else {
            if (EditorUtility.DisplayDialog("AssetBundle",
               "Sheet Version. Please enter numbers only.", "OK")) {

            }
            return false;
        }

        return true;
    }

    bool CheckValidFTPInputData()
    {
        if (string.IsNullOrEmpty(_assetBundlesSetting.ftpURL)) {
            if (EditorUtility.DisplayDialog("AssetBundle",
               "Please Input FTP URL", "OK")) {

            }
            return false;
        }

        if (string.IsNullOrEmpty(_assetBundlesSetting.ftpID)) {
            if (EditorUtility.DisplayDialog("AssetBundle",
               "Please Input FTP ID", "OK")) {

            }
            return false;
        }

        return true;
    }

    void InitAssetBundles()
    {
        SetAssetBundleBranchesPath();

        _bundleEvents.Clear();

        _assetBundlePath = AssetBundleUtil.GetProjectRootPath() + "AssetBundles";
        if (!Directory.Exists(_assetBundlePath)) {
            Directory.CreateDirectory(_assetBundlePath);
        }

        LoadAssetBundlesSetting();
        _ftpPW = "";
        InitAssetVersionInfo();
    }

    void InitAssetVersionInfo()
    {
        _curBundleAppversionInfo = _curTargetSettingInfo.GetCurAppVersionInfo();

        if(_curBundleAppversionInfo != null) {
            _resVersionStr = string.Format("{0}", _curBundleAppversionInfo.resourceVersion);
            _sheetVerStr = string.Format("{0}", _curBundleAppversionInfo.sheetVersion);
        }
    }

    void LoadAssetBundlesSetting()
    {
        _assetBundlesSetting = AssetBundleJson.LoadAssetBundlesSettingInfo(AssetBundleUtil.GetAssetBundleInfoRootPath(), _assetBundlesSettingFile);
        if (_assetBundlesSetting == null) {
            _assetBundlesSetting = new AssetBundlesSettingInfo();
            _assetBundlesSetting.Init();
        }

        _curTargetSettingInfo = GetBundleTargetSettingInfo((AssetBundleTarget)_assetBundlesSetting.assetBundleTarget, (AssetBundleBuildKind)_assetBundlesSetting.buildKind);
    }

    public BundleToolTargetSetting GetBundleTargetSettingInfo(AssetBundleTarget bundleTarget, AssetBundleBuildKind buildKind)
    {
        string bundleTargetFile = string.Format(AssetBundleUtil.assetBundleTargetFile, bundleTarget.ToString(), buildKind.ToString());

        BundleToolTargetSetting retValue = AssetBundleJson.LoadAssetBundlesTargetInfo(AssetBundleUtil.GetAssetBundleInfoRootPath(), bundleTargetFile);
        if(retValue == null) {
            retValue = new BundleToolTargetSetting();
            retValue.Init();
            retValue.assetBundleTarget = (int)bundleTarget;
            retValue.buildKind = (int)buildKind;
        }

        return retValue;
    }

    void SaveAssetBundleSetting()
    {
        if(_curBundleAppversionInfo != null) {
            int resVersion = 0;
            if (int.TryParse(_resVersionStr, out resVersion)) {
                _curBundleAppversionInfo.resourceVersion = resVersion;
            }

            int sheetVer = 0;
            if (int.TryParse(_sheetVerStr, out sheetVer)) {
                _curBundleAppversionInfo.sheetVersion = sheetVer;
            }
        }

        AssetBundleJson.WriteAssetBundlesSetting(AssetBundleUtil.GetAssetBundleInfoRootPath(), _assetBundlesSettingFile, _assetBundlesSetting);

        AssetBundleTarget bundleTarget = (AssetBundleTarget)_curTargetSettingInfo.assetBundleTarget;
        AssetBundleBuildKind buildKind = (AssetBundleBuildKind)_curTargetSettingInfo.buildKind;
        if(_curTargetSettingInfo == null)
            _curTargetSettingInfo = GetBundleTargetSettingInfo(bundleTarget, buildKind);

        string bundleTargetFile = string.Format(AssetBundleUtil.assetBundleTargetFile, bundleTarget.ToString(), buildKind.ToString());
        AssetBundleJson.WriteAssetBundlesTargetInfo(AssetBundleUtil.GetAssetBundleInfoRootPath(), bundleTargetFile, _curTargetSettingInfo);
    }

    #endregion

    #region Button Event

    void OnIncreaseResVerions()
    {
        IncreaseResourcesVersion();

        SaveAssetBundleSetting();
    }

    void OnBuildResAssetBundle()
    {
        if(_curTargetSettingInfo == null) {
            return;
        }

        if(!CheckPlatform()) {
            string errorLog = string.Format("Please check the platform. Current Editor Platform : {0}", EditorUserBuildSettings.activeBuildTarget.ToString());
            if (EditorUtility.DisplayDialog("AssetBundle",
                       errorLog, "OK")) {

            }
            return;
        }

        AssetBundleCheckVerState checkVerState = _curTargetSettingInfo.CheckValidResVersion(_resVersionStr, (AssetBundleVersionKind)_curBundleAppversionInfo.versionKind);

        if (checkVerState != AssetBundleCheckVerState.Valid) {
            string errorLog = string.Format("NotValid Version Resource : {0}", checkVerState.ToString());
            if (EditorUtility.DisplayDialog("AssetBundle",
                       errorLog, "OK")) {

            }
            return;
        }

        string assetType = "Resources";

        _bundleEvents.Clear();

        if (_curBundleAppversionInfo.versionKind == (int)AssetBundleVersionKind.ResOrSheetPatchUp) {
            if (!CheckValidInputData())
                return;

            if (!CheckValidFTPInputData())
                return;

            _bundleEvents.Enqueue(new AssetBundleWindowEvent(() => LoadCompareResAssetBundlePatchInfo(assetType, _curBundleAppversionInfo.streamingAssetResVer, _comparePatchInfoList)));
        }

        _bundleEvents.Enqueue(new AssetBundleWindowEvent(BuildResourceAssetBundles));
        _bundleEvents.Enqueue(new AssetBundleWindowEvent(() => CopyBundlesToAssetBundleBranches(assetType, _resVersionStr)));
        if (_curBundleAppversionInfo.versionKind == (int)AssetBundleVersionKind.StreamingAssetBuild) {
            _bundleEvents.Enqueue(new AssetBundleWindowEvent(CopyResBundlesToStreamingAsset));
        } else if(_curBundleAppversionInfo.versionKind == (int)AssetBundleVersionKind.ResOrSheetPatchUp) {
            _bundleEvents.Enqueue(new AssetBundleWindowEvent(() => UploadSFtpAssetBundleList(AssetFTPUploaderManager.FTPUploadKind.Resources, "Res", _resVersionStr, true)));
        }

        _bundleEvents.Dequeue().Execute();

        SaveAssetBundleSetting();
    }

    bool CheckPlatform()
    {
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android) {
            if (_assetBundlesSetting.assetBundleTarget != (int)AssetBundleTarget.Android) {
                return false;
            }
        } else if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS) {
            if (_assetBundlesSetting.assetBundleTarget != (int)AssetBundleTarget.iOS) {
                return false;
            }
        }

        return true;
    }

    void OnTotalResBuildWork()
    {
        _isTotalResourceBuild = true;
        _isTotalAssetBundleBuild = false;
        BuildResourceAssetBundles();

        SaveAssetBundleSetting();
    }

    void OnIncreaseSheetVersion()
    {
        IncreaseSheetVersion();

        SaveAssetBundleSetting();
    }

    void OnBuildSheetAssetBundle()
    {
        if(_curTargetSettingInfo == null)
            return;

        if (!CheckPlatform()) {
            string errorLog = string.Format("Please check the platform. Current Editor Platform : {0}", EditorUserBuildSettings.activeBuildTarget.ToString());
            if (EditorUtility.DisplayDialog("AssetBundle",
                       errorLog, "OK")) {

            }
            return;
        }

        AssetBundleCheckVerState checkVerState = _curTargetSettingInfo.CheckValidSheetVersion(_sheetVerStr, (AssetBundleVersionKind)_curBundleAppversionInfo.versionKind);

        if (checkVerState != AssetBundleCheckVerState.Valid) {
            string errorLog = string.Format("NotValid Version Sheet : {0}", checkVerState.ToString());
            if (EditorUtility.DisplayDialog("AssetBundle",
                       errorLog, "OK")) {

            }
            return;
        }

        string assetType = "Sheet";

        _patchEachAssetBundleList.Clear();

        _bundleEvents.Clear();

        if (_curBundleAppversionInfo.versionKind == (int)AssetBundleVersionKind.ResOrSheetPatchUp) {
            if (!CheckValidInputData())
                return;

            if (!CheckValidFTPInputData())
                return;

            _bundleEvents.Enqueue(new AssetBundleWindowEvent(() => LoadCompareResAssetBundlePatchInfo(assetType, _curBundleAppversionInfo.streamingAssetSheetVer, _comparePatchInfoList)));
            _bundleEvents.Enqueue(new AssetBundleWindowEvent(() => LoadCompareResAssetBundlePatchInfo("SheetEach", _curBundleAppversionInfo.streamingAssetSheetVer, _compareEachSheetPatchInfoList)));
        }

        _bundleEvents.Enqueue(new AssetBundleWindowEvent(BuildSheetAssetBundles));
        _bundleEvents.Enqueue(new AssetBundleWindowEvent(() => CopyBundlesToAssetBundleBranches(assetType, _sheetVerStr)));

        _bundleEvents.Enqueue(new AssetBundleWindowEvent(BuildEachSheetAssetBundles));
        _bundleEvents.Enqueue(new AssetBundleWindowEvent(() => CopyBundlesToAssetBundleBranches("SheetEach", _sheetVerStr)));

        if (_curBundleAppversionInfo.versionKind == (int)AssetBundleVersionKind.StreamingAssetBuild) {
            _bundleEvents.Enqueue(new AssetBundleWindowEvent(CopySheetBundlesToStreamingAsset));
        } else if (_curBundleAppversionInfo.versionKind == (int)AssetBundleVersionKind.ResOrSheetPatchUp) {
            _bundleEvents.Enqueue(new AssetBundleWindowEvent(() => UploadSFtpAssetBundleList(AssetFTPUploaderManager.FTPUploadKind.Sheet, "Sheet", _sheetVerStr, true)));
        }

        _bundleEvents.Dequeue().Execute();

        SaveAssetBundleSetting();
    }

    void OnTotalSheetBuildWork()
    {
        _isTotalSheetBuild = true;
        _isTotalAssetBundleBuild = false;
        BuildSheetAssetBundles();

        SaveAssetBundleSetting();
    }

    void OnIncreaseResAndSheetVersion()
    {
        IncreaseResourcesVersion();
        IncreaseSheetVersion();

        SaveAssetBundleSetting();
    }

    void OnBuildAssetBundleResAndSheet()
    {
        _bundleEvents.Clear();

        if (!CheckPlatform()) {
            string errorLog = string.Format("Please check the platform. Current Editor Platform : {0}", EditorUserBuildSettings.activeBuildTarget.ToString());
            if (EditorUtility.DisplayDialog("AssetBundle",
                       errorLog, "OK")) {

            }
            return;
        }

        AssetBundleCheckVerState checkResVerState = _curTargetSettingInfo.CheckValidResVersion(_resVersionStr, (AssetBundleVersionKind)_curBundleAppversionInfo.versionKind);

        if (checkResVerState != AssetBundleCheckVerState.Valid) {
            string errorLog = string.Format("NotValid Version Resource : {0}", checkResVerState.ToString());
            if (EditorUtility.DisplayDialog("AssetBundle",
                       errorLog, "OK")) {

            }
            return;
        }

        AssetBundleCheckVerState checkSheetVerState = _curTargetSettingInfo.CheckValidSheetVersion(_sheetVerStr, (AssetBundleVersionKind)_curBundleAppversionInfo.versionKind);

        if (checkSheetVerState != AssetBundleCheckVerState.Valid) {
            string errorLog = string.Format("NotValid Version Sheet : {0}", checkSheetVerState.ToString());
            if (EditorUtility.DisplayDialog("AssetBundle",
                       errorLog, "OK")) {

            }
            return;
        }

        // Resources
        string resAssetType = "Resources";

        if (_curBundleAppversionInfo.versionKind == (int)AssetBundleVersionKind.ResOrSheetPatchUp) {
            if (!CheckValidInputData())
                return;

            if (!CheckValidFTPInputData())
                return;

            _bundleEvents.Enqueue(new AssetBundleWindowEvent(() => LoadCompareResAssetBundlePatchInfo(resAssetType, _curBundleAppversionInfo.streamingAssetResVer, _comparePatchInfoList)));
        }

        _bundleEvents.Enqueue(new AssetBundleWindowEvent(BuildResourceAssetBundles));
        _bundleEvents.Enqueue(new AssetBundleWindowEvent(() => CopyBundlesToAssetBundleBranches(resAssetType, _resVersionStr)));
        if (_curBundleAppversionInfo.versionKind == (int)AssetBundleVersionKind.StreamingAssetBuild) {
            _bundleEvents.Enqueue(new AssetBundleWindowEvent(CopyResBundlesToStreamingAsset));
        } else if (_curBundleAppversionInfo.versionKind == (int)AssetBundleVersionKind.ResOrSheetPatchUp) {
            _bundleEvents.Enqueue(new AssetBundleWindowEvent(() => UploadSFtpAssetBundleList(AssetFTPUploaderManager.FTPUploadKind.Resources, "Res", _resVersionStr, true)));
        }

        // Sheet
        _patchEachAssetBundleList.Clear();
        string sheetAssetType = "Sheet";

        if (_curBundleAppversionInfo.versionKind == (int)AssetBundleVersionKind.ResOrSheetPatchUp) {
            _bundleEvents.Enqueue(new AssetBundleWindowEvent(() => LoadCompareResAssetBundlePatchInfo(sheetAssetType, _curBundleAppversionInfo.streamingAssetSheetVer, _comparePatchInfoList)));
            _bundleEvents.Enqueue(new AssetBundleWindowEvent(() => LoadCompareResAssetBundlePatchInfo("SheetEach", _curBundleAppversionInfo.streamingAssetSheetVer, _compareEachSheetPatchInfoList)));
        }

        _bundleEvents.Enqueue(new AssetBundleWindowEvent(BuildSheetAssetBundles));
        _bundleEvents.Enqueue(new AssetBundleWindowEvent(() => CopyBundlesToAssetBundleBranches(sheetAssetType, _sheetVerStr)));

        _bundleEvents.Enqueue(new AssetBundleWindowEvent(BuildEachSheetAssetBundles));
        _bundleEvents.Enqueue(new AssetBundleWindowEvent(() => CopyBundlesToAssetBundleBranches("SheetEach", _sheetVerStr)));

        if (_curBundleAppversionInfo.versionKind == (int)AssetBundleVersionKind.StreamingAssetBuild) {
            _bundleEvents.Enqueue(new AssetBundleWindowEvent(CopySheetBundlesToStreamingAsset));
        } else if (_curBundleAppversionInfo.versionKind == (int)AssetBundleVersionKind.ResOrSheetPatchUp) {
            _bundleEvents.Enqueue(new AssetBundleWindowEvent(() => UploadSFtpAssetBundleList(AssetFTPUploaderManager.FTPUploadKind.Sheet, "Sheet", _sheetVerStr, true)));
        }

        SaveAssetBundleSetting();

        _bundleEvents.Dequeue().Execute();
    }

    #endregion

    #region CallBack Methods

    void OnCompleteSheetFTPUpload()
    {
        if (_isTotalAssetBundleBuild) {
            if (EditorUtility.DisplayDialog("AssetBundle",
               "Finish Total Resource, Sheet Build & FTP Upload", "OK")) {

            }
        } else {
            if (EditorUtility.DisplayDialog("AssetBundle",
               "Finish Sheet FTP Upload AssetBundle", "OK")) {

            }
        }
    }

    void OnFTPUploadState(AssetFTPUploadState uploadState)
    {
        switch (uploadState) {
            case AssetFTPUploadState.FTPUploadStart:
                break;
            case AssetFTPUploadState.FTPUploading:
                break;
            case AssetFTPUploadState.FTPUploadFinish:
                if (_ftpUploaderManager.UploadKind == AssetFTPUploaderManager.FTPUploadKind.Resources) {
                    _curBundleAppversionInfo.AddPatchUpResVersion(_resVersionStr);
                } else if (_ftpUploaderManager.UploadKind == AssetFTPUploaderManager.FTPUploadKind.Sheet) {
                    _curBundleAppversionInfo.AddPatchUpSheetVersion(_sheetVerStr);
                }

                SaveAssetBundleSetting();

                if (_bundleEvents.Count > 0) {
                    _bundleEvents.Dequeue().Execute();
                } else {
                    if (_ftpUploaderManager.UploadKind == AssetFTPUploaderManager.FTPUploadKind.Resources) {
                        if (EditorUtility.DisplayDialog("AssetBundle",
                           "Finish Build Resources AssetBundles", "OK")) {

                        }
                    } else if (_ftpUploaderManager.UploadKind == AssetFTPUploaderManager.FTPUploadKind.Sheet) {
                        StringBuilder sb = new StringBuilder();
                        sb.Append("Finish Build Sheet AssetBundles");
                        for (int i = 0; i < _patchEachAssetBundleList.Count; i++) {
                            if (i == 0) {
                                sb.Append("\n");
                            } else {
                                sb.Append(", ");
                            }
                            sb.Append(_patchEachAssetBundleList[i]);
                        }
                        if (EditorUtility.DisplayDialog("AssetBundle",
                               sb.ToString(), "OK")) {

                        }
                    }
                }
                break;
        }
    }

    void OnCompleteResBuildAssetBundle(bool isSuccess)
    {
        if(isSuccess) {
            if(_bundleEvents.Count > 0) {
                _bundleEvents.Dequeue().Execute();
            } else {
                if (EditorUtility.DisplayDialog("AssetBundle",
                       "Finish Build Resource AssetBundles", "OK")) {

                }
            }
        }
    }

    void OnCompleteSheetBuildAssetBundle(bool isSuccess)
    {
        if (isSuccess) {
            if (_bundleEvents.Count > 0) {
                _bundleEvents.Dequeue().Execute();
            } else {
                if (EditorUtility.DisplayDialog("AssetBundle",
                       "Finish Build Sheet AssetBundles", "OK")) {

                }
            }
        }
    }

    void OnMakeResPatchInfo()
    {
        int patchCount = 0;
        if (_curBundleAppversionInfo.versionKind == (int)AssetBundleVersionKind.ResOrSheetPatchUp) {
            patchCount = _curBundleAppversionInfo.GetResPatchCount(_resVersionStr);
        }
        EditorCoroutine.start(UpdateMakePatchInfo("Resources", _resVersionStr, patchCount, _comparePatchInfoList, _patchAssetBundleList, null));
    }

    void OnMakeSheetPatchInfo()
    {

    }

    void OnCopyResToStreamingAssets()
    {
        string assetType = "Resources";
        string destPath = Application.dataPath + "/StreamingAssets/" + string.Format("AssetBundles/{0}/", assetType);
        EditorCoroutine.start(UpdateCopyBundleToDestPath(assetType, _resVersionStr, destPath, null));
    }

    #endregion
}

#endif
