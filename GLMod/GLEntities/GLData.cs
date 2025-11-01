using System;
using System.Collections.Generic;
using System.Text;

namespace GLMod.GLEntities
{
    public class GLData
    {
        public string id { get; set; }
        public string value { get; set; }

        public GLData(string id, string value)
        {
            this.id = id;
            this.value = value;
        }
    }
}
