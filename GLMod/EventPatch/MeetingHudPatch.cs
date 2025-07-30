using HarmonyLib;
using InnerNet;
using System;
using UnityEngine;

namespace GLMod
{
    // Votes
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.BloopAVoteIcon))]
    public class BloopAVoteIconPatch
    {
        public static void Postfix(MeetingHud __instance, [HarmonyArgument(0)] NetworkedPlayerInfo voterPlayer, [HarmonyArgument(1)] int index, [HarmonyArgument(2)] Transform parent)
        {   
            if (GLMod.existService("Votes") || GLMod.debug)
            {
                try
                {
                    byte targetId = 0;
                    bool found = false;
                    for (int i = 0; i < MeetingHud.Instance.playerStates.Length; i++)
                    {
                        PlayerVoteArea playerVoteArea = MeetingHud.Instance.playerStates[i];
                        if (playerVoteArea.TargetPlayerId == voterPlayer.PlayerId)
                        {
                            targetId = playerVoteArea.VotedFor;
                            found = true;
                            break;
                        }
                    }
                    if (!found) return;

                    PlayerControl target = null;
                    foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                    {
                        if (p.PlayerId == targetId)
                        {
                            target = p;
                            break;
                        }
                    }
                    if (target)
                    {
                        GLMod.currentGame.addAction(voterPlayer.PlayerName, target.Data.PlayerName, "voted");
                    } else
                    {
                        GLMod.currentGame.addAction(voterPlayer.PlayerName, "", "voted");
                    }
                    
                } catch (Exception e)
                {
                    GLMod.log("[VanillaVotes] Catch exception " + e.Message);
                }
                
            }
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    public class MeetingHudStartPatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            long timestampSeconds = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (player.Data.IsDead) continue;
                float x = player.MyPhysics.body.transform.position.x;
                float y = player.MyPhysics.body.transform.position.y;
                GLMod.currentGame.addPosition(player.Data.PlayerName, x, y, timestampSeconds.ToString());
            }
        }
    }
}
