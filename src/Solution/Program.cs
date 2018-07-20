namespace Sample
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var state = new TState();
            state.Load("problems/LA001_tgt.mdl");
        }
    }
}