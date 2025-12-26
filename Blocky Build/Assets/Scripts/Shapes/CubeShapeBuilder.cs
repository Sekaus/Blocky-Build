using Godot;
using System.Collections.Generic;

public class CubeShapeBuilder : IBlockShapeBuilder {
    // Full 0-1 UV mapping for a quad
    static readonly Vector2[] QuadUVs = new Vector2[]
    {
        new Vector2(0, 0),
        new Vector2(1, 0),
        new Vector2(1, 1),
        new Vector2(0, 1)
    };

    public void Build(
        Block block, Chunk chunk,
        int x, int y, int z,
        List<Vector3> verts,
        List<Vector3> norms,
        List<Vector2> uvs,
        List<int> inds,
        ref int index) {
        foreach (var face in FaceData.Faces) {
            int nx = x + face.Direction.X;
            int ny = y + face.Direction.Y;
            int nz = z + face.Direction.Z;

            // Skip if neighbor is solid
            if (nx >= 0 && ny >= 0 && nz >= 0 &&
                nx < Chunk.Width && ny < Chunk.Height && nz < Chunk.Depth) {
                var neighbor = chunk.GetBlock(nx, ny, nz);
                if (neighbor != null && neighbor.IsSolid)
                    continue;
            }

            // Add face vertices
            for (int i = 0; i < 4; i++) {
                verts.Add(new Vector3(x, y, z) + face.Vertices[i]);
                norms.Add(face.Normal);
                uvs.Add(QuadUVs[i]); // Full texture UV
            }

            // Add two triangles per quad
            inds.Add(index + 0);
            inds.Add(index + 1);
            inds.Add(index + 2);
            inds.Add(index + 0);
            inds.Add(index + 2);
            inds.Add(index + 3);

            index += 4;
        }
    }
}