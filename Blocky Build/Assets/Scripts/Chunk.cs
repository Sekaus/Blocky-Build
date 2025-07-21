using Godot;
using Godot.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Godot.HttpRequest;

public class Chunk {
    private readonly System.Collections.Generic.Dictionary<Vector3I, Block> blocks = new();
    private readonly object blockLock = new();

    private readonly TaskCompletionSource<bool> readySource = new();
    public Task ReadyTask => readySource.Task;

    private readonly ConcurrentQueue<Block> culledBlocks = new();
    public ConcurrentQueue<Block> Blocks => culledBlocks;

    public Chunk(Vector3I chunkPosition, WorldData.WorldType worldType, WorldData.WorldLayer[][] worldLayers) {
        Task.Run(() => GenChunk(chunkPosition, worldType, worldLayers));
    }

    private void GenChunk(Vector3I atChunk, WorldData.WorldType worldType, WorldData.WorldLayer[][] worldLayers) {
        lock (blockLock) {
            int atLayer = GameSettings.DefaultBedrockLevel;
            Vector3I offset = atChunk * GameSettings.ChunkRadius;

            foreach (var worldLayer in worldLayers[(int)worldType]) {
                for (int y = atLayer; y < atLayer + worldLayer.height; y++) {
                    for (int x = -GameSettings.ChunkRadius + offset.X; x < GameSettings.ChunkRadius + offset.X; x++) {
                        for (int z = -GameSettings.ChunkRadius + offset.Z; z < GameSettings.ChunkRadius + offset.Z; z++) {
                            var newBlock = Register.Blocks[worldLayer.blockName]?.Instantiate<Block>();
                            var pos = new Vector3I(x, y, z);
                            newBlock.Translate(pos);
                            blocks[pos] = newBlock;
                        }
                    }
                }
                atLayer += worldLayer.height;
            }
        }

        readySource.TrySetResult(true);
    }

    public async Task CullChunk(CancellationToken ct = default) {
        System.Collections.Generic.Dictionary<Vector3I, Block> snapshot;

        lock (blockLock) {
            snapshot = new System.Collections.Generic.Dictionary<Vector3I, Block>(blocks);
        }

        var keys = snapshot.Keys.ToHashSet();

        var options = new ParallelOptions {
            CancellationToken = ct,
            MaxDegreeOfParallelism = System.Environment.ProcessorCount
        };

        await Parallel.ForEachAsync(snapshot, options, (kv, token) => {
            var key = kv.Key;
            var block = kv.Value;

            bool isExposed = !keys.Contains(new(key.X + 1, key.Y, key.Z)) ||
                             !keys.Contains(new(key.X - 1, key.Y, key.Z)) ||
                             !keys.Contains(new(key.X, key.Y + 1, key.Z)) ||
                             !keys.Contains(new(key.X, key.Y - 1, key.Z)) ||
                             !keys.Contains(new(key.X, key.Y, key.Z + 1)) ||
                             !keys.Contains(new(key.X, key.Y, key.Z - 1));

            if (isExposed)
                culledBlocks.Enqueue(block);

            return ValueTask.CompletedTask;
        });
    }
}
