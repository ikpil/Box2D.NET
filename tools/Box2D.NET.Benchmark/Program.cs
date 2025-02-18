using System.Reflection;
using BenchmarkDotNet.Running;

public static class Program
{
    public static int Main(string[] args)
    {
        var switcher = BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly());

        if (args == null || args.Length == 0)
        {
            switcher.RunAll();
        }
        else
        {
            switcher.Run(args);
        }

        return 0;
    }
}