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
    private readonly ConcurrentDictionary<Vector3I, BlockData> _rawBlocks = new();
    public ConcurrentDictionary<Vector3I, BlockData> Blocks {
        get {
            return _rawBlocks;
        }
    }

    private readonly TaskCompletionSource<bool> _dataReady = new();
    public Task DataReady => _dataReady.Task;

    public Chunk(Vector3I chunkPosition, WorldData worldData, WorldData.WorldLayer[][] worldLayers) {
        Position = chunkPosition;
        // Kick off only your pure-data work in the threadpool:
        ThreadPool.QueueUserWorkItem(_ => GenerateRawData(worldData, worldLayers));
    }

    private void GenerateRawData(WorldData worldData, WorldData.WorldLayer[][] worldLayers) {
        var offset = Position * GameSettings.ChunkRadius;
        int atLayer = GameSettings.DefaultBedrockLevel;

        var layerDefs = worldLayers[(int)worldData.Type];

        foreach (var layer in layerDefs) {
            var blockData = Register.BlockDataMap[layer.blockName];
            for (int y = atLayer; y < atLayer + layer.height; y++) {
                for (int x = -GameSettings.ChunkRadius + offset.X;
                     x < GameSettings.ChunkRadius + offset.X; x++) {
                    for (int z = -GameSettings.ChunkRadius + offset.Z;
                         z < GameSettings.ChunkRadius + offset.Z; z++) {
                        Vector3I pos = new Vector3I(x, y, z);
                        _rawBlocks.TryAdd(pos, blockData);
                    }
                }
            }
            atLayer += layer.height;
        }

        var airBlock = new BlockData();

        _dataReady.TrySetResult(true);
    }

    public bool IsAirAt(Vector3I pos) {
        return !_rawBlocks.TryGetValue(pos, out var data) || data.BlockName == "air";
    }

    public void AddBlock(Vector3I pos, BlockData blockData) {
        if (IsAirAt(pos)) {
            _rawBlocks[pos] = blockData;
        }
        else if (_rawBlocks.TryGetValue(pos, out var oldData)) {
            _rawBlocks.TryUpdate(pos, blockData, oldData);
        }
    }

    public void RemoveBlock(Vector3I pos) {
        if (_rawBlocks.TryGetValue(pos, out var oldData)) {
            // Replace with air by removing the key entirely
            _rawBlocks.TryRemove(pos, out _);
        }
    }
}