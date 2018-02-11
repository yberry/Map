using UnityEngine;
using System.IO;

public class HexCell : MonoBehaviour {

    public HexCoordinates coordinates;    
    public RectTransform uiRect;
    public HexGridChunk chunk;

    [SerializeField]
    HexCell[] neighbors;
    [SerializeField]
    bool[] roads;

    int elevation = int.MinValue;
    int waterLevel;
    int terrainTypeIndex;

    bool hasIncomingRiver, hasOutgoingRiver;
    HexDirection incomingRiver, outgoingRiver;

    int urbanLevel, farmLevel, plantLevel;
    bool walled;

    #region Properties

    public int Elevation
    {
        get
        {
            return elevation;
        }

        set
        {
            if (elevation == value)
            {
                return;
            }
            elevation = value;

            RefreshPosition();

            ValidateRivers();

            for (int i = 0; i < roads.Length; i++)
            {
                if (roads[i] && GetElevationDifference((HexDirection)i) > 1)
                {
                    SetRoad(i, false);
                }
            }

            Refresh();
        }
    }

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

    public Vector3 Position
    {
        get
        {
            return transform.localPosition;
        }
    }

    public Color Color
    {
        get
        {
            return HexMetrics.colors[terrainTypeIndex];
        }
    }

    public int TerrainTypeIndex
    {
        get
        {
            return terrainTypeIndex;
        }

        set
        {
            if (terrainTypeIndex != value)
            {
                terrainTypeIndex = value;
                Refresh();
            }
        }
    }

    public bool HasIncomingRiver
    {
        get
        {
            return hasIncomingRiver;
        }
    }

    public bool HasOutgoingRiver
    {
        get
        {
            return hasOutgoingRiver;
        }
    }

    public HexDirection IncomingRiver
    {
        get
        {
            return incomingRiver;
        }
    }

    public HexDirection OutgoingRiver
    {
        get
        {
            return outgoingRiver;
        }
    }

    public bool HasRiver
    {
        get
        {
            return hasIncomingRiver || hasOutgoingRiver;
        }
    }

    public bool HasRiverBeginOrEnd
    {
        get
        {
            return hasIncomingRiver != hasOutgoingRiver;
        }
    }

