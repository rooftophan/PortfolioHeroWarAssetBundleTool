using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AssetBundleTarget
{
    Android     = 0,
    iOS         = 1,
}

public enum AssetBundleBuildKind
{
    Daily   = 0,
    Dev     = 1,
    ENT     = 2,
    FGT     = 3,
    INAPP   = 4,
    LIVE    = 5,
}

public enum AssetBundleVersionKind
{
    StreamingAssetBuild     = 0, // StreamingAsset Build
    ResOrSheetPatchUp       = 1,
}

public enum AssetBundleFileKind
{
    Resource,
    Sheet,
}

public enum AssetBundleProgressType
{
    CreateFTPResFolder,
    UploadFTPRes,
    CreateFTPPlanDataFolder,
    UploadFTPPlanData
}

public enum AssetBundleCheckVerState
{
    Valid = 0,
    VersionNullOrEmpty,
    ExistVersionInfo,
    AppVersionInfoNULL,
}

public class BundleToolTargetSetting
{
    public int curAppVersionIndex;

    public int assetBundleTarget;
    public int buildKind;

    public List<AssetBundlesAppVersionInfo> appVersionInfos;

    public void Init()
    {
        curAppVersionIndex = -1;
        assetBundleTarget = (int)AssetBundleTarget.Android;
        buildKind = (int)AssetBundleBuildKind.Daily;

        appVersionInfos = new List<AssetBundlesAppVersionInfo>();
    }

    public AssetBundlesAppVersionInfo GetCurAppVersionInfo()
    {
        if(appVersionInfos != null && appVersionInfos.Count > 0 && appVersionInfos.Count > curAppVersionIndex) {
            if(curAppVersionIndex == -1) {
                curAppVersionIndex = 0;
            }

            return appVersionInfos[curAppVersionIndex];
        }

        return null;
    }

    public string[] GetAppVersionArray()
    {
        if(appVersionInfos == null || appVersionInfos.Count == 0)
            return null;

        string[] retValues = new string[appVersionInfos.Count];
        for(int i = 0;i< appVersionInfos.Count;i++) {
            retValues[i] = appVersionInfos[i].appVersion;
        }

        return retValues;
    }

    public int GetLastResVersion()
    {
        List<string> versionList = new List<string>();
        int lastVersion = 0;
        if(appVersionInfos != null && appVersionInfos.Count > 0) {
            for(int i = 0;i< appVersionInfos.Count;i++) {
                if(!string.IsNullOrEmpty(appVersionInfos[i].streamingAssetResVer)) {
                    versionList.Add(appVersionInfos[i].streamingAssetResVer);
                }
                
                if(appVersionInfos[i].patchUpResVerList != null && appVersionInfos[i].patchUpResVerList.Count > 0) {
                    for(int j = 0;j< appVersionInfos[i].patchUpResVerList.Count;j++) {
                        if(!string.IsNullOrEmpty(appVersionInfos[i].patchUpResVerList[j])) {
                            versionList.Add(appVersionInfos[i].patchUpResVerList[j]);
                        }
                    }
                }
            }
        }

        for(int i = 0;i< versionList.Count;i++) {
            int outVersion = -1;
            if(int.TryParse(versionList[i], out outVersion)) {
                if(lastVersion < outVersion) {
                    lastVersion = outVersion;
                }
            }
        }

        lastVersion += 1;

        return lastVersion;
    }

    public int GetLastSheetVersion()
    {
        List<string> versionList = new List<string>();
        int lastVersion = 0;
        if (appVersionInfos != null && appVersionInfos.Count > 0) {
            for (int i = 0; i < appVersionInfos.Count; i++) {
                if (!string.IsNullOrEmpty(appVersionInfos[i].streamingAssetSheetVer)) {
                    versionList.Add(appVersionInfos[i].streamingAssetSheetVer);
                }

                if (appVersionInfos[i].patchUpSheetVerList != null && appVersionInfos[i].patchUpSheetVerList.Count > 0) {
                    for (int j = 0; j < appVersionInfos[i].patchUpSheetVerList.Count; j++) {
                        if (!string.IsNullOrEmpty(appVersionInfos[i].patchUpSheetVerList[j])) {
                            versionList.Add(appVersionInfos[i].patchUpSheetVerList[j]);
                        }
                    }
                }
            }
        }

        for (int i = 0; i < versionList.Count; i++) {
            int outVersion = -1;
            if (int.TryParse(versionList[i], out outVersion)) {
                if (lastVersion < outVersion) {
                    lastVersion = outVersion;
                }
            }
        }

        lastVersion += 1;

        return lastVersion;
    }

