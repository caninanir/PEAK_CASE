using UnityEngine;

public class GridCell : MonoBehaviour
{
    public int x { get; private set; }
    public int y { get; private set; }
    public BaseItem currentItem { get; private set; }

    public void Initialize(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public void SetItem(BaseItem item)
    {
        currentItem = item;
        
        if (item != null)
        {
            item.SetCell(this);
        }
    }

    public BaseItem RemoveItem()
    {
        BaseItem removedItem = currentItem;
        
        if (currentItem != null)
        {
            currentItem.SetCell(null);
            currentItem = null;
        }
        
        return removedItem;
    }

    public bool IsEmpty()
    {
        return currentItem == null;
    }

    public Vector3 GetWorldPosition()
    {
        return transform.position;
    }

    public void HighlightCell(bool highlight)
    {
    }

    public bool CanAcceptItem()
    {
        return IsEmpty();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.9f);
        
        if (!IsEmpty())
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.7f);
        }
    }
}
