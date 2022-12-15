using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;

public enum AssetBundleGroupType
{
    DataSheet,
    MapData,
    TextData,
    SubData,
}

public class AssetBundleBuildManager
{
    #region Variables

    static bool _isGroupBundle = true;
    static Dictionary<string, AssetBundleGroupInfo> _groupAssetBundleList = null;

    #endregion

    #region Methods

    public static void Init()
    {
        _groupAssetBundleList = new Dictionary<string, AssetBundleGroupInfo>();
    }

    public static bool BuildResAssetBundles(string bundlePath, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform, AssetBundlesSettingInfo setting)
    {
        string outAssetBundleDirectory = "AssetBundles/" + targetPlatform.ToString() + "/Resources";
        string realPath = AssetBundleUtil.GetProjectRootPath() + outAssetBundleDirectory;
        if (!Directory.Exists(realPath)) {
            Directory.CreateDirectory(realPath);
        }

        AssetBundleBuild[] resBundleBuilds = GetResAssetBundleList(bundlePath);

        AssetBundleManifest bundleManifest = BuildPipeline.BuildAssetBundles(outAssetBundleDirectory, resBundleBuilds,
                                        assetBundleOptions,
                                        targetPlatform);

        if (bundleManifest == null) {
            Debug.Log(string.Format("BuildAllAssetBundles bundleManifest == null"));
            return false;
        }

        return true;
    }

    public static bool BuildSheetAssetBundles(string bundlePath, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform, AssetBundlesSettingInfo setting)
    {
        string outAssetBundleDirectory = "AssetBundles/" + targetPlatform.ToString() + "/Sheet";
        string realPath = AssetBundleUtil.GetProjectRootPath() + outAssetBundleDirectory;
        if (!Directory.Exists(realPath)) {
            Directory.CreateDirectory(realPath);
        }

        AssetBundleBuild[] resBundleBuilds = GetSheetAssetBundleGroupList(bundlePath);

        AssetBundleManifest bundleManifest = BuildPipeline.BuildAssetBundles(outAssetBundleDirectory, resBundleBuilds,
                                        assetBundleOptions,
                                        targetPlatform);

        if (bundleManifest == null) {
            Debug.Log(string.Format("BuildAllAssetBundles bundleManifest == null"));
            return false;
        }

        return true;
    }

    public static bool BuildEachSheetAssetBundles(string bundlePath, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform, AssetBundlesSettingInfo setting)
    {
        string outAssetBundleDirectory = "AssetBundles/" + targetPlatform.ToString() + "/SheetEach";
        string realPath = AssetBundleUtil.GetProjectRootPath() + outAssetBundleDirectory;
        if (!Directory.Exists(realPath)) {
            Directory.CreateDirectory(realPath);
        }

        AssetBundleBuild[] resBundleBuilds = GetSheetEachAssetBundleList(bundlePath);

        AssetBundleManifest bundleManifest = BuildPipeline.BuildAssetBundles(outAssetBundleDirectory, resBundleBuilds,
                                        assetBundleOptions,
                                        targetPlatform);

        if (bundleManifest == null) {
            Debug.Log(string.Format("BuildAllAssetBundles bundleManifest == null"));
            return false;
        }

        return true;
    }

