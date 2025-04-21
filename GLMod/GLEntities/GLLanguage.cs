using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GLMod
{
    public class GLLanguage
    {
        public string code;
        public string name;
        public List<GLTranslation> translations;

        public GLLanguage(string code, string name)
        {
            this.code = code;
            this.name = name;
            this.translations = new List<GLTranslation>() { };
        }

        public GLLanguage()
        {
            this.code = "";
            this.name = "";
            this.translations = new List<GLTranslation>() { };
        }

        public async Task load()
        {
            string translationsURL = GLMod.api + "/trans/" + this.code;
            string tr = "";
            try
            {
                tr = await HttpHelper.Client.GetStringAsync(translationsURL);
            }
            catch (Exception e)
            {
                GLMod.log("Error language load: " + e.Message);
            }
            translations = GLJson.Deserialize<List<GLTranslation>>(tr);
        }
    }
}
