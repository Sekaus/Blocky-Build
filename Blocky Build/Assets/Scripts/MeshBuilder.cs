using Godot;
using System.Collections.Generic;

class SurfaceBuildData {
    public List<Vector3> Vertices = new();
    public List<Vector3> Normals = new();
    public List<Vector2> UVs = new();
    public List<int> Indices = new();
}

public static class MeshBuilder {
    public static ArrayMesh BuildChunkMesh(Chunk chunk) {
        var mesh = new ArrayMesh();

        // Each block type gets a separate surface
        var surfaces = new Dictionary<Block, SurfaceBuildData>();

        for (int x = 0; x < Chunk.Width; x++)
            for (int y = 0; y < Chunk.Height; y++)
                for (int z = 0; z < Chunk.Depth; z++) {
                    var block = chunk.GetBlock(x, y, z);
                    if (block == null || !block.IsSolid)
                        continue;

                    if (!surfaces.TryGetValue(block, out var surface)) {
                        surface = new SurfaceBuildData();
                        surfaces[block] = surface;
                    }

                    int indexOffset = surface.Vertices.Count; // start index for this block
                    var builder = BlockShapeRegistry.Get(block.Shape);
                    builder.Build(
                        block,
                        chunk,
                        x, y, z,
                        surface.Vertices,
                        surface.Normals,
                        surface.UVs,
                        surface.Indices,
                        ref indexOffset
                    );
                }

        // Convert each block type surface to ArrayMesh surface
        foreach (var kv in surfaces) {
            var block = kv.Key;
            var s = kv.Value;

            var arrays = new Godot.Collections.Array();
            arrays.Resize((int)ArrayMesh.ArrayType.Max);
            arrays[(int)ArrayMesh.ArrayType.Vertex] = s.Vertices.ToArray();
            arrays[(int)ArrayMesh.ArrayType.Normal] = s.Normals.ToArray();
            arrays[(int)ArrayMesh.ArrayType.TexUV] = s.UVs.ToArray();
            arrays[(int)ArrayMesh.ArrayType.Index] = s.Indices.ToArray();

            int surfaceIndex = mesh.GetSurfaceCount();
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

            // Assign the block's material
            if (block.Material != null)
                mesh.SurfaceSetMaterial(surfaceIndex, block.Material);
        }

        return mesh;
    }
}