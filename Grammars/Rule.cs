using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grammars
{
    public class Rule
    {
        public NonTerminal NonTerminal { get; private set; }
        public List<Seq> Alts { get; private set; }
        public bool IsStart { get; set; }

        public Rule(NonTerminal nonTerminal, IEnumerable<Seq> alts)
        {
            this.NonTerminal = nonTerminal;
            this.Alts = alts.ToList();
        }

        public override string ToString()
        {
            return this.NonTerminal.Name + " --> " + string.Join("|", this.Alts);
        }
    }
}
