using System.Collections.Generic;
using Godot;

public interface IBlockShapeBuilder {
    void Build(
        Block block,
        Chunk chunk,
        int x, int y, int z,
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<int> indices,
        ref int indexOffset);
}