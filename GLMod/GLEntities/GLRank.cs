using System;
using System.Collections.Generic;
using System.Text;

namespace GLMod
{
    public class GLRank
    {
        public int id { get; set; }
        public string name { get; set; }
        public string link { get; set; }

        public int percent { get; set; }
        public GLRank()
        {
            this.id = -1;   
            this.name = null;
            this.link = null;
            this.percent = 0;
        }

        public GLRank(int id, string name, string link, int percent)
        {
            this.id = id;
            this.name = name;
            this.link = link;
            this.percent = percent;
        }
    }
}
