using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using static System.Reflection.Metadata.BlobBuilder;
public partial class WorldData : Node {
    private readonly ConcurrentDictionary<Vector3I, Chunk> genChunks = new();
    private readonly ConcurrentDictionary<Vector3I, Chunk> loadedChunks = new();

    public int ChunkCount => genChunks.Count;

    public enum WorldType { Flat }

    public struct WorldLayer {
        public string blockName;
        public int height;
    }

    public WorldLayer[][] worldTypeLayers = new WorldLayer[4][];
    public WorldType Type = WorldType.Flat;

    Register register;

    public override void _Ready() {
        register = GetParent().GetNode<Register>("%Register");

        worldTypeLayers[(int)WorldType.Flat] = new[] {
            new WorldLayer() { blockName = "Bedrock", height = 1 },
            new WorldLayer() { blockName = "Stone", height = 4 },
            new WorldLayer() { blockName = "Dirt", height = 1 },
            new WorldLayer() { blockName = "GrassBlock", height = 1 },
        };
    }

    public async Task GenChunk(Vector3I chunkPosition) {
        var chunk = new Chunk(chunkPosition, Type, worldTypeLayers);
        genChunks[chunkPosition] = chunk;
        await chunk.ReadyTask;
    }

    public async Task LoadChunk(Vector3I chunkPosition) {
        if (!genChunks.TryGetValue(chunkPosition, out var chunk)) return;
        if (loadedChunks.ContainsKey(chunkPosition)) return;

        await chunk.ReadyTask; // Ensure generation is finished
        await chunk.CullChunk();

        while (chunk.Blocks.TryDequeue(out var block)) {
            if (block.GetParent() != this && !block.HasMeta("add_to_world")) {
                block.SetMeta("add_to_world", true);
                CallDeferred("add_child", block);
            }
        }

        loadedChunks.TryAdd(chunkPosition, chunk);
    }

    public async Task LoadChunks() {
        foreach (var kv in genChunks.ToArray()) {
            if (!loadedChunks.ContainsKey(kv.Key)) {
                await LoadChunk(kv.Key);
            }
        }
    }
}