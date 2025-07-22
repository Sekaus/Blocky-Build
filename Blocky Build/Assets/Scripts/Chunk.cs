using Godot;
using Godot.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static System.Reflection.Metadata.BlobBuilder;

public class Chunk {
    public readonly Vector3I Position;
    // Pure data: which block type belongs at which coordinate
    private readonly System.Collections.Generic.Dictionary<Vector3I, string> _rawBlocks = new();
    public System.Collections.Generic.Dictionary<Vector3I, string> Blocks {
        get { 
            return _rawBlocks; 
        }
    }

    private readonly TaskCompletionSource<bool> _dataReady = new();
    public Task DataReady => _dataReady.Task;

    public Chunk(Vector3I chunkPosition, WorldData.WorldType worldType, WorldData.WorldLayer[][] worldLayers) {
        Position = chunkPosition;
        // Kick off only your pure-data work in the threadpool:
        ThreadPool.QueueUserWorkItem(_ => GenerateRawData(worldType, worldLayers));
    }

    private void GenerateRawData(WorldData.WorldType worldType, WorldData.WorldLayer[][] worldLayers) {
        var offset = Position * GameSettings.ChunkRadius;
        int atLayer = GameSettings.DefaultBedrockLevel;

        foreach (var layer in worldLayers[(int)worldType]) {
            for (int y = atLayer; y < atLayer + layer.height; y++) {
                for (int x = -GameSettings.ChunkRadius + offset.X;
                     x < GameSettings.ChunkRadius + offset.X; x++) {
                    for (int z = -GameSettings.ChunkRadius + offset.Z;
                         z < GameSettings.ChunkRadius + offset.Z; z++) {
                        _rawBlocks.Add(new Vector3I(x, y, z), layer.blockName);
                    }
                }
            }
            atLayer += layer.height;
        }

        _dataReady.TrySetResult(true);
    }

    /*public void CullChunk() {
        Parallel.ForEach(_rawBlocks, block => {
            int X = block.Key.X;
            int Y = block.Key.Y;
            int Z = block.Key.Z;
            bool isExposed = !_rawBlocks.ContainsKey(new(X + 1, Y, Z)) ||
                             !_rawBlocks.ContainsKey(new(X - 1, Y, Z)) ||
                             !_rawBlocks.ContainsKey(new(X, Y + 1, Z)) ||
                             !_rawBlocks.ContainsKey(new(X, Y - 1, Z)) ||
                             !_rawBlocks.ContainsKey(new(X, Y, Z + 1)) ||
                             !_rawBlocks.ContainsKey(new(X, Y, Z - 1));

            if (isExposed)
                culledBlocks.TryAdd(block.Key, block.Value);
        });
    }*/
}
