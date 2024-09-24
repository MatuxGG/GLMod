using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

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

        public void load()
        {
            string translationsURL = GLMod.api + "/trans/" + this.code;
            string tr = "";
            try
            {
                using (var client = Utils.getClient())
                {
                    tr = client.DownloadString(translationsURL);
                }
            }
            catch (Exception e)
            {

            }
            translations = GLJson.Deserialize<List<GLTranslation>>(tr);
        }
    }
}
