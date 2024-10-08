﻿using System;
using System.Collections.Generic;
using System.Text;

namespace GLMod
{
    public class GLPlayer
    {
        public string login { get; set; }
        public string playerName { get; set; }
        public string role { get; set; }
        public string team { get; set; }
        public string tasks { get; set; }
        public string tasksDead { get; set; }
        public string tasksMax { get; set; }
        public string win { get; set; }

        public GLPlayer()
        {
            this.login = "";
            this.playerName = "";
            this.role = "";
            this.team = "";
            this.tasks = "0";
            this.tasksDead = "0";
            this.tasksMax = "0";
            this.win = "0";
        }

        public GLPlayer(string login, string playerName, string role, string team)
        {
            this.login = login;
            this.playerName = playerName;
            this.role = role;
            this.team = team;
            this.tasks = "0";
            this.tasksDead = "0";
            this.tasksMax = "0";
            this.win = "0";
        }

        public GLPlayer(string login, string playerName, string role, string team, string exiled, string kills,
            string killed, string killedFirst, string bodyReported, string emergencyCalled, string tasks, string tasksDead, string tasksMax, string win)
        {
            this.login = login;
            this.playerName = playerName;
            this.role = role;
            this.team = team;
            this.tasks = tasks;
            this.tasksDead = tasksDead;
            this.tasksMax = tasksMax;
            this.win = win;
        }

        public void addTasks()
        {
            this.tasks = (int.Parse(this.tasks) + 1).ToString();
        }
        public void addTasksDead()
        {
            this.tasksDead = (int.Parse(this.tasksDead) + 1).ToString();
        }

        public void setTasksMax(int tasksMax)
        {
            this.tasksMax = tasksMax.ToString();
        }

        public void setWin()
        {
            this.win = "1";
        }
    }
}