    public AssetBundlesAppVersionInfo AddAppVersion(string addAppVersion)
    {
        if(string.IsNullOrEmpty(addAppVersion))
            return null;

        if (appVersionInfos != null && appVersionInfos.Count > 0) {
            for (int i = 0; i < appVersionInfos.Count; i++) {
                if (appVersionInfos[i].appVersion == addAppVersion) {
                    return null;
                }
            }
        }

        if(appVersionInfos == null)
            appVersionInfos = new List<AssetBundlesAppVersionInfo>();

        AssetBundlesAppVersionInfo inputAppVersionInfo = new AssetBundlesAppVersionInfo();
        inputAppVersionInfo.Init();
        inputAppVersionInfo.appVersion = addAppVersion;
        appVersionInfos.Add(inputAppVersionInfo);
        if(curAppVersionIndex == -1) {
            curAppVersionIndex = 0;
        }

        return inputAppVersionInfo;
    }

    public bool RemoveAppVersionInfo(string removeVersion)
    {
        if(appVersionInfos != null && appVersionInfos.Count > 0) {
            for (int i = 0; i < appVersionInfos.Count; i++) {
                if(appVersionInfos[i].appVersion == removeVersion) {
                    appVersionInfos.RemoveAt(i);
                    return true;
                }
            }
        }

        return false;
    }

    public AssetBundleCheckVerState CheckValidResVersion(string resVer, AssetBundleVersionKind versionKind)
    {
        if(string.IsNullOrEmpty(resVer)) {
            return AssetBundleCheckVerState.VersionNullOrEmpty;
        }

        AssetBundlesAppVersionInfo curAppVersionInfo = GetCurAppVersionInfo();
        if(curAppVersionInfo == null)
            return AssetBundleCheckVerState.AppVersionInfoNULL;

        if(versionKind == AssetBundleVersionKind.StreamingAssetBuild) {
            if(curAppVersionInfo.CheckExistResVersion(resVer)) {
                return AssetBundleCheckVerState.ExistVersionInfo;
            }

            if(appVersionInfos != null && appVersionInfos.Count > 0) {
                for(int i = 0;i< appVersionInfos.Count;i++) {
                    if(appVersionInfos[i].appVersion == curAppVersionInfo.appVersion)
                        continue;

                    if(appVersionInfos[i].streamingAssetResVer == resVer)
                        return AssetBundleCheckVerState.ExistVersionInfo;

                    if(appVersionInfos[i].CheckExistResVersion(resVer))
                        return AssetBundleCheckVerState.ExistVersionInfo;
                }
            }

        } else if(versionKind == AssetBundleVersionKind.ResOrSheetPatchUp) {
            if(curAppVersionInfo.streamingAssetResVer == resVer)
                return AssetBundleCheckVerState.ExistVersionInfo;

            if (appVersionInfos != null && appVersionInfos.Count > 0) {
                for (int i = 0; i < appVersionInfos.Count; i++) {
                    if (appVersionInfos[i].appVersion == curAppVersionInfo.appVersion)
                        continue;

                    if (appVersionInfos[i].streamingAssetResVer == resVer)
                        return AssetBundleCheckVerState.ExistVersionInfo;

                    if (appVersionInfos[i].CheckExistResVersion(resVer))
                        return AssetBundleCheckVerState.ExistVersionInfo;
                }
            }
        }

        return AssetBundleCheckVerState.Valid;
    }

