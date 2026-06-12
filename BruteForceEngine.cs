using System;
using System.Threading;
using System.Threading.Tasks;

namespace BruteForceCracker
{
    public class BruteForceEngine
    {
        private readonly PasswordHasher _hasher = new PasswordHasher();
        private const string CHARSET = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public string BuildMultiThread(string targetHash, Action<string> progressReport, CancellationToken token)
        {
            int maxThreads = Math.Max(1, Environment.ProcessorCount - 1);
            ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = maxThreads, CancellationToken = token };
            
            string foundPassword = null;

            for (int length = 1; length <= 6; length++)
            {
                if (token.IsCancellationRequested || foundPassword != null) break;

                try
                {
                    Parallel.ForEach(CHARSET, options, (firstChar, state) =>
                    {
                        if (foundPassword != null || token.IsCancellationRequested) state.Stop();

                        Search(firstChar.ToString(), length - 1, targetHash, ref foundPassword, progressReport, state, token);
                    });
                }
                catch (OperationCanceledException) { }
            }
            return foundPassword;
        }

        public string BuildSingleThread(string targetHash, CancellationToken token)
        {
            string foundPassword = null;
            for (int length = 1; length <= 6; length++)
            {
                if (token.IsCancellationRequested || foundPassword != null) break;
                SearchSingle("", length, targetHash, ref foundPassword, token);
            }
            return foundPassword;
        }

        private void Search(string current, int lengthLeft, string target, ref string found, Action<string> report, ParallelLoopState state, CancellationToken token)
        {
            if (token.IsCancellationRequested || state.IsStopped || found != null) return;

            if (lengthLeft == 0)
            {
                if (Random.Shared.Next(0, 300000) == 7) report(current);

                if (_hasher.ComputeHash(current) == target)
                {
                    found = current;
                    state.Stop();
                }
                return;
            }

            for (int i = 0; i < CHARSET.Length; i++)
            {
                Search(current + CHARSET[i], lengthLeft - 1, target, ref found, report, state, token);
            }
        }

        private void SearchSingle(string current, int lengthLeft, string target, ref string found, CancellationToken token)
        {
            if (token.IsCancellationRequested || found != null) return;

            if (lengthLeft == 0)
            {
                if (_hasher.ComputeHash(current) == target) found = current;
                return;
            }

            for (int i = 0; i < CHARSET.Length; i++)
            {
                SearchSingle(current + CHARSET[i], lengthLeft - 1, target, ref found, token);
            }
        }
    }
}