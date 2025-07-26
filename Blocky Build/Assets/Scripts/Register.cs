using Godot;
using System;
using System.Collections.Generic;
using static System.Reflection.Metadata.BlobBuilder;

// This is the register over all content in this game
public partial class Register : Node {
    public static CSharpScript DefaultBlockBehaviorScript = GD.Load<CSharpScript>("res://Assets/Scripts/BlockBehaviorScripts/Default.cs");

    [Export]
    public PackedScene[] BlockScenes;
    public static System.Collections.Generic.Dictionary<string, RegisterVariant> Blocks = new();
    public static readonly System.Collections.Generic.Dictionary<string, RegisterVariant> BlockDataMap = new();

    [Export]
    public PackedScene[] ItemScenes;
    public static System.Collections.Generic.Dictionary<string, RegisterVariant> Items = new();

    [Export]
    public PackedScene[] GUIElementScenes;
    public static System.Collections.Generic.Dictionary<string, PackedScene> GUIElements = new();

    public partial class RegisterVariant : Node {
        private object value;

        public RegisterVariant(object value) {
            this.value = value;
        }

        public T Get<T>() => (T)value;

        public RegisterVariant this[string key] {
            get {
                if (value is Dictionary<string, object> dict && dict.TryGetValue(key, out var obj)) {
                    if (obj is Dictionary<string, object> nestedDict && nestedDict.TryGetValue(key, out var nestedObj)) {
                        if (nestedObj is RegisterVariant nestedRegisterVariant)
                            return nestedRegisterVariant;
                        throw new Exception($"Nested value at '{key}' is not a RegisterVariant.");
                    }
                }
                throw new InvalidOperationException("RegisterVariant does not contain a Dictionary.");
            }
            set {
                if (this.value is Dictionary<string, object> dict)
                    dict[key] = value;
                else
                    throw new InvalidOperationException("RegisterVariant does not contain a Dictionary.");
            }
        }

        public T Instantiate<T>(PackedScene.GenEditState editState = PackedScene.GenEditState.Disabled) where T : Node {
            Type typeOfT = typeof(T);
            Node instance;

            if (value is PackedScene packedScene) {
                instance = packedScene.Instantiate(editState);
            }
            else if (value is Dictionary<string, PackedScene> packedDict &&
                     packedDict.TryGetValue("Default", out var defaultScene)) {
                instance = defaultScene.Instantiate(editState);
            }
            else {
                throw new NotImplementedException("Instantiate can only be used on PackedScene or Dictionary<string, PackedScene>.");
            }

            // Special handling for blocks-as-items
            if (typeOfT == typeof(Item)) {
                if (instance is Block block) {
                    instance = LoadBlockAsItem(block);
                }
                else if (instance is not Item) {
                    throw new InvalidCastException($"Unable to cast instance of type '{instance.GetType().Name}' to type 'Item'.");
                }
            }

            return instance as T;
        }


        public void Add(string key, object val) {
            if (value is Dictionary<string, object> dict)
                dict.Add(key, val);
            else
                throw new InvalidOperationException("Cannot use .Add on non-dictionary RegisterVariant.");
        }

        public ICollection<string> Keys {
            get {
                if (value is Dictionary<string, object> dict)
                    return dict.Keys;
                throw new NotSupportedException();
            }
        }

        public ICollection<object> Values {
            get {
                if (value is Dictionary<string, object> dict)
                    return dict.Values;
                throw new NotSupportedException();
            }
        }

        public object ToObject() => value;
        public T To<T>() => (T)value;
    }

    // Load in block instance as item
    public static Item LoadBlockAsItem(Block blockInstance) {
        // Instantiate the Item from the Items dictionary
        Item newItem = Items["BlockItem"].Instantiate<Item>();

        // Remove the block from its current parent if any
        if (blockInstance.GetParent() != null) {
            blockInstance.GetParent().RemoveChild(blockInstance);
        }

        // Move the Mesh child to the new item
        Node meshChild = blockInstance.GetNode<Node>("Mesh");
        if (meshChild != null) {
            // Unset the owner before removing and adding
            meshChild.Owner = null;
            blockInstance.RemoveChild(meshChild);
            newItem.AddChild(meshChild);
            meshChild.Owner = newItem; // Ensure proper ownership after adding
        }

        // Copy data from the block instance to the new item instance
        newItem.ItemName = blockInstance.BlockName;
        newItem.Tags = blockInstance.Tags;

        // Free the block instance
        blockInstance.QueueFree();

        return newItem;
    }

