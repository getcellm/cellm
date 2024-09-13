namespace Cellm.Models;

internal interface ICache
{
    public void Set<TKey, TValue>(TKey key, TValue value);

    public bool TryGetValue<TKey>(TKey key, out object? value);
}
