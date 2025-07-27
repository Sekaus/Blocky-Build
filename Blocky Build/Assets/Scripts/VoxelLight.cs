using Godot;
using System.Collections.Generic;

public partial class VoxelLight {
    public static Dictionary<Vector3I, byte> CalculateLight(Chunk chunk, Vector3I sunDirection, List<Vector3I> lightSourcePositions) {
        var result = new Dictionary<Vector3I, byte>();
        Queue<Vector3I> queue = new();
        var blocks = chunk.Blocks;

        // Start with sunlight from top
        foreach (var pos in blocks.Keys) {
            if (chunk.IsAirAt(pos) && pos.Y == GameSettings.ChunkHeight - 1) {
                result[pos] = 15;
                queue.Enqueue(pos);
            }
        }

        // Add torches
        foreach (var torch in lightSourcePositions) {
            result[torch] = 15;
            queue.Enqueue(torch);
        }

        Vector3I[] dirs = {
            Vector3I.Right, Vector3I.Left, Vector3I.Forward, Vector3I.Back, Vector3I.Up, Vector3I.Down
        };

        while (queue.Count > 0) {
            var pos = queue.Dequeue();
            byte level = result[pos];
            if (level <= 1)
                continue;

            foreach (var dir in dirs) {
                var next = pos + dir;
                if (blocks.TryGetValue(next, out var bd) && bd.BlockName != "air")
                    continue;

                byte nextLevel = (byte)(level - 1);
                if (!result.ContainsKey(next) || result[next] < nextLevel) {
                    result[next] = nextLevel;
                    queue.Enqueue(next);
                }
            }
        }

        return result; // Dictionary<Vector3I, light level 0–15>
    }
}