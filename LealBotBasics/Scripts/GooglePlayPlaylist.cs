using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace LealBotBasics.Scripts {
    public class GooglePlayPlaylist {
        public static Regex ItemRegex = new Regex("<div itemprop=\"alternativeHeadline\">(.+?)<\\/div>");
        public static Regex NameRegex = new Regex("<meta property=\"og:title\" content=\"(.+?)\"\\/>");
        public static Regex RedirectRegex = new Regex(@"<a href='\/music\/preview\/pl\/(.+?)'>Click here if you're not redirected<\/a>");

        public bool isValid = false;
        public string name = "Unknown";
        public string[] items = null;
        public int length = 0;

        public GooglePlayPlaylist(string uri) {
            Get(uri);
        }

        void Get(string uri) {
            WebRequest webReq = WebRequest.CreateHttp(uri);
            WebResponse res = webReq.GetResponse();
            Stream stream = res.GetResponseStream();
            string page;
            using(StreamReader reader = new StreamReader(stream)) {
                page = reader.ReadToEnd();
            }
            stream.Dispose();
            stream = null;
            res.Dispose();
            res = null;

            MatchCollection matches = ItemRegex.Matches(page);

            //Redirect Page
            if(matches.Count == 0) {
                Match rMatch = RedirectRegex.Match(page);
                if(rMatch.Length != 0) {
                    string redirectTo = rMatch.Value.Substring("<a href='".Length);
                    redirectTo = redirectTo.Remove(redirectTo.Length - "'>Click here if you're not redirected</a>".Length);
                    Get("http://play.google.com/" + redirectTo);
                }
                page = null;
                return;
            }

            StringBuilder builder = new StringBuilder();
            XmlWriter writer = XmlWriter.Create(builder);
            writer.WriteStartDocument();
            writer.WriteStartElement("root");

            //Name
            Match nMatch = NameRegex.Match(page);
            name = nMatch.Value.Substring("<meta property=\"og: title\" content=\"".Length - 1);
            name = name.Remove(name.Length - 3);

            writer.WriteStartElement("name");
            writer.WriteRaw(name);
            writer.WriteEndElement();

            //Items
            foreach(Match m in matches) {
                writer.WriteRaw(m.Value);
            }
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Close();
            writer.Dispose();
            writer = null;
            
            //Read
            XDocument doc = XDocument.Parse(builder.ToString());
            XElement root = doc.Element("root");
            name = root.Element("name").Value;

            IEnumerable<XElement> elms = root.Elements("div");

            length = matches.Count;
            items = new string[length];

            int i = 0;
            foreach(XElement elm in elms)
                items[i++] = elm.Value;

            isValid = true;

            builder.Clear();
            builder = null;
            page = null;
        }
    }
}
