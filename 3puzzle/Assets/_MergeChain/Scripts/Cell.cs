using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

namespace MergeChain
{
    public enum StatusCell
    {
        Empty,
        NonEmpty}

    ;

    /// <summary>
    /// To record the location of tile data.
    /// </summary>
    [System.Serializable]
    public class Cell
    {
        public StatusCell cellType;
        public Vector3 positionInParent;

        public bool isEmpty { get { return cellType == StatusCell.Empty; } }

        public void SetRandomTile(int total)
        {
            cellType = (StatusCell)Random.Range(0, total) + 1;
        }

        public void InitCell()
        {
            cellType = StatusCell.NonEmpty;
        }

        public Cell Clone()
        {
            Cell c = new Cell();
            c.cellType = this.cellType;
            c.positionInParent = this.positionInParent;
            return c;
        }
    }
}