using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;
using System.Linq;
using System.Threading;

#if UNITY_EDITOR
using UnityEditor;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using Renci.SshNet.Common;

public enum AssetFTPUploadState
{
    FTPUploadStart,
    FTPUploading,
    FTPUploadFinish,
}

public class AssetFTPUploaderManager
{
    #region SubClass

    public class AssetBundleUploadInfo
    {
        public AssetBundleUploadInfo(string uri, string local, List<string> paths)
        {
            ftpUri = uri;
            localPath = local;
            pathList = paths;
        }

        public string ftpUri;
        public string localPath;
        public List<string> pathList;
        public bool isUploading = false;
    }

    #endregion

    #region Definitions

    public enum AssetFTPUploadStep
    {
        None,
        MakeAssetBundleDirectory,
        UploadAssetBundle,
        MakeSFTPBundleDirectory,
        UploadSFTPAssetBundle,
    }

    public enum FTPUploadKind
    {
        Resources,
        Sheet,
    }

    #endregion

    #region Variables

    AssetFTPUploadStep _assetUploadStep;
    FTPUploadKind _uploadKind;

    string _ftpUploadAssetPath;
    string _rootLocalPath;

    string _ftpURL;
    string _ftpID;
    string _ftpPW;

    string _assetBundlesFileName;

    AssetBundleManifest _mainAssetBundleManifest = null;
    List<string> _assetBundleNameList = null;

    Queue<string> _pathQueue = new Queue<string>();
    Queue<AssetBundleUploadInfo> _uploadAssetInfoQueue = new Queue<AssetBundleUploadInfo>();

    string _curCreatePathName = "";
    AssetBundleUploadInfo _curUploadInfo = null;

    Action<AssetFTPUploadState> _onFTPUploadState = null;

    int _curCreatePathCount = 0;
    int _createPathMaxCount = 0;

    int _curUploadCount = 0;
    int _uploadMaxCount = 0;

    bool _isUploadState = false;
    bool _isUpdatePathCreate = false;

    int _curSaveCheckUploadCount = 0;
    int _uploadSaveIntervalCount = 1000;

    Dictionary<string, string> _pathMakeList = new Dictionary<string, string>();

    //AssetUploadCompleteListInfo _uploadCompleteListInfo = null;

    AssetBundleTarget _bundleTarget;
    AssetBundleBuildKind _buildKind;
    string _curUploadVersion;

    ConnectionInfo _sftpConnectInfo = null;
    string _sftpKeyFile;

    #endregion

    #region Properties

    public string RootLocalPath
    {
        get { return _rootLocalPath; }
        set { _rootLocalPath = value; }
    }

    public AssetFTPUploadStep AssetUploadStep
    {
        get { return _assetUploadStep; }
    }

    public FTPUploadKind UploadKind
    {
        get { return _uploadKind; }
        set { _uploadKind = value; }
    }

    public Action<AssetFTPUploadState> OnFTPUploadState
    {
        get { return _onFTPUploadState; }
        set { _onFTPUploadState = value; }
    }

    public int CurUploadCount
    {
        get { return _curUploadCount; }
        set { _curUploadCount = value; }
    }

    public int UploadMaxCount
    {
        get { return _uploadMaxCount; }
        set { _uploadMaxCount = value; }
    }

    public AssetBundleTarget BundleTarget
    {
        get { return _bundleTarget; }
        set { _bundleTarget = value; }
    }

    public AssetBundleBuildKind BuildKind
    {
        get { return _buildKind; }
        set { _buildKind = value; }
    }

    public string CurUploadVersion
    {
        get { return _curUploadVersion; }
        set { _curUploadVersion = value; }
    }

    public string SftpKeyFile
    {
        get { return _sftpKeyFile; }
        set { _sftpKeyFile = value; }
    }

    #endregion

    #region Methods

    public void InitAssetFTPUploader()
    {
        _assetUploadStep = AssetFTPUploadStep.None;
        _pathQueue.Clear();
        _curUploadInfo = null;

        _pathMakeList.Clear();
    }

