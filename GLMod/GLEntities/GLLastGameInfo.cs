using System.Collections.Generic;

namespace GLMod.GLEntities
{
    /// <summary>
    /// The game itself.
    /// </summary>
    public class GLLastGameInfo
    {
        public long id { get; set; }

        /// <summary>Recap page on the site.</summary>
        public string url { get; set; }

        public string mod { get; set; }
        public int? season { get; set; }
        public bool ranked { get; set; }
        public string map { get; set; }

        /// <summary>Start, Unix seconds.</summary>
        public long? start { get; set; }

        /// <summary>End, Unix seconds.</summary>
        public long? end { get; set; }

        /// <summary>Duration in seconds.</summary>
        public long? duration { get; set; }

        public int? turns { get; set; }
        public int? meetings { get; set; }
        public int? players { get; set; }

        /// <summary>Winning teams (e.g. "imp").</summary>
        public List<string> winners { get; set; }
    }
}
