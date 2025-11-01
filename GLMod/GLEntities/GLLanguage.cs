using System;
using System.Collections.Generic;
using System.Collections;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using GLMod.Class;

namespace GLMod.GLEntities
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

        public IEnumerator load()
        {
            string translationsURL = GLMod.api + "/trans/" + this.code;
            string tr = null;
            string error = null;
            bool done = false;

            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    tr = await HttpHelper.Client.GetStringAsync(translationsURL);
                }
                catch (Exception e)
                {
                    error = e.Message;
                }
                finally
                {
                    done = true;
                }
            });

            // Attendre la fin du chargement
            while (!done)
                yield return null;

            // Vérifier l'erreur
            if (error != null)
            {
                GLMod.log("Error language load: " + error);
                yield break;
            }

            // Désérialiser les traductions
            translations = GLJson.Deserialize<List<GLTranslation>>(tr);
        }
    }
}
