using UnityEngine;
using System.IO;

/// <summary>
/// 全自动日志捕获工具
/// 线程安全，不影响游戏性能
/// </summary>
public static class LogToFile
{
    // ************************* 可配置参数 *************************
    /// <summary>
    /// 单个日志文件最大大小（默认5MB，单位：字节）
    /// </summary>
    private const long MAX_LOG_SIZE = 5 * 1024 * 1024;
    /// <summary>
    /// 日志文件名前缀
    /// </summary>
    private const string LOG_PREFIX = "UnityLog";
    // **************************************************************

    private static string _logFilePath;
    private static readonly object _lockObj = new object();

    /// <summary>
    /// 游戏启动自动初始化（无需挂载到物体）
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        // 核心优化：编辑器模式下直接退出，不生成日志文件
#if UNITY_EDITOR
        return;
#else
        // 初始化日志文件路径
        CreateNewLogFile();
        // 注册全局日志监听
        Application.logMessageReceived += WriteLogToFile;
        // 启动标记
        Debug.Log($"【日志系统】初始化完成 | 日志路径：{_logFilePath}");
#endif
    }

    /// <summary>
    /// 创建新的日志文件（首次启动/文件超限时调用）
    /// </summary>
    private static void CreateNewLogFile()
    {
        string timeStamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        // 持久化路径（全平台通用，拥有读写权限）
        _logFilePath = Path.Combine(Application.streamingAssetsPath, $"{LOG_PREFIX}_{timeStamp}.txt");
    }

    /// <summary>
    /// 检查当前日志文件大小，超限则创建新文件
    /// </summary>
    private static void CheckLogFileSize()
    {
        if (File.Exists(_logFilePath))
        {
            FileInfo fileInfo = new FileInfo(_logFilePath);
            if (fileInfo.Length > MAX_LOG_SIZE)
            {
                CreateNewLogFile();
            }
        }
    }

    /// <summary>
    /// 写入日志到文件（全局监听所有Debug.Log）
    /// </summary>
    private static void WriteLogToFile(string logContent, string _, LogType logType)
    {
        // 多线程加锁，防止日志乱码/丢失
        lock (_lockObj)
        {
            try
            {
                // 检查文件大小，自动分割
                CheckLogFileSize();
                // 写入日志（仅时间+类型+内容，无堆栈）
                using (StreamWriter sw = File.AppendText(_logFilePath))
                {
                    sw.WriteLine($"[{System.DateTime.Now:HH:mm:ss}] [{logType}] => {logContent}");
                }
            }
            catch
            {
                // 忽略日志写入异常，避免影响游戏运行
            }
        }
    }
}