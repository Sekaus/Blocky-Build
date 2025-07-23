using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using static System.Reflection.Metadata.BlobBuilder;
public partial class WorldData : Node {
    private readonly ConcurrentDictionary<Vector3I, Chunk> genChunks = new();

    public (bool, Chunk) GetChunk(Vector3I chunkPosition) { 
        bool gotSomething = genChunks.TryGetValue(chunkPosition, out var chunk);

        if (gotSomething)
            return (true, chunk);
        return (false, null);
    }

    public Vector3I[] GetChunkPositions () {
        return genChunks.Keys.ToArray();
    }

    public int ChunkCount => genChunks.Count;

    public enum WorldType { Flat }

    public struct WorldLayer {
        public string blockName;
        public int height;
    }

    public WorldLayer[][] worldTypeLayers = new WorldLayer[4][];
    public WorldType Type = WorldType.Flat;

    public override void _Ready() {
        worldTypeLayers[(int)WorldType.Flat] = new[] {
            new WorldLayer() { blockName = "Bedrock", height = 1 },
            new WorldLayer() { blockName = "Stone", height = 4 },
            new WorldLayer() { blockName = "Dirt", height = 1 },
            new WorldLayer() { blockName = "GrassBlock", height = 1 },
        };
    }

    public async Task GenChunk(Vector3I chunkPosition) {
        var chunk = new Chunk(chunkPosition, this, worldTypeLayers);
        genChunks[chunkPosition] = chunk;
        await chunk.DataReady;
    }

    /*public async Task LoadChunk(Vector3I chunkPosition) {
        if (!genChunks.TryGetValue(chunkPosition, out var chunk)) return;
        if (loadedChunks.ContainsKey(chunkPosition)) return;

        // Wait until the raw‑data is ready
        await chunk.DataReady;

        // Now, *on the main thread*, instantiate all the Blocks:
        foreach (var (pos, blockName) in chunk.Blocks) {
            var blockScene = Register.Blocks[blockName];
            if (blockScene == null)
                continue;

            var block = blockScene.Instantiate<Block>();
            block.Translate(pos); 
            AddChild(block);                       // attach to scene
        }

        loadedChunks.TryAdd(chunkPosition, chunk);
    }

    public async Task LoadChunks() {
        foreach (var kv in genChunks.ToArray()) {
            if (!loadedChunks.ContainsKey(kv.Key)) {
                await LoadChunk(kv.Key);
            }
        }
    }*/
}