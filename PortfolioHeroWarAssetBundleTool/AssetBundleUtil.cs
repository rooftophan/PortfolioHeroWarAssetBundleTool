using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class AssetBundleUtil
{
    public const string originBundlePath = "/_Package/Bundle/";

    public const string ftpRootPath = "";
    public const string assetCompleteListInfo = "{0}_{1}_{2}_AssetUploadCompleteList.dat";
    public const string bundlePatchInfoFile = "patchinfo_{0}.bytes";

    public const string assetBundleTargetFile = "AssetBundle_{0}_{1}.dat";

    public static string GetAssetBundleNamePath(string assetbundleName)
    {
        int lastIndex = assetbundleName.LastIndexOf('/');
        if(lastIndex <= 0)
            return "";
        
        return assetbundleName.Substring(0, lastIndex + 1);
    }

    public static List<string> GetAssetBundleNamePathList(string assetbundleName)
    {
        List<string> retValue = new List<string>();

        string[] splits = assetbundleName.Split('/');
        if (splits != null && splits.Length > 1) {
            for (int i = 0; i < splits.Length - 1; i++) {
                retValue.Add(splits[i]);
            }
        }

        return retValue;
    }

    public static string GetProjectRootPath()
    {
        return Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/') + 1);
    }

    public static string GetAssetBundleInfoRootPath()
    {
        string rootProjectPath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/'));
        rootProjectPath = rootProjectPath.Substring(0, rootProjectPath.LastIndexOf('/'));
        return rootProjectPath.Substring(0, rootProjectPath.LastIndexOf('/') + 1) + "AssetBundleBranches/AssetBundleToolInfo/";
    }

    public static long GetFileSize(string absolutePath)
    {
        System.IO.FileInfo fileInfo = new System.IO.FileInfo(absolutePath);
        if (fileInfo.Exists) {
            return fileInfo.Length;
        }
        return 0L;
    }

    public static AssetBundlePatchInfoData GetBundlePatchInfoData(string filePath)
    {
        AssetBundlePatchInfoData retValue = null;

        string readFile = File.ReadAllText(filePath, Encoding.UTF8);

        return retValue;
    }
}
