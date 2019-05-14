using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Brainfuck.NET
{
    public class Precompiler
    {
        public struct OPS
        {
            public States s;
            public int v;
        }

        private List<OPS> ops = new List<OPS>();

        public enum States
        {
            DATA,
            PTR,
            OPEN_LOOP,
            CLOSE_LOOP,
            PRINT,
            READ,
            NONE,
        }

        private void StateChanged(States s, int value)
        {
            var o = new OPS
            {
                s = s,
                v = value
            };

            ops.Add(o);
        }

        public Precompiler(string path)
        {

            var bfSource = File.ReadAllText(path);

            var value = 0;
            var state = States.NONE;

            foreach (var symbol in bfSource)
            {
                switch (symbol)
                {
                    case '+':
                        if (state == States.DATA)
                        {
                            value++;
                        }
                        else
                        {
                            StateChanged(state,value);
                            value = 1;
                            state = States.DATA;
                        }
                        break;
                    case '-':
                        if (state == States.DATA)
                        {
                            value--;
                        }
                        else
                        {
                            StateChanged(state, value);
                            value = -1;
                            state = States.DATA;
                        }
                        break;
                    case '<':
                        if (state == States.PTR)
                        {
                            value--;
                        }
                        else
                        {
                            StateChanged(state, value);
                            value = -1;
                            state = States.PTR;
                        }
                        break;
                    case '>':
                        if (state == States.PTR)
                        {
                            value++;
                        }
                        else
                        {
                            StateChanged(state, value);
                            value = 1;
                            state = States.PTR;
                        }
                        break;
                    case '[':
                        StateChanged(state, value);
                        state = States.OPEN_LOOP;
                        value = 0;
                        break;
                    case ']':
                        StateChanged(state, value);
                        state = States.CLOSE_LOOP;
                        value = 0;
                        break;
                    case '.':
                        StateChanged(state, value);
                        state = States.PRINT;
                        value = 0;
                        break;
                    case ',':
                        StateChanged(state, value);
                        state = States.READ;
                        value = 0;
                        break;
                }
            }
            StateChanged(state, value);
        }

        public IEnumerable<OPS> Precompiled
        {
            get
            {
                return this.ops;
            }
        }

    }
}
