using System;

namespace MystIVAssetExplorer;

public interface ILease<out T> : IDisposable
{
    T LeasedInstance { get; }
}
