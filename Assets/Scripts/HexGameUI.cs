using UnityEngine.EventSystems;
using UnityEngine;

public class HexGameUI : MonoBehaviour {

    public HexGrid grid;

    HexCell currentCell;
    HexUnit selectedUnit;

    public void SetEditMode(bool toggle)
    {
        enabled = !toggle;
        grid.ShowUI(!toggle);
    }

    bool UpdateCurrentCell()
    {
        HexCell cell = grid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
        if (cell != currentCell)
        {
            currentCell = cell;
            return true;
        }
        return false;
    }

    void DoSelection()
    {
        UpdateCurrentCell();
        if (currentCell)
        {
            selectedUnit = currentCell.Unit;
        }
    }

    private void Update()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            if (Input.GetMouseButtonDown(0))
            {
                DoSelection();
            }
        }
    }
}
