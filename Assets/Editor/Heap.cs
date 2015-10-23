using System.Collections.Generic;


public class Heap
{
    public class Node : System.IComparable
    {
        public float error_;
        public int x_;
        public int y_;
        public int index_;

        public int CompareTo(object obj)
        {
            Node node = obj as Node;
            return node.error_.CompareTo(error_);
        }
    };

    private List<Node> list_ = new List<Node>();

    public int Count
    {
        get { return list_.Count; }
    }

    public Heap()
    {
    }

    public void clear()
    {
        list_.Clear();
    }

    public void add(Node n)
    {
        n.index_ = list_.Count;
        list_.Add(n);
        upheap(n.index_);
    }

    public Node pop_max()
    {
        if(list_.Count <= 0) {
            return null;
        }

        Node node = list_[0];
        swap(0, list_.Count - 1);
        list_.RemoveAt(list_.Count - 1);
        downheap(0);
        node.index_ = -1;
        return node;
    }

    public void update(Node node, float newError)
    {
        float oldError = node.error_;
        node.error_ = newError;
        if(oldError < newError) {
            upheap(node.index_);
        } else if(newError < oldError) {
            downheap(node.index_);
        }
    }

    private void swap(int i0, int i1)
    {
        list_[i0].index_ = i1;
        list_[i1].index_ = i0;
        Node n = list_[i0];
        list_[i0] = list_[i1];
        list_[i1] = n;
    }

    private void upheap(int index)
    {
        while(0 < index) {
            int parent = (index - 1) >> 1;
            if(list_[parent].error_ < list_[index].error_) {
                swap(parent, index);
                index = parent;
            } else {
                return;
            }
        }
    }

    private void downheap(int index)
    {
        while(index < list_.Count) {
            int left = (index << 1) + 1;
            if(list_.Count <= left) {
                return;
            }

            int right = left + 1;
            if(list_.Count <= right) {
                if(list_[index].error_ < list_[left].error_) {
                    swap(left, index);
                    index = left;
                } else {
                    return;
                }

            } else {
                int child = (list_[left].error_ < list_[right].error_) ? right : left;
                if(list_[index].error_ < list_[child].error_) {
                    swap(child, index);
                    index = child;
                } else {
                    return;
                }
            }
        }
    }
}
