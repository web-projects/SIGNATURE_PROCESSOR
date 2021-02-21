using System.Collections.Generic;

namespace Core.Patterns.Search
{
    public sealed class TrieNode
    {
        public char Value { get; }
        public Dictionary<char, TrieNode> ChildrenMap { get; } = new Dictionary<char, TrieNode>();
        public TrieNode Parent { get; }
        public int Depth { get; }

        public TrieNode(char value, int depth, TrieNode parent)
        {
            Value = value;
            Depth = depth;
            Parent = parent;
        }

        public bool IsLeaf() => ChildrenMap.Count == 0;

        public TrieNode FindChildNode(char c) => ChildrenMap.ContainsKey(c) ? ChildrenMap[c] : null;

        public void DeleteChildNode(char c)
        {
            if (ChildrenMap.ContainsKey(c))
            {
                ChildrenMap.Remove(c);
            }
        }
    }
}