    static AssetBundleBuild[] GetResAssetBundleList(string bundlePath)
    {
        List<AssetBundleBuild> retValue = new List<AssetBundleBuild>();
        string[] bundleFiles = Directory.GetFiles(Application.dataPath + bundlePath, "*.*", SearchOption.AllDirectories);

        if(_groupAssetBundleList == null)
            _groupAssetBundleList = new Dictionary<string, AssetBundleGroupInfo>();

        _groupAssetBundleList.Clear();
        if (bundleFiles != null && bundleFiles.Length > 0) {
            for (int i = 0; i < bundleFiles.Length; i++) {
                string filePathName = bundleFiles[i];
                if (!CheckValidPath(filePathName)) continue;

                int fileLength = filePathName.Length;
                if (fileLength > 5) {
                    string lastName = filePathName.Substring(fileLength - 5, 5);
                    if (lastName == ".meta") {
                        continue;
                    }
                }

                string assetPath = "Assets" + filePathName.Replace(Application.dataPath, "").Replace('\\', '/');

                string assetBundleName = assetPath.Replace("Assets" + bundlePath, "");
                char[] nameChars = assetBundleName.ToCharArray();
                int pointIndex = -1;
                for (int j = nameChars.Length - 1; j >= 0; j--) {
                    if (nameChars[j] == '.') {
                        pointIndex = j;
                        break;
                    }
                }

                if (pointIndex != -1)
                    assetBundleName = assetBundleName.Substring(0, pointIndex);

                assetBundleName = assetBundleName.ToLower();

                if(CheckSingleAssetBundle(assetBundleName)) {
                    string[] bundleNameSplits = assetBundleName.Split('/');
                    for(int j = 0;j< bundleNameSplits.Length;j++) {
                        if(j > 1) {
                            assetBundleName += '_';
                        }

                        if(j == 0) {
                            assetBundleName = bundleNameSplits[j] + '/';
                        } else {
                            assetBundleName += bundleNameSplits[j];
                        }
                    }
                } else if (CheckSceneAssetBundle(assetBundleName)) {
                    string[] bundleNameSplits = assetBundleName.Split('/');
                    assetBundleName = string.Format("scene/{0}", bundleNameSplits[bundleNameSplits.Length -1]);
                } else {
                    if (_isGroupBundle) {
                        if (AddResGroupAssetBundleList(assetBundleName, filePathName)) continue;
                    }
                }

                AssetBundleBuild inputBundleBuild = new AssetBundleBuild();
                inputBundleBuild.assetBundleName = assetBundleName;
                inputBundleBuild.assetBundleVariant = "unity3d";
                string[] assetNames = new string[1];
                assetNames[0] = assetPath;
                inputBundleBuild.assetNames = assetNames;

                retValue.Add(inputBundleBuild);
            }
        }

        if(_groupAssetBundleList.Count > 0) {
            List<string> groupKeys = _groupAssetBundleList.Keys.ToList();
            for(int i = 0;i< groupKeys.Count;i++) {
                retValue.Add(GetGroupAssetBunldeBuild(_groupAssetBundleList[groupKeys[i]]));
            }
        }

        return retValue.ToArray();
    }

    static bool CheckSingleAssetBundle(string assetBundleName)
    {
        if (assetBundleName.Contains("audio/bgm/") || assetBundleName.Contains("audio/etc/") ||
            assetBundleName.Contains("ui/prefab/npc/") || assetBundleName.Contains("ui/prefab/card/") ||
            assetBundleName.Contains("ui/texture/env_texture/") || assetBundleName.Contains("ui/texture/bg_blur/")) {
            return true;
        }

        return false;
    }

    static bool CheckSceneAssetBundle(string assetBundleName)
    {
        if (assetBundleName.Contains("scene/stage/")) {
            return true;
        }

        return false;
    }

    static bool AddResGroupAssetBundleList(string assetBundleName, string filePathName, int addNumber = 0)
    {
        string groupName = "";
        if(addNumber == 0) {
            groupName = GetBundleGroupName(assetBundleName);
        } else {
            groupName = string.Format("{0}_{1}", GetBundleGroupName(assetBundleName), addNumber);
        }

        if(!CheckFileValidSize(groupName)) {
            addNumber++;
            return AddResGroupAssetBundleList(assetBundleName, filePathName, addNumber);
        }

        char[] spearator = {'\\', '/'};
        string[] filePathSplit = filePathName.Split(spearator);
        string lastAddSplit = filePathSplit[filePathSplit.Length - 1];

        if (!string.IsNullOrEmpty(groupName)) {
            if (_groupAssetBundleList.ContainsKey(groupName)) {
                
                bool isExist = false;
               
                for (int i = 0;i< _groupAssetBundleList[groupName].groupPathInfos.Count;i++) {
                    AssetBundleGroupPathInfo groupPathInfo = _groupAssetBundleList[groupName].groupPathInfos[i];

                    if (groupPathInfo.lastFileName == lastAddSplit) {
                        Debug.Log(string.Format("AddGroupAssetBundleList Exist assetBundleName : {0}", assetBundleName));
                        isExist = true;
                        break;
                    }
                }

                if(!isExist) {
                    AssetBundleGroupPathInfo inputGroupPathInfo = new AssetBundleGroupPathInfo();
                    inputGroupPathInfo.lastFileName = lastAddSplit;
                    inputGroupPathInfo.filePathName = filePathName;
                    _groupAssetBundleList[groupName].AddGroupPathInfo(inputGroupPathInfo);
                } else {
                    addNumber++;
                    return AddResGroupAssetBundleList(assetBundleName, filePathName, addNumber);
                }
            } else {
                AssetBundleGroupInfo inputGroupInfo = new AssetBundleGroupInfo();
                inputGroupInfo.assetBundleName = groupName;
                AssetBundleGroupPathInfo inputGroupPathInfo = new AssetBundleGroupPathInfo();
                inputGroupPathInfo.lastFileName = lastAddSplit;
                inputGroupPathInfo.filePathName = filePathName;
                inputGroupInfo.AddGroupPathInfo(inputGroupPathInfo);
                _groupAssetBundleList.Add(groupName, inputGroupInfo);
            }

            return true;
        }

        return false;
    }

