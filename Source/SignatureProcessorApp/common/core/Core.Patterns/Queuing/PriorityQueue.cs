using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Core.Patterns.Queuing
{
    public class PriorityQueue<T> 
        where T : IComparable<T>, IPriorityQueueItem
    {
        private readonly List<T> data;
        private readonly Semaphore signal;

        public PriorityQueue()
        {
            this.data = new List<T>();
            this.signal = new Semaphore(0, int.MaxValue);
        }

        public void Enqueue(T item)
        {
            lock (this.data)
            {
                data.Add(item);
                int childIndex = data.Count - 1;

                while (childIndex > 0)
                {
                    int parentIndex = (childIndex - 1) / 2;
                    if (data[childIndex].CompareTo(data[parentIndex]) >= 0)
                    {
                        break;
                    }
                    T tmp = data[childIndex]; data[childIndex] = data[parentIndex]; data[parentIndex] = tmp;
                    childIndex = parentIndex;
                }
            }
            this.signal.Release();
        }

        public T Dequeue()
        {
            this.signal.WaitOne();

            lock (this.data)
            {
                int lastIndex = data.Count - 1;
                T head = data[0];
                data[0] = data[lastIndex];
                data.RemoveAt(lastIndex);

                lastIndex--;           // last index (after removal)
                int parentIndex = 0;   // start at front of pq

                while (true)
                {
                    int childIndex = parentIndex * 2 + 1;
                    if (childIndex > lastIndex)
                    {
                        break;
                    }
                    int rc = childIndex + 1;
                    if (rc <= lastIndex && data[rc].CompareTo(data[childIndex]) < 0)
                    {
                        childIndex = rc;
                    }
                    if (data[parentIndex].CompareTo(data[childIndex]) <= 0)
                    {
                        break;
                    }
                    T tmp = data[parentIndex]; data[parentIndex] = data[childIndex]; data[childIndex] = tmp;
                    parentIndex = childIndex;
                }

                return head;
            }
        }

        public T Peek()
        {
            T head = data[0];
            return head;
        }

        public int Count()
        {
            return data.Count;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(10);
            for (int i = 0; i < data.Count; i++)
            {
                sb.Append($"{data[i].ToString()} ");
            }
            sb.Append($"count = {data.Count}");
            return sb.ToString();
        }

        public bool IsConsistent()
        {
            if (data.Count == 0)
            {
                return true;
            }

            int li = data.Count - 1;

            for (int parentIndex = 0; parentIndex < data.Count; parentIndex++)
            {
                int lci = 2 * parentIndex + 1;
                int rci = 2 * parentIndex + 2;

                if (lci <= li && data[parentIndex].CompareTo(data[lci]) > 0)
                {
                    return false;
                }

                if (rci <= li && data[parentIndex].CompareTo(data[rci]) > 0)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
