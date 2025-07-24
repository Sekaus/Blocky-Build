using Godot;
using Godot.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

public partial class ChunkRenderer : Node3D {
    private System.Collections.Generic.Dictionary<Vector3I, MeshInstance3D> _chunkInstances = new();
    private System.Collections.Generic.Dictionary<Vector3I, StaticBody3D> _collisionBodies = new();

    // Caches to avoid re-instantiating PackedScenes every build
    private System.Collections.Generic.Dictionary<string, ArrayMesh> _meshCache = new();
    private System.Collections.Generic.Dictionary<string, Material> _materialCache = new();

    public override void _Ready() {
        // Pre-cache each block type's mesh and material
        foreach (var kv in Register.Blocks) {
            string key = kv.Key;
            var packed = kv.Value;
            var inst = packed.Instantiate<Node3D>();
            var csg = inst.GetNode<CsgMesh3D>("Mesh");
            if (csg != null) {
                _meshCache[key]     = csg.Mesh as ArrayMesh;
                _materialCache[key] = csg.Material;
            }
            inst.QueueFree();
        }
    }

    public void BuildChunkMesh(Vector3I chunkPos, System.Collections.Generic.Dictionary<Vector3I, BlockData> blocks) {
        // Remove old mesh instance if exists
        if (_chunkInstances.TryGetValue(chunkPos, out var oldMesh)) {
            oldMesh.QueueFree();
            _chunkInstances.Remove(chunkPos);
        }
        // Remove old collision body if exists
        if (_collisionBodies.TryGetValue(chunkPos, out var oldBody)) {
            oldBody.QueueFree();
            _collisionBodies.Remove(chunkPos);
        }

        // Compute world origin for this chunk
        var origin = chunkPos * GameSettings.ChunkRadius;
        var combined = BuildCombinedMesh(blocks, origin);

        // Create and place mesh instance
        var mi = new MeshInstance3D {
            Mesh      = combined,
            Transform = new Transform3D(Basis.Identity, origin)
        };
        AddChild(mi);
        _chunkInstances[chunkPos] = mi;

        // Create collision body for this chunk
        var body = new StaticBody3D();
        var shapeNode = new CollisionShape3D();
        // Extract triangles once
        var tris = ExtractTriangles(combined);
        shapeNode.Shape = new ConcavePolygonShape3D { Data = tris };

        // Position collision body at the same origin as the mesh
        body.Transform = new Transform3D(Basis.Identity, origin);
        body.AddChild(shapeNode);
        body.AddToGroup("chunk");
        AddChild(body);
        _collisionBodies[chunkPos] = body;
    }

    private ArrayMesh BuildCombinedMesh(System.Collections.Generic.Dictionary<Vector3I, BlockData> blocks, Vector3 origin) {
        var mesh = new ArrayMesh();
        // Group by block type
        var groups = new System.Collections.Generic.Dictionary<string, List<Vector3I>>();
        foreach (var kv in blocks) {
            if (!groups.TryGetValue(kv.Value.BlockName, out var list))
                list = groups[kv.Value.BlockName] = new List<Vector3I>();
            list.Add(kv.Key);
        }
        // Build surfaces
        foreach (var kv in groups) {
            if (!_meshCache.TryGetValue(kv.Key, out var srcMesh) ||
                !_materialCache.TryGetValue(kv.Key, out var mat))
                continue;

            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var indices = new List<int>();
            int vertBase = 0;

            foreach (var coord in kv.Value) {
                var basePos = coord * GameSettings.BlockRenderScale;
                for (int s = 0; s < srcMesh.GetSurfaceCount(); s++) {
                    var arr = srcMesh.SurfaceGetArrays(s);
                    var sv = (Vector3[])arr[(int)ArrayMesh.ArrayType.Vertex];
                    var sn = (Vector3[])arr[(int)ArrayMesh.ArrayType.Normal];
                    var st = (Vector2[])arr[(int)ArrayMesh.ArrayType.TexUV];
                    var si = (int[])arr[(int)ArrayMesh.ArrayType.Index];

                    for (int i = 0; i < sv.Length; i++) {
                        verts.Add(sv[i] + basePos);
                        norms.Add(sn[i]);
                        uvs.Add(st[i]);
                    }
                    for (int i = 0; i < si.Length; i++)
                        indices.Add(si[i] + vertBase);

                    vertBase += sv.Length;
                }
            }

            var arrays = new Godot.Collections.Array();
            arrays.Resize((int)Mesh.ArrayType.Max);
            arrays[(int)Mesh.ArrayType.Vertex]  = verts.ToArray();
            arrays[(int)Mesh.ArrayType.Normal]  = norms.ToArray();
            arrays[(int)Mesh.ArrayType.TexUV]   = uvs.ToArray();
            arrays[(int)Mesh.ArrayType.Index]   = indices.ToArray();

            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
            mesh.SurfaceSetMaterial(mesh.GetSurfaceCount()-1, mat);
        }
        return mesh;
    }