    public void UploadFtpAssetBundleList(string uploadAssetPath, string bundleInfoPath, string bundlesFileName, string url, string id, string pw)
    {
        _ftpURL = url;
        _ftpID = id;
        _ftpPW = pw;
        _ftpUploadAssetPath = uploadAssetPath;

        _assetBundlesFileName = bundlesFileName;

        AssetBundle bundle = AssetBundle.LoadFromFile(bundleInfoPath);
        if(bundle == null) {
            if (EditorUtility.DisplayDialog("AssetBundle",
               "AssetBundleBuildListInfo Is Null.", "OK")) {

            }
            return;
        }
        _mainAssetBundleManifest = bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        string[] assetBundles = _mainAssetBundleManifest.GetAllAssetBundles();

        _assetBundleNameList = assetBundles.ToList();

        bundle.Unload(false);

        SetAssetFTPUploadStep(AssetFTPUploadStep.MakeAssetBundleDirectory);
    }

    public void UploadSFtpAssetBundleList(string uploadAssetPath, string bundleInfoPath, string bundlesFileName, string host, string id)
    {
        _ftpURL = host;
        _ftpID = id;
        _ftpUploadAssetPath = uploadAssetPath;

        string fileName = Path.Combine(BannerToolUtil.GetSFTPRootPath(), _sftpKeyFile);
        var pk = new PrivateKeyFile(fileName);
        var keyFiles = new[] { pk };

        var methods = new List<AuthenticationMethod>();
        methods.Add(new PrivateKeyAuthenticationMethod(id, keyFiles));

        _sftpConnectInfo = new ConnectionInfo(host,
            id,
            methods.ToArray());

        _assetBundlesFileName = bundlesFileName;

        AssetBundle bundle = AssetBundle.LoadFromFile(bundleInfoPath);
        if (bundle == null) {
            if (EditorUtility.DisplayDialog("AssetBundle",
               "AssetBundleBuildListInfo Is Null.", "OK")) {

            }
            return;
        }
        _mainAssetBundleManifest = bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        string[] assetBundles = _mainAssetBundleManifest.GetAllAssetBundles();

        _assetBundleNameList = assetBundles.ToList();

        bundle.Unload(false);

        SetAssetFTPUploadStep(AssetFTPUploadStep.MakeSFTPBundleDirectory);
    }

    public void UploadFtpAssetBundlePatchList(string uploadAssetPath, List<string> patchList, string bundlesFileName, string url, string id, string pw)
    {
        _ftpURL = url;
        _ftpID = id;
        _ftpPW = pw;
        _ftpUploadAssetPath = uploadAssetPath;

        _assetBundlesFileName = bundlesFileName;

        _assetBundleNameList = patchList;

        SetAssetFTPUploadStep(AssetFTPUploadStep.MakeAssetBundleDirectory);
    }

    public void UploadSFtpAssetBundlePatchList(string uploadAssetPath, List<string> patchList, string bundlesFileName, string host, string id)
    {
        _ftpURL = host;
        _ftpID = id;

        string fileName = Path.Combine(BannerToolUtil.GetSFTPRootPath(), _sftpKeyFile);
        var pk = new PrivateKeyFile(fileName);
        var keyFiles = new[] { pk };

        var methods = new List<AuthenticationMethod>();
        methods.Add(new PrivateKeyAuthenticationMethod(id, keyFiles));

        _sftpConnectInfo = new ConnectionInfo(host,
            id,
            methods.ToArray());

        _ftpUploadAssetPath = uploadAssetPath;

        _assetBundlesFileName = bundlesFileName;

        _assetBundleNameList = patchList;

        SetAssetFTPUploadStep(AssetFTPUploadStep.MakeSFTPBundleDirectory);
    }

