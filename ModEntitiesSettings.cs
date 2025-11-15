using System.Drawing;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using Newtonsoft.Json;

namespace ModEntities;

public class ModEntitiesSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new ToggleNode(false);
    public ToggleNode IgnoreFullscreenPanels { get; set; } = new();
    public ToggleNode IgnoreLargePanels { get; set; } = new();
    public RangeNode<int> EntityLookupRange { get; set; } = new RangeNode<int>(120, 0, 200);

    [Menu(null, CollapsedByDefault = true)]
    public ContentNode<EntityGroup> EntityGroups { get; set; } = new ContentNode<EntityGroup>() { ItemFactory = () => new EntityGroup() };

    [Menu(null, CollapsedByDefault = true)]
    public ContentNode<TextNode> BlacklistTemplates { get; set; } = new ContentNode<TextNode>() { UseFlatItems = true, ItemFactory = CreateBlankTextNode };

    public ToggleNode CollectUnknownEffects { get; set; } = new ToggleNode(true);

    [JsonIgnore]
    public ButtonNode RemoveMatchedUnknownEffects { get; set; } = new ButtonNode();

    [Menu(null, CollapsedByDefault = true)]
    public ContentNode<TextNode> UnknownEffects { get; set; } = new ContentNode<TextNode>() { UseFlatItems = true, };

    private static TextNode CreateBlankTextNode()
    {
        TextNode textNode = new TextNode("^$");
        textNode.OnValueChanged = OnValueChanged;
        return textNode;
    }

    public static void OnValueChanged()
    {
        // If any template paths change, we need to trigger a full refresh
        //TileEntities.NeedFreshEntityParse = true;
    }
}

public class EntityGroup
{
    public ContentNode<TextNode> PathTemplates { get; set; } = new ContentNode<TextNode>() { UseFlatItems = true, ItemFactory = CreateBlankTextNode };
    public RangeNode<int> BaseSizeOverride { get; set; } = new RangeNode<int>(0, 0, 2000);
    public TextNode Name { get; set; } = new TextNode("");
    public ToggleNode ShowOnMap { get; set; } = new ToggleNode(false);

    private static TextNode CreateBlankTextNode()
    {
        TextNode textNode = new TextNode("^$");
        textNode.OnValueChanged = OnValueChanged;
        return textNode;
    }

    public static void OnValueChanged()
    {
        // If any template paths change, we need to trigger a full refresh
        //TileEntities.NeedFreshEntityParse = true;
    }


}