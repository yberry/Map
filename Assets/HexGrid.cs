using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour {

    public int width = 6;
    public int height = 6;

    public Color defaultColor = Color.white;
    public Color touchedColor = Color.magenta;

    public HexCell cellPrefab;
    public Text cellLabelPrefab;

    HexCell[] cells;
    Canvas gridCanvas;
    HexMesh hexMesh;

    private void Awake()
    {
        gridCanvas = GetComponentInChildren<Canvas>();
        hexMesh = GetComponentInChildren<HexMesh>();

        cells = new HexCell[height * width];

        for (int z = 0, i = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }

    private void Start()
    {
        hexMesh.Triangulate(cells);
    }

    void CreateCell(int x, int z, int i)
    {
        Vector3 position = new Vector3(
            (x + z * 0.5f - z / 2) * 2f * HexMetrics.innerRadius,
            0f,
            z * 1.5f * HexMetrics.outerRadius
        );

        HexCell cell = cells[i] = Instantiate(cellPrefab);
        cell.transform.SetParent(transform, false);
        cell.transform.localPosition = position;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.color = defaultColor;

        if (x > 0)
        {
            cell[HexDirection.W] = cells[i - 1];
        }
        if (z > 0)
        {
            if ((z & 1) == 0)
            {
                cell[HexDirection.SE] = cells[i - width];
                if (x > 0)
                {
                    cell[HexDirection.SW] = cells[i - width - 1];
                }
            }
            else
            {
                cell[HexDirection.SW] = cells[i - width];
                if (x < width - 1)
                {
                    cell[HexDirection.SE] = cells[i - width + 1];
                }
            }
        }

        Text label = Instantiate(cellLabelPrefab);
        label.rectTransform.SetParent(gridCanvas.transform, false);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        label.text = cell.coordinates.ToStringOnSeparateLines();

        cell.uiRect = label.rectTransform;
    }

    public HexCell GetCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * width + coordinates.Z / 2;
        return cells[index];
    }

    public void Refresh()
    {
        hexMesh.Triangulate(cells);
    }
}
