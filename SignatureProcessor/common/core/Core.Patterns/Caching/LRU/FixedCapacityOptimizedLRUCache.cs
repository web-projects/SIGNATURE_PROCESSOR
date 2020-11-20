using System;
using System.Collections;
using System.Collections.Generic;

namespace Core.Patterns.Caching.LRU
{
    /// <summary>
    /// The fixed Capacity Optimized LRU (Least Recently Used) Cache is a nifty high 
    /// performance cache implementation that is designed for maximum performance and 
    /// the least amount of stress on the GC. A total Capacity must be set on the cache in
    /// order to utilize it and it uses its own pointer arithmetic in order to
    /// reduce the amount of allocations necessary.
    /// 
    /// A hashmap implementation is used along with fixed Capacity integer arrays
    /// to provide custom pointer management for Previous, Next and items. There are
    /// also pointers to the Head of the list and the Tail of the list for convenience.
    /// 
    /// With most implementations of a caching system new reference classes are created
    /// in order to track heap allocations. The heap allocations often end up in areas
    /// of memory that are sometimes quite far apart depending on memory fragmentation. 
    /// In order to give a helping hand to the .NET Optimizing Compiler we utilize the
    /// integer arrays to first demand contiguous addresses which makes it prime candidate
    /// for optimizations to take place.
    /// 
    /// The Next thing we do is utilize the hashmap in order to store the key and the indice
    /// where it can be found so we have O(1) lookup times on our items. Lastly, we change
    /// indices to control pointer management keeping shifting of existing items at O(1) time.
    /// The tradeoff we make is that by utilizing multiple arrays we take slightly more
    /// memory but the reliability of never needing to worry about our pointer tracking nodes
    /// causing unnecessary GC bottlenecks is well worth it!
    /// 
    /// Lastly, memory addresses always go from SMALLER to LARGER but stack addresses go
    /// from the current ESP - [4, 8, 12, etc..] depending on the location of the parameter
    /// which also helps to further speed up this LRU Cache implementation by leveraging 
    /// the arrays because the optimizer can optimize the low level memory access in both
    /// areas!
    /// </summary>
    internal sealed class FixedCapacityOptimizedLRUCache<K, V> : IEnumerable<V>
    {
        private const int DefaultCapacity = 0x20;
        private const int MaxCapacity = 0x7a120;

        private readonly Dictionary<K, int> indiceMap;

        private int head;
        private int tail;
        private int[] next;
        private int[] prev;
        private K[] keys;
        private V[] values;

        public int Size { get; private set; }
        public int Capacity { get; }

        public FixedCapacityOptimizedLRUCache(int capacity = DefaultCapacity)
        {
            if (capacity > MaxCapacity)
            {
                throw new ArgumentOutOfRangeException(nameof(Capacity), $"Capacity of '{Capacity}' exceeds max Capacity of {MaxCapacity.ToString("N2")} items.");
            }
            else if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(Capacity), $"Capacity cannot be less than zero");
            }

            Capacity = capacity;

            keys = new K[Capacity];
            values = new V[Capacity];
            next = new int[Capacity];
            prev = new int[Capacity];
            indiceMap = new Dictionary<K, int>(Capacity);
        }

        public void Set(K key, V value)
        {
            if (indiceMap.TryGetValue(key, out int indice))
            {
                SetKeyValue(key, value, indice);
                if (indice == head)
                {
                    return;
                }
                else if (indice == tail)
                {
                    EvictTail();
                }
                Promote(indice, true);
            }
            else if (Size == 0)
            {
                Size++;
                SetKeyValue(key, value, 0);
            }
            else if (Size == Capacity)
            {
                int newTail = tail;
                EvictTail();
                SetKeyValue(key, value, newTail);
                Promote(newTail, true);
            }
            else
            {
                int nextAvailableIndice = Size++;
                SetKeyValue(key, value, nextAvailableIndice);
                Promote(nextAvailableIndice);
            }
        }

        public V Get(K key)
        {
            if (indiceMap.TryGetValue(key, out int indice))
            {
                if (indice == head)
                {
                    return values[indice];
                }
                else if (indice == tail)
                {
                    EvictTail();
                }

                Promote(indice, true);
                return values[indice];
            }
            return default;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<V> GetEnumerator()
        {
            if (Size == 0)
            {
                yield break;
            }

            int currentIndex = head;
            int nextPointerIndice = 0;

            for (int i = 0; i < Size; i++)
            {
                if (i > 0)
                {
                    currentIndex = nextPointerIndice;
                }
                nextPointerIndice = prev[currentIndex];
                yield return values[currentIndex];
            }
        }

        private void SetKeyValue(K key, V value, int indice)
        {
            indiceMap[key] = indice;
            keys[indice] = key;
            values[indice] = value;
        }

        private void EvictTail()
        {
            int newTail = next[tail];
            prev[newTail] = 0;
            tail = newTail;
        }

        private void Promote(int indice, bool considerOverflow = false)
        {
            if (indice == head)
            {
                return;
            }

            if (considerOverflow)
            {
                if (next[prev[indice]] == indice)
                {
                    next[prev[indice]] = next[indice];
                }
                if (prev[next[indice]] == indice)
                {
                    prev[next[indice]] = prev[indice];
                }
            }

            next[head] = indice;
            prev[indice] = head;
            next[indice] = 0;
            head = indice;
        }
    }
}
