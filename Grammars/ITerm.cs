using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grammars
{
    public interface ITerm
    {
        string Name { get; }
        bool IsTerminal { get; }
    }

    public class NonTerminal : ITerm
    {
        public string Name { get; private set; }
        public bool IsTerminal { get { return false; } }

        public NonTerminal(string name)
        {
            this.Name = name;
        }

        public Rule this[params ITerm[] args]
        {
            get { return new Rule(this, new[] { new Seq(args) }); }
        }
    }

    public class Terminal : ITerm
    {
        public string Name { get; private set; }
        public bool IsTerminal { get { return true; } }

        public Terminal(string name)
        {
            this.Name = name;
        }
    }
}
