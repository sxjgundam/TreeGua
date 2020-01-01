using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MergeChain
{
    /// <summary>
    /// To record the location of data.
    /// </summary>
    public class ItemPoint
    {
        public int x, y;

        public ItemPoint(int px, int py)
        {
            x = px;
            y = py;
        }

        public override string ToString()
        {
            return " (" + x + ", " + y + ") ";
        }

        public bool CompareItemPoint(System.Object obj)
        {
            if (obj == null)
                return false;
            ItemPoint itemPointObj = (ItemPoint)obj;
            if ((System.Object)itemPointObj == null)
                return false;
            else
                return (x == itemPointObj.x && y == itemPointObj.y);
        }

        public ItemPoint Clone()
        {
            ItemPoint p = new ItemPoint(x, y);
            return p;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, 0);
        }
    }
}