using Godot;
using System;
using static System.Reflection.Metadata.BlobBuilder;

// This is the register over all content in this game
public partial class Register : Node {
    public static CSharpScript DefaultBlockBehaviorScript = GD.Load<CSharpScript>("res://Assets/Scripts/BlockBehaviorScripts/Default.cs");

    [Export]
    public PackedScene[] BlockScenes;
    public static Godot.Collections.Dictionary<string, RegisterVariant> Blocks  = new Godot.Collections.Dictionary<string, RegisterVariant>();
    public static readonly System.Collections.Generic.Dictionary<string, BlockData> BlockDataMap = new();

    [Export]
    public PackedScene[] ItemScenes;
    public static Godot.Collections.Dictionary<string, RegisterVariant> Items = new Godot.Collections.Dictionary<string, RegisterVariant>();

    [Export]
    public PackedScene[] GUIElementScenes;
    public static Godot.Collections.Dictionary<string, PackedScene> GUIElements = new Godot.Collections.Dictionary<string, PackedScene>();

    public partial class RegisterVariant : Node {
        private Variant variant;

        public RegisterVariant(Variant variant) {
            this.variant = variant;
        }

        public PackedScene this[string key] {
            get {
                if (variant.VariantType == Variant.Type.Dictionary) {
                    var dict = (Godot.Collections.Dictionary)variant;
                    if (dict.ContainsKey(key)) {
                        if (dict[key].Obj is Godot.Collections.Dictionary nestedDict && nestedDict.ContainsKey(key)) {
                            if (nestedDict[key].Obj is PackedScene nestedScene)
                                return nestedScene;
                            else
                                throw new Exception($"Key '{key}' exists but is not a PackedScene.");
                        }
                        else if (dict[key].Obj is PackedScene scene)
                            return scene;
                        else
                            throw new Exception($"Key '{key}' exists but is not a PackedScene.");
                    }
                    else
                        throw new Exception($"Key '{key}' not found in the dictionary.");
                }
                else
                    throw new InvalidOperationException($"Variant is not a Godot.Collections.Dictionary.");
            }
            set {
                if (variant.VariantType == Variant.Type.Dictionary) {
                    var dict = (Godot.Collections.Dictionary)variant;
                    dict[key] = value;
                }
                else
                    throw new InvalidOperationException($"Cannot set variant for '{key}' (not a Godot.Collections.Dictionary).");
            }
        }

        public T Instantiate<T>(PackedScene.GenEditState editState = PackedScene.GenEditState.Disabled) where T : Node {
            Type typeOfT = typeof(T);
            Node instance;

            if (variant.Obj is PackedScene packedScene) {
                instance = packedScene.Instantiate(editState);
            }
            else if (variant.Obj is Godot.Collections.Dictionary dict && dict.ContainsKey("Default") && dict["Default"].Obj is PackedScene defaultScene) {
                instance = defaultScene.Instantiate(editState);
            }
            else {
                throw new NotImplementedException("Instantiate can only be used on types of class PackedScene.");
            }

            if (typeOfT == typeof(Item)) {
                if (instance is Block block) {
                    instance = LoadBlockAsItem(block);
                }
                else if (!(instance is Item)) {
                    throw new InvalidCastException($"Unable to cast instance of type '{instance.GetType().Name}' to type 'Item'.");
                }
            }

            return instance as T;
        }

        public void Add(string key, Variant value) {
            if (variant.VariantType == Variant.Type.Dictionary)
                ((Godot.Collections.Dictionary)variant).Add(key, value);
            else
                throw new InvalidOperationException("Cannot use .Add on non-dictionary variants.");
        }

        public System.Collections.Generic.ICollection<string> Keys {
            get {
                if (variant.VariantType == Variant.Type.Dictionary)
                    return ((Godot.Collections.Dictionary<string, PackedScene>)variant).Keys;
                else
                    throw new NotImplementedException("Cannot use .Keys on non-dictionary variants.");
            }
        }

        public System.Collections.Generic.ICollection<PackedScene> Values {
            get {
                if (variant.VariantType == Variant.Type.Dictionary)
                    return ((Godot.Collections.Dictionary<string, PackedScene>)variant).Values;
                else
                    throw new NotImplementedException("Cannot use .Values on non-dictionary variants.");
            }
        }

        public T ToVariant<T>() where T : Node {
            return (T)variant;
        }

        public Variant ToVariant() {
            return variant;
        }
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

            if (blockSceneInstance.VariationOfBlock == "") {
                Blocks.Add(blockSceneInstance.BlockName, new RegisterVariant(blockScene));
            }
            else {
                if (Blocks.ContainsKey(blockSceneInstance.VariationOfBlock)) {
                    if (Blocks[blockSceneInstance.VariationOfBlock].ToVariant().Obj is PackedScene oldBlockScene) {
                        // Convert the single PackedScene entry to a Godot.Collections.Dictionary entry if not already done
                        Blocks[blockSceneInstance.VariationOfBlock] = new RegisterVariant(new Godot.Collections.Dictionary { ["Default"] = oldBlockScene });
                    }
                }

                string keyName = blockSceneInstance.BlockName;
                int index = keyName.IndexOf(blockSceneInstance.VariationOfBlock);
                keyName = (index < 0) ? keyName : keyName.Remove(index, blockSceneInstance.VariationOfBlock.Length);

                if (Blocks[blockSceneInstance.VariationOfBlock].ToVariant().Obj is Godot.Collections.Dictionary dict)
                    dict[keyName] = blockScene;
                else
                    throw new InvalidOperationException($"Expected dictionary but found {Blocks[blockSceneInstance.VariationOfBlock].ToVariant().Obj.GetType().Name}");
            }

            var data = new BlockData(blockSceneInstance);
            BlockDataMap[blockSceneInstance.BlockName] = data;
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