    public AssetBundleCheckVerState CheckValidSheetVersion(string sheetVer, AssetBundleVersionKind versionKind)
    {
        if (string.IsNullOrEmpty(sheetVer)) {
            return AssetBundleCheckVerState.VersionNullOrEmpty;
        }

        AssetBundlesAppVersionInfo curAppVersionInfo = GetCurAppVersionInfo();
        if (curAppVersionInfo == null)
            return AssetBundleCheckVerState.AppVersionInfoNULL;

        if (versionKind == AssetBundleVersionKind.StreamingAssetBuild) {
            if (curAppVersionInfo.CheckExistSheetVersion(sheetVer)) {
                return AssetBundleCheckVerState.ExistVersionInfo;
            }

            if (appVersionInfos != null && appVersionInfos.Count > 0) {
                for (int i = 0; i < appVersionInfos.Count; i++) {
                    if (appVersionInfos[i].appVersion == curAppVersionInfo.appVersion)
                        continue;

                    if (appVersionInfos[i].streamingAssetSheetVer == sheetVer)
                        return AssetBundleCheckVerState.ExistVersionInfo;

                    if (appVersionInfos[i].CheckExistSheetVersion(sheetVer))
                        return AssetBundleCheckVerState.ExistVersionInfo;
                }
            }

        } else if (versionKind == AssetBundleVersionKind.ResOrSheetPatchUp) {
            if (curAppVersionInfo.streamingAssetSheetVer == sheetVer)
                return AssetBundleCheckVerState.ExistVersionInfo;

            if (appVersionInfos != null && appVersionInfos.Count > 0) {
                for (int i = 0; i < appVersionInfos.Count; i++) {
                    if (appVersionInfos[i].appVersion == curAppVersionInfo.appVersion)
                        continue;

                    if (appVersionInfos[i].streamingAssetSheetVer == sheetVer)
                        return AssetBundleCheckVerState.ExistVersionInfo;

                    if (appVersionInfos[i].CheckExistSheetVersion(sheetVer))
                        return AssetBundleCheckVerState.ExistVersionInfo;
                }
            }
        }

        return AssetBundleCheckVerState.Valid;
    }
}

public class AssetBundlesAppVersionInfo
{
    public string appVersion;
    //public int compareResVersion;
    public int resourceVersion;

    //public int compareSheetVersion;
    public int sheetVersion;

    public int versionKind;

    public string streamingAssetResVer;
    public List<string> patchUpResVerList;

    public string streamingAssetSheetVer;
    public List<string> patchUpSheetVerList;

    //public Dictionary<string, BundleToolTargetSetting> bundleTargetSettings;

    public void Init()
    {
        //compareResVersion = 0;
        resourceVersion = 0;

        //compareSheetVersion = 0;
        sheetVersion = 0;

        streamingAssetResVer = "";
        patchUpResVerList = new List<string>();

        streamingAssetSheetVer = "";
        patchUpSheetVerList = new List<string>();

        versionKind = (int)AssetBundleVersionKind.StreamingAssetBuild;

        //bundleTargetSettings = new Dictionary<string, BundleToolTargetSetting>();
    }

    public bool CheckExistResVersion(string resVer)
    {
        if (patchUpResVerList != null && patchUpResVerList.Count > 0) {
            for (int i = 0; i < patchUpResVerList.Count; i++) {
                if (patchUpResVerList[i] == resVer) {
                    return true;
                }
            }
        }

        return false;
    }

    public bool CheckExistSheetVersion(string sheetVer)
    {
        if (patchUpSheetVerList != null && patchUpSheetVerList.Count > 0) {
            for (int i = 0; i < patchUpSheetVerList.Count; i++) {
                if (patchUpSheetVerList[i] == sheetVer) {
                    return true;
                }
            }
        }

        return false;
    }

    public void AddPatchUpResVersion(string addVer)
    {
        if(patchUpResVerList == null) {
            patchUpResVerList = new List<string>();
            patchUpResVerList.Add(addVer);
            return;
        }

        for(int i = 0;i< patchUpResVerList.Count;i++) {
            if(patchUpResVerList[i] == addVer) {
                return;
            }
        }

        patchUpResVerList.Add(addVer);
    }

