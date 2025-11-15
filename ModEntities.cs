using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ExileCore.Shared.Nodes;
using GameOffsets;

namespace ModEntities;

public class ModEntities : BaseSettingsPlugin<ModEntitiesSettings>
{
    private readonly ConditionalWeakTable<string, Func<string, bool>> _pathMatchers = [];

    private List<Entity> viableEntities = new List<Entity>();

    private List<Tuple<Entity, EntityGroup, int>> matchedEntities = new List<Tuple<Entity, EntityGroup, int>>();

    public override Job Tick()
    {
        // We will parse entities every tick as ghosts may enter new rares
        ParseEntities();

        return null;
    }

    public override void Render()
    {
        if (!Settings.IgnoreFullscreenPanels &&
            GameController.Game.IngameState.IngameUi.FullscreenPanels.Any(x => x.IsVisible) ||
            !Settings.IgnoreLargePanels &&
            GameController.Game.IngameState.IngameUi.LargePanels.Any(x => x.IsVisible))
            return;

        DrawMatches();
    }

    private bool IsMatch(string template, string path)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return false;
        }

        return _pathMatchers.GetValue(template, t =>
        {
            var regexes = t.Split('&').Select(x => x.StartsWith('!')
                    ? (false, x[1..])
                    : (true, x))
                .Select(p => (p.Item1, new Regex(p.Item2, RegexOptions.IgnoreCase)))
                .ToList();
            return s => regexes.All(x => x.Item2.IsMatch(s) == x.Item1);
        })!(path);
    }

    public override void AreaChange(AreaInstance area)
    {
        base.AreaChange(area);

        matchedEntities = new List<Tuple<Entity, EntityGroup, int>>();
        viableEntities = new List<Entity>();
    }

    public override void EntityAdded(Entity entity)
    {
        base.EntityAdded(entity);

        // We only want to add unique and rare monsters as they can have ghosts added
        if (entity.IsValid && (entity.Rarity == MonsterRarity.Unique || entity.Rarity == MonsterRarity.Rare) && entity.IsAlive)
        {
            viableEntities.Add(entity);
        }
    }

    public override void EntityRemoved(Entity entity)
    {
        base.EntityRemoved(entity);

        viableEntities.Remove(entity);
    }

    private void ParseEntities()
    {
        // Clear out matched entities
        matchedEntities.Clear();

        foreach (var entity in viableEntities)
        {
            if (entity == null || !entity.IsValid || !entity.IsAlive) continue;

            var entityMods = entity.GetComponent<ObjectMagicProperties>()?.Mods;

            if (entityMods == null) continue;

            Dictionary<EntityGroup, int> entityGroupMatches = new Dictionary<EntityGroup, int>();

            foreach (var mod in entityMods)
            {
                // This could just be a temp error... lets not necessarily reject it
                if (string.IsNullOrEmpty(mod))
                {
                    continue;
                }

                if (Settings.BlacklistTemplates.Content.Any(x => IsMatch(x.Value, mod)))
                {
                    continue;
                }

                var matchingGroup = Settings.EntityGroups.Content.FirstOrDefault(g => g.PathTemplates.Content.Any(p => IsMatch(p.Value, mod)));
                if (matchingGroup != null)
                {
                    if (!entityGroupMatches.TryGetValue(matchingGroup, out int total))
                    {
                        entityGroupMatches.Add(matchingGroup, 1);
                    }
                    else
                    {
                        entityGroupMatches[matchingGroup] = total + 1;
                    }

                }
                else
                {
                    if (Settings.CollectUnknownEffects)
                    {
                        if (!Settings.UnknownEffects.Content.Any(x => x.Value == mod))
                        {
                            Settings.UnknownEffects.Content.Add(new TextNode(mod));
                        }
                    }
                }
            }

            if (entityGroupMatches.Count > 0)
            {
                var largestMatch = entityGroupMatches.OrderBy(x => x.Value).FirstOrDefault();

                matchedEntities.Add(new Tuple<Entity, EntityGroup, int>(entity, largestMatch.Key, largestMatch.Value));
            }
        }
    }

    private void DrawMatches()
    {
        foreach(var matchedEntity in matchedEntities)
        {
            var entity = matchedEntity.Item1;
            var entityGroup = matchedEntity.Item2;
            var entityName = entityGroup.Name == null ? entity.Path : entityGroup.Name.Value;

            // Prob should have a config... but for now lets just append matched count.
            if (matchedEntity.Item3 > 1)
            {
                entityName = $"[{matchedEntity.Item3}] {entityName}";
            }


            if (entityGroup.ShowOnMap)
            {
                SharpDX.Color mapTextColor = SharpDX.Color.White;
                if (matchedEntity.Item3 == 2)
                {
                    mapTextColor = SharpDX.Color.Orange;
                }
                else if (matchedEntity.Item3 >= 3)
                {
                    mapTextColor = SharpDX.Color.Red;
                }

                Graphics.DrawTextWithBackground(entityName, GameController.IngameState.Data.GetGridMapScreenPosition(entity.GridPosNum), mapTextColor, FontAlign.Center, SharpDX.Color.Black);
            }
            else
            {
                // If its out of our range... ignore it
                if (entity.DistancePlayer >= Settings.EntityLookupRange)
                {
                    continue;
                }

                var pos = entity.PosNum;
                var z = GameController.IngameState.Data.GetTerrainHeightAt(pos.Xy());
                var worldPos = new Vector3(pos.Xy(), z);
                var screenPos = GameController.IngameState.Camera.WorldToScreen(worldPos);
                Graphics.DrawText(entityName, screenPos);
            }
        }
        
    }
}
