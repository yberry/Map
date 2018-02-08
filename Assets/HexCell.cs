﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexCell : MonoBehaviour {

    public HexCoordinates coordinates;

    public Color color;

    [SerializeField]
    HexCell[] neighbors;

    public HexCell this[HexDirection direction]
    {
        get
        {
            return neighbors[(int)direction];
        }

        set
        {
            neighbors[(int)direction] = value;
            value.neighbors[(int)direction.Opposite()] = this;
        }
    }

    /*public HexCell GetNeighbor(HexDirection direction)
    {
        return neighbors[(int)direction];
    }

    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        neighbors[(int)direction] = cell;
        cell.neighbors[(int)direction.Opposite()] = this;
    }*/
}
