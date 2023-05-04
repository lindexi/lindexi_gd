namespace DireljelcoDaicejuniredere;

internal enum RtsLockType : uint
{
    RtsObjLock = 1,
    RtsSyncEventLock = 2,
    RtsAsyncEventLock = 4,
    RtsExcludeCallback = 8,
    RtsSyncObjLock = 11, // 0x0000000B
    RtsAsyncObjLock = 13, // 0x0000000D
}