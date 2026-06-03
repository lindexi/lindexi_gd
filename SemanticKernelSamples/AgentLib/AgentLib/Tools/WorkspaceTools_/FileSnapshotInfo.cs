using System;

namespace AgentLib.Tools;

/// <summary>
/// 记录读取文件时的文件快照信息，用于后续写入时检测文件是否被外部修改。
/// </summary>
/// <param name="Length">文件大小（字节）。</param>
/// <param name="LastWriteTime">文件最后写入时间（UTC）。</param>
public readonly record struct FileSnapshotInfo(long Length, DateTime LastWriteTime);