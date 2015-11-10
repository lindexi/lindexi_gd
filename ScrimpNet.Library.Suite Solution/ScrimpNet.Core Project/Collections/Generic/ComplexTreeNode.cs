// This collection of non-binary tree data structures created by Dan Vanderboom.
// Critical Development blog: http://dvanderboom.wordpress.com
// Original Tree<T> blog article: http://dvanderboom.wordpress.com/2008/03/15/treet-implementing-a-non-binary-tree-in-c/
// Linked-in: http://www.linkedin.com/profile?viewProfile=&key=13009616&trk=tab_pro

using System;
using System.Text;
using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace System.Collections.Generic
{
    /// <summary>
    /// Represents a node in a Tree structure, with a parent node and zero or more child nodes.
    /// </summary>
    /// <remarks>Called 'ComplexTreeNode' since 'TreeNode' is used in WinForms TreeView control</remarks>
    [DataContract]
    public class ComplexTreeNode<T> : IDisposable where T : ComplexTreeNode<T>
    {
        private T _Parent;
        /// <summary>
        /// Parent element 
        /// </summary>
        [XmlIgnore]
        public T Parent
        {
            get { return _Parent; }
            set
            {
                if (value == _Parent)
                {
                    return;
                }

                //if (_Parent != null) //ScrimpNet.com modification
                //{
                    
                //    _Parent.Children.Remove((T)this);
                //}

                if (value != null && !value.Children.Contains((T)this))
                {
                    value.Children.Add((T)this);
                }

                _Parent = value;
            }
        }

        /// <summary>
        /// Root node of this tree
        /// </summary>
        public T Root
        {
            get
            {
                //return (Parent == null) ? this : Parent.Root;

                ComplexTreeNode<T> node = this;
                while (node.Parent != null)
                {
                    node = node.Parent;
                }
                return (T)node;
            }
        }

        private ComplexTreeNodeList<T> _Children;

        /// <summary>
        /// List of first degree children of this node.  Null or empty list if no children
        /// </summary>
        [DataMember]
        public virtual ComplexTreeNodeList<T> Children
        {
            get { return _Children; }
            private set { _Children = value; }
        }

        private TreeTraversalDirection _DisposeTraversal = TreeTraversalDirection.BottomUp;
        /// <summary>
        /// Specifies the pattern for traversing the Tree for disposing of resources. Default is BottomUp.
        /// </summary>
        public TreeTraversalDirection DisposeTraversal
        {
            get { return _DisposeTraversal; }
            set { _DisposeTraversal = value; }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ComplexTreeNode()
        {
            Parent = null;
            Children = new ComplexTreeNodeList<T>(this);
        }

        /// <summary>
        /// Attach a tree to an existing tree node
        /// </summary>
        /// <param name="Parent">Set the parent of this tree</param>
        public ComplexTreeNode(T Parent)
        {
            this.Parent = Parent;
            Children = new ComplexTreeNodeList<T>(this);
        }

        /// <summary>
        /// Add a list of children to this tree making this root as an object
        /// </summary>
        /// <param name="Children">Hydrated children to populate this tree with</param>
        public ComplexTreeNode(ComplexTreeNodeList<T> Children)
        {
            Parent = null;
            this.Children = Children;
            Children.Parent = (T)this;
        }

        /// <summary>
        /// Add this node to a tree.  This node's parent will be <paramref name="Parent"/>
        /// </summary>
        /// <param name="Parent">Parent this node will have</param>
        /// <param name="Children">List of children to add to this node</param>
        public ComplexTreeNode(T Parent, ComplexTreeNodeList<T> Children):this(Children)
        {
            this.Parent = Parent;
            
        }

        /// <summary>
        /// Reports a depth of nesting in the tree, starting at 0 for the root.
        /// </summary>
        public int Depth
        {
            get
            {
                //return (Parent == null ? -1 : Parent.Depth) + 1;

                int depth = 0;
                ComplexTreeNode<T> node = this;
                while (node.Parent != null)
                {
                    node = node.Parent;
                    depth++;
                }
                return depth;
            }
        }

        /// <summary>
        /// Converts nodes to string
        /// </summary>
        /// <returns>Standard node format</returns>
        public override string ToString()
        {
            string Description = "Depth=" + Depth.ToString() + ", Children=" + Children.Count.ToString();
            if (this == Root)
            {
                Description += " (Root)";
            }
            return Description;
        }

        #region IDisposable

        private bool _IsDisposed;
        /// <summary>
        /// Check to see if this node has been successfully disposed
        /// </summary>
        public bool IsDisposed
        {
            get { return _IsDisposed; }
        }

        /// <summary>
        /// Remove unmanaged resources that might be held by the tree
        /// </summary>
        public virtual void Dispose()
        {
            CheckDisposed();

            // clean up contained objects (in Value property)
            if (DisposeTraversal == TreeTraversalDirection.BottomUp)
            {
                foreach (ComplexTreeNode<T> node in Children)
                {
                    node.Dispose();
                }
            }

            OnDisposing();

            if (DisposeTraversal == TreeTraversalDirection.TopDown)
            {
                foreach (ComplexTreeNode<T> node in Children)
                {
                    node.Dispose();
                }
            }

            // TODO: clean up the tree itself

            _IsDisposed = true;
        }
        /// <summary>
        /// Fired when this node is disposed
        /// </summary>
        public event EventHandler Disposing;

        /// <summary>
        /// Called when node is disposing
        /// </summary>
        protected void OnDisposing()
        {
            if (Disposing != null)
            {
                Disposing(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Check to see if this node is already disposed
        /// </summary>
        protected void CheckDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        #endregion
    }
}