using System;
using System.Collections.Generic;
using System.Text;

namespace GLMod
{
    public class GLTranslation
    {
        public string original;
        public string translation;

        public GLTranslation(string original, string translation)
        {
            this.original = original;
            this.translation = translation;
        }

        public GLTranslation()
        {
            this.original = "";
            this.translation = "";
        }

    }
}
