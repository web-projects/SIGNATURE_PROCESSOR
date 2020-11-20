using System;
using System.Collections.Generic;

namespace Core.Patterns.Search
{
    /// <summary>
    /// A trie is a tree data structure that stores words by compressing common prefixes.
    /// </summary>
    public class Trie
    {
        private static readonly char TrieBranchEndChar = '$';
        private readonly TrieNode rootNode;

        public Trie() => rootNode = new TrieNode('^', 0, null);

        public bool IgnoreCase { get; set; }

        public TrieNode Prefix(string s)
        {
            TrieNode currentTrieNode = rootNode;
            TrieNode result = currentTrieNode;

            foreach (char c in s)
            {
                currentTrieNode = currentTrieNode.FindChildNode(IgnoreCase ? char.ToUpper(c) : c);
                if (currentTrieNode is null)
                {
                    break;
                }

                result = currentTrieNode;
            }

            return result;
        }

        public bool Search(string s)
        {
            var prefix = Prefix(s);
            return prefix.Depth == s.Length && prefix.FindChildNode(TrieBranchEndChar) != null;
        }

        public bool Search(ReadOnlySpan<char> s)
        {
            TrieNode currentTrieNode = rootNode;
            TrieNode result = currentTrieNode;

            foreach (char c in s)
            {
                currentTrieNode = currentTrieNode.FindChildNode(IgnoreCase ? char.ToUpper(c) : c);
                if (currentTrieNode is null)
                {
                    break;
                }
                result = currentTrieNode;
            }

            return result.Depth == s.Length && result.FindChildNode(TrieBranchEndChar) != null;
        }

        public void InsertRange(List<string> items)
            => items.ForEach((string item) => Insert(item));

        public void Insert(string s)
        {
            TrieNode commonPrefix = Prefix(s);
            TrieNode current = commonPrefix;

            for (int i = current.Depth; i < s.Length; i++)
            {
                char c = IgnoreCase ? char.ToUpper(s[i]) : s[i];
                TrieNode newTrieNode = new TrieNode(c, current.Depth + 1, current);
                current.ChildrenMap[c] = newTrieNode;
                current = newTrieNode;
            }

            current.ChildrenMap[TrieBranchEndChar] = new TrieNode(TrieBranchEndChar, current.Depth + 1, current);
        }

        public void Delete(string s)
        {
            if (Search(s))
            {
                TrieNode node = Prefix(s).FindChildNode(TrieBranchEndChar);

                while (node.IsLeaf())
                {
                    TrieNode parent = node.Parent;
                    if (parent == null) // Root node has a null parent.
                        break;
                    parent.DeleteChildNode(node.Value);
                    node = parent;
                }
            }
        }
    }
}