    public static MeshLibrary CreateMeshLibraryForBlocks() {
        var lib = new MeshLibrary();
        int nextId = 0;

        foreach (var kv in BlockDataMap) {
            var regVar = kv.Value;

            if (regVar.ToObject() is BlockData data) {
                TryAddBlockMesh(lib, data, ref nextId);
            }
            else if (regVar.ToObject() is Dictionary<string, object> dict) {
                foreach (var obj in dict.Values) {
                    if (obj is BlockData nestedData)
                        TryAddBlockMesh(lib, nestedData, ref nextId);
                }
            }
        }

        return lib;
    }

    private static void TryAddBlockMesh(MeshLibrary lib, BlockData data, ref int nextId) {
        var csg = data.CsgMesh3D;
        if (csg == null || csg.Mesh == null)
            return;

        var mesh = csg.Mesh.Duplicate() as ArrayMesh;
        var mat = csg.Material;

        for (int s = 0; s < mesh.GetSurfaceCount(); s++)
            mesh.SurfaceSetMaterial(s, mat);

        lib.CreateItem(nextId);
        lib.SetItemName(nextId, data.BlockName);
        lib.SetItemMesh(nextId, mesh);

        var concave = new ConcavePolygonShape3D {
            Data = ExtractTriangles(mesh)
        };
        var shapes = new Godot.Collections.Array();
        shapes.Add(concave);
        lib.SetItemShapes(nextId, shapes);

        nextId++;
    }

    /// <summary>
    /// Given an ArrayMesh, extract all triangles into a flat Vector3[].
    /// </summary>
    private static Vector3[] ExtractTriangles(ArrayMesh mesh) {
        var tris = new List<Vector3>();

        // For each surface:
        for (int s = 0; s < mesh.GetSurfaceCount(); s++) {
            var arr = mesh.SurfaceGetArrays(s);
            var verts = (Vector3[])arr[(int)ArrayMesh.ArrayType.Vertex];
            var indices = (int[])arr[(int)ArrayMesh.ArrayType.Index];

            // Every 3 indices is a triangle:
            for (int i = 0; i < indices.Length; i += 3) {
                tris.Add(verts[indices[i + 0]]);
                tris.Add(verts[indices[i + 1]]);
                tris.Add(verts[indices[i + 2]]);
            }
        }

        return tris.ToArray();
    }

    public override void _EnterTree() {
        // Load in items
        foreach (PackedScene itemScene in ItemScenes) {
            Item itemSceneInstance = itemScene.Instantiate<Item>();
            Items.Add(itemSceneInstance.ItemName, new RegisterVariant(itemScene));
            itemSceneInstance.QueueFree();
        }

        // Load in blocks
        foreach (PackedScene blockScene in BlockScenes) {
            Block blockSceneInstance = blockScene.Instantiate<Block>();
            RegisterVariant data = new RegisterVariant(new BlockData(blockSceneInstance));

            if (blockSceneInstance.VariationOfBlock == "") {
                Blocks.Add(blockSceneInstance.BlockName, new RegisterVariant(blockScene));
                BlockDataMap.Add(blockSceneInstance.BlockName, data);
            }
            else {
                if (Blocks.ContainsKey(blockSceneInstance.VariationOfBlock)) {
                    if (Blocks[blockSceneInstance.VariationOfBlock].ToObject() is PackedScene oldBlockScene) {
                        // Convert the single PackedScene entry to a Godot.Collections.Dictionary entry if not already done
                        Blocks[blockSceneInstance.VariationOfBlock] = new RegisterVariant(new System.Collections.Generic.Dictionary<string, PackedScene> { ["Default"] = oldBlockScene });
                        BlockDataMap[blockSceneInstance.VariationOfBlock] = new RegisterVariant(new System.Collections.Generic.Dictionary<string, BlockData> { ["Default"] = BlockDataMap[blockSceneInstance.VariationOfBlock].To<BlockData>() });
                    }
                }

                string keyName = blockSceneInstance.BlockName;
                int index = keyName.IndexOf(blockSceneInstance.VariationOfBlock);
                keyName = (index < 0) ? keyName : keyName.Remove(index, blockSceneInstance.VariationOfBlock.Length);

                if (Blocks[blockSceneInstance.VariationOfBlock].ToObject() is Dictionary<string, PackedScene> dict) {
                    dict[keyName] = blockScene;
                    BlockDataMap[keyName] = data;
                }
                else
                    throw new InvalidOperationException($"Expected dictionary but found {Blocks[blockSceneInstance.VariationOfBlock].ToObject().GetType().Name}");
            }

            blockSceneInstance.QueueFree();
        }

        // Load in GUI elements
        foreach (PackedScene GUIElementScene in GUIElementScenes) {
            var guiElementSceneInstance = GUIElementScene.Instantiate();
            GUIElements.Add(guiElementSceneInstance.Name, GUIElementScene);
            guiElementSceneInstance.QueueFree();
        }
    }
}