/// Json parsing data
public class AssetBundlePatchData
{
    public string assetName;
    public string[] fileNames;
    public string assetHash;
    public string md5Hash;
    
    public string[] dependencies;
    
    public long size;
}

/// Json parsing data
public class AssetBundlePatchInfoData
{
    public string version;
    public int patchCount;
    public string[] patchList;
    public AssetBundlePatchData[] datas;
}