    static bool CheckFileValidSize(string groupName)
    {
        if (!_groupAssetBundleList.ContainsKey(groupName))
            return true;

        AssetBundleGroupInfo bundleGroupInfo = _groupAssetBundleList[groupName];

        long checkSize = -1;

        if (groupName.Contains("audio/common")) {
            checkSize = 3096000;
        } else if (groupName.Contains("audio/emotion")) {
            checkSize = 10240000;
        } else if (groupName.Contains("audio/arena")) {
            checkSize = 5096000;
        } else if (groupName.Contains("character/common")) {
            checkSize = 5120000;
        } else if (groupName.Contains("effect")) {
            checkSize = 2048000;
        } else if (groupName.Contains("ui/icon")) {
            checkSize = 1024000;
        }

        if (checkSize != -1) {
            if (bundleGroupInfo.groupSize > checkSize) {
                return false;
            }
        }

        return true;
    }

    static string GetBundleGroupName(string assetBundleName)
    {
        string retValue = "";

        string[] nameSplits = assetBundleName.Split('/');

        if(nameSplits.Length > 3) {
            if (nameSplits.Length > 4 && CheckFourthDepth(nameSplits)) {
                retValue = nameSplits[0] + '/' + nameSplits[1] + '_' + nameSplits[2] + '_' + nameSplits[3];
            } else if(nameSplits.Length == 4 && CheckFourthDepth(nameSplits)) {
                retValue = nameSplits[0] + '/' + nameSplits[1] + '_' + nameSplits[2];
            } else if (CheckThirdDepth(nameSplits)) {
                retValue = nameSplits[0] + '/' + nameSplits[1] + '_' + nameSplits[2];
            } else {
                retValue = nameSplits[0] + '/' + nameSplits[1];
            }


        } else if(nameSplits.Length == 3) {
            retValue = nameSplits[0] + '/' + nameSplits[1];

        } else if(nameSplits.Length == 2) {
            retValue = nameSplits[0] + '/' + nameSplits[0] + "grp";
        }

        return retValue;
    }

    static bool CheckThirdDepth(string[] nameSplits)
    {
        if(nameSplits[0] == "character" || (nameSplits[0] == "audio" && nameSplits[1] == "arena") ||
            (nameSplits[0] == "audio" && nameSplits[1] == "emotion") || (nameSplits[0] == "audio" && nameSplits[1] == "monster") ||
            (nameSplits[0] == "audio" && nameSplits[1] == "mutant") || (nameSplits[0] == "audio" && nameSplits[1] == "unite") ||
            (nameSplits[0] == "cutscene" && nameSplits[1] == "prefab") || (nameSplits[0] == "scene" && nameSplits[1] == "stage") ||
            (nameSplits[0] == "ui" && nameSplits[1] == "atlas") || (nameSplits[0] == "ui" && nameSplits[1] == "font") ||
            (nameSplits[0] == "ui" && nameSplits[1] == "icon") || (nameSplits[0] == "ui" && nameSplits[1] == "prefab") ||
            (nameSplits[0] == "ui" && nameSplits[1] == "texture")) {
            return true;
        }

        return false;
    }

    static bool CheckFourthDepth(string[] nameSplits)
    {
        if(nameSplits[0] == "effect" && nameSplits[1] == "fx_source" && nameSplits[2] == "anim") {
            return true;
        } else if (nameSplits[0] == "ui" && nameSplits[1] == "atlas" && nameSplits[2] == "local") {
            return true;
        }

        return false;
    }

    static AssetBundleBuild[] GetSheetEachAssetBundleList(string bundlePath)
    {
        List<AssetBundleBuild> retValue = new List<AssetBundleBuild>();

        // Data Sheet
        string[] dataSheetFiles = Directory.GetFiles(Application.dataPath + bundlePath + "Data/", "*.*", SearchOption.AllDirectories);
        AddBundleBuildFile(dataSheetFiles, bundlePath, retValue);

        // TextData
        string[] textDataFiles = Directory.GetFiles(Application.dataPath + bundlePath + "TextData/", "*.*", SearchOption.AllDirectories);
        AddBundleBuildFile(textDataFiles, bundlePath, retValue);

        // MapData
        string[] mapDataFiles = Directory.GetFiles(Application.dataPath + bundlePath + "MapData/", "*.*", SearchOption.AllDirectories);
        AddBundleBuildFile(mapDataFiles, bundlePath, retValue);

        // SubData
        string[] subDataFiles = Directory.GetFiles(Application.dataPath + bundlePath + "SubData/", "*.*", SearchOption.AllDirectories);
        AddBundleBuildFile(subDataFiles, bundlePath, retValue);

        return retValue.ToArray();
    }

