using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;
using System.Text;
using System;
using System.IO;

public class AssetBundleJson
{
    #region AssetBundlesSetting

    public static void WriteAssetBundlesSetting(string path, string fileName, AssetBundlesSettingInfo bundleSettingInfo)
    {
        if(bundleSettingInfo == null)
            return;

        try {
            BundleFile.WriteStringToFilePath(JsonMapper.ToJson(bundleSettingInfo), path, fileName);
        } catch (Exception ex) {
            Debug.Log(string.Format("WriteAssetBundlesSetting exception : {0}", ex));
        }
    }

    public static void WriteTempSheetByteFile(string path, string fileName, TempTextSheetOutputData textSheetOutPutData)
    {
        if (textSheetOutPutData == null)
            return;

        try {
            BundleFile.WriteStringToFilePath(JsonMapper.ToJson(textSheetOutPutData), path, fileName);
        } catch (Exception ex) {
            Debug.Log(string.Format("WriteAssetBundlesSetting exception : {0}", ex));
        }
    }

    public static AssetBundlesSettingInfo LoadAssetBundlesSettingInfo(string path, string fileName)
    {
        AssetBundlesSettingInfo retValue = null;

        try {
            string loadFile = BundleFile.ReadStringFromFilePath(path, fileName);
            if (!string.IsNullOrEmpty(loadFile)) {
                retValue = JsonMapper.ToObject<AssetBundlesSettingInfo>(loadFile);
            }
        } catch(Exception ex) {
            Debug.Log(string.Format("LoadAssetBundlesSettingInfo exception : {0}", ex));
        }

        return retValue;
    }

    #endregion

    #region AssetBundlesTargetInfo

    public static void WriteAssetBundlesTargetInfo(string path, string fileName, BundleToolTargetSetting bundleTargetInfo)
    {
        if (bundleTargetInfo == null)
            return;

        try {
            BundleFile.WriteStringToFilePath(JsonMapper.ToJson(bundleTargetInfo), path, fileName);
        } catch (Exception ex) {
            Debug.Log(string.Format("WriteAssetBundlesSetting exception : {0}", ex));
        }
    }

    public static BundleToolTargetSetting LoadAssetBundlesTargetInfo(string path, string fileName)
    {
        BundleToolTargetSetting retValue = null;

        try {
            string loadFile = BundleFile.ReadStringFromFilePath(path, fileName);
            if (!string.IsNullOrEmpty(loadFile)) {
                retValue = JsonMapper.ToObject<BundleToolTargetSetting>(loadFile);
            }
        } catch (Exception ex) {
            Debug.Log(string.Format("LoadAssetBundlesTargetInfo exception : {0}", ex));
        }

        return retValue;
    }

    #endregion

    #region AssetBundle Upload Info

    public static void WriteUploadCompleteListInfo(string path, string fileName, AssetUploadCompleteListInfo completeListInfo)
    {
        if(completeListInfo == null)
            return;

        string writeList = JsonMapper.ToJson(completeListInfo);
        BundleFile.WriteStringToFilePath(writeList, path, fileName);
    }

    public static AssetUploadCompleteListInfo LoadAssetUploadCompleteListInfo(string path, string fileName)
    {
        AssetUploadCompleteListInfo retValue = null;

        try {
            string loadFile = BundleFile.ReadStringFromFilePath(path, fileName);
            if (!string.IsNullOrEmpty(loadFile)) {
                retValue = JsonMapper.ToObject<AssetUploadCompleteListInfo>(loadFile);
            }
        } catch (Exception ex) {
            Debug.Log(string.Format("LoadAssetUploadCompleteListInfo exception : {0}", ex));
        }

        return retValue;
    }

    #endregion

    #region Bundle Intergrity Info

    public static void WriteBundlePatchInfo(string path, string fileName, AssetBundlePatchInfoData patchInfoDataInfo, bool compress)
    {
        if (patchInfoDataInfo == null)
            return;

        string writeList = JsonMapper.ToJson(patchInfoDataInfo);
        if(compress) {
            string filePath = Path.Combine(path, fileName);
            byte[] infoBytes = Encoding.UTF8.GetBytes(writeList);
            byte[] compressBytes = CLZF2.Compress(infoBytes);
            BundleFile.WriteBytesToFilePath(compressBytes, path, fileName);
        } else {
            BundleFile.WriteStringToFilePath(writeList, path, fileName);
        }
    }

    public static AssetBundlePatchInfoData LoadBundlePatchInfo(string filePath, bool compress)
    {
        AssetBundlePatchInfoData retValue = null;

        try {
            if(compress) {
                retValue = BundleFile.ReadCLZFFilePath<AssetBundlePatchInfoData>(filePath);
            } else {
                string loadFile = BundleFile.ReadTextFromFilePath(filePath);
                if (!string.IsNullOrEmpty(loadFile)) {
                    retValue = JsonMapper.ToObject<AssetBundlePatchInfoData>(loadFile);
                }
            }
           
        } catch (Exception ex) {
            Debug.Log(string.Format("LoadAssetUploadCompleteListInfo exception : {0}", ex));
        }

        return retValue;
    }

    #endregion
}
