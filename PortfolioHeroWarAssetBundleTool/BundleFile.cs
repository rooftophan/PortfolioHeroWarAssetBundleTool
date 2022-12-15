using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System.Linq;
using LitJson;

public class BundleFile
{
    public static void WriteStringToFile(string str, string filename)
    {
        string path = PathForDocumentsFile(filename);
        File.WriteAllBytes(path, Encoding.UTF8.GetBytes(str));
    }

    public static void WriteStringToFilePath(string str, string path, string filename)
    {
        if(!Directory.Exists(path)) {
            Directory.CreateDirectory(path);
        }

        string filePath =  Path.Combine(path, filename);
        File.WriteAllBytes(filePath, Encoding.UTF8.GetBytes(str));
    }

    public static void WriteBytesToFilePath(byte[] writeBytes, string path, string filename)
    {
        if (!Directory.Exists(path)) {
            Directory.CreateDirectory(path);
        }

        string filePath = Path.Combine(path, filename);
        File.WriteAllBytes(filePath, writeBytes);
    }

    public static void WriteStringToFilePathEncryption(string str, string path, string filename)
    {
        if (!Directory.Exists(path)) {
            Directory.CreateDirectory(path);
        }

        string filePath = Path.Combine(path, filename);

        string encrypt = StringEncription.AESEncrypt128(str);

        File.WriteAllBytes(filePath, Encoding.UTF8.GetBytes(encrypt));
    }

    public static string ReadStringFromFile(string filename)
    {
        string path = PathForDocumentsFile(filename);

        string str;
        str = null;

        if (File.Exists(path)) {
            str = File.ReadAllText(path, Encoding.UTF8);
            return str;
        } else {
            return null;
        }
    }

    public static string ReadStringFromFilePath(string path, string filename)
    {
        string filePath = Path.Combine(path, filename);

        if (File.Exists(filePath)) {
            return File.ReadAllText(filePath, Encoding.UTF8);
        } else {
            return null;
        }
    }

    public static byte[] ReadBytesFromFilePath(string filePath)
    {
        if (File.Exists(filePath)) {
            return File.ReadAllBytes(filePath);
        } else {
            return null;
        }
    }

    public static T ReadCLZFFileByBytes<T>(byte[] fileBytes) where T : class
    {
        T t = null;

        if (fileBytes == null || fileBytes.Length == 0)
            return null;

        byte[] decompressBytes = CLZF2.Decompress(fileBytes);
        string sText = Encoding.UTF8.GetString(decompressBytes);
        t = JsonMapper.ToObject<T>(sText);

        return t;
    }

    public static string ReadCLZFFileByBytes(byte[] fileBytes)
    {

        if (fileBytes == null || fileBytes.Length == 0)
            return null;

        byte[] decompressBytes = CLZF2.Decompress(fileBytes);
        string sText = Encoding.UTF8.GetString(decompressBytes);

        return sText;
    }

    public static T ReadCLZFFilePath<T>(string filePath) where T : class
    {
        T t = null;

        byte[] readBytes = ReadBytesFromFilePath(filePath);

        if(readBytes == null || readBytes.Length == 0)
            return null;

        byte[] decompressBytes = CLZF2.Decompress(readBytes);
        string sText = Encoding.UTF8.GetString(decompressBytes);
        t = JsonMapper.ToObject<T>(sText);

        return t;
    }

    public static string ReadStringFromFilePathDecrypt(string path, string filename)
    {
        string filePath = Path.Combine(path, filename);

        string str;
        str = null;

        if (File.Exists(filePath)) {
            str = File.ReadAllText(filePath, Encoding.UTF8);
            return str;
        } else {
            return null;
        }
    }

    public static string ReadTextFromFilePath(string filePath)
    {
        if (File.Exists(filePath)) {
            string str = File.ReadAllText(filePath, Encoding.UTF8);
            return str;
        } else {
            return null;
        }
    }

    public static void FileDelete(string filename)
    {
        string path = PathForDocumentsFile(filename);
        if (File.Exists(path)) {
            File.Delete(path);
        }
    }

    public static void FileDeletePath(string path, string filename)
    {
        string filePath = Path.Combine(path, filename);
        if (File.Exists(filePath)) {
            File.Delete(filePath);
        }
    }

    public static string PathForDocumentsFile(string filename)
    {
        if (Application.platform == RuntimePlatform.IPhonePlayer) {
            return string.Format("{0}/{1}", Application.persistentDataPath, filename);
        } else if (Application.platform == RuntimePlatform.Android) {
            string path = Application.persistentDataPath;
            path = path.Substring(0, path.LastIndexOf('/'));
            return Path.Combine(path, filename);
        } else {
            string path = Application.dataPath;
            path = path.Substring(0, path.LastIndexOf('/'));
            return Path.Combine(path, filename);
        }
    }

    public static string PathNameForDocumentsFile()
    {
        string path = Application.dataPath;
        if (Application.platform == RuntimePlatform.IPhonePlayer) {
            path = Application.dataPath.Substring(0, Application.dataPath.Length - 5);
            return path.Substring(0, path.LastIndexOf('/') + 1);
        } else if (Application.platform == RuntimePlatform.Android) {
            path = Application.persistentDataPath;
            return path.Substring(0, path.LastIndexOf('/') + 1);
        }
        return path.Substring(0, path.LastIndexOf('/') + 1);
    }

    public static void DeleteFileAndDirectory(string delPath)
    {
        if(Directory.Exists(delPath)) {
            DirectoryInfo di = new DirectoryInfo(delPath);
            FileInfo[] fileInfos = di.GetFiles();
            if(fileInfos != null && fileInfos.Length > 0) {
                for(int i = 0;i< fileInfos.Length;i++) {
                    fileInfos[i].Delete();
                }
            }

            DirectoryInfo[] dirInfos = di.GetDirectories();
            if(dirInfos != null && dirInfos.Length > 0) {
                for(int i = 0;i< dirInfos.Length;i++) {
                    dirInfos[i].Delete(true);
                }
            }
        }
    }
}