    public void AddPatchUpSheetVersion(string addVer)
    {
        if (patchUpSheetVerList == null) {
            patchUpSheetVerList = new List<string>();
            patchUpSheetVerList.Add(addVer);
            return;
        }

        for (int i = 0; i < patchUpSheetVerList.Count; i++) {
            if (patchUpSheetVerList[i] == addVer) {
                return;
            }
        }

        patchUpSheetVerList.Add(addVer);
    }

    public string GetPatchUpResVerListStr()
    {
        string retValue = "";

        if(patchUpResVerList != null && patchUpResVerList.Count > 0) {
            for(int i = 0;i < patchUpResVerList.Count;i++) {
                if(i > 0) {
                    retValue += ", ";
                }

                retValue += patchUpResVerList[i];
            }
        }

        return retValue;
    }

    public void RemoveResVerList(string removeVer)
    {
        if (patchUpResVerList != null && patchUpResVerList.Count > 0) {
            for (int i = 0; i < patchUpResVerList.Count; i++) {
                if(patchUpResVerList[i] == removeVer) {
                    patchUpResVerList.RemoveAt(i);
                    break;
                }
            }
        }
    }

    public string GetPatchUpSheetVerListStr()
    {
        string retValue = "";

        if (patchUpSheetVerList != null && patchUpSheetVerList.Count > 0) {
            for (int i = 0; i < patchUpSheetVerList.Count; i++) {
                if (i > 0) {
                    retValue += ", ";
                }

                retValue += patchUpSheetVerList[i];
            }
        }

        return retValue;
    }

    public void RemoveSheetVerList(string removeVer)
    {
        if (patchUpSheetVerList != null && patchUpSheetVerList.Count > 0) {
            for (int i = 0; i < patchUpSheetVerList.Count; i++) {
                if (patchUpSheetVerList[i] == removeVer) {
                    patchUpSheetVerList.RemoveAt(i);
                    break;
                }
            }
        }
    }

    public int GetResPatchCount(string resVer)
    {
        int retValue = 0;
        if (streamingAssetResVer == resVer) {
            retValue = 0;
        } else {
            retValue++;
            for (int i = 0;i< patchUpResVerList.Count; i++) {
                if(patchUpResVerList[i] == resVer) {
                    break;
                }

                retValue++;
            }
        }

        return retValue;
    }

    public int GetSheetPatchCount(string sheetVer)
    {
        int retValue = 0;
        if (streamingAssetSheetVer == sheetVer) {
            retValue = 0;
        } else {
            retValue++;
            for (int i = 0; i < patchUpSheetVerList.Count; i++) {
                if (patchUpSheetVerList[i] == sheetVer) {
                    break;
                }

                retValue++;
            }
        }

        return retValue;
    }
}

public class AssetBundlesSettingInfo
{
    public int assetBundleTarget;
    public int buildKind;

    public string ftpURL;
    public string ftpID;

    public void Init()
    {
        assetBundleTarget = (int)AssetBundleTarget.Android;
        buildKind = (int)AssetBundleBuildKind.Daily;

        ftpURL = "";
        ftpID = "";
    }
}

public class AssetUploadCompleteInfo
{
    public string temp = "";
}

public class AssetUploadCompleteListInfo
{
    public bool isComplete = false;
    public Dictionary<string, AssetUploadCompleteInfo> uploadCompleteList;
}

public class AssetBundleGroupPathInfo
{
    public long size;
    public string lastFileName;
    public string filePathName;
}

public class AssetBundleGroupInfo
{
    public string assetBundleName;
    public long groupSize = 0;
    public List<AssetBundleGroupPathInfo> groupPathInfos = new List<AssetBundleGroupPathInfo>();

    public void AddGroupPathInfo(AssetBundleGroupPathInfo inputPathInfo)
    {
        inputPathInfo.size = AssetBundleUtil.GetFileSize(inputPathInfo.filePathName);
        groupSize += inputPathInfo.size;
        groupPathInfos.Add(inputPathInfo);
    }
}

public class TempTextSheetOutputInfo
{
    public string textKey;
    public string textValue;
    public int byteSize;
}

public class TempTextSheetOutputData
{
    public List<TempTextSheetOutputInfo> tempTextSheetOutputList = new List<TempTextSheetOutputInfo>();
}