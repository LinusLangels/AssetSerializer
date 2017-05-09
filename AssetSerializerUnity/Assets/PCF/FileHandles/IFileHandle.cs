using UnityEngine;
using System.Collections;
using System.IO;

namespace PCFFileFormat
{
public interface IFileHandle
{
    string Extension { get; }
    string Name { get; }
    string FullName { get; }
    bool Exists { get; }
    bool Internal { get; }

    void Delete();
    Stream GetFileStream(FileMode mode);
}
}
