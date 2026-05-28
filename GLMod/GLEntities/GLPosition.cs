namespace GLMod.GLEntities
{
    public class GLPosition
    {
        public float x { get; set; }
        public float y { get; set; }
        public string triggerTime { get; set; }
        public string turn { get; set; }

        public GLPosition() { }

        public GLPosition(float x, float y, string triggerTime, string turn)
        {
            this.x = x;
            this.y = y;
            this.triggerTime = triggerTime;
            this.turn = turn;
        }
    }
}
