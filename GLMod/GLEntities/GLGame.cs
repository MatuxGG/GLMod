using System;
using System.Collections.Generic;
using System.Text;

namespace GLMod
{
    public class GLGame
    {
        public string id { get; set; }
        public string code { get; set; }
        public string map { get; set; }

        public string modName { get; set; }
        public string ranked { get; set; }
        public string winner { get; set; }
        public string turns { get; set; }
        public List<GLPlayer> players { get; set; }
        public List<GLAction> actions { get; set; }

        public GLGame(string code, string map, bool ranked, string modName)
        {
            this.id = null;
            this.code = code;
            this.map = map;
            this.ranked = ranked ? "1" : "0";
            this.winner = "";
            this.players = new List<GLPlayer>() { };
            this.turns = "1";
            this.actions = new List<GLAction>() { };
            this.modName = modName;
        }

        public GLGame(string id, string code, string map, string modName, string ranked, string winner, string turns, List<GLPlayer> players, List<GLAction> actions)
        {
            this.id = id;
            this.code = code;
            this.map = map;
            this.modName = modName;
            this.ranked = ranked;
            this.winner = winner;
            this.players = players;
            this.turns = turns;
            this.actions = actions;
        }

        public void addPlayer(string login, string playerName, string role, string team, string color)
        {
            this.players.Add(new GLPlayer(login, playerName, role, team, color));
        }

        public void setId(int id)
        {
            this.id = id.ToString();
        }

        public int getId()
        {
            return int.Parse(this.id);
        }

        public void setWinner(string winner)
        {
            this.winner = winner;
            this.players.FindAll(p => p.team == winner).ForEach(p => p.setWin());
        }

        public void setWinners(List<string> winners)
        {
            this.winner = winners[0];
            this.players.FindAll(p => winners.Contains(p.team)).ForEach(p => p.setWin());
        }

        public void addTurn()
        {
            int turn = int.Parse(this.turns);
            this.turns = turn < 1000 ? (turn + 1000).ToString() : (turn - 999).ToString();
        }
        public void addAction(string source, string target, string action)
        {
            long timestampSeconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            this.actions.Add(new GLAction(this.turns, source, target, action, timestampSeconds.ToString()));
        }

        public void addPosition(string playerName, float x, float y, string timestampStr)
        {
            this.players.Find(p => p.playerName == playerName).addPosition(x, y, timestampStr);
        }
    }
}