    static void AddBundleBuildFile(string[] bundleFiles, string bundlePath, List<AssetBundleBuild> bundleBuildList)
    {
        if (bundleFiles != null && bundleFiles.Length > 0) {
            for (int i = 0; i < bundleFiles.Length; i++) {
                string filePathName = bundleFiles[i];
                int fileLength = filePathName.Length;
                if (fileLength > 5) {
                    string lastName = filePathName.Substring(fileLength - 5, 5);
                    if (lastName == ".meta") {
                        continue;
                    }
                }

                string assetPath = "Assets" + filePathName.Replace(Application.dataPath, "").Replace('\\', '/');
                string assetBundleName = assetPath.Replace("Assets" + bundlePath, "");
                char[] nameChars = assetBundleName.ToCharArray();
                int pointIndex = -1;
                for (int j = nameChars.Length - 1; j >= 0; j--) {
                    if (nameChars[j] == '.') {
                        pointIndex = j;
                        break;
                    }
                }

                if (pointIndex != -1)
                    assetBundleName = assetBundleName.Substring(0, pointIndex);

                assetBundleName = assetBundleName.ToLower();

                string[] bundleNameSplits = assetBundleName.Split('/');
                for (int j = 0; j < bundleNameSplits.Length; j++) {
                    if (j > 1) {
                        assetBundleName += '_';
                    }

                    if (j == 0) {
                        assetBundleName = bundleNameSplits[j] + '/';
                    } else {
                        assetBundleName += bundleNameSplits[j];
                    }
                }

                AssetBundleBuild inputBundleBuild = new AssetBundleBuild();
                inputBundleBuild.assetBundleName = assetBundleName;
                inputBundleBuild.assetBundleVariant = "unity3d";
                string[] assetNames = new string[1];
                assetNames[0] = assetPath;
                inputBundleBuild.assetNames = assetNames;

                bundleBuildList.Add(inputBundleBuild);
            }
        }
    }

    static AssetBundleBuild[] GetSheetAssetBundleGroupList(string bundlePath)
    {
        List<AssetBundleBuild> retValue = new List<AssetBundleBuild>();
        
        if (_groupAssetBundleList == null)
            _groupAssetBundleList = new Dictionary<string, AssetBundleGroupInfo>();

        _groupAssetBundleList.Clear();

        // Data Sheet
        string[] dataSheetFiles = Directory.GetFiles(Application.dataPath + bundlePath + "Data/", "*.*", SearchOption.AllDirectories);
        if (dataSheetFiles != null && dataSheetFiles.Length > 0) {
            for(int i = 0;i< dataSheetFiles.Length;i++) {
                string filePathName = dataSheetFiles[i];

                int fileLength = filePathName.Length;
                if (fileLength > 5) {
                    string lastName = filePathName.Substring(fileLength - 5, 5);
                    if (lastName == ".meta") {
                        continue;
                    }
                }

                AddSheetGroupAssetBundleList("moisture_1", filePathName);
            }
        }

        // TextData
        string[] textDataFiles = Directory.GetFiles(Application.dataPath + bundlePath + "TextData/", "*.*", SearchOption.AllDirectories);
        if (textDataFiles != null && textDataFiles.Length > 0) {
            for (int i = 0; i < textDataFiles.Length; i++) {
                string filePathName = textDataFiles[i];

                int fileLength = filePathName.Length;
                if (fileLength > 5) {
                    string lastName = filePathName.Substring(fileLength - 5, 5);
                    if (lastName == ".meta") {
                        continue;
                    }
                }

                AddSheetGroupAssetBundleList("moisture_2", filePathName);
            }
        }

        // MapData
        string[] mapDataFiles = Directory.GetFiles(Application.dataPath + bundlePath + "MapData/", "*.*", SearchOption.AllDirectories);
        if (mapDataFiles != null && mapDataFiles.Length > 0) {
            for (int i = 0; i < mapDataFiles.Length; i++) {
                string filePathName = mapDataFiles[i];

                int fileLength = filePathName.Length;
                if (fileLength > 5) {
                    string lastName = filePathName.Substring(fileLength - 5, 5);
                    if (lastName == ".meta") {
                        continue;
                    }
                }

                AddSheetGroupAssetBundleList("moisture_3", filePathName);
            }
        }

        // SubData
        string[] subDataFiles = Directory.GetFiles(Application.dataPath + bundlePath + "SubData/", "*.*", SearchOption.AllDirectories);
        if (subDataFiles != null && subDataFiles.Length > 0) {
            for (int i = 0; i < subDataFiles.Length; i++) {
                string filePathName = subDataFiles[i];

                int fileLength = filePathName.Length;
                if (fileLength > 5) {
                    string lastName = filePathName.Substring(fileLength - 5, 5);
                    if (lastName == ".meta") {
                        continue;
                    }
                }

                AddSheetGroupAssetBundleList("moisture_4", filePathName);
            }
        }

        if (_groupAssetBundleList.Count > 0) {
            List<string> groupKeys = _groupAssetBundleList.Keys.ToList();
            for (int i = 0; i < groupKeys.Count; i++) {
                retValue.Add(GetGroupAssetBunldeBuild(_groupAssetBundleList[groupKeys[i]]));
            }
        }

        return retValue.ToArray();
    }

