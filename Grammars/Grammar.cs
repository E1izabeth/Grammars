using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grammars
{
    public class Grammar
    {
        public List<Rule> Rules { get; private set; }

        public Action<string> Log { get; set; }

        public Grammar(IEnumerable<Rule> rules)
        {
            this.Rules = rules.ToList();
        }

        public Grammar RemoveNonProducingRules()
        {
            this.Log?.Invoke("RemoveNonProducingRules()");

            var VN = new Dictionary<string, Rule>();
            var n = 0;

            var working = true;
            while (working)
            {
                this.LogSet("VN" + n, VN.Keys);

                working = false;
                foreach (var rule in this.Rules.Where(r => !VN.ContainsKey(r.NonTerminal.Name)))
                {
                    if (rule.Alts.Any(s => s.Chain.All(t => t.IsTerminal) || s.Chain.OfType<NonTerminal>().All(nt => VN.ContainsKey(nt.Name))))
                    {
                        VN.Add(rule.NonTerminal.Name, rule);
                        working = true;
                    }
                }

                n++;
            }

            this.LogSet("VN" + n, VN.Keys);

            return new Grammar(this.Rules.Select(r => new Rule(r.NonTerminal, r.Alts.Where(s => s.Chain.OfType<NonTerminal>().All(nt => VN.ContainsKey(nt.Name)))) { IsStart = r.IsStart })
                                   .Where(r => r.Alts.Count > 0 && VN.ContainsKey(r.NonTerminal.Name)))
            { Log = this.Log };
        }

        public string CollectDebugInfo()
        {
            return string.Join(Environment.NewLine, this.Rules);
        }

        public Grammar RemoveNonReachableRules()
        {
            this.Log?.Invoke("RemoveNonReachableRules()");

            var VN = this.Rules.Where(r => r.IsStart).ToDictionary(r => r.NonTerminal.Name);

            var VT = new HashSet<string>();
            int n = 0;

            var working = true;
            while (working)
            {
                this.LogSet("VN" + n, VN.Keys);
                this.LogSet("VT" + n, VT);

                foreach (var rule in this.Rules)
                {
                    working = false;
                    foreach (var alt in rule.Alts)
                    {
                        foreach (var x in alt.Chain)
                        {
                            if (VN.ContainsKey(rule.NonTerminal.Name))
                            {
                                if (x.IsTerminal)
                                {
                                    VT.Add(x.Name);
                                }
                                else
                                {
                                    if (!VN.ContainsKey(x.Name))
                                    {
                                        VN.Add(x.Name, this.Rules.Find(r => r.NonTerminal.Name == x.Name));
                                        working = true;
                                    }
                                }
                            }
                        }
                    }
                }

                n++;
            }

            this.LogSet("VN" + n, VN.Keys);
            this.LogSet("VT" + n, VT);

            return new Grammar(
                this.Rules.Select(r => new Rule(
                    r.NonTerminal,
                    r.Alts.Where(s => s.Chain.OfType<NonTerminal>().All(nt => VN.ContainsKey(nt.Name)) && s.Chain.OfType<Terminal>().All(t => VT.Contains(t.Name)))
                )
                { IsStart = r.IsStart }).Where(r => r.Alts.Count > 0 && VN.ContainsKey(r.NonTerminal.Name))
            )
            { Log = this.Log };
        }

        void LogSet(string name, IEnumerable<object> vals)
        {
            this.Log?.Invoke("\t" + name + " = {" + string.Join(", ", vals) + "}");
        }

        public Grammar RemoveEpsilonRules(bool noRootEps = false, bool noLog = false)
        {
            if (!noLog)
                this.Log?.Invoke("RemoveEpsilonRules()");

            var Nullable = new Dictionary<string, Seq>();
            var n = 0;

            var working = true;
            while (working)
            {
                if (!noLog)
                    this.LogSet("Nullable" + n, Nullable.Keys);

                working = false;
                foreach (var rule in this.Rules)
                {
                    if (!Nullable.ContainsKey(rule.NonTerminal.Name))
                    {
                        foreach (var seq in rule.Alts)
                        {
                            if (seq.IsNullable || seq.Chain.All(t => !t.IsTerminal && Nullable.ContainsKey(t.Name)))
                            {
                                Nullable.Add(rule.NonTerminal.Name, seq);
                                working = true;
                                break;
                            }
                        }
                    }
                }

                n++;
            }
            if (!noLog)
                this.LogSet("Nullable" + n, Nullable.Keys);

            var P = this.Rules.Select(r => new Rule(r.NonTerminal, r.Alts.Where(s => !s.IsNullable)) { IsStart = r.IsStart })
                        .Where(r => r.Alts.Count > 0).ToDictionary(r => r.NonTerminal.Name);

            if (!noRootEps)
            {
                var s = this.Rules.FirstOrDefault(r => r.IsStart);

                if (s != null && Nullable.ContainsKey(s.NonTerminal.Name))
                {
                    if (!P.TryGetValue(s.NonTerminal.Name, out var ps))
                        P.Add(s.NonTerminal.Name, new Rule(s.NonTerminal, new Seq[0]) { IsStart = s.IsStart });

                    ps.Alts.Add(new Seq(new List<ITerm> { new Terminal("ε") }));
                }
            }

            if (!noLog)
                this.LogSet("P'", P.Values);

            var g2 = new Grammar(P.Values.Select(r => new Rule(
                r.NonTerminal,
                r.Alts.Select(s => new { s, nt = s.Chain.Select((t, i) => new { p = Nullable.ContainsKey(t.Name), i }).Where(t => t.p)
                                                        .Select((t, j) => new { t.i, j }).ToDictionary(t => t.i, t => t.j) })
                      .SelectMany(s => Enumerable.Range(0, (int)Math.Pow(2, s.nt.Count))
                                                 .Select(bits => Enumerable.Range(0, s.nt.Count).Select(i => ((bits >> i) & 1) > 0).ToArray())
                                                 .Select(e => new Seq(s.s.Chain.Where((t, i) => !s.nt.TryGetValue(i, out var j) || e[j])))
                                                 .Where(ss => ss.Chain.Count > 0))
                      .Distinct()
            )
            { IsStart = r.IsStart }))
            { Log = this.Log };

            return g2;
        }

        private static bool TryGetChainingSubset(IEnumerable<Rule> rules, NonTerminal nt, out HashSet<string> chainings)
        {
            /*
                Chain(A) = {A}
                Prev = ∅
                while(Chine(A) != Prev){
                    Temp = Chain(A) - Prev
                    Prev = Chain(A)
                    foreach B in Temp
                        foreach B → C in P
                            Chain(A).add(C)
                }
             */

            chainings = new HashSet<string>(new[] { nt.Name });
            var prev = new HashSet<string>();
            var working = true;
            var ok = false;

            while (working)
            {
                working = false;
                var temp = chainings.Where(s => !prev.Contains(s)).ToArray();
                prev = new HashSet<string>(chainings);

                foreach (var ntName in temp)
                {
                    foreach (var seq in rules.Where(r => r.NonTerminal.Name == ntName).SelectMany(r => r.Alts).Where(s => s.IsChaining))
                    {
                        chainings.Add(seq.Chain.First().Name);
                        working = true;
                        ok = true;
                    }
                }
            }

            return ok;
        }

        public Grammar RemoveChainingRules(bool noRootEps = false)
        {
            this.Log?.Invoke("RemoveChainingRules()");

            /*
                P’ = P - {(A → α) ∈ P | ∀ α ∉ VN} //все кроме цепных правил
                foreach A in VN {
                    if (Chain(A) != {A})
                        foreach B in Chain(A)
                            foreach B → α in P
                                if (α ∉ VN )
                                    P’.add(A → α)
}
             */

            var weps = this.RemoveEpsilonRules(true, true);

            var P = weps.Rules.Select(r => new Rule(r.NonTerminal, r.Alts.Where(s => !s.IsChaining)) { IsStart = r.IsStart })
                        .Where(r => r.Alts.Count > 0)
                        .ToDictionary(r => r.NonTerminal.Name);

            this.LogSet("P'", P.Values);

            foreach (var rule in weps.Rules)
            {
                if (TryGetChainingSubset(weps.Rules, rule.NonTerminal, out var chainings))
                {
                    this.LogSet("Chain(" + rule.NonTerminal.Name + ")", chainings);

                    foreach (var seq in chainings.SelectMany(s => weps.Rules.First(r => r.NonTerminal.Name == s).Alts))
                    {
                        if (!seq.IsChaining)
                        {
                            if (!P.TryGetValue(rule.NonTerminal.Name, out var s))
                                P.Add(rule.NonTerminal.Name, new Rule(rule.NonTerminal, new Seq[0]) { IsStart = rule.IsStart });

                            if (!s.Alts.Any(es => es.Equals(seq)))
                            {
                                s.Alts.Add(seq);
                                this.Log?.Invoke("\tadd " + rule.NonTerminal.Name + " --> " + seq);
                            }
                        }
                    }
                }
                else
                {
                    this.Log?.Invoke("\tskip " + rule.NonTerminal.Name);
                }

                this.LogSet("P'", P.Values);
            }

            if (!noRootEps)
            {
                var s = this.Rules.FirstOrDefault(r => r.IsStart);

                if (s?.Alts?.Any(ss => ss.IsNullable) ?? false)
                {
                    if (!P.TryGetValue(s.NonTerminal.Name, out var ps))
                        P.Add(s.NonTerminal.Name, new Rule(s.NonTerminal, new Seq[0]) { IsStart = s.IsStart });

                    ps.Alts.Add(new Seq(new List<ITerm> { new Terminal("ε") }));
                }
            }

            return new Grammar(P.Values) { Log = this.Log };
        }

    }
}