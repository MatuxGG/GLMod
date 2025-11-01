using System;
using System.Collections.Generic;
using System.Text;

namespace GLMod.GLEntities
{
    public class GLItem
    {
        public string id { get; set; }
        public string name { get; set; }

        public GLItem(string id, string name)
        {
            this.id = id;
            this.name = name;
        }
    }
}