    public IEnumerator UpdateCreateFolder()
    {
        while(_isUpdatePathCreate) {
            if(_pathQueue.Count == 0) {
                _isUpdatePathCreate = false;
                CheckNextCreateFolderStep();
                continue;
            }

            if(_curCreatePathName != _pathQueue.Peek()) {
                _curCreatePathName = _pathQueue.Peek();
                FtpCreateFolder(_curCreatePathName, _ftpID, _ftpPW);

                if(_assetUploadStep == AssetFTPUploadStep.MakeAssetBundleDirectory) {
                    string progressTitle = "";
                    if (_uploadKind == FTPUploadKind.Resources) {
                        progressTitle = "FTP Create Path Resources";
                    } else {
                        progressTitle = "FTP Create Path Sheet";
                    }

                    EditorUtility.DisplayProgressBar(progressTitle, _curCreatePathName, (float)_curCreatePathCount / (float)_createPathMaxCount);
                }
            }

            yield return null;
        }

        if (_assetUploadStep == AssetFTPUploadStep.MakeAssetBundleDirectory)
            EditorUtility.ClearProgressBar();
    }

    public IEnumerator UpdateSFTPCreateFolder()
    {
        using (var sftp = new SftpClient(_sftpConnectInfo)) {
            sftp.Connect();

            while (_isUpdatePathCreate) {
                if (_pathQueue.Count == 0) {
                    _isUpdatePathCreate = false;
                    CheckNextCreateFolderStep();
                    continue;
                }

                if (_curCreatePathName != _pathQueue.Peek()) {
                    _curCreatePathName = _pathQueue.Peek();
                    CreateSFtpFolder(sftp, _curCreatePathName);

                    if (_assetUploadStep == AssetFTPUploadStep.MakeSFTPBundleDirectory) {
                        string progressTitle = "";
                        if (_uploadKind == FTPUploadKind.Resources) {
                            progressTitle = "FTP Create Path Resources";
                        } else {
                            progressTitle = "FTP Create Path Sheet";
                        }

                        EditorUtility.DisplayProgressBar(progressTitle, _curCreatePathName, (float)_curCreatePathCount / (float)_createPathMaxCount);
                    }
                }

                yield return null;
            }

            sftp.Disconnect();
            sftp.Dispose();
        }

        if (_assetUploadStep == AssetFTPUploadStep.MakeSFTPBundleDirectory)
            EditorUtility.ClearProgressBar();
    }

    public bool DirectoryExists(string directory)
    {
        bool directoryExists;
        var request = (FtpWebRequest)FtpWebRequest.Create(directory);
        request.Method = WebRequestMethods.Ftp.ListDirectory;
        request.Credentials = new NetworkCredential(_ftpID, _ftpPW);

        try {
            using (var resp = (FtpWebResponse)request.GetResponse()) 
            {
                Debug.Log(string.Format("DirectoryExists Exist dicrectory : {0}", directory));
                directoryExists = true;
                resp.Close();
            }
        } catch (WebException) {
            directoryExists = false;
        }
        return directoryExists;
    }

    public IEnumerator UpdateUploadAsset()
    {
        while(_isUploadState) {
            if (_uploadAssetInfoQueue.Count == 0) {
                FinishFTPUpload();
                continue;
            }

            if (_curUploadInfo != _uploadAssetInfoQueue.Peek()) {
                _curUploadInfo = _uploadAssetInfoQueue.Peek();

                if (_curUploadInfo.pathList != null && _curUploadInfo.pathList.Count > 0) {
                    _createPathMaxCount = 0;
                    for (int i = 0; i < _curUploadInfo.pathList.Count; i++) {
                        if (!_pathMakeList.ContainsKey(_curUploadInfo.pathList[i])) {
                            _pathQueue.Enqueue(_curUploadInfo.pathList[i]);
                            _createPathMaxCount++;
                        }
                    }
                }

                if (_pathQueue.Count > 0) {
                    _isUpdatePathCreate = true;
                    _curCreatePathCount = 0;
                    EditorCoroutine.start(UpdateCreateFolder());
                    continue;
                }

                UploadAssetBundle(_curUploadInfo.ftpUri, _curUploadInfo.localPath, _ftpID, _ftpPW);

                string progressTitle = "";
                if(_uploadKind == FTPUploadKind.Resources) {
                    progressTitle = "FTP Upload Resources";
                } else {
                    progressTitle = "FTP Upload Sheet";
                }
                EditorUtility.DisplayProgressBar(progressTitle, _curUploadInfo.localPath, (float)_curUploadCount / (float)_uploadMaxCount);
            }

            yield return null;
        }

        EditorUtility.ClearProgressBar();
    }

