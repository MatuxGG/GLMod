using System;
using System.Collections.Generic;
using System.Text;

namespace GLMod
{
    public class GLAction
    {
        public string turn { get; set; }
        public string source { get; set; }
        public string target { get; set; }
        public string action { get; set; }
        public string triggerTimeMs { get; set; }

        public GLAction(string turn, string source, string target, string action, string triggerTime)
        {
            this.turn = turn;
            this.source = source;
            this.target = target;
            this.action = action;
            this.triggerTimeMs = triggerTime;
        }

    }
}
