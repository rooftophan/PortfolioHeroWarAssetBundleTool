using LitJson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AssetBundleManager : MonoBehaviour
{
    #region Serialize Variables

#pragma warning disable 649

    [SerializeField] AssetBundleTarget _bundleTarget = default(AssetBundleTarget);
    [SerializeField] AssetBundleBuildKind _bundleBuildKind = default(AssetBundleBuildKind);

    [SerializeField] string _resourceVersion = default(string);
    [SerializeField] string _planDataVersion = default(string);

    [SerializeField] Slider _progressSlider = default(Slider);
    [SerializeField] Text _downCountText = default(Text);
    [SerializeField] Text _curDownAssetNameText = default(Text);

#pragma warning restore 649

    #endregion

    #region Variables

    private string _assetBundleUrl = string.Empty;
    string _ftpDownPath;

    AssetBundleDefinitions.AssetBundleStep _bundleStep;

    string _mainAssetBundleName;
    int _curAssetBundleIndex = 0;
    AssetBundleManifest _mainAssetBundleManifest = null;
    List<string> _assetBundleNameList = null;

    int _totalAssetCount;
    int _curAssetCount;

    string _assetBundlePatchInfoFile;

    AssetBundlePatchInfoData _patchInfoData = null;

    #endregion

    #region MonoBehaviour Methods

    private void Awake()
    {
        _assetBundleUrl = GameSystem.Instance.SystemData.gameConfigData.FTPURL;
        
        _bundleStep = AssetBundleDefinitions.AssetBundleStep.None;
        InitAssetPath();
        _curAssetCount = 0;
        _totalAssetCount = 0;
        _curDownAssetNameText.text = "";
        SetCurDownCount();
    }

    #endregion

    #region Methods

    void InitAssetPath()
    {
        
    }

    void DownloadAssetBundlesMain()
    {
        StartCoroutine(DownLoadAssetBundle(_ftpDownPath, _mainAssetBundleName, OnCompleteAssetBundleDown, OnFailWebRequest));
    }

    void DownloadAssetBundlesPatchInfo()
    {
        StartCoroutine(DownLoadAssetBundle(_ftpDownPath, _assetBundlePatchInfoFile, OnCompleteAssetBundleDown, OnFailWebRequest));
    }

    void StartDownloadAssetBundle()
    {
        _curAssetBundleIndex = 0;
        if (_assetBundleNameList != null) {
            if (_curAssetBundleIndex < _assetBundleNameList.Count) {
                string bundleName = _assetBundleNameList[_curAssetBundleIndex];
                _curAssetBundleIndex++;
                StartCoroutine(DownLoadAssetBundle(_ftpDownPath, bundleName, OnCompleteAssetBundleDown, OnFailWebRequest));
            }
        }
    }

    void SetBundleStep(AssetBundleDefinitions.AssetBundleStep step)
    {
        _bundleStep = step;
        switch(_bundleStep) {
            case AssetBundleDefinitions.AssetBundleStep.AssetBundlesPatchInfoDown:
                DownloadAssetBundlesPatchInfo();
                break;
            case AssetBundleDefinitions.AssetBundleStep.AssetBundlesMainDown:
                DownloadAssetBundlesMain();
                break;
            case AssetBundleDefinitions.AssetBundleStep.AssetBundleListDown:
                StartDownloadAssetBundle();
                break;
        }
    }

    #endregion

    #region Coroutine Methods

    IEnumerator DownLoadAssetBundle(string filePath, string assetBundleName, Action<string, string, UnityWebRequest> onComplete, Action<string, string, string> onFail)
    {
        string url = _assetBundleUrl + filePath + assetBundleName + "?t=" + TimeUtil.GetTimeStamp().ToString();

        UnityWebRequest request = null;

        if(_bundleStep == AssetBundleDefinitions.AssetBundleStep.AssetBundlesPatchInfoDown) {
            request = UnityWebRequest.Get(url);
        } else {
            request = UnityWebRequestAssetBundle.GetAssetBundle(url, 0);
        }

        yield return request.SendWebRequest();
        if (request.isNetworkError || request.isHttpError) {
            Debug.LogError(request.error);
            if (onFail != null)
                onFail(filePath, assetBundleName, request.error);
        } else {
            Debug.Log("File successfully downloaded and saved to " + filePath);
            
            if (onComplete != null)
                onComplete(filePath, assetBundleName, request);
        }
    }

    void SetCurDownCount()
    {
        _downCountText.text = string.Format("{0} / {1}", _curAssetCount, _totalAssetCount);
        _progressSlider.value = (float)_curAssetCount / (float)_totalAssetCount;
    }

    #endregion

    #region CallBack Methods

    void OnCompleteAssetBundleDown(string filePath, string assetBundleName, UnityWebRequest request)
    {
        AssetBundle bundle = null;

        if(_bundleStep == AssetBundleDefinitions.AssetBundleStep.AssetBundlesPatchInfoDown) {
            byte[] data = request.downloadHandler.data;
            string result = System.Text.Encoding.UTF8.GetString(data);

            _patchInfoData = JsonMapper.ToObject<AssetBundlePatchInfoData>(result);
            _assetBundleNameList = _patchInfoData.patchList.ToList();
            _totalAssetCount = _assetBundleNameList.Count;
            SetCurDownCount();
            SetBundleStep(AssetBundleDefinitions.AssetBundleStep.AssetBundleListDown);
        } else if (_bundleStep == AssetBundleDefinitions.AssetBundleStep.AssetBundlesMainDown) {
            bundle = DownloadHandlerAssetBundle.GetContent(request);
            _mainAssetBundleManifest = bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            _assetBundleNameList = _mainAssetBundleManifest.GetAllAssetBundles().ToList();
            _totalAssetCount = _assetBundleNameList.Count;
            SetCurDownCount();
            SetBundleStep(AssetBundleDefinitions.AssetBundleStep.AssetBundleListDown);
        } else if (_bundleStep == AssetBundleDefinitions.AssetBundleStep.AssetBundleListDown) {
            bundle = DownloadHandlerAssetBundle.GetContent(request);
            _curDownAssetNameText.text = assetBundleName;
            _curAssetCount++;
            SetCurDownCount();
            if (_assetBundleNameList != null) {
                if (_curAssetBundleIndex < _assetBundleNameList.Count) {
                    string bundleName = _assetBundleNameList[_curAssetBundleIndex];
                    _curAssetBundleIndex++;
                    StartCoroutine(DownLoadAssetBundle(_ftpDownPath, bundleName, OnCompleteAssetBundleDown, OnFailWebRequest));
                } else {
                    SetBundleStep(AssetBundleDefinitions.AssetBundleStep.Complete);
                }
            }
        }

        if(bundle != null)
            bundle.Unload(false);
    }

    void OnFailWebRequest(string filePath, string fileName, string error)
    {

    }

    public void OnStartResourceAssetBundleDown()
    {
        if(_bundleStep != AssetBundleDefinitions.AssetBundleStep.None)
            return;

        _mainAssetBundleName = "Resources";

        _assetBundlePatchInfoFile = string.Format("patchinfo_{0}.bytes", _resourceVersion);

        _curAssetCount = 0;
        _ftpDownPath = AssetBundleUtil.ftpRootPath + string.Format("{0}_{1}_Res_{2}/", _bundleTarget.ToString(), _bundleBuildKind.ToString(), _resourceVersion);
        SetBundleStep(AssetBundleDefinitions.AssetBundleStep.AssetBundlesPatchInfoDown);
    }

    public void OnResetBundleInfo()
    {
        _curAssetBundleIndex = 0;

        _mainAssetBundleManifest = null;
        if(_assetBundleNameList != null)
            _assetBundleNameList.Clear();
        _assetBundleNameList = null;

        _bundleStep = AssetBundleDefinitions.AssetBundleStep.None;
        InitAssetPath();
        _curAssetCount = 0;
        _totalAssetCount = 0;
        _curDownAssetNameText.text = "";
        SetCurDownCount();
    }

    #endregion
}
