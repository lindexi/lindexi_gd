using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace JerecarwirakalHallnanemne
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var random = new Random();

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var nairqearjojaJemjaremkes = new BTree<NairqearjojaJemjaremke>();
            for (var i = 0; i < 100000; i++)
            {
                nairqearjojaJemjaremkes.Insert(new NairqearjojaJemjaremke
                {
                    WaleawhalharWogerjedearwhel = random.Next()
                });
            }

            stopwatch.Stop();

            Console.WriteLine(stopwatch.ElapsedTicks);

            stopwatch.Restart();

            var find = 0;
            for (var i = 0; i < 1000000; i++)
            {
                var f = i;
                if (nairqearjojaJemjaremkes.Find(n =>
                    f.CompareTo(n.WaleawhalharWogerjedearwhel)) != null)
                {
                    find++;
                }
            }

            stopwatch.Stop();

            Console.WriteLine(stopwatch.ElapsedMilliseconds);
            Console.WriteLine($"Find {find}");

            stopwatch.Restart();

            find = 0;

            for (var i = 0; i < 1000000; i++)
            {
                if (nairqearjojaJemjaremkes.Find(new NairqearjojaJemjaremke
                {
                    WaleawhalharWogerjedearwhel = i
                }) != null)
                {
                    find++;
                }
            }

            stopwatch.Stop();

            Console.WriteLine(stopwatch.ElapsedMilliseconds);
            Console.WriteLine($"Find {find}");

            stopwatch.Restart();

            find = 0;
            var nairqearjojaJemjaremkeComparer = new NairqearjojaJemjaremkeComparer();

            for (var i = 0; i < 1000000; i++)
            {
                nairqearjojaJemjaremkeComparer.NairqearjojaJemjaremke = i;
                if (nairqearjojaJemjaremkes.Find(nairqearjojaJemjaremkeComparer) != null)
                {
                    find++;
                }
            }

            stopwatch.Stop();

            Console.WriteLine(stopwatch.ElapsedMilliseconds);
            Console.WriteLine($"Find {find}");

            stopwatch.Restart();
            var list = new SortedList<int, NairqearjojaJemjaremke>();

            for (var i = 0; i < 100000; i++)
            {
                var nairqearjojaJemjaremke = new NairqearjojaJemjaremke
                {
                    WaleawhalharWogerjedearwhel = i
                };
                list.Add(nairqearjojaJemjaremke.WaleawhalharWogerjedearwhel,nairqearjojaJemjaremke);
            }
            stopwatch.Stop();

            Console.WriteLine(stopwatch.ElapsedMilliseconds);

            stopwatch.Restart();

            find = 0;

            for (var i = 0; i < 100000; i++)
            {
                if (list.TryGetValue(i,out _))
                {
                    find++;
                }
            }

            stopwatch.Stop();

            Console.WriteLine(stopwatch.ElapsedMilliseconds);
            Console.WriteLine($"Find {find}");
        }

        private class NairqearjojaJemjaremkeComparer : IValueComparer<NairqearjojaJemjaremke>
        {
            public int NairqearjojaJemjaremke { set; get; }

            /// <inheritdoc />
            public int CompareTo(NairqearjojaJemjaremke value)
            {
                return NairqearjojaJemjaremke.CompareTo(value.WaleawhalharWogerjedearwhel);
            }
        }

        private class NairqearjojaJemjaremke : IComparable<NairqearjojaJemjaremke>, IComparable
        {
            public int WaleawhalharWogerjedearwhel { set; get; }

            /// <inheritdoc />
            public int CompareTo(object? obj)
            {
                return CompareTo((NairqearjojaJemjaremke)obj);
            }

            /// <inheritdoc />
            public int CompareTo(NairqearjojaJemjaremke other)
            {
                if (ReferenceEquals(this, other)) return 0;
                if (ReferenceEquals(null, other)) return 1;
                return WaleawhalharWogerjedearwhel.CompareTo(other.WaleawhalharWogerjedearwhel);
            }
        }
    }

    /// <summary>
    /// 值比较方法
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IValueComparer<in T>
    {
        /// <summary>
        /// 传入的 <paramref name="t"/> 就是存放在树里面的数据
        /// <para></para>
        /// 采用 <code>x.CompareTo(<paramref name="t"/>)</code> 的方法，注意判断顺序不能相反
        /// </summary>
        /// <param name="t"></param>
        /// <returns>如果存根 x 比 <paramref name="t"/> 大，返回 1 等于返回 0 的值</returns>
        int CompareTo(T t);
    }

    /// <summary>
    /// A B-tree implementation.
    /// </summary>
    /// https://github.com/justcoding121/Advanced-Algorithms
    public class BTree<T> : IEnumerable<T> where T : IComparable
    {
        /// <summary>
        /// 创建 B树
        /// </summary>
        /// <param name="maxKeysPerNode">默认一个 Node 有多少个数据，默认值是 3 个，注意不能传入小于3个</param>
        public BTree(int maxKeysPerNode = 3)
        {
            if (maxKeysPerNode < 3)
            {
                throw new ArgumentException(
                    $"Max keys per node should be atleast 3. Current value is {maxKeysPerNode}");
            }

            _maxKeysPerNode = maxKeysPerNode;
            _minKeysPerNode = maxKeysPerNode / 2;
        }

        /// <summary>
        /// 总共有多少数据
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Time complexity: O(log(n)).
        /// </summary>
        public T Max
        {
            get
            {
                if (Root == null) return default;

                var maxNode = FindMaxNode(Root);
                return maxNode.Keys[maxNode.KeyCount - 1];
            }
        }

        /// <summary>
        /// Time complexity: O(log(n)).
        /// </summary>
        public T Min
        {
            get
            {
                if (Root == null) return default;

                var minNode = FindMinNode(Root);
                return minNode.Keys[0];
            }
        }

        public T Find(IValueComparer<T> comparer)
        {
            return Find(Root, comparer);
        }

        public T Find(T value)
        {
            return Find(Root, value);
        }

        public T Find(Func<T, int> comparer)
        {
            return Find(Root, new DelegateComparer(comparer));
        }

        /// <summary>
        /// Time complexity: O(log(n)).
        /// </summary>
        public void Insert(T newValue)
        {
            if (Root == null)
            {
                Root = new BTreeNode<T>(_maxKeysPerNode, null) { Keys = { [0] = newValue } };
                Root.KeyCount++;
                Count++;
                return;
            }

            var leafToInsert = FindInsertionLeaf(Root, newValue);
            InsertAndSplit(ref leafToInsert, newValue, null, null);
            Count++;
        }

        /// <summary>
        /// Time complexity: O(log(n)).
        /// </summary>
        public void Delete(T value)
        {
            var node = FindDeletionNode(Root, value);

            if (node == null)
            {
                throw new Exception("Item do not exist in this tree.");
            }

            for (var i = 0; i < node.KeyCount; i++)
            {
                if (value.CompareTo(node.Keys[i]) != 0)
                {
                    continue;
                }

                //if node is leaf and no underflow
                //then just remove the node
                if (node.IsLeaf)
                {
                    RemoveAt(node.Keys, i);
                    node.KeyCount--;

                    Balance(node);
                }
                else
                {
                    //replace with max node of left tree
                    var maxNode = FindMaxNode(node.Children[i]);
                    node.Keys[i] = maxNode.Keys[maxNode.KeyCount - 1];

                    RemoveAt(maxNode.Keys, maxNode.KeyCount - 1);
                    maxNode.KeyCount--;

                    Balance(maxNode);
                }

                Count--;
                return;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new BTreeEnumerator<T>(Root);
        }

        private readonly int _maxKeysPerNode;
        private readonly int _minKeysPerNode;

        private BTreeNode<T> Root { set; get; }

        private static T Find(BTreeNode<T> node, IValueComparer<T> comparer)
        {
            //if leaf then its time to insert
            if (node.IsLeaf)
            {
                for (var i = 0; i < node.KeyCount; i++)
                {
                    var value = node.Keys[i];
                    if (comparer.CompareTo(value) == 0)
                    {
                        return value;
                    }
                }
            }
            else
            {
                //if not leaf then drill down to leaf
                for (var i = 0; i < node.KeyCount; i++)
                {
                    var value = node.Keys[i];
                    if (comparer.CompareTo(value) == 0)
                    {
                        return value;
                    }

                    //current value is less than new value
                    //drill down to left child of current value
                    if (comparer.CompareTo(value) < 0)
                    {
                        return Find(node.Children[i], comparer);
                    }
                    //current value is grearer than new value
                    //and current value is last element 

                    if (node.KeyCount == i + 1)
                    {
                        return Find(node.Children[i + 1], comparer);
                    }
                }
            }

            return default;
        }

        /// <summary>
        /// Find the value node under given node.
        /// </summary>
        private static T Find(BTreeNode<T> node, T value)
        {
            return Find(node, new DefaultComparer(value));
        }


        /// <summary>
        /// Find the leaf node to start initial insertion
        /// </summary>
        private static BTreeNode<T> FindInsertionLeaf(BTreeNode<T> node, T newValue)
        {
            //if leaf then its time to insert
            if (node.IsLeaf)
            {
                return node;
            }

            //if not leaf then drill down to leaf
            for (var i = 0; i < node.KeyCount; i++)
            {
                //current value is less than new value
                //drill down to left child of current value
                if (newValue.CompareTo(node.Keys[i]) < 0)
                {
                    return FindInsertionLeaf(node.Children[i], newValue);
                }
                //current value is grearer than new value
                //and current value is last element 

                if (node.KeyCount == i + 1)
                {
                    return FindInsertionLeaf(node.Children[i + 1], newValue);
                }
            }

            return node;
        }

        /// <summary>
        /// Insert and split recursively up until no split is required
        /// </summary>
        private void InsertAndSplit(ref BTreeNode<T> node, T newValue,
            BTreeNode<T> newValueLeft, BTreeNode<T> newValueRight)
        {
            //add new item to current node
            if (node == null)
            {
                node = new BTreeNode<T>(_maxKeysPerNode, null);
                Root = node;
            }

            //newValue have room to fit in this node
            //so just insert in right spot in asc order of keys
            if (node.KeyCount != _maxKeysPerNode)
            {
                InsertToNotFullNode(ref node, newValue, newValueLeft, newValueRight);
                return;
            }

            //if node is full then split node
            //and  then insert new median to parent.

            //divide the current node values + new Node as left and right sub nodes
            var left = new BTreeNode<T>(_maxKeysPerNode, null);
            var right = new BTreeNode<T>(_maxKeysPerNode, null);

            //median of current Node
            var currentMedianIndex = node.GetMedianIndex();

            //init currentNode under consideration to left
            var currentNode = left;
            var currentNodeIndex = 0;

            //new Median also takes new Value in to Account
            var newMedian = default(T);
            var newMedianSet = false;
            var newValueInserted = false;

            //keep track of each insertion
            var insertionCount = 0;

            //insert newValue and existing values in sorted order
            //to left and right nodes
            //set new median during sorting
            for (var i = 0; i < node.KeyCount; i++)
            {
                //if insertion count reached new median
                //set the new median by picking the next smallest value
                if (!newMedianSet && insertionCount == currentMedianIndex)
                {
                    newMedianSet = true;

                    //median can be the new value or node.keys[i] (next node key)
                    //whichever is smaller
                    if (!newValueInserted && newValue.CompareTo(node.Keys[i]) < 0)
                    {
                        //median is new value
                        newMedian = newValue;
                        newValueInserted = true;

                        if (newValueLeft != null)
                        {
                            SetChild(currentNode, currentNode.KeyCount, newValueLeft);
                        }

                        //now fill right node
                        currentNode = right;
                        currentNodeIndex = 0;

                        if (newValueRight != null)
                        {
                            SetChild(currentNode, 0, newValueRight);
                        }

                        i--;
                        insertionCount++;
                        continue;
                    }

                    //median is next node
                    newMedian = node.Keys[i];

                    //now fill right node
                    currentNode = right;
                    currentNodeIndex = 0;

                    continue;
                }

                //pick the smaller among newValue and node.Keys[i]
                //and insert in to currentNode (left and right nodes)
                //if new Value was already inserted then just copy from node.Keys in sequence
                //since node.Keys is already in sorted order it should be fine
                if (newValueInserted || node.Keys[i].CompareTo(newValue) < 0)
                {
                    currentNode.Keys[currentNodeIndex] = node.Keys[i];
                    currentNode.KeyCount++;

                    //if child is set don't set again
                    //the child was already set by last newValueRight or last node
                    if (currentNode.Children[currentNodeIndex] == null)
                    {
                        SetChild(currentNode, currentNodeIndex, node.Children[i]);
                    }

                    SetChild(currentNode, currentNodeIndex + 1, node.Children[i + 1]);
                }
                else
                {
                    currentNode.Keys[currentNodeIndex] = newValue;
                    currentNode.KeyCount++;

                    SetChild(currentNode, currentNodeIndex, newValueLeft);
                    SetChild(currentNode, currentNodeIndex + 1, newValueRight);

                    i--;
                    newValueInserted = true;
                }

                currentNodeIndex++;
                insertionCount++;
            }

            //could be that thew newKey is the greatest 
            //so insert at end
            if (!newValueInserted)
            {
                currentNode.Keys[currentNodeIndex] = newValue;
                currentNode.KeyCount++;

                SetChild(currentNode, currentNodeIndex, newValueLeft);
                SetChild(currentNode, currentNodeIndex + 1, newValueRight);
            }

            //insert overflow element (newMedian) to parent
            var parent = node.Parent;
            InsertAndSplit(ref parent, newMedian, left, right);
        }

        /// <summary>
        /// Insert to a node that is not full
        /// </summary>
        private static void InsertToNotFullNode(ref BTreeNode<T> node, T newValue,
            BTreeNode<T> newValueLeft, BTreeNode<T> newValueRight)
        {
            var inserted = false;

            //insert in sorted order
            for (var i = 0; i < node.KeyCount; i++)
            {
                if (newValue.CompareTo(node.Keys[i]) >= 0)
                {
                    continue;
                }

                InsertAt(node.Keys, i, newValue);
                node.KeyCount++;

                //Insert children if any
                SetChild(node, i, newValueLeft);
                InsertChild(node, i + 1, newValueRight);


                inserted = true;
                break;
            }

            //newValue is the greatest
            //element should be inserted at the end then
            if (inserted)
            {
                return;
            }

            node.Keys[node.KeyCount] = newValue;
            node.KeyCount++;

            SetChild(node, node.KeyCount - 1, newValueLeft);
            SetChild(node, node.KeyCount, newValueRight);
        }

        /// <summary>
        /// return the node containing max value which will be a leaf at the right most
        /// </summary>
        private static BTreeNode<T> FindMinNode(BTreeNode<T> node)
        {
            //if leaf return node
            return node.IsLeaf ? node : FindMinNode(node.Children[0]);
        }

        /// <summary>
        /// return the node containing max value which will be a leaf at the right most
        /// </summary>
        private static BTreeNode<T> FindMaxNode(BTreeNode<T> node)
        {
            //if leaf return node
            return node.IsLeaf ? node : FindMaxNode(node.Children[node.KeyCount]);
        }

        /// <summary>
        /// Balance a node which is short of Keys by rotations or merge
        /// </summary>
        private void Balance(BTreeNode<T> node)
        {
            if (node == Root || node.KeyCount >= _minKeysPerNode)
            {
                return;
            }

            var rightSibling = GetRightSibling(node);

            if (rightSibling != null
                && rightSibling.KeyCount > _minKeysPerNode)
            {
                LeftRotate(node, rightSibling);
                return;
            }

            var leftSibling = GetLeftSibling(node);

            if (leftSibling != null
                && leftSibling.KeyCount > _minKeysPerNode)
            {
                RightRotate(leftSibling, node);
                return;
            }

            if (rightSibling != null)
            {
                Sandwich(node, rightSibling);
            }
            else
            {
                Sandwich(leftSibling, node);
            }
        }

        /// <summary>
        ///  merge two adjacent siblings to one node
        /// </summary>
        private void Sandwich(BTreeNode<T> leftSibling, BTreeNode<T> rightSibling)
        {
            var separatorIndex = GetNextSeparatorIndex(leftSibling);
            var parent = leftSibling.Parent;

            var newNode = new BTreeNode<T>(_maxKeysPerNode, leftSibling.Parent);
            var newIndex = 0;

            for (var i = 0; i < leftSibling.KeyCount; i++)
            {
                newNode.Keys[newIndex] = leftSibling.Keys[i];

                if (leftSibling.Children[i] != null)
                {
                    SetChild(newNode, newIndex, leftSibling.Children[i]);
                }

                if (leftSibling.Children[i + 1] != null)
                {
                    SetChild(newNode, newIndex + 1, leftSibling.Children[i + 1]);
                }

                newIndex++;
            }

            //special case when left sibling is empty 
            if (leftSibling.KeyCount == 0 && leftSibling.Children[0] != null)
            {
                SetChild(newNode, newIndex, leftSibling.Children[0]);
            }

            newNode.Keys[newIndex] = parent.Keys[separatorIndex];
            newIndex++;

            for (var i = 0; i < rightSibling.KeyCount; i++)
            {
                newNode.Keys[newIndex] = rightSibling.Keys[i];

                if (rightSibling.Children[i] != null)
                {
                    SetChild(newNode, newIndex, rightSibling.Children[i]);
                }

                if (rightSibling.Children[i + 1] != null)
                {
                    SetChild(newNode, newIndex + 1, rightSibling.Children[i + 1]);
                }

                newIndex++;
            }

            //special case when left sibling is empty 
            if (rightSibling.KeyCount == 0 && rightSibling.Children[0] != null)
            {
                SetChild(newNode, newIndex, rightSibling.Children[0]);
            }

            newNode.KeyCount = newIndex;
            SetChild(parent, separatorIndex, newNode);
            RemoveAt(parent.Keys, separatorIndex);
            parent.KeyCount--;

            RemoveChild(parent, separatorIndex + 1);


            if (parent.KeyCount == 0
                && parent == Root)
            {
                Root = newNode;
                Root.Parent = null;

                if (Root.KeyCount == 0)
                {
                    Root = null;
                }

                return;
            }

            if (parent.KeyCount < _minKeysPerNode)
            {
                Balance(parent);
            }
        }

        /// <summary>
        /// do a right rotation 
        /// </summary>
        private static void RightRotate(BTreeNode<T> leftSibling, BTreeNode<T> rightSibling)
        {
            var parentIndex = GetNextSeparatorIndex(leftSibling);

            InsertAt(rightSibling.Keys, 0, rightSibling.Parent.Keys[parentIndex]);
            rightSibling.KeyCount++;

            InsertChild(rightSibling, 0, leftSibling.Children[leftSibling.KeyCount]);

            rightSibling.Parent.Keys[parentIndex] = leftSibling.Keys[leftSibling.KeyCount - 1];

            RemoveAt(leftSibling.Keys, leftSibling.KeyCount - 1);
            leftSibling.KeyCount--;

            RemoveChild(leftSibling, leftSibling.KeyCount + 1);
        }

        /// <summary>
        /// do a left rotation
        /// </summary>
        private static void LeftRotate(BTreeNode<T> leftSibling, BTreeNode<T> rightSibling)
        {
            var parentIndex = GetNextSeparatorIndex(leftSibling);
            leftSibling.Keys[leftSibling.KeyCount] = leftSibling.Parent.Keys[parentIndex];
            leftSibling.KeyCount++;

            SetChild(leftSibling, leftSibling.KeyCount, rightSibling.Children[0]);


            leftSibling.Parent.Keys[parentIndex] = rightSibling.Keys[0];

            RemoveAt(rightSibling.Keys, 0);
            rightSibling.KeyCount--;

            RemoveChild(rightSibling, 0);
        }

        /// <summary>
        /// Locate the node in which the item to delete exist
        /// </summary>
        private static BTreeNode<T> FindDeletionNode(BTreeNode<T> node, T value)
        {
            //if leaf then its time to insert
            if (node.IsLeaf)
            {
                for (var i = 0; i < node.KeyCount; i++)
                {
                    if (value.CompareTo(node.Keys[i]) == 0)
                    {
                        return node;
                    }
                }
            }
            else
            {
                //if not leaf then drill down to leaf
                for (var i = 0; i < node.KeyCount; i++)
                {
                    if (value.CompareTo(node.Keys[i]) == 0)
                    {
                        return node;
                    }

                    //current value is less than new value
                    //drill down to left child of current value
                    if (value.CompareTo(node.Keys[i]) < 0)
                    {
                        return FindDeletionNode(node.Children[i], value);
                    }
                    //current value is grearer than new value
                    //and current value is last element 

                    if (node.KeyCount == i + 1)
                    {
                        return FindDeletionNode(node.Children[i + 1], value);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Get next key separator index after this child Node in parent 
        /// </summary>
        private static int GetNextSeparatorIndex(BTreeNode<T> node)
        {
            var parent = node.Parent;

            if (node.Index == 0)
            {
                return 0;
            }

            if (node.Index == parent.KeyCount)
            {
                return node.Index - 1;
            }

            return node.Index;
        }

        /// <summary>
        /// get the right sibling node
        /// </summary>
        private static BTreeNode<T> GetRightSibling(BTreeNode<T> node)
        {
            var parent = node.Parent;

            return node.Index == parent.KeyCount ? null : parent.Children[node.Index + 1];
        }

        /// <summary>
        /// get left sibling node
        /// </summary>
        private static BTreeNode<T> GetLeftSibling(BTreeNode<T> node)
        {
            return node.Index == 0 ? null : node.Parent.Children[node.Index - 1];
        }

        private static void SetChild(BTreeNode<T> parent, int childIndex, BTreeNode<T> child)
        {
            parent.Children[childIndex] = child;

            if (child == null)
            {
                return;
            }

            child.Parent = parent;
            child.Index = childIndex;
        }

        private static void InsertChild(BTreeNode<T> parent, int childIndex, BTreeNode<T> child)
        {
            InsertAt(parent.Children, childIndex, child);

            if (child != null)
            {
                child.Parent = parent;
            }

            //update indices
            for (var i = childIndex; i <= parent.KeyCount; i++)
            {
                if (parent.Children[i] != null)
                {
                    parent.Children[i].Index = i;
                }
            }
        }

        private static void RemoveChild(BTreeNode<T> parent, int childIndex)
        {
            RemoveAt(parent.Children, childIndex);

            //update indices
            for (var i = childIndex; i <= parent.KeyCount; i++)
            {
                if (parent.Children[i] != null)
                {
                    parent.Children[i].Index = i;
                }
            }
        }

        /// <summary>
        /// Shift array right at index to make room for new insertion
        /// And then insert at index
        /// Assumes array have atleast one empty index at end
        /// </summary>
        private static void InsertAt<TS>(TS[] array, int index, TS newValue)
        {
            //shift elements right by one indice from index
            Array.Copy(array, index, array, index + 1, array.Length - index - 1);
            //now set the value
            array[index] = newValue;
        }

        /// <summary>
        /// Shift array left at index    
        /// </summary>
        private static void RemoveAt<TS>(TS[] array, int index)
        {
            //shift elements right by one indice from index
            Array.Copy(array, index + 1, array, index, array.Length - index - 1);
        }

        private class DelegateComparer : IValueComparer<T>
        {
            public DelegateComparer(Func<T, int> comparer)
            {
                _comparer = comparer;
            }

            /// <inheritdoc />
            public int CompareTo(T t)
            {
                return _comparer(t);
            }

            private readonly Func<T, int> _comparer;
        }

        private class DefaultComparer : IValueComparer<T>
        {
            public DefaultComparer(T compareObject)
            {
                CompareObject = compareObject;
            }

            /// <inheritdoc />
            public int CompareTo(T t)
            {
                return CompareObject.CompareTo(t);
            }

            private T CompareObject { get; }
        }
    }

    /// <summary>
    /// abstract node shared by both B and B+ tree nodes
    /// so that we can use this for common tests across B and B+ tree
    /// </summary>
    internal abstract class BNode<T> where T : IComparable
    {
        internal BNode(int maxKeysPerNode)
        {
            Keys = new T[maxKeysPerNode];
        }

        /// <summary>
        /// Array Index of this node in parent's Children array
        /// </summary>
        internal int Index { set; get; }

        internal T[] Keys { get; }
        internal int KeyCount { get; set; }

        //for common unit testing across B and B+ tree
        internal abstract BNode<T> GetParent();
        internal abstract BNode<T>[] GetChildren();

        internal int GetMedianIndex()
        {
            return (KeyCount / 2) + 1;
        }
    }

    internal class BTreeNode<T> : BNode<T> where T : IComparable
    {
        internal BTreeNode(int maxKeysPerNode, BTreeNode<T> parent)
            : base(maxKeysPerNode)
        {
            Parent = parent;
            Children = new BTreeNode<T>[maxKeysPerNode + 1];
        }

        internal BTreeNode<T> Parent { get; set; }
        internal BTreeNode<T>[] Children { get; }

        internal bool IsLeaf => Children[0] == null;

        /// <summary>
        /// For shared test method accross B and B+ tree
        /// </summary>
        internal override BNode<T> GetParent()
        {
            return Parent;
        }

        /// <summary>
        /// For shared test method accross B and B+ tree
        /// </summary>
        internal override BNode<T>[] GetChildren()
        {
            return Children;
        }
    }

    internal class BTreeEnumerator<T> : IEnumerator<T> where T : IComparable
    {
        internal BTreeEnumerator(BTreeNode<T> root)
        {
            _root = root;
        }

        public bool MoveNext()
        {
            if (_root == null)
            {
                return false;
            }

            if (_progress == null)
            {
                _current = _root;
                _progress = new Stack<BTreeNode<T>>(_root.Children.Take(_root.KeyCount + 1).Where(x => x != null));
                return _current.KeyCount > 0;
            }

            if (_current != null && _index + 1 < _current.KeyCount)
            {
                _index++;
                return true;
            }

            if (_progress.Count > 0)
            {
                _index = 0;

                _current = _progress.Pop();

                foreach (var child in _current.Children.Take(_current.KeyCount + 1).Where(x => x != null))
                {
                    _progress.Push(child);
                }

                return true;
            }

            return false;
        }

        public void Reset()
        {
            _progress = null;
            _current = null;
            _index = 0;
        }

        object IEnumerator.Current => Current;

        public T Current => _current.Keys[_index];

        public void Dispose()
        {
            _progress = null;
        }

        private readonly BTreeNode<T> _root;

        private BTreeNode<T> _current;
        private int _index;
        private Stack<BTreeNode<T>> _progress;
    }
}