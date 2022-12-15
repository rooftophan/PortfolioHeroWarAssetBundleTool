using LitJson;
using System;
using System.Collections.Generic;

public class AssetBundlePatchInfoModel
{

    private AssetBundlePatchInfoData _data;
    public Dictionary<string, AssetBundlePatchData> dicDatas;

    public AssetBundlePatchInfoModel(byte[] patchBytes)
    {
        dicDatas = new Dictionary<string, AssetBundlePatchData>();
        Init(patchBytes);
    }

    public void Init(byte[] patchBytes)
    {
        var data = BundleFile.ReadCLZFFileByBytes<AssetBundlePatchInfoData>(patchBytes);

        Init(data);
    }

    private void Init(AssetBundlePatchInfoData data)
    {
        _data = data;
        SettingDatas();
    }
    
    public string GetVersion()
    {
        return _data.version;
    }

    public int GetPatchCount()
    {
        return _data.patchCount;
    }

    public string[] GetPatchList()
    {
        if (_data.patchList == null)
            return new string[0];

        return _data.patchList;
    }

    public bool SettingDatas()
    {
        if (_data == null) return false;

        dicDatas.Clear();

        if (dicDatas.Count <= 0)
        {
            foreach (var data in _data.datas)
            {
                if (!dicDatas.ContainsKey(data.assetName))
                {
                    dicDatas.Add(data.assetName, data);
                }
            }
        }

        return true;
    }

    public AssetBundlePatchData GetData(string assetName)
    {
        AssetBundlePatchData data = null;
        if (dicDatas != null)
        {
            dicDatas.TryGetValue(assetName, out data);
        }
        return data;
    }
}