    public IEnumerator UpdateSFTPUploadAsset()
    {
        using (var sftp = new SftpClient(_sftpConnectInfo)) {
            sftp.Connect();

            while (_isUploadState) {
                if (_uploadAssetInfoQueue.Count == 0) {
                    FinishFTPUpload();
                    continue;
                }

                if (_curUploadInfo != _uploadAssetInfoQueue.Peek()) {
                    _curUploadInfo = _uploadAssetInfoQueue.Peek();

                    if (_curUploadInfo.pathList != null && _curUploadInfo.pathList.Count > 0) {
                        _createPathMaxCount = 0;
                        for (int i = 0; i < _curUploadInfo.pathList.Count; i++) {
                            if (!_pathMakeList.ContainsKey(_curUploadInfo.pathList[i])) {
                                _pathQueue.Enqueue(_curUploadInfo.pathList[i]);
                                _createPathMaxCount++;
                            }
                        }
                    }

                    if (_pathQueue.Count > 0) {
                        foreach (var path in _pathQueue) {
                            CreateOnlySFtpFolder(sftp, path);
                        }
                        _pathQueue.Clear();
                        _createPathMaxCount = 0;
                    }

                    UploadSFTPAssetBundle(_curUploadInfo.ftpUri, _curUploadInfo.localPath, sftp);

                    string progressTitle = "";
                    if (_uploadKind == FTPUploadKind.Resources) {
                        progressTitle = "FTP Upload Resources";
                    } else {
                        progressTitle = "FTP Upload Sheet";
                    }
                    EditorUtility.DisplayProgressBar(progressTitle, _curUploadInfo.localPath, (float)_curUploadCount / (float)_uploadMaxCount);
                }

                yield return null;
            }

            sftp.Disconnect();
            sftp.Dispose();
        }

        EditorUtility.ClearProgressBar();
    }

    void SetAssetFTPUploadStep(AssetFTPUploadStep step)
    {
        _assetUploadStep = step;
        switch (_assetUploadStep) {
            case AssetFTPUploadStep.MakeAssetBundleDirectory:
                SetMakeAssetBundleDirectoryStep();
                break;
            case AssetFTPUploadStep.UploadAssetBundle:
                SetUploadFTPAssetBundleStep();
                break;
            case AssetFTPUploadStep.MakeSFTPBundleDirectory:
                SetMakeSFTPBundleDirectoryStep();
                break;
            case AssetFTPUploadStep.UploadSFTPAssetBundle:
                SetUploadSFTPAssetBundleStep();
                break;
        }
    }

    void SetMakeAssetBundleDirectoryStep()
    {
        _pathQueue.Clear();

        _curCreatePathName = "";
        _curCreatePathCount = 0;
        _createPathMaxCount = 0;

        if (_assetBundleNameList.Count == 0) {
            if(_onFTPUploadState != null)
                _onFTPUploadState(AssetFTPUploadState.FTPUploadFinish);
            return;
        }

        _pathQueue.Enqueue(_ftpURL + _ftpUploadAssetPath);
        _createPathMaxCount++;

        if(_pathQueue.Count > 0) {
            _isUpdatePathCreate = true;
            EditorCoroutine.start(UpdateCreateFolder());
        } else {
            SetAssetFTPUploadStep(AssetFTPUploadStep.UploadAssetBundle);
        }
    }

