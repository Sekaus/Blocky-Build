using Godot;

// FaceIndex mapping (for clarity)
public enum FaceIndex {
    Up = 0,
    Down = 1,
    North = 2, // front
    South = 3, // back
    East = 4,  // right
    West = 5   // left
}

public class Face {
    public Vector3I Direction;    // offset to neighbor
    public Vector3 Normal;
    public Vector3[] Vertices;   // 4 vertices (in local block space)
    public int FaceIndex;        // 0-5 to index Block.FaceUVs

    public Face(Vector3I direction, Vector3 normal, Vector3[] vertices, int faceIndex) {
        Direction = direction;
        Normal = normal;
        Vertices = vertices;
        FaceIndex = faceIndex;
    }
}

public static class FaceData {
    // Note: vertex ordering matches the winding used by MeshBuilder (CCW)
    public static readonly Face[] Faces =
    {
        // Up (0)
        new Face(new Vector3I(0, 1, 0), Vector3.Up, new Vector3[]
        {
            new Vector3(0,1,0), new Vector3(1,1,0), new Vector3(1,1,1), new Vector3(0,1,1)
        }, (int)FaceIndex.Up),

        // Down (1)
        new Face(new Vector3I(0, -1, 0), Vector3.Down, new Vector3[]
        {
            new Vector3(0,0,0), new Vector3(0,0,1), new Vector3(1,0,1), new Vector3(1,0,0)
        }, (int)FaceIndex.Down),

        // North / Front (2)
        new Face(new Vector3I(0, 0, 1), Vector3.Forward, new Vector3[]
        {
            new Vector3(0,0,1), new Vector3(0,1,1), new Vector3(1,1,1), new Vector3(1,0,1)
        }, (int)FaceIndex.North),

        // South / Back (3)
        new Face(new Vector3I(0, 0, -1), Vector3.Back, new Vector3[]
        {
            new Vector3(1,0,0), new Vector3(1,1,0), new Vector3(0,1,0), new Vector3(0,0,0)
        }, (int)FaceIndex.South),

        // East / Right (4)
        new Face(new Vector3I(1, 0, 0), Vector3.Right, new Vector3[]
        {
            new Vector3(1,0,1), new Vector3(1,1,1), new Vector3(1,1,0), new Vector3(1,0,0)
        }, (int)FaceIndex.East),

        // West / Left (5)
        new Face(new Vector3I(-1, 0, 0), Vector3.Left, new Vector3[]
        {
            new Vector3(0,0,0), new Vector3(0,1,0), new Vector3(0,1,1), new Vector3(0,0,1)
        }, (int)FaceIndex.West)
    };
}