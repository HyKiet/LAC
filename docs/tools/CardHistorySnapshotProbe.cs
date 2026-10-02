// Chạy nguyên file bằng Unity-MCP script-execute, className CardHistorySnapshotProbe,
// methodName Run. Không đặt vào Assets: probe chỉ dành cho phiên kiểm thử tạm.
using System;
using System.Linq;
using System.Reflection;
using LAC.Cards;
using LAC.Core;
using LAC.Player;
using Mirror;
using UnityEngine;

public static class CardHistorySnapshotProbe
{
    public static string Run()
    {
        if (!Application.isPlaying || !NetworkServer.active || PlayerRegistry.Count != 1)
            throw new InvalidOperationException("Cần Arena host một người; probe bắt đầu lại ván thử.");
        var run = RunManager.Instance;
        run.ReportPlayerDown();
        run.RestartRun();
        var player = PlayerRegistry.All[0];
        player.SetCharacter(CharacterId.Tam);
        var grants = (SyncList<RunManager.CardGrant>)typeof(RunManager)
            .GetField("_cardGrants", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(run);
        var recipe = CardEvolutionCatalog.Recipes.First(r => r.Id == "NoThan");
        foreach (var ingredient in recipe.Ingredients)
            for (int i = 0; i < ingredient.Stacks; i++)
            {
                player.Upgrades.Apply(ingredient.Card, player.GetComponent<PlayerHealth>());
                grants.Add(new RunManager.CardGrant { Player = player.netId, Card = ingredient.Card.Id });
            }
        Debug.Log("[HistorySnapshotHost] player=" + player.netId + " "
            + LAC.Cards.Editor.CardEvolutionCatalogChecks.Signature(player.Upgrades));
        return "grants=" + grants.Count + " evolution=" + player.Upgrades.HasEvolution(recipe.Id);
    }
}