    void SetMakeSFTPBundleDirectoryStep()
    {
        _pathQueue.Clear();

        _curCreatePathName = "";
        _curCreatePathCount = 0;
        _createPathMaxCount = 0;

        if (_assetBundleNameList.Count == 0) {
            if (_onFTPUploadState != null)
                _onFTPUploadState(AssetFTPUploadState.FTPUploadFinish);
            return;
        }

        _pathQueue.Enqueue(_ftpUploadAssetPath);
        _createPathMaxCount++;

        if (_pathQueue.Count > 0) {
            _isUpdatePathCreate = true;
            EditorCoroutine.start(UpdateSFTPCreateFolder());
        } else {
            SetAssetFTPUploadStep(AssetFTPUploadStep.UploadSFTPAssetBundle);
        }
    }

    void SetUploadFTPAssetBundleStep()
    {
        _curUploadInfo = null;

        _curUploadCount = 0;
        _uploadMaxCount = 0;
        _uploadAssetInfoQueue.Clear();

        _curSaveCheckUploadCount = 0;

        string ftpPath = _ftpURL + _ftpUploadAssetPath + '/';

        // PatchInfo
        string patchInfoFile = string.Format(AssetBundleUtil.bundlePatchInfoFile, _curUploadVersion);
        string patchInfoFtpUri = ftpPath + patchInfoFile;
        string patchInfoLocalPath = _rootLocalPath + patchInfoFile;
        AddAssetBundleInfo(patchInfoFtpUri, patchInfoLocalPath, null);

        // AssetBundles
        string assetBundlesFtpUri = ftpPath + _assetBundlesFileName;
        string assetBundlesLocalPath = _rootLocalPath + _assetBundlesFileName;
        AddAssetBundleInfo(assetBundlesFtpUri, assetBundlesLocalPath, null);

        // AssetBundles Manifest
        string assetBundlesFtpUriManifest = ftpPath + _assetBundlesFileName + ".manifest";
        string assetBundlesLocalPathManifest = _rootLocalPath + _assetBundlesFileName + ".manifest";
        AddAssetBundleInfo(assetBundlesFtpUriManifest, assetBundlesLocalPathManifest, null);

        Dictionary<string, string> cachePathList = new Dictionary<string, string>();

        for (int i = 0; i < _assetBundleNameList.Count; i++) {
            string bundleName = _assetBundleNameList[i];
            List<string> pathList = AssetBundleUtil.GetAssetBundleNamePathList(bundleName);
            List<string> inputPathList = new List<string>();
            if (pathList.Count > 0) {
                string curPath = "";
                for (int j = 0; j < pathList.Count; j++) {
                    if (string.IsNullOrEmpty(pathList[j])) continue;

                    curPath += "/" + pathList[j];
                    if (!cachePathList.ContainsKey(curPath)) {
                        cachePathList.Add(curPath, "");
                        string ftpPathUri = _ftpURL + _ftpUploadAssetPath + curPath;
                        inputPathList.Add(ftpPathUri);
                    }
                }
            }

            string ftpUri = ftpPath + bundleName;
            string localPath = _rootLocalPath + bundleName;
            AddAssetBundleInfo(ftpUri, localPath, inputPathList);
        }

        if (_onFTPUploadState != null) {
            _onFTPUploadState(AssetFTPUploadState.FTPUploadStart);
        }

        _isUploadState = true;

        EditorCoroutine.start(UpdateUploadAsset());
    }

    void SetUploadSFTPAssetBundleStep()
    {
        _curUploadInfo = null;

        _curUploadCount = 0;
        _uploadMaxCount = 0;
        _uploadAssetInfoQueue.Clear();

        _curSaveCheckUploadCount = 0;

        string ftpPath = string.Format("./{0}/", _ftpUploadAssetPath);

        // PatchInfo
        string patchInfoFile = string.Format(AssetBundleUtil.bundlePatchInfoFile, _curUploadVersion);
        string patchInfoFtpUri = ftpPath + patchInfoFile;
        string patchInfoLocalPath = _rootLocalPath + patchInfoFile;
        AddAssetBundleInfo(patchInfoFtpUri, patchInfoLocalPath, null);

        // AssetBundles
        string assetBundlesFtpUri = ftpPath + _assetBundlesFileName;
        string assetBundlesLocalPath = _rootLocalPath + _assetBundlesFileName;
        AddAssetBundleInfo(assetBundlesFtpUri, assetBundlesLocalPath, null);

        // AssetBundles Manifest
        string assetBundlesFtpUriManifest = ftpPath + _assetBundlesFileName + ".manifest";
        string assetBundlesLocalPathManifest = _rootLocalPath + _assetBundlesFileName + ".manifest";
        AddAssetBundleInfo(assetBundlesFtpUriManifest, assetBundlesLocalPathManifest, null);

        Dictionary<string, string> cachePathList = new Dictionary<string, string>();

        for (int i = 0; i < _assetBundleNameList.Count; i++) {
            string bundleName = _assetBundleNameList[i];
            List<string> pathList = AssetBundleUtil.GetAssetBundleNamePathList(bundleName);
            List<string> inputPathList = new List<string>();
            if (pathList.Count > 0) {
                string curPath = "";
                for (int j = 0; j < pathList.Count; j++) {
                    if (string.IsNullOrEmpty(pathList[j])) continue;

                    curPath += "/" + pathList[j];
                    if (!cachePathList.ContainsKey(curPath)) {
                        cachePathList.Add(curPath, "");
                        string ftpPathUri = _ftpUploadAssetPath + curPath;
                        inputPathList.Add(ftpPathUri);
                    }
                }
            }

            string ftpUri = ftpPath + bundleName;
            string localPath = _rootLocalPath + bundleName;
            AddAssetBundleInfo(ftpUri, localPath, inputPathList);
        }

        if (_onFTPUploadState != null) {
            _onFTPUploadState(AssetFTPUploadState.FTPUploadStart);
        }

        _isUploadState = true;

        EditorCoroutine.start(UpdateSFTPUploadAsset());
    }

    void AddAssetBundleInfo(string ftpUri, string localPath, List<string> pathList)
    {
        _uploadAssetInfoQueue.Enqueue(new AssetBundleUploadInfo(ftpUri, localPath, pathList));

        _uploadMaxCount++;
    }

    void FtpCreateFolder(string uriPath, string id, string pw)
    {
        if (string.IsNullOrEmpty(uriPath)) return;

        try {
            FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(uriPath);
            request.Method = WebRequestMethods.Ftp.MakeDirectory;
            request.Credentials = new NetworkCredential(id, pw);

            using (var resp = (FtpWebResponse)request.GetResponse()) {
                Debug.Log(resp.StatusCode);

                resp.Close();
                _pathQueue.Dequeue();

                _curCreatePathCount++;

                if(!_pathMakeList.ContainsKey(uriPath))
                    _pathMakeList.Add(uriPath, "");
                Debug.Log(string.Format("Success FtpCreateFolder uriPath : {0}, StatusCode : {1}", uriPath, resp.StatusCode));
            }
        } catch (WebException ex) {
            FtpWebResponse response = (FtpWebResponse)ex.Response;
            if(response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable) {
                response.Close();

                _pathQueue.Dequeue();
                _curCreatePathCount++;

                if (!_pathMakeList.ContainsKey(uriPath))
                    _pathMakeList.Add(uriPath, "");
                Debug.Log(string.Format("FtpCreateFolder response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable, uriPath : {0}", uriPath));
            } else {
                response.Close();
                string exString = string.Format("Fail uriPath : {0}, reason : {1}", uriPath, ex);
                Debug.Log(exString);
                _isUpdatePathCreate = false;
                EditorUtility.ClearProgressBar();
                if (EditorUtility.DisplayDialog("AssetBundle",
                   exString, "OK")) {

                }
            }
        }
    }

    void CreateSFtpFolder(SftpClient client, string createPathName)
    {
        try {
            SftpFileAttributes attrs = client.GetAttributes(createPathName);
            if (!attrs.IsDirectory) {
                throw new Exception("not directory");
            }
        } catch (SftpPathNotFoundException) {
            client.CreateDirectory(createPathName);
        }

        _pathQueue.Dequeue();
        _curCreatePathCount++;
    }

    void CheckNextCreateFolderStep()
    {
        if (_assetUploadStep == AssetFTPUploadStep.MakeAssetBundleDirectory) {
            SetAssetFTPUploadStep(AssetFTPUploadStep.UploadAssetBundle);
        } else if(_assetUploadStep == AssetFTPUploadStep.UploadAssetBundle) {
            _curUploadInfo = null;
        } else if (_assetUploadStep == AssetFTPUploadStep.MakeSFTPBundleDirectory) {
            SetAssetFTPUploadStep(AssetFTPUploadStep.UploadSFTPAssetBundle);
        } else if (_assetUploadStep == AssetFTPUploadStep.UploadSFTPAssetBundle) {
            _curUploadInfo = null;
        }
    }

    void UploadAssetBundle(string uri, string localPath, string id, string pw)
    {
        if (string.IsNullOrEmpty(uri)) return;
        if (string.IsNullOrEmpty(localPath)) return;

        try {
            FileStream fs = new FileStream(localPath, FileMode.Open, FileAccess.Read);
            if (fs != null) {
                byte[] contents = new byte[fs.Length];
                if (contents != null) {
                    fs.Read(contents, 0, (int)contents.Length);
                } else return;

                FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(uri);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(id, pw);
                request.ContentLength = fs.Length;

                Stream rqStream = request.GetRequestStream();
                rqStream.Write(contents, 0, contents.Length);
                rqStream.Close();

                using (var resp = (FtpWebResponse)request.GetResponse()) {
                    resp.Close();

                    _uploadAssetInfoQueue.Dequeue();
                    _curUploadCount++;

                    Debug.LogFormat("Success UpLoad : {0}, StatusCode : {1}", uri, resp.StatusCode);
                }
            }
        } catch (Exception ex) {
            string exString = string.Format("Exception uri : {0}, localPath : {1}, reason : {2}", uri, localPath, ex);
            Debug.Log(exString);
            _isUploadState = false;

            EditorUtility.ClearProgressBar();
            if (EditorUtility.DisplayDialog("AssetBundle",
               exString, "OK")) {

            }
        }
    }

    void UploadSFTPAssetBundle(string uri, string localPath, SftpClient client)
    {
        if (string.IsNullOrEmpty(uri)) return;
        if (string.IsNullOrEmpty(localPath)) return;

        try {
            FileStream fs = new FileStream(localPath, FileMode.Open, FileAccess.Read);
            if (fs != null) {
                client.UploadFile(fs, uri);

                _uploadAssetInfoQueue.Dequeue();
                _curUploadCount++;
            }
        } catch (Exception ex) {
            string exString = string.Format("Exception uri : {0}, localPath : {1}, reason : {2}", uri, localPath, ex);
            Debug.Log(exString);
            _isUploadState = false;

            EditorUtility.ClearProgressBar();
            if (EditorUtility.DisplayDialog("AssetBundle",
               exString, "OK")) {

            }
        }
    }

    string GetUploadCompleteFileName()
    {
        return string.Format(AssetBundleUtil.assetCompleteListInfo, _bundleTarget.ToString(), _buildKind.ToString(), _curUploadVersion);
    }

    void FinishFTPUpload()
    {
        if (_onFTPUploadState != null) {
            _onFTPUploadState(AssetFTPUploadState.FTPUploadFinish);
        }

        _assetUploadStep = AssetFTPUploadStep.None;
        
        _isUploadState = false;
        Debug.Log(string.Format("FinishFTPUpload"));
    }

    void CreateOnlySFtpFolder(SftpClient client, string createPathName)
    {
        try {
            SftpFileAttributes attrs = client.GetAttributes(createPathName);

            if (!attrs.IsDirectory) {
                throw new Exception("not directory");
            }
        } catch (SftpPathNotFoundException) {
            client.CreateDirectory(createPathName);
        }
    }

    #endregion
}

#endif