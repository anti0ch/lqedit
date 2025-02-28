using System;
using System.Numerics;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using ImGuiNET;
using System.Text;
using Dalamud.Interface.Windowing;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Runtime.InteropServices;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.System.String;

namespace SamplePlugin.Windows
{
    public class LeveQuestData
    {
        public string Title = "My Custom Leve";
        public string Description = "This is a custom levequest description.\nSupports line breaks!";
        public string Objectives = "Report to the Peaks.";
        public string LocationAndInfo = "Ala Gannha Leves\nLocation: The Peaks/Time Limit: 20m";
        public string Client = "Adventurers' Guild";
        public int Level = 1;
        // public int GilReward = 1000;
        // public int ExpReward = 10000;
    }

    internal class TexturePreset
    {
        public string Description { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    public class MainWindow : Window, IDisposable
    {
        private Plugin Plugin;
        private readonly ITargetManager _targetManager;
        private readonly IAddonLifecycle _addonLifecycle;
        private readonly IGameGui _gameGui;
        private readonly IPluginLog _pluginLog;

        private LeveQuestData _customLeve = new();
        private bool _isModifying = false;
        private string _customTexturePath = string.Empty;
        private readonly TexturePreset[] _texturePresets = new[]
        {
            // Combat Leves
            new TexturePreset { Description = "Diligence (kill targets for loot)", Path = "ui/icon/080000/080025_hr1.tex", Category = "Combat" },
            new TexturePreset { Description = "Valor (kill targets)", Path = "ui/icon/080000/080021_hr1.tex", Category = "Combat" },
            new TexturePreset { Description = "Tenacity (kill fleeing target)", Path = "ui/icon/080000/080022_hr1.tex", Category = "Combat" },
            new TexturePreset { Description = "Veracity (find and kill hidden target)", Path = "ui/icon/080000/080028_hr1.tex", Category = "Combat" },
            new TexturePreset { Description = "Wisdom (necrologos)", Path = "ui/icon/080000/080023_hr1.tex", Category = "Combat" },
            new TexturePreset { Description = "Justice (lure and kill targets)", Path = "ui/icon/080000/080024_hr1.tex", Category = "Combat" },
            new TexturePreset { Description = "Temperance (patrol)", Path = "ui/icon/080000/080026_hr1.tex", Category = "Combat" },
            new TexturePreset { Description = "Confidence (retrieve items and kill)", Path = "ui/icon/080000/080055_hr1.tex", Category = "Combat" },
            new TexturePreset { Description = "Resolve (pacify target)", Path = "ui/icon/080000/080038_hr1.tex", Category = "Combat" },
            new TexturePreset { Description = "Sympathy (escort)", Path = "ui/icon/080000/080056_hr1.tex", Category = "Combat" },
            new TexturePreset { Description = "Equity (find and kill hidden target)", Path = "ui/icon/080000/080049_hr1.tex", Category = "Combat" },
            new TexturePreset { Description = "Promptitude (mass subjugation)", Path = "ui/icon/080000/080036_hr1.tex", Category = "Combat" },
            new TexturePreset { Description = "Prudence (kill target and reinforcements)", Path = "ui/icon/080000/080037_hr1.tex", Category = "Combat" },
            new TexturePreset { Description = "Unity (defend from attackers)", Path = "ui/icon/080000/080051_hr1.tex", Category = "Combat" },
            
            // Gathering Leves
            new TexturePreset { Description = "Candor (gather with limited nodes)", Path = "ui/icon/080000/080030_hr1.tex", Category = "Gathering" },
            new TexturePreset { Description = "Munificence (gather)", Path = "ui/icon/080000/080044_hr1.tex", Category = "Gathering" },
            new TexturePreset { Description = "Piety (gather with score)", Path = "ui/icon/080000/080029_hr1.tex", Category = "Gathering" },
            new TexturePreset { Description = "Benevolence (gather few with score)", Path = "ui/icon/080000/080040_hr1.tex", Category = "Gathering" },
            new TexturePreset { Description = "Concord (deliver fish)", Path = "ui/icon/080000/080057_hr1.tex", Category = "Fishing" },
            new TexturePreset { Description = "Sincerity (deliver fish + bonus)", Path = "ui/icon/080000/080045_hr1.tex", Category = "Fishing" },
            
            // Crafting Leves
            new TexturePreset { Description = "Charity (deliver crafts + bonus)", Path = "ui/icon/080000/080041_hr1.tex", Category = "Crafting" },
            new TexturePreset { Description = "Constancy (deliver crafts)", Path = "ui/icon/080000/080033_hr1.tex", Category = "Crafting" },
            new TexturePreset { Description = "Ingenuity (deliver crafts from afar)", Path = "ui/icon/080000/080034_hr1.tex", Category = "Crafting" },
        };

        private readonly TexturePreset[] _dutyCategoryPresets = new[]
        {
            new TexturePreset { Description = "Combat", Path = "ui/icon/062000/062501_hr1.tex", Category = "Duty Category" },

            new TexturePreset { Description = "Leatherworker", Path = "ui/icon/062000/062506_hr1.tex", Category = "Duty Category" },
            // new TexturePreset { Description = "Weaver", Path = "ui/icon/062000/062507_hr1.tex", Category = "Duty Category" },
            // new TexturePreset { Description = "Weapon Smith", Path = "ui/icon/062000/062508_hr1.tex", Category = "Duty Category" },
            // new TexturePreset { Description = "Armorer", Path = "ui/icon/062000/062509_hr1.tex", Category = "Duty Category" },
            // new TexturePreset { Description = "Goldsmith", Path = "ui/icon/062000/062510_hr1.tex", Category = "Duty Category" },
            // new TexturePreset { Description = "Alchemist", Path = "ui/icon/062000/062511_hr1.tex", Category = "Duty Category" },
            
            // More can be added later
        };

        private string _dutyCategoryTexturePath = string.Empty;

        private class JournalTextNodes
        {
            public IntPtr TitleNode;        // Main Text Node 19
            public IntPtr LevelNode;        // Main Text Node 48
            public IntPtr ClientNode;       // Canvas Text Node 1
            public IntPtr DescriptionNode;  // Canvas Text Node 117
            public IntPtr LocationNode;     // Canvas Text Node 81
            public IntPtr ObjectivesNode;   // Nested in MCN sibling to "Objectives" text
            public IntPtr DutyCategoryNode;    // Image Node at index 53
            // public IntPtr GilNode;         // Nested in MCN[1] > TextNineGrid
            // public IntPtr ExpNode;         // Nested in MCN[2] > TextNineGrid
        }

        public MainWindow(Plugin plugin, ITargetManager targetManager, IAddonLifecycle addonLifecycle, IGameGui gameGui, IPluginLog pluginLog)
            : base("Levequest Editor", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(375, 330),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };

            Plugin = plugin;
            _targetManager = targetManager;
            _addonLifecycle = addonLifecycle;
            _gameGui = gameGui;
            _pluginLog = pluginLog;

            // Subscribe to both PreSetup and PreDraw
            _addonLifecycle.RegisterListener(AddonEvent.PreSetup, "JournalDetail", OnJournalOpen);
            _addonLifecycle.RegisterListener(AddonEvent.PreDraw, "JournalDetail", OnJournalOpen);
        }

        public void Dispose()
        {
            _addonLifecycle.UnregisterListener(AddonEvent.PreSetup, "JournalDetail", OnJournalOpen);
            _addonLifecycle.UnregisterListener(AddonEvent.PreDraw, "JournalDetail", OnJournalOpen);
        }

        private void OnJournalOpen(AddonEvent type, AddonArgs args)
        {
            try
            {
                // Only process in PreSetup to avoid duplicate updates
                if (type != AddonEvent.PreSetup) return;

                var journalAddon = _gameGui.GetAddonByName("JournalDetail");
                if (journalAddon == IntPtr.Zero) return;

                if (_isModifying)
                {
                    ModifyJournalText(journalAddon);
                }
            }
            catch (Exception ex)
            {
                _pluginLog.Error($"Error modifying journal: {ex}");
            }
        }

        private unsafe void TraverseComponentNode(AtkComponentNode* componentNode, JournalTextNodes nodes, string path, int depth = 0)
        {
            if (componentNode == null || componentNode->Component == null) return;

            var indent = new string(' ', depth * 2);
            _pluginLog.Debug($"{indent}[{path}] Component with {componentNode->Component->UldManager.NodeListCount} nodes");

            // Check this component's nodes
            for (var i = 0; i < componentNode->Component->UldManager.NodeListCount; i++)
            {
                var node = componentNode->Component->UldManager.NodeList[i];
                if (node == null) continue;

                var nodePath = $"{path}[{i}]";
                
                // Log all node types and addresses
                _pluginLog.Debug($"{indent}Node {nodePath}: Type={node->Type} Addr={((IntPtr)node):X}");

                if (node->Type == NodeType.Text)
                {
                    var textNode = (AtkTextNode*)node;
                    if (!textNode->NodeText.IsEmpty)
                    {
                        var text = textNode->NodeText.ToString();
                        _pluginLog.Debug($"{indent}Text: \"{text}\"");
                        _pluginLog.Debug($"{indent}Parent: {((IntPtr)node->ParentNode):X}");

                        // Check for specific nodes based on path and content
                        if (path == "Canvas[14]") // Main JournalCanvas component
                        {
                            switch (i)
                            {
                                case 1:
                                    nodes.ClientNode = (IntPtr)node;
                                    _pluginLog.Debug($"{indent}-> Found Client Node");
                                    break;
                                case 81:
                                    nodes.LocationNode = (IntPtr)node;
                                    _pluginLog.Debug($"{indent}-> Found Location Node");
                                    break;
                                case 117:
                                    nodes.DescriptionNode = (IntPtr)node;
                                    _pluginLog.Debug($"{indent}-> Found Description Node");
                                    break;
                            }
                        }
                        else if (path == "Canvas[14][90]" && i == 1) // Objectives node
                        {
                            nodes.ObjectivesNode = (IntPtr)node;
                            _pluginLog.Debug($"{indent}-> Found Objectives Node");
                        }
                        /* Comment out gil/exp node detection
                        else if (text.Contains("gil") && !text.Contains("guild"))
                        {
                            nodes.GilNode = (IntPtr)node;
                            _pluginLog.Debug($"{indent}-> Found Gil Node");
                        }
                        else if (text.Contains("experience points"))
                        {
                            nodes.ExpNode = (IntPtr)node;
                            _pluginLog.Debug($"{indent}-> Found Exp Node");
                        }
                        */
                    }
                }
                // For component nodes, traverse all of them for now to find gil/exp
                else if ((ushort)node->Type >= 1000)
                {
                    var componentInfo = $"Type={(ushort)node->Type}";
                    if (node->Type == (NodeType)1010) componentInfo += " (JournalCanvas)";
                    else if (node->Type == (NodeType)1019) componentInfo += " (TextNineGrid)";
                    else if (node->Type == (NodeType)1017) componentInfo += " (MultipurposeComponent)";
                    
                    _pluginLog.Debug($"{indent}Component: {componentInfo}");
                    TraverseComponentNode((AtkComponentNode*)node, nodes, nodePath, depth + 1);
                }
            }
        }

        private unsafe void ModifyJournalText(IntPtr addon)
        {
            try
            {
                if (addon == IntPtr.Zero)
                {
                    _pluginLog.Error("Invalid addon pointer");
                    return;
                }

                _pluginLog.Debug("=== Starting Journal Text Analysis ===");
                var nodes = new JournalTextNodes();
                var atkUnitBase = (AtkUnitBase*)addon;

                // First find all the nodes we want to modify
                var componentCount = atkUnitBase->UldManager.NodeListCount;
                _pluginLog.Debug($"Found {componentCount} nodes to analyze");

                // Log what we're planning to modify
                _pluginLog.Debug("Planning to set:");
                _pluginLog.Debug($"  Title: {_customLeve.Title}");
                _pluginLog.Debug($"  Level: Lv. {_customLeve.Level}");
                _pluginLog.Debug($"  Client: Client: {_customLeve.Client}");
                _pluginLog.Debug($"  Description: {_customLeve.Description}");
                _pluginLog.Debug($"  Location: {_customLeve.LocationAndInfo}");

                // First pass - just find and log the nodes
                for (var i = 0; i < componentCount; i++)
                {
                    var node = atkUnitBase->UldManager.NodeList[i];
                    if (node == null) continue;

                    // Special logging for nodes at index 50 and 54
                    if (i == 50 || i == 54)
                    {
                        var nodeInfo = $"Node {i}: Type={node->Type} Addr={((IntPtr)node):X}";
                        if ((ushort)node->Type >= 1000)
                        {
                            var componentNode = (AtkComponentNode*)node;
                            nodeInfo += $" ComponentType={(ushort)node->Type}";
                            if (componentNode->Component != null)
                            {
                                nodeInfo += $" ChildCount={componentNode->Component->UldManager.NodeListCount}";
                                
                                // Log information about each child node
                                for (var j = 0; j < componentNode->Component->UldManager.NodeListCount; j++)
                                {
                                    var childNode = componentNode->Component->UldManager.NodeList[j];
                                    if (childNode == null) continue;

                                    if (childNode->Type == NodeType.Image)
                                    {
                                        var imageNode = (AtkImageNode*)childNode;
                                        var texInfo = imageNode->PartsList->Parts[0].UldAsset;
                                        var texPath = texInfo != null ? Marshal.PtrToStringAnsi((IntPtr)texInfo->AtkTexture.Resource->TexFileResourceHandle->ResourceHandle.FileName.BufferPtr) : "null";
                                        _pluginLog.Debug($"  Child {j}: Image Node, Texture Path: {texPath}");
                                    }
                                }
                            }
                        }
                        _pluginLog.Debug($"=== Detailed info for index {i} ===");
                        _pluginLog.Debug(nodeInfo);
                        _pluginLog.Debug($"ParentNode: {((IntPtr)node->ParentNode):X}");
                    }

                    // Log nodes with type >= 1000
                    if ((ushort)node->Type >= 1000)
                    {
                        var componentInfo = $"Type={(ushort)node->Type}";
                        if (node->Type == (NodeType)1010) componentInfo += " (JournalCanvas)";
                        else if (node->Type == (NodeType)1019) componentInfo += " (TextNineGrid)";
                        else if (node->Type == (NodeType)1017) componentInfo += " (MultipurposeComponent)";
                        
                        _pluginLog.Debug($"Found component at index {i}: {componentInfo}");
                    }

                    // Check JournalCanvas component
                    if ((ushort)node->Type == 1010)
                    {
                        _pluginLog.Debug($"Found JournalCanvas component at index {i}");
                        var componentNode = (AtkComponentNode*)node;
                        TraverseComponentNode(componentNode, nodes, $"Canvas[{i}]");
                    }
                    // Check main text nodes
                    else if (node->Type == NodeType.Text)
                    {
                        var textNode = (AtkTextNode*)node;
                        if (textNode->NodeText.IsEmpty) continue;

                        _pluginLog.Debug($"Found main text node {i}: {textNode->NodeText}");
                        
                        // Store nodes based on their index
                        switch (i)
                        {
                            case 19:
                                nodes.TitleNode = (IntPtr)node;
                                _pluginLog.Debug("  -> Identified as Title node");
                                break;
                            case 48:
                                nodes.LevelNode = (IntPtr)node;
                                _pluginLog.Debug("  -> Identified as Level node");
                                break;
                        }
                    }

                    // Store the DutyCategoryNode
                    if (i == 53 && node->Type == NodeType.Image)
                    {
                        nodes.DutyCategoryNode = (IntPtr)node;
                        _pluginLog.Debug("  -> Identified as DutyCategoryNode");
                    }
                }

                // Add detailed logging for finding the DutyCategoryImageNode
                _pluginLog.Debug("=== Searching for DutyCategoryImageNode ===");
                for (var i = 0; i < componentCount; i++)
                {
                    var node = atkUnitBase->UldManager.NodeList[i];
                    if (node == null) continue;

                    // Log all image nodes and their relationships
                    if (node->Type == NodeType.Image)
                    {
                        var imageNode = (AtkImageNode*)node;
                        var texPath = "null";
                        
                        try
                        {
                            if (imageNode->PartsList != null && 
                                imageNode->PartsList->Parts != null && 
                                imageNode->PartsList->Parts[0].UldAsset != null &&
                                imageNode->PartsList->Parts[0].UldAsset->AtkTexture.Resource != null &&
                                imageNode->PartsList->Parts[0].UldAsset->AtkTexture.Resource->TexFileResourceHandle != null &&
                                imageNode->PartsList->Parts[0].UldAsset->AtkTexture.Resource->TexFileResourceHandle->ResourceHandle.FileName.BufferPtr != null)
                            {
                                texPath = Marshal.PtrToStringAnsi((IntPtr)imageNode->PartsList->Parts[0].UldAsset->AtkTexture.Resource->TexFileResourceHandle->ResourceHandle.FileName.BufferPtr);
                            }
                        }
                        catch (Exception ex)
                        {
                            _pluginLog.Debug($"Error getting texture path: {ex.Message}");
                        }

                        _pluginLog.Debug($"Found Image Node at index {i}:");
                        _pluginLog.Debug($"  Address: {((IntPtr)node):X}");
                        _pluginLog.Debug($"  Parent: {((IntPtr)node->ParentNode):X}");
                        _pluginLog.Debug($"  Prev Sibling: {((IntPtr)node->PrevSiblingNode):X}");
                        _pluginLog.Debug($"  Next Sibling: {((IntPtr)node->NextSiblingNode):X}");
                        _pluginLog.Debug($"  Texture Path: {texPath}");
                        _pluginLog.Debug($"  Size: {imageNode->AtkResNode.Width}x{imageNode->AtkResNode.Height}");
                        _pluginLog.Debug($"  Position: ({imageNode->AtkResNode.X}, {imageNode->AtkResNode.Y})");
                    }
                    // Also log component nodes to help identify relationships
                    else if ((ushort)node->Type >= 1000)
                    {
                        _pluginLog.Debug($"Found Component Node at index {i}:");
                        _pluginLog.Debug($"  Address: {((IntPtr)node):X}");
                        _pluginLog.Debug($"  Type: {(ushort)node->Type}");
                        _pluginLog.Debug($"  Parent: {((IntPtr)node->ParentNode):X}");
                        _pluginLog.Debug($"  Prev Sibling: {((IntPtr)node->PrevSiblingNode):X}");
                        _pluginLog.Debug($"  Next Sibling: {((IntPtr)node->NextSiblingNode):X}");
                    }
                }

                // Only proceed with modifications if enabled
                if (!_isModifying)
                {
                    _pluginLog.Debug("Modifications are disabled - analysis complete");
                    return;
                }

                _pluginLog.Debug("=== Starting Text Modifications ===");

                // Now modify the nodes with our custom text
                if (nodes.TitleNode != IntPtr.Zero && !string.IsNullOrEmpty(_customLeve.Title))
                {
                    _pluginLog.Debug("Attempting to modify Title node...");
                    ModifyTextNode(nodes.TitleNode, ValidateAndConvertString(_customLeve.Title, 100, "Title"));
                }
                
                if (nodes.LevelNode != IntPtr.Zero && _customLeve.Level > 0)
                {
                    _pluginLog.Debug("Attempting to modify Level node...");
                    ModifyTextNode(nodes.LevelNode, ValidateAndConvertString(_customLeve.Level.ToString(), 10, "Level"));
                }
                
                if (nodes.ClientNode != IntPtr.Zero && !string.IsNullOrEmpty(_customLeve.Client))
                {
                    _pluginLog.Debug("Attempting to modify Client node...");
                    ModifyTextNode(nodes.ClientNode, ValidateAndConvertString($"Client: {_customLeve.Client}", 100, "Client"));
                }
                
                if (nodes.DescriptionNode != IntPtr.Zero && !string.IsNullOrEmpty(_customLeve.Description))
                {
                    _pluginLog.Debug("Attempting to modify Description node...");
                    ModifyTextNode(nodes.DescriptionNode, ValidateAndConvertString(_customLeve.Description, 1000, "Description"));
                }
                
                if (nodes.LocationNode != IntPtr.Zero && !string.IsNullOrEmpty(_customLeve.LocationAndInfo))
                {
                    _pluginLog.Debug("Attempting to modify Location node...");
                    ModifyTextNode(nodes.LocationNode, ValidateAndConvertString(_customLeve.LocationAndInfo, 1000, "Location"));
                }

                if (nodes.ObjectivesNode != IntPtr.Zero && !string.IsNullOrEmpty(_customLeve.Objectives))
                {
                    _pluginLog.Debug("Attempting to modify Objectives node...");
                    ModifyTextNode(nodes.ObjectivesNode, ValidateAndConvertString(_customLeve.Objectives, 1000, "Objectives"));
                }
                
                if (nodes.DutyCategoryNode != IntPtr.Zero && !string.IsNullOrEmpty(_dutyCategoryTexturePath))
                {
                    _pluginLog.Debug("Attempting to modify DutyCategoryNode texture...");
                    var imageNode = (AtkImageNode*)nodes.DutyCategoryNode;
                    UpdateImageNodeTexture(addon, 53, _dutyCategoryTexturePath);
                    _pluginLog.Debug($"Updated DutyCategoryNode texture to: {_dutyCategoryTexturePath}");
                }
                
                /* Comment out gil/exp modifications
                if (nodes.GilNode != IntPtr.Zero)
                {
                    _pluginLog.Debug("Attempting to modify Gil node...");
                    ModifyTextNode(nodes.GilNode, ValidateAndConvertString($"{_customLeve.GilReward} gil", 100, "Gil"));
                }
                
                if (nodes.ExpNode != IntPtr.Zero)
                {
                    _pluginLog.Debug("Attempting to modify Exp node...");
                    ModifyTextNode(nodes.ExpNode, ValidateAndConvertString($"{_customLeve.ExpReward} experience points", 100, "Exp"));
                }
                */

                _pluginLog.Debug("=== Text Modifications Complete ===");
            }
            catch (Exception ex)
            {
                _pluginLog.Error($"Error in ModifyJournalText: {ex.Message}");
            }
        }

        private unsafe bool ModifyTextNode(IntPtr address, SeString? newText)
        {
            if (address == IntPtr.Zero || newText == null)
            {
                _pluginLog.Error($"Invalid parameters for text modification");
                return false;
            }
            
            try
            {
                var textNode = (AtkTextNode*)address;
                
                // Validate the node is actually a text node
                if (textNode->AtkResNode.Type != NodeType.Text)
                {
                    _pluginLog.Error($"Node at {address:X} is not a text node");
                    return false;
                }

                // Get the encoded text
                var bytes = newText.Encode();
                
                // Instead of allocating new memory, try to use the existing buffer
                var currentPtr = textNode->NodeText.StringPtr;
                var currentBufSize = textNode->NodeText.BufSize;
                
                if (currentPtr != null && currentBufSize >= bytes.Length + 1)
                {
                    // If the existing buffer is big enough, just update it
                    Marshal.Copy(bytes, 0, (IntPtr)currentPtr, bytes.Length);
                    Marshal.WriteByte((IntPtr)currentPtr + bytes.Length, 0);
                    
                    // Update the length
                    textNode->NodeText.StringLength = (uint)bytes.Length;
                    
                    return true;
                }
                else
                {
                    _pluginLog.Error($"Buffer too small or null: {currentBufSize} < {bytes.Length + 1}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _pluginLog.Error($"Error modifying text node at {address:X}: {ex.Message}");
                return false;
            }
        }

        private SeString? ValidateAndConvertString(string input, int maxLength, string fieldName)
        {
            try
            {
                // Special handling for Level field
                if (fieldName == "Level")
                {
                    // Just create a simple text payload for the level
                    return new SeString(new List<Payload> { 
                        new Dalamud.Game.Text.SeStringHandling.Payloads.TextPayload($"Lv. {input}")
                    });
                }

                if (input.Length > maxLength)
                {
                    _pluginLog.Error($"{fieldName} exceeds maximum length of {maxLength} characters");
                    return null;
                }

                if (input.Contains('\0'))
                {
                    _pluginLog.Error($"{fieldName} contains invalid null characters");
                    return null;
                }

                var payloads = new List<Payload>();
                var currentIndex = 0;

                while (currentIndex < input.Length)
                {
                    // Find the next tag
                    var emStart = input.IndexOf("<em>", currentIndex);
                    var emEnd = input.IndexOf("</em>", currentIndex);
                    var colorTypeStart = input.IndexOf("<colortype(", currentIndex);
                    var colorTypeEnd = input.IndexOf(")>", colorTypeStart + 10);  // 10 is length of "<colortype("
                    var edgeColorTypeStart = input.IndexOf("<edgecolortype(", currentIndex);
                    var edgeColorTypeEnd = input.IndexOf(")>", edgeColorTypeStart + 14); // 14 is length of "<edgecolortype("

                    var nextTag = new[] { 
                        emStart == -1 ? int.MaxValue : emStart,
                        emEnd == -1 ? int.MaxValue : emEnd,
                        colorTypeStart == -1 ? int.MaxValue : colorTypeStart,
                        edgeColorTypeStart == -1 ? int.MaxValue : edgeColorTypeStart
                    }.Min();

                    if (nextTag == int.MaxValue)
                    {
                        // No more tags, add remaining text
                        if (currentIndex < input.Length)
                        {
                            payloads.Add(new Dalamud.Game.Text.SeStringHandling.Payloads.TextPayload(
                                input.Substring(currentIndex)));
                        }
                        break;
                    }

                    // Add text before the tag
                    if (nextTag > currentIndex)
                    {
                        payloads.Add(new Dalamud.Game.Text.SeStringHandling.Payloads.TextPayload(
                            input.Substring(currentIndex, nextTag - currentIndex)));
                    }

                    // Process the tag
                    if (nextTag == emStart)
                    {
                        payloads.Add(new Dalamud.Game.Text.SeStringHandling.Payloads.EmphasisItalicPayload(true));
                        currentIndex = emStart + 4; // Length of "<em>"
                    }
                    else if (nextTag == emEnd)
                    {
                        payloads.Add(new Dalamud.Game.Text.SeStringHandling.Payloads.EmphasisItalicPayload(false));
                        currentIndex = emEnd + 5; // Length of "</em>"
                    }
                    else if (nextTag == colorTypeStart && colorTypeEnd != -1)
                    {
                        var startParenIndex = input.IndexOf("(", colorTypeStart);
                        if (startParenIndex != -1)
                        {
                            var valueStr = input.Substring(startParenIndex + 1, colorTypeEnd - (startParenIndex + 1));
                            if (ushort.TryParse(valueStr, out ushort colorKey))
                            {
                                payloads.Add(new Dalamud.Game.Text.SeStringHandling.Payloads.UIForegroundPayload(colorKey));
                                currentIndex = colorTypeEnd + 2; // +2 for ")>"
                            }
                            else
                            {
                                _pluginLog.Error($"Invalid colortype value: {valueStr}");
                                currentIndex = colorTypeEnd + 2;
                            }
                        }
                        else
                        {
                            currentIndex = colorTypeStart + 10;
                        }
                    }
                    else if (nextTag == edgeColorTypeStart && edgeColorTypeEnd != -1)
                    {
                        var startParenIndex = input.IndexOf("(", edgeColorTypeStart);
                        if (startParenIndex != -1)
                        {
                            var valueStr = input.Substring(startParenIndex + 1, edgeColorTypeEnd - (startParenIndex + 1));
                            if (ushort.TryParse(valueStr, out ushort edgeKey))
                            {
                                payloads.Add(new Dalamud.Game.Text.SeStringHandling.Payloads.UIGlowPayload(edgeKey));
                                currentIndex = edgeColorTypeEnd + 2; // +2 for ")>"
                            }
                            else
                            {
                                _pluginLog.Error($"Invalid edgecolortype value: {valueStr}");
                                currentIndex = edgeColorTypeEnd + 2;
                            }
                        }
                        else
                        {
                            currentIndex = edgeColorTypeStart + 14;
                        }
                    }
                }

                return new SeString(payloads);
            }
            catch (Exception ex)
            {
                _pluginLog.Error($"Error converting {fieldName}: {ex.Message}");
                return null;
            }
        }

        private unsafe void UpdateNodeTexture(IntPtr addon, int nodeIndex, string texturePath)
        {
            try
            {
                _pluginLog.Debug($"=== Starting texture update for node {nodeIndex} ===");
                _pluginLog.Debug($"Texture path: {texturePath}");
                
                var atkUnitBase = (AtkUnitBase*)addon;
                var node = atkUnitBase->UldManager.NodeList[nodeIndex];
                if (node == null)
                {
                    _pluginLog.Error($"Node at index {nodeIndex} is null");
                    return;
                }
                
                _pluginLog.Debug($"Node type: {node->Type}");
                if ((ushort)node->Type < 1000)
                {
                    _pluginLog.Error($"Node at index {nodeIndex} is not a component node (type: {node->Type})");
                    return;
                }

                var componentNode = (AtkComponentNode*)node;
                if (componentNode->Component == null)
                {
                    _pluginLog.Error("Component is null");
                    return;
                }

                _pluginLog.Debug($"Component has {componentNode->Component->UldManager.NodeListCount} child nodes");
                var childNode = componentNode->Component->UldManager.NodeList[0];
                if (childNode == null)
                {
                    _pluginLog.Error("Child node is null");
                    return;
                }

                _pluginLog.Debug($"Child node type: {childNode->Type}");
                if (childNode->Type != NodeType.Image)
                {
                    _pluginLog.Error($"Child node is not an image node (type: {childNode->Type})");
                    return;
                }

                var imageNode = (AtkImageNode*)childNode;
                if (imageNode != null)
                {
                    _pluginLog.Debug("Loading new texture...");
                    imageNode->LoadTexture(texturePath);
                    imageNode->AtkResNode.NodeFlags |= (NodeFlags)0x2;
                    _pluginLog.Debug("Texture update complete");
                }
                else
                {
                    _pluginLog.Error("Failed to cast to image node");
                }
            }
            catch (Exception ex)
            {
                _pluginLog.Error($"Error updating texture: {ex}");
            }
        }

        private unsafe void UpdateImageNodeTexture(IntPtr addon, int nodeIndex, string texturePath)
        {
            try
            {
                _pluginLog.Debug($"=== Starting direct image texture update for node {nodeIndex} ===");
                _pluginLog.Debug($"Texture path: {texturePath}");
                
                var atkUnitBase = (AtkUnitBase*)addon;
                var node = atkUnitBase->UldManager.NodeList[nodeIndex];
                if (node == null)
                {
                    _pluginLog.Error($"Node at index {nodeIndex} is null");
                    return;
                }
                
                _pluginLog.Debug($"Node type: {node->Type}");
                if (node->Type != NodeType.Image)
                {
                    _pluginLog.Error($"Node at index {nodeIndex} is not an image node (type: {node->Type})");
                    return;
                }

                var imageNode = (AtkImageNode*)node;
                if (imageNode != null)
                {
                    _pluginLog.Debug("Loading new texture...");
                    imageNode->LoadTexture(texturePath);
                    imageNode->AtkResNode.NodeFlags |= (NodeFlags)0x2;
                    _pluginLog.Debug("Texture update complete");
                }
                else
                {
                    _pluginLog.Error("Failed to cast to image node");
                }
            }
            catch (Exception ex)
            {
                _pluginLog.Error($"Error updating texture: {ex}");
            }
        }

        public override void Draw()
        {
            ImGui.Text("Levequest Editor");
            ImGui.Separator();

            ImGui.Text("Custom Leve Settings");
            
            // Basic Information
            ImGui.InputText("Title", ref _customLeve.Title, 100);
            ImGui.InputInt("Level", ref _customLeve.Level);
            
            // Comment out Rewards section
            /* 
            // Rewards
            ImGui.InputInt("Gil Reward", ref _customLeve.GilReward);
            ImGui.InputInt("Experience Reward", ref _customLeve.ExpReward);
            */
            
            // Detailed Information
            if (ImGui.CollapsingHeader("Detailed Information"))
            {
                ImGui.InputTextMultiline("Description", ref _customLeve.Description, 1000, new Vector2(0, 60));
                ImGui.InputTextMultiline("Objectives", ref _customLeve.Objectives, 1000, new Vector2(0, 60));
                ImGui.InputTextMultiline("Location and Information", ref _customLeve.LocationAndInfo, 1000, new Vector2(0, 60));
                ImGui.InputTextMultiline("Client", ref _customLeve.Client, 100, new Vector2(0, 60));
            }

            // Add after the "Detailed Information" section
            if (ImGui.CollapsingHeader("Texture Settings"))
            {
                ImGui.InputText("Custom Texture Path", ref _customTexturePath, 256);
                
                if (ImGui.BeginCombo("Texture Presets", "Select a preset..."))
                {
                    string lastCategory = "";
                    foreach (var preset in _texturePresets)
                    {
                        // Add category separator
                        if (lastCategory != preset.Category)
                        {
                            ImGui.Separator();
                            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), preset.Category);
                            lastCategory = preset.Category;
                        }

                        bool isSelected = _customTexturePath == preset.Path;
                        if (ImGui.Selectable(preset.Description, isSelected))
                        {
                            _customTexturePath = preset.Path;
                        }

                        if (isSelected)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                    }
                    ImGui.EndCombo();
                }

                if (ImGui.Button("Update Texture"))
                {
                    var journalAddon = _gameGui.GetAddonByName("JournalDetail");
                    if (journalAddon != IntPtr.Zero && !string.IsNullOrEmpty(_customTexturePath))
                    {
                        UpdateNodeTexture(journalAddon, 50, _customTexturePath);
                    }
                    else
                    {
                        _pluginLog.Debug(journalAddon == IntPtr.Zero ? 
                            "Journal window not found - open the journal first!" : 
                            "No texture path specified");
                    }
                }
            }

            if (ImGui.CollapsingHeader("Duty Category Icon"))
            {
                ImGui.InputText("Custom Category Path", ref _dutyCategoryTexturePath, 256);
                
                if (ImGui.BeginCombo("Category Presets", "Select a preset..."))
                {
                    foreach (var preset in _dutyCategoryPresets)
                    {
                        bool isSelected = _dutyCategoryTexturePath == preset.Path;
                        if (ImGui.Selectable(preset.Description, isSelected))
                        {
                            _dutyCategoryTexturePath = preset.Path;
                        }

                        if (isSelected)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                    }
                    ImGui.EndCombo();
                }

                if (ImGui.Button("Update Category Icon"))
                {
                    var journalAddon = _gameGui.GetAddonByName("JournalDetail");
                    if (journalAddon != IntPtr.Zero && !string.IsNullOrEmpty(_dutyCategoryTexturePath))
                    {
                        UpdateImageNodeTexture(journalAddon, 53, _dutyCategoryTexturePath);
                    }
                    else
                    {
                        _pluginLog.Debug(journalAddon == IntPtr.Zero ? 
                            "Journal window not found - open the journal first!" : 
                            "No category texture path specified");
                    }
                }
            }

            ImGui.Separator();

            // Toggle modification
            if (ImGui.Checkbox("Enable Modifications", ref _isModifying))
            {
                if (_isModifying)
                {
                    _pluginLog.Debug("Modifications enabled - will modify journal when opened");
                }
                else
                {
                    _pluginLog.Debug("Modifications disabled");
                }
            }

            if (ImGui.Button("Apply Changes Now"))
            {
                var journalAddon = _gameGui.GetAddonByName("JournalDetail");
                if (journalAddon != IntPtr.Zero)
                {
                    ModifyJournalText(journalAddon);
                }
                else
                {
                    _pluginLog.Debug("Journal window not found - open the journal first!");
                }
            }

            ImGui.Spacing();
            
            if (ImGui.Button("Show Settings"))
            {
                Plugin.ToggleConfigUI();
            }
        }
    }
}

