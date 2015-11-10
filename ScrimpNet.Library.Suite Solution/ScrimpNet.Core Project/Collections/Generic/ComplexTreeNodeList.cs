// This collection of non-binary tree data structures created by Dan Vanderboom.
// Critical Development blog: http://dvanderboom.wordpress.com
// Original Tree<T> blog article: http://dvanderboom.wordpress.com/2008/03/15/treet-implementing-a-non-binary-tree-in-c/
// Linked-in: http://www.linkedin.com/profile?viewProfile=&key=13009616&trk=tab_pro

using System;
using System.Text;
using System.Runtime.Serialization;

namespace System.Collections.Generic
{
    /// <summary>
    /// Contains a list of ComplexTreeNode (or ComplexTreeNode-derived) objects, with the capability of linking parents and children in both directions.
    /// </summary>
    [DataContract]
    [CollectionDataContract]
    public class ComplexTreeNodeList<T> : List<T> where T : ComplexTreeNode<T>
    {
        private T _parent;
        /// <summary>
        /// The node that owns this list
        /// </summary>
        public T Parent
        {
            get
            {
                return _parent;
            }
            set
            {
                _parent = value;
            }
        }
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="Parent">Parent node to attach this list to</param>
        public ComplexTreeNodeList(ComplexTreeNode<T> Parent)
        {
            this.Parent = (T)Parent;
        }

        /// <summary>
        /// Add a node to this list
        /// </summary>
        /// <param name="Node">Node to add to lsit</param>
        /// <returns>A reference to newly added list</returns>
        public new T Add(T Node)
        {
            base.Add(Node);
            Node.Parent = Parent;
            return Node;
        }

        /// <summary>
        /// Remove a node from this list.  Parent is
        /// </summary>
        /// <param name="Node">Node that will be removed</param>
        /// <returns>A reference to newly removed node</returns>
        public new T Remove(T Node)
        {
            
            //-------------------------------------------------------
            // no action needed
            //-------------------------------------------------------
            if (Node == null) return Node;


            //-------------------------------------------------------
            // remove from list
            //-------------------------------------------------------
            int index = base.IndexOf(Node);
            if (index >= 0)
            {
                base.RemoveAt(index);
            }

            Node.Parent = null;
            return Node;
           
        }

        /// <summary>
        /// Returns total number of elements in this list
        /// </summary>
        /// <returns>Total number of elements in this list</returns>
        public override string ToString()
        {
            return "Count=" + Count.ToString();
        }
    }
}