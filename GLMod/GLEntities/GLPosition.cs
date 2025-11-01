using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLMod.GLEntities
{
    public class GLPosition
    {
        public float x { get; set; }
        public float y { get; set; }
        public string triggerTime { get; set; }
        public string turn {  get; set; }
        public GLPosition(float x, float y, string triggerTime)
        {
            this.x = x;
            this.y = y;
            this.triggerTime = triggerTime;
            this.turn = GLMod.currentGame.turns;
        }
    }
}
