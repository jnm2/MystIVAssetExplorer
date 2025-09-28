using System;
using System.Threading;

namespace MystIVAssetExplorer;

public sealed class ReferenceCountedDisposable<T> where T : IDisposable
{
    private readonly object lockObject = new();
    private T? instance;
    private int referenceCount = 1;

    public ReferenceCountedDisposable(T instance, out ILease<T> initialLease)
    {
        this.instance = instance;
        initialLease = new DecrementLease(this);
    }

    public ILease<T>? TryLease()
    {
        lock (lockObject)
        {
            if (referenceCount == 0)
                return null;

            referenceCount++;
            return new DecrementLease(this);
        }
    }

    private void Decrement()
    {
        var shouldDispose = false;
        var instanceToDispose = default(T);

        lock (lockObject)
        {
            if (referenceCount == 0)
                throw new InvalidOperationException("More increments than decrements");

            referenceCount--;
            if (referenceCount == 0)
            {
                shouldDispose = true;
                instanceToDispose = instance;
                instance = default;
            }
        }

        if (shouldDispose)
            instanceToDispose!.Dispose();
    }

    private sealed class DecrementLease(ReferenceCountedDisposable<T> owner) : ILease<T>
    {
        private ReferenceCountedDisposable<T>? owner = owner;

        public T LeasedInstance => (Volatile.Read(ref owner) ?? throw new ObjectDisposedException(nameof(ILease<>))).instance!;

        public void Dispose() => Interlocked.Exchange(ref owner, null)?.Decrement();
    }
}
