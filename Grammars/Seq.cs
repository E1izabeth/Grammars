using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Grammars
{
    public class Seq : IEquatable<Seq>, IComparable<Seq>
    {
        public List<ITerm> Chain { get; private set; }

        public bool IsNullable { get { return this.Chain.Count == 1 && this.Chain.First().Name == "ε"; } }
        public bool IsChaining { get { return this.Chain.Count == 1 && !this.Chain.First().IsTerminal; } }

        public Seq(IEnumerable<ITerm> chains)
        {
            this.Chain = chains.ToList(); ;
        }

        public override string ToString()
        {
            return string.Join(string.Empty, this.Chain.Select(t => t.Name));
        }

        public bool Equals(Seq other)
        {
            return this.ToString() == other.ToString();
        }

        public int CompareTo(Seq other)
        {
            return this.ToString().CompareTo(other.ToString());
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj as Seq)?.Equals(this) ?? false;
        }
    }
}