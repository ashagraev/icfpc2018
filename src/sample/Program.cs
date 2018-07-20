using System;
using System.IO;
using System.Text;
using sample;

namespace Sample
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            TState state = new TState();
            state.Load("problems/LA001_tgt.mdl");
        }
    }
}