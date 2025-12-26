using System.Collections.Generic;
using Godot;

public class CrossShapeBuilder : IBlockShapeBuilder {
    static readonly Vector2[] QuadUVs = new Vector2[]
    {
        new Vector2(0,0),
        new Vector2(1,0),
        new Vector2(1,1),
        new Vector2(0,1)
    };

    public void Build(Block block, Chunk chunk, int x, int y, int z,
        List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> indices, ref int indexOffset) {
        // First diagonal quad
        Vector3[] quad1 = new Vector3[]
        {
            new Vector3(0,0,0), new Vector3(1,0,1), new Vector3(1,1,1), new Vector3(0,1,0)
        };

        // Second diagonal quad
        Vector3[] quad2 = new Vector3[]
        {
            new Vector3(1,0,0), new Vector3(0,0,1), new Vector3(0,1,1), new Vector3(1,1,0)
        };

        for (int q = 0; q < 2; q++) {
            Vector3[] quad = q == 0 ? quad1 : quad2;

            for (int i = 0; i < 4; i++) {
                vertices.Add(new Vector3(x + quad[i].X, y + quad[i].Y, z + quad[i].Z));
                normals.Add(Vector3.Up);
                uvs.Add(QuadUVs[i]); // Use simple 0-1 UVs
            }

            // Two triangles per quad
            indices.Add(indexOffset + 0);
            indices.Add(indexOffset + 1);
            indices.Add(indexOffset + 2);
            indices.Add(indexOffset + 0);
            indices.Add(indexOffset + 2);
            indices.Add(indexOffset + 3);

            indexOffset += 4;
        }
    }
}