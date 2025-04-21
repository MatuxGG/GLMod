using System;
using System.Collections.Generic;
using System.Text;

namespace GLMod
{
    public class GLDataList
    {
        public List<GLData> datas { get; set; }

        public GLDataList(List<GLData> datas)
        {
            this.datas = datas;
        }

        public string get(string id)
        {
            GLData data = datas.FindAll(d => d.id == id)[0];
            if (data == null) return null;
            return data.value;
        }
    }
}