    private Vector3[] ExtractTriangles(ArrayMesh combinedMesh) {
        var tris = new List<Vector3>();
        int offset = 0;

        for (int s = 0; s < combinedMesh.GetSurfaceCount(); s++) {
            var arr = combinedMesh.SurfaceGetArrays(s);
            var v = (Vector3[])arr[(int)ArrayMesh.ArrayType.Vertex];
            var ix = (int[])arr[(int)ArrayMesh.ArrayType.Index];

            for (int i = 0; i < ix.Length; i += 3) {
                tris.Add(v[ix[i + 0]]);
                tris.Add(v[ix[i + 1]]);
                tris.Add(v[ix[i + 2]]);
            }

            offset += v.Length;
        }

        return tris.ToArray();
    }

    // A thread-safe queue of rebuild requests
    private readonly ConcurrentQueue<(Vector3I chunkPos, System.Collections.Generic.Dictionary<Vector3I, BlockData> blocks)> _rebuildQueue = new ConcurrentQueue<(Vector3I, System.Collections.Generic.Dictionary<Vector3I, BlockData>)>();

    public void RequestChunkRebuild(Vector3I chunkPos, System.Collections.Generic.Dictionary<Vector3I, BlockData> blocks) {
        // Enqueue the data (light) and kick off a background mesh gen
        _rebuildQueue.Enqueue((chunkPos, blocks));

        ThreadPool.QueueUserWorkItem(_ => {
            if (_rebuildQueue.TryDequeue(out var job)) {
                // 1) Build the mesh off the main thread
                var mesh = BuildCombinedMesh(job.blocks, job.chunkPos * GameSettings.BlockRenderScale);
                var tris = ExtractTriangles(mesh);

                // 2) Enqueue the final scene update back on the main thread
                MainThreadDispatcher.Enqueue(() => {
                    ApplyChunkMeshAndCollision(job.chunkPos, mesh, tris);
                });
            }
        });
    }

    private void ApplyChunkMeshAndCollision(Vector3I chunkPos, ArrayMesh mesh, Vector3[] tris) {
        // Remove old
        if (_chunkInstances.TryGetValue(chunkPos, out var old)) { old.QueueFree(); _chunkInstances.Remove(chunkPos); }
        if (_collisionBodies.TryGetValue(chunkPos, out var oldBody)) { oldBody.QueueFree(); _collisionBodies.Remove(chunkPos); }

        // Add new mesh instance
        var origin = chunkPos * GameSettings.BlockRenderScale;
        var mi = new MeshInstance3D { Mesh = mesh, Transform = new Transform3D(Basis.Identity, origin) };
        AddChild(mi);
        _chunkInstances[chunkPos] = mi;

        // Add collision shape
        var body = new StaticBody3D();
        var shapeNode = new CollisionShape3D { Shape = new ConcavePolygonShape3D { Data = tris } };
        body.Transform = new Transform3D(Basis.Identity, origin);
        body.AddChild(shapeNode);
        AddChild(body);
        _collisionBodies[chunkPos] = body;
    }
}