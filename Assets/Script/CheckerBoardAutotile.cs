using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "CheckerboardAutotile", menuName = "Tiles/Checkerboard Autotile")]
public class CheckerboardAutotile : TileBase
{
    // Enum for tile types
    public enum TileType
    {
        Isolated = 0,
        Center = 1,
        EdgeTop = 2,     // Missing tile on top
        EdgeRight = 3,   // Missing tile on right
        EdgeBottom = 4,  // Missing tile on bottom
        EdgeLeft = 5,    // Missing tile on left
        CornerBL = 6,    // Bottom-left corner with neighbors above and to the right
        CornerTL = 7,    // Top-left corner with neighbors below and to the right
        CornerTR = 8,    // Top-right corner with neighbors below and to the left
        CornerBR = 9,    // Bottom-right corner with neighbors above and to the left
        InnerTR = 10,    // Inner corner with hollow in top-right (missing diagonal)
        InnerBR = 11,    // Inner corner with hollow in bottom-right (missing diagonal)
        InnerBL = 12,    // Inner corner with hollow in bottom-left (missing diagonal)
        InnerTL = 13,    // Inner corner with hollow in top-left (missing diagonal)
        LineHLeft = 14,
        LineHMiddle = 15,
        LineHRight = 16,
        LineVTop = 17,
        LineVMiddle = 18,
        LineVBottom = 19
    }

    [System.Serializable]
    public struct TilePair
    {
        public Sprite beige;
        public Sprite maroon;
    }

    [Tooltip("Define sprites for each tile type")]
    public TilePair[] tileTypes = new TilePair[20]; // Size matches enum count

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        TileType tileType = DetermineTileType(position, tilemap);

        // Get sprite pair for this tile type
        TilePair pair = tileTypes[(int)tileType];

        // Handle empty tile (no sprite)
        if (pair.beige == null && pair.maroon == null)
        {
            return;
        }

        // Checkerboard logic: (x + y) % 2 determines color
        bool isBeige = (position.x + position.y) % 2 == 0;
        tileData.sprite = isBeige ? pair.beige : pair.maroon;
        tileData.flags = TileFlags.LockAll;
        tileData.colliderType = Tile.ColliderType.None;
    }

    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                Vector3Int neighborPos = new Vector3Int(position.x + x, position.y + y, position.z);
                tilemap.RefreshTile(neighborPos);
            }
        }
    }

    private TileType DetermineTileType(Vector3Int pos, ITilemap tilemap)
    {
        // Check for the presence of neighboring tiles
        bool top = HasTile(tilemap, pos + new Vector3Int(0, 1, 0));
        bool right = HasTile(tilemap, pos + new Vector3Int(1, 0, 0));
        bool bottom = HasTile(tilemap, pos + new Vector3Int(0, -1, 0));
        bool left = HasTile(tilemap, pos + new Vector3Int(-1, 0, 0));

        // Diagonals
        bool topRight = HasTile(tilemap, pos + new Vector3Int(1, 1, 0));
        bool bottomRight = HasTile(tilemap, pos + new Vector3Int(1, -1, 0));
        bool bottomLeft = HasTile(tilemap, pos + new Vector3Int(-1, -1, 0));
        bool topLeft = HasTile(tilemap, pos + new Vector3Int(-1, 1, 0));

        // Count the number of cardinal neighbors
        int cardinalCount = 0;
        if (top) cardinalCount++;
        if (right) cardinalCount++;
        if (bottom) cardinalCount++;
        if (left) cardinalCount++;

        // Determine tile type based on neighbors
        if (cardinalCount == 0)
        {
            return TileType.Isolated; // No neighbors
        }
        else if (cardinalCount == 4)
        {
            // Check for inner corners - all cardinal neighbors but missing a diagonal
            if (!topRight) return TileType.InnerBL;    // Swapped with InnerTR
            if (!bottomRight) return TileType.InnerTL; // Swapped with InnerBR
            if (!bottomLeft) return TileType.InnerTR;  // Swapped with InnerBL
            if (!topLeft) return TileType.InnerBR;     // Swapped with InnerTL

            return TileType.Center; // All cardinal and diagonal neighbors
        }

        else if (cardinalCount == 3)
        {
            // Edge tiles (missing one cardinal direction)
            if (!top) return TileType.EdgeTop;
            if (!right) return TileType.EdgeRight;
            if (!bottom) return TileType.EdgeBottom;
            if (!left) return TileType.EdgeLeft;
        }
        else if (cardinalCount == 2)
        {
            // Check for corners (two adjacent neighbors)
            if (top && right) return TileType.CornerBL;    // Bottom-left corner
            if (right && bottom) return TileType.CornerTL; // Top-left corner
            if (bottom && left) return TileType.CornerTR;  // Top-right corner
            if (left && top) return TileType.CornerBR;     // Bottom-right corner

            // Check for line segments
            if (left && right)
            {
                // Horizontal line, need to determine if it's an end or middle
                bool leftOfLeft = HasTile(tilemap, pos + new Vector3Int(-2, 0, 0));
                bool rightOfRight = HasTile(tilemap, pos + new Vector3Int(2, 0, 0));

                if (!leftOfLeft && rightOfRight) return TileType.LineHLeft;
                if (leftOfLeft && !rightOfRight) return TileType.LineHRight;
                return TileType.LineHMiddle;
            }

            if (top && bottom)
            {
                // Vertical line, need to determine if it's an end or middle
                bool topOfTop = HasTile(tilemap, pos + new Vector3Int(0, 2, 0));
                bool bottomOfBottom = HasTile(tilemap, pos + new Vector3Int(0, -2, 0));

                if (!topOfTop && bottomOfBottom) return TileType.LineVTop;
                if (topOfTop && !bottomOfBottom) return TileType.LineVBottom;
                return TileType.LineVMiddle;
            }
        }
        else if (cardinalCount == 1)
        {
            // Single tile connections
            if (top) return TileType.EdgeTop;
            if (right) return TileType.EdgeRight;
            if (bottom) return TileType.EdgeBottom;
            if (left) return TileType.EdgeLeft;
        }

        // Default to isolated if no pattern is recognized
        return TileType.Isolated;
    }

    private bool HasTile(ITilemap tilemap, Vector3Int pos)
    {
        return tilemap.GetTile(pos) != null;
    }
}