    static void AddSheetGroupAssetBundleList(string groupName, string filePathName)
    {
        if (_groupAssetBundleList.ContainsKey(groupName)) {
            AssetBundleGroupInfo bundleGroupInfo = _groupAssetBundleList[groupName];
            AssetBundleGroupPathInfo inputGroupPathInfo = new AssetBundleGroupPathInfo();
            inputGroupPathInfo.filePathName = filePathName;
            bundleGroupInfo.AddGroupPathInfo(inputGroupPathInfo);
        } else {
            AssetBundleGroupInfo bundleGroupInfo = new AssetBundleGroupInfo();
            string assetParentPath = "Assets" + filePathName.Replace(Application.dataPath, "").Replace('\\', '/');
            assetParentPath = assetParentPath.Substring(0, assetParentPath.LastIndexOf('/'));
            string assetBundleName = assetParentPath.Replace("Assets" + AssetBundleUtil.originBundlePath, "");
            bundleGroupInfo.assetBundleName = assetBundleName.Substring(0, assetBundleName.LastIndexOf('/') + 1) + groupName;

            AssetBundleGroupPathInfo inputGroupPathInfo = new AssetBundleGroupPathInfo();
            inputGroupPathInfo.filePathName = filePathName;
            bundleGroupInfo.AddGroupPathInfo(inputGroupPathInfo);
            _groupAssetBundleList.Add(groupName, bundleGroupInfo);
        }
    }

    static AssetBundleBuild GetGroupAssetBunldeBuild(AssetBundleGroupInfo bundleGroupInfo)
    {
        AssetBundleBuild retValue = new AssetBundleBuild();

        string[] assetNames = new string[bundleGroupInfo.groupPathInfos.Count];
        for(int i =0;i< bundleGroupInfo.groupPathInfos.Count;i++) {
            string filePathName = bundleGroupInfo.groupPathInfos[i].filePathName;
            string assetPath = "Assets" + filePathName.Replace(Application.dataPath, "").Replace('\\', '/');
            assetNames[i] = assetPath;
        }

        retValue.assetBundleName = bundleGroupInfo.assetBundleName;
        retValue.assetNames = assetNames;
        retValue.assetBundleVariant = "unity3d";

        return retValue;
    }

    static bool CheckValidPath(string path)
    {
        if(string.IsNullOrEmpty(path))
            return false;

        path = path.Replace('\\', '/');

        string[] pathSplits = path.Split('/');
        if(pathSplits != null && pathSplits.Length > 0) {
            for(int i = 0;i< pathSplits.Length;i++) {
                if(pathSplits[i] == "Data")
                    return false;

                if(pathSplits[i] == "TextData")
                    return false;

                if (pathSplits[i] == "MapData")
                    return false;

                if (pathSplits[i] == "SubData")
                    return false;
            }

            string lastPath = pathSplits[pathSplits.Length - 1];
            if(!string.IsNullOrEmpty(lastPath)) {
                char[] lastPathChars = lastPath.ToCharArray();
                if(lastPathChars[0] == '.') {
                    return false;
                }
            }
        }

        return true;
    }

    public static void Release()
    {
        if(_groupAssetBundleList != null)
            _groupAssetBundleList.Clear();
        _groupAssetBundleList = null;
    }

    #endregion
}

#endif
