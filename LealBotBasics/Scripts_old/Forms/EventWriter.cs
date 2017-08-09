using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DiscordBot.Scripts.Forms {
    public class EventWriter: TextWriter {
        public override Encoding Encoding { get { return Encoding.UTF8; } }
        public EventHandler<char> CharWritten;

        public override void Write(char value) {
            base.Write(value);
            CharWritten?.Invoke(this, value);
        }
    }
}
