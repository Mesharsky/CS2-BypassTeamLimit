using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace Mesharsky_TeamLimitBypass;

public class Mesharsky_TeamLimitBypass : BasePlugin
{
    public override string ModuleName => "[CS2] Team Limit Bypass";

    public override string ModuleDescription => "Bypass hardcoded team limits";

    public override string ModuleAuthor => "Mesharsky";

    public override string ModuleVersion => "0.3";

    public enum JoinTeamReason
    {
        OneTeamChange = 1,
        TeamsFull = 2,
        TerroristTeamFull = 7,
        CTTeamFull = 8
    }

    public int TerroristSpawns = -1;
    public int CTSpawns = -1;
    public Dictionary<CCSPlayerController, int> SelectedTeam = [];

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnMapStart>((mapName) =>
        {
            AddTimer(0.1f, () =>
            {
                TerroristSpawns = 0;
                CTSpawns = 0;

                var tSpawns = Utilities.FindAllEntitiesByDesignerName<CEntityInstance>("info_player_terrorist");
                var ctSpawns = Utilities.FindAllEntitiesByDesignerName<CEntityInstance>("info_player_counterterrorist");

                foreach (var spawn in tSpawns)
                {
                    TerroristSpawns++;
                }

                foreach (var spawn in ctSpawns)
                {
                    CTSpawns++;
                }
            });
        });


        RegisterListener<Listeners.OnClientConnected>((slot) =>
        {
            var player = Utilities.GetPlayerFromSlot(slot);

            if (player == null)
                return;

            SelectedTeam[player] = 0;
        });


        AddCommandListener("jointeam", Command_JoinTeam);
    }

    [GameEventHandler]
    public HookResult TeamJoinFailed(EventJointeamFailed @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player == null || !player.IsValid)
            return HookResult.Continue;

        JoinTeamReason m_eReason = (JoinTeamReason)@event.Reason;

        int m_iTs = GetTeamPlayerCount(CsTeam.Terrorist);
        int m_iCTs = GetTeamPlayerCount(CsTeam.CounterTerrorist);

        if (!SelectedTeam.ContainsKey(player))
            SelectedTeam[player] = 0;

        switch (m_eReason)
        {
            case JoinTeamReason.OneTeamChange:
            {
                return HookResult.Continue;
            }

            case JoinTeamReason.TeamsFull:

                if (m_iCTs == CTSpawns && m_iTs == TerroristSpawns)
                    return HookResult.Continue;

                break;

            case JoinTeamReason.TerroristTeamFull:
                if (m_iTs == TerroristSpawns)
                    return HookResult.Continue;

                break;

            case JoinTeamReason.CTTeamFull:
                if (m_iCTs == CTSpawns)
                    return HookResult.Continue;

                break;

            default:
            {
                return HookResult.Continue;
            }
        }

        player.ChangeTeam((CsTeam)SelectedTeam[player]);
        return HookResult.Handled;
    }

    private HookResult Command_JoinTeam(CCSPlayerController? player, CommandInfo info)
    {
        if (player != null && player.IsValid)
        {
            int startIndex = 0;
            if (info.ArgCount > 0 && info.ArgByIndex(0).ToLower() == "jointeam")
            {
                startIndex = 1;
            }

            if (info.ArgCount > startIndex)
            {
                string teamArg = info.ArgByIndex(startIndex);

                if (int.TryParse(teamArg, out int teamId))
                {
                    if (teamId >= (int)CsTeam.Spectator && teamId <= (int)CsTeam.CounterTerrorist)
                    {
                        SelectedTeam[player] = teamId;
                    }
                }
                else
                {
                    Console.WriteLine("Failed to parse team ID.");
                }
            }
        }

        return HookResult.Continue;  
    }

    public static int GetTeamPlayerCount(CsTeam team)
    {
        return Utilities.GetPlayers().Count(p => p.Team == team);
    }
}