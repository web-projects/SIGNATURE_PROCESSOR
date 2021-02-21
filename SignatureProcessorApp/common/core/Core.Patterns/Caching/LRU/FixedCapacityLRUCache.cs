using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Core.Patterns.Caching.LRU
{
    internal sealed class FixedCapacityLRUCache<K, V> : IEnumerable<V>
    {
        private const int DefaultCapacity = 0x20;
        private const int MaxCapacity = 0x7a120;

        internal class LRUNode<TKey, TValue>
        {
            internal LRUNode<TKey, TValue> prev;
            internal LRUNode<TKey, TValue> next;
            internal TKey key;
            internal TValue value;

            internal LRUNode(TKey key, TValue value)
            {
                this.key = key;
                this.value = value;
            }
        }

        private LRUNode<K, V> head;
        private LRUNode<K, V> tail;
        private readonly int calculatedConcurrencyLevel;
        private readonly ConcurrentDictionary<K, LRUNode<K, V>> itemMap;

        public int Capacity { get; }
        public int Size { get; private set; }

        public FixedCapacityLRUCache(int capacity = DefaultCapacity)
        {
            if (capacity > MaxCapacity)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), $"Capacity of '{capacity}' exceeds max capacity of {MaxCapacity.ToString("N2")} items.");
            }
            else if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), $"Capacity cannot be less than zero");
            }

            Capacity = capacity;
            calculatedConcurrencyLevel = (int)((0x19 * Environment.ProcessorCount) * 0.25);
            itemMap = new ConcurrentDictionary<K, LRUNode<K, V>>(calculatedConcurrencyLevel, this.Capacity);
        }

        public void Set(K key, V value)
        {
            if (itemMap.TryGetValue(key, out LRUNode<K, V> existingNode))
            {
                LRUNode<K, V> node = existingNode;
                node.value = value;
                if (existingNode == head)
                {
                    return;
                }
                else if (existingNode == tail)
                {
                    node = EvictTail(key, value);
                }
                else
                {
                    node.prev.next = node.next;
                    node.next.prev = node.prev;
                }
                Promote(node);
            }
            else if (Size == 0)
            {
                LRUNode<K, V> headNode = new LRUNode<K, V>(key, value);
                head = tail = headNode;
                itemMap.TryAdd(key, headNode);
                Size++;
            }
            else if (Size == Capacity)
            {
                itemMap.TryRemove(tail.key, out LRUNode<K, V> _);
                LRUNode<K, V> oldTail = EvictTail(key, value);
                Promote(oldTail);
                itemMap.TryAdd(key, oldTail);
            }
            else
            {
                LRUNode<K, V> node = new LRUNode<K, V>(key, value);
                itemMap.TryAdd(key, node);
                Size++;
                Promote(node);
            }
        }

        public V Get(K key)
        {
            LRUNode<K, V> node;
            if (!itemMap.TryGetValue(key, out LRUNode<K, V> existingNode))
            {
                return default;
            }
            else if (existingNode == head)
            {
                return existingNode.value;
            }
            else if (existingNode == tail)
            {
                node = EvictTail(key, existingNode.value);
            }
            else
            {
                node = existingNode;
                node.prev.next = node.next;
                node.next.prev = node.prev;
            }
            Promote(node);
            return node.value;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<V> GetEnumerator()
        {
            if (Size == 0)
            {
                yield break;
            }

            LRUNode<K, V> node = head;
            while (node != null)
            {
                V value = node.value;
                node = node.next;
                yield return value;
            }
        }

        private LRUNode<K, V> EvictTail(K key, V value)
        {
            LRUNode<K, V> oldTail = tail;
            oldTail.key = key;
            oldTail.value = value;

            tail.prev.next = null;
            tail = tail.prev;

            oldTail.prev = null;
            return oldTail;
        }

        private void Promote(LRUNode<K, V> node)
        {
            head.prev = node;
            node.prev = null;
            node.next = head;
            head = node;
        }
    }
}