    public float SteamBedY
    {
        get
        {
            return (elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep;
        }
    }

    public float RiverSurfaceY
    {
        get
        {
            return (elevation + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep;
        }
    }

    public HexDirection RiverBeginOrEndDirection
    {
        get
        {
            return hasIncomingRiver ? incomingRiver : outgoingRiver;
        }
    }

    public bool HasRoads
    {
        get
        {
            for (int i = 0; i < roads.Length; i++)
            {
                if (roads[i])
                {
                    return true;
                }
            }
            return false;
        }
    }

    public int WaterLevel
    {
        get
        {
            return waterLevel;
        }

        set
        {
            if (waterLevel == value)
            {
                return;
            }
            waterLevel = value;
            ValidateRivers();
            Refresh();
        }
    }

    public bool IsUnderwater
    {
        get
        {
            return waterLevel > elevation;
        }
    }

    public float WaterSurfaceY
    {
        get
        {
            return (waterLevel + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep;
        }
    }

    public int UrbanLevel
    {
        get
        {
            return urbanLevel;
        }

        set
        {
            if (urbanLevel != value)
            {
                urbanLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public int FarmLevel
    {
        get
        {
            return farmLevel;
        }

        set
        {
            if (farmLevel != value)
            {
                farmLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public int PlantLevel
    {
        get
        {
            return plantLevel;
        }

        set
        {
            if (plantLevel != value)
            {
                plantLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public bool Walled
    {
        get
        {
            return walled;
        }

        set
        {
            if (walled != value)
            {
                walled = value;
                Refresh();
            }
        }
    }

    #endregion

    void RefreshPosition()
    {
        Vector3 position = transform.localPosition;
        position.y = elevation * HexMetrics.elevationStep;
        position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.elevationPerturbStrength;
        transform.localPosition = position;

        Vector3 uiPosition = uiRect.localPosition;
        uiPosition.z = -position.y;
        uiRect.localPosition = uiPosition;
    }

    public HexEdgeType GetEdgeType(HexDirection direction)
    {
        return HexMetrics.GetEdgeType(elevation, neighbors[(int)direction].elevation);
    }

    public HexEdgeType GetEdgeType(HexCell otherCell)
    {
        return HexMetrics.GetEdgeType(elevation, otherCell.elevation);
    }

    public int GetElevationDifference(HexDirection direction)
    {
        int difference = elevation - this[direction].elevation;
        return difference >= 0 ? difference : -difference;
    }

    void Refresh()
    {
        if (chunk)
        {
            chunk.Refresh();
            for (int i = 0; i < neighbors.Length; i++)
            {
                HexCell neighbor = neighbors[i];
                if (neighbor != null && neighbor.chunk != chunk)
                {
                    neighbor.chunk.Refresh();
                }
            }
        }
    }

    public bool HasRiverThroughEdge(HexDirection direction)
    {
        return hasIncomingRiver && incomingRiver == direction ||
            hasOutgoingRiver && outgoingRiver == direction;
    }

    public void RemoveOutgoingRiver()
    {
        if (!hasOutgoingRiver)
        {
            return;
        }
        hasOutgoingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = this[outgoingRiver];
        neighbor.hasIncomingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    public void RemoveIncomingRiver()
    {
        if (!hasIncomingRiver)
        {
            return;
        }
        hasIncomingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = this[incomingRiver];
        neighbor.hasOutgoingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    bool IsValidRiverDestination(HexCell neighbor)
    {
        return neighbor && (elevation >= neighbor.elevation || waterLevel == neighbor.elevation);
    }

    void RefreshSelfOnly()
    {
        chunk.Refresh();
    }

    public void RemoveRiver()
    {
        RemoveOutgoingRiver();
        RemoveIncomingRiver();
    }

    public void SetOutgoingRiver(HexDirection direction)
    {
        if (hasOutgoingRiver && outgoingRiver == direction)
        {
            return;
        }

        HexCell neighbor = this[direction];
        if (!IsValidRiverDestination(neighbor))
        {
            return;
        }

        RemoveOutgoingRiver();
        if (hasIncomingRiver && incomingRiver == direction)
        {
            RemoveIncomingRiver();
        }

        hasOutgoingRiver = true;
        outgoingRiver = direction;

        neighbor.RemoveIncomingRiver();
        neighbor.hasIncomingRiver = true;
        neighbor.incomingRiver = direction.Opposite();

        SetRoad((int)direction, false);
    }

    void ValidateRivers()
    {
        if (hasOutgoingRiver && !IsValidRiverDestination(this[outgoingRiver]))
        {
            RemoveOutgoingRiver();
        }
        if (hasIncomingRiver && !this[incomingRiver].IsValidRiverDestination(this))
        {
            RemoveIncomingRiver();
        }
    }

    public bool HasRoadThroughEdge(HexDirection direction)
    {
        return roads[(int)direction];
    }

    public void AddRoad(HexDirection direction)
    {
        if (!roads[(int)direction] && !HasRiverThroughEdge(direction) && GetElevationDifference(direction) <= 1)
        {
            SetRoad((int)direction, true);
        }
    }

    public void RemoveRoads()
    {
        for (int i = 0; i < neighbors.Length; i++)
        {
            if (roads[i])
            {
                SetRoad(i, false);
            }
        }
    }

    void SetRoad(int index, bool state)
    {
        roads[index] = state;
        neighbors[index].roads[(int)((HexDirection)index).Opposite()] = state;
        neighbors[index].RefreshSelfOnly();
        RefreshSelfOnly();
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write((byte)terrainTypeIndex);
        writer.Write((byte)elevation);
        writer.Write((byte)waterLevel);
        writer.Write((byte)urbanLevel);
        writer.Write((byte)farmLevel);
        writer.Write((byte)plantLevel);
        writer.Write(walled);
        writer.Write(hasIncomingRiver);
        writer.Write((byte)incomingRiver);
        writer.Write(hasOutgoingRiver);
        writer.Write((byte)outgoingRiver);

        foreach (bool road in roads)
        {
            writer.Write(road);
        }
    }

    public void Load(BinaryReader reader)
    {
        terrainTypeIndex = reader.ReadByte();
        elevation = reader.ReadByte();
        RefreshPosition();
        waterLevel = reader.ReadByte();
        urbanLevel = reader.ReadByte();
        farmLevel = reader.ReadByte();
        plantLevel = reader.ReadByte();
        walled = reader.ReadBoolean();
        hasIncomingRiver = reader.ReadBoolean();
        incomingRiver = (HexDirection)reader.ReadByte();
        hasOutgoingRiver = reader.ReadBoolean();
        outgoingRiver = (HexDirection)reader.ReadByte();

        for (int i = 0; i < roads.Length; i++)
        {
            roads[i] = reader.ReadBoolean();
        }
    }
}
