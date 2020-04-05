using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grammars
{
    class Program
    {
        static ITerm CreateTerm(char ch)
        {
            ITerm result;

            if (char.IsLower(ch))
                result = new Terminal(ch.ToString());
            else
                result = new NonTerminal(ch.ToString());

            return result;
        }

        static Grammar ParseGrammar(string text)
        {
            var grammar = new Grammar(
                text.Replace(" ", "").Split('\n')
                    .Select(rule => rule.Split(":=".ToArray<char>(), StringSplitOptions.RemoveEmptyEntries))
                    .Select(t => new Rule(
                            new NonTerminal(t.First()),
                            t.Last().Trim().Split('|').Select(s => new Seq(s.Select(CreateTerm)))
                    ))
            );
            grammar.Rules.First().IsStart = true;
            grammar.Log = Console.WriteLine;
            return grammar;
        }



        class Counter
        {
            public int Module { get; }

            private List<int> _nums;

            public Counter(int module)
            {
                this.Module = module;
                _nums = new List<int>();
            }

            public void Increment()
            {
                if (_nums.Count == 0)
                    _nums.Add(0);

                _nums[0]++;
                for (int i = 0; i < _nums.Count; i++)
                {
                    if (_nums[i] >= this.Module)
                    {
                        _nums[i] = 0;
                        if (i + 1 < _nums.Count)
                        {
                            _nums[i + 1]++;
                        }
                        else
                        {
                            _nums.Add(1);
                        }
                    }
                }
            }

            public override string ToString()
            {
                string result = "";
                for (int i = _nums.Count - 1; i >= 0; i--)
                {
                    result = string.Concat(result, _nums[i] < 10 ? _nums[i].ToString() : ((char)(_nums[i] - 10 + 'a')).ToString());
                }

                return result;
            }
        }

        static Grammar MakeGrammar()
        {
            var I = new NonTerminal("I");
            var T = new NonTerminal("T");
            var M = new NonTerminal("M");
            var K = new NonTerminal("K");

            var a = new Terminal("a");
            var b = new Terminal("b");
            var c = new Terminal("c");
            var b1 = new Terminal("(");
            var b2 = new Terminal(")");
            var s1 = new Terminal("+");
            var s2 = new Terminal("-");
            var s3 = new Terminal("*");
            var s4 = new Terminal("/");

            var rules = new[]
            {
                I[T],
                I[I, s1, T],
                I[I, s2, T],
                T[M],
                T[T, s3, M],
                T[T, s4, M],
                M[b1, I, b2],
                M[K],
                K[a],
                K[b],
                K[c],
            };

            var grammar = new Grammar(
                rules.GroupBy(r => r.NonTerminal.Name)
                     .Select(g => new Rule(g.First().NonTerminal, g.SelectMany(r => r.Alts)))
            ) { Log = Console.WriteLine };
            grammar.Rules.First().IsStart = true;
            return grammar;
        }

        static void Main(string[] args)
        {

            Console.OutputEncoding = Encoding.UTF8;

            var g4 = MakeGrammar();


            string s2 = @"S := ABC | aBC
                          A := aA | BC
                          B := bB | ε
                          C := cC | ε";

            //string s1 = @"S := A | B
            //                A := aB | bS | b
            //                B := AB | Ba | AS | b
            //                C := b";

            string s1 = @"S := aC | bA
                        A := cAB
                        B := aC
                        C := bA | d";

            string s3 = @"S := Aa | B
                          A := a | bc | B
                          B := A | bb";

            /*string s4 = @"S := ABC | aA| bB | ε
                          A := B | ε | aB | b
                          B := C | ε | bA | a
                          C := B | bS | ε";*/


            var g1 = ParseGrammar(s1);
            var g2 = ParseGrammar(s2);
            var g3 = ParseGrammar(s3);
            //var g40 = ParseGrammar(s4);

            Console.WriteLine("--------------------------------------------");
            Console.WriteLine("Task1");
            Console.WriteLine("Input:");
            Console.WriteLine(g1.CollectDebugInfo());

            var g11 = g1.RemoveNonProducingRules();
            Console.WriteLine();
            Console.WriteLine(g11.CollectDebugInfo());

            var g111 = g11.RemoveNonReachableRules();
            Console.WriteLine();
            Console.WriteLine(g111.CollectDebugInfo());

            Console.WriteLine("--------------------------------------------");
            Console.WriteLine("Task2");
            Console.WriteLine("Input:");
            Console.WriteLine(g2.CollectDebugInfo());

            var g22 = g2.RemoveEpsilonRules();
            Console.WriteLine();
            Console.WriteLine(g22.CollectDebugInfo());

            Console.WriteLine("--------------------------------------------");
            Console.WriteLine("Task3");
            Console.WriteLine("Input:");
            Console.WriteLine(g3.CollectDebugInfo());

            var g33 = g2.RemoveChainingRules();
            Console.WriteLine();
            Console.WriteLine(g33.CollectDebugInfo());
            
            Console.WriteLine("--------------------------------------------");
            Console.WriteLine("Task4");
            Console.WriteLine("Input:");
            Console.WriteLine(g4.CollectDebugInfo());

            var g41 = g4.RemoveNonProducingRules();
            Console.WriteLine();
            Console.WriteLine(g41.CollectDebugInfo());

            var g42 = g41.RemoveNonReachableRules();
            Console.WriteLine();
            Console.WriteLine(g42.CollectDebugInfo());

            var g43 = g42.RemoveEpsilonRules();
            Console.WriteLine();
            Console.WriteLine(g43.CollectDebugInfo());

            var g44 = g43.RemoveChainingRules(true);
            Console.WriteLine();
            Console.WriteLine(g44.CollectDebugInfo());

            Console.WriteLine();
        }
    }
}
