using Godot;
using System.Collections.Generic;

public class SlabShapeBuilder : IBlockShapeBuilder {
    private static readonly Vector3[] Top =
    {
        new(0,0.5f,0), new(1,0.5f,0),
        new(1,0.5f,1), new(0,0.5f,1)
    };

    public void Build(
        Block block, Chunk chunk,
        int x, int y, int z,
        List<Vector3> v, List<Vector3> n,
        List<Vector2> uv, List<int> i,
        ref int idx) {
        // Bottom face always visible
        // Sides only half-height
        // Top visible if block above is air
        // (implementation follows same pattern as cube)
    }
}