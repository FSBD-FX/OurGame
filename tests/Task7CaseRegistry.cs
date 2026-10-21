using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Knight.Tests;

public enum Task7CaseDisposition
{
    Unclassified,
    Executable,
    Retired
}

public sealed record Task7Case(
    string Name,
    Task7CaseDisposition Disposition,
    Func<Task> Execute);

public sealed record Task7CaseRun(
    IReadOnlySet<string> All,
    IReadOnlySet<string> Executable,
    IReadOnlySet<string> Retired,
    IReadOnlySet<string> Started,
    IReadOnlySet<string> Completed);

public static class Task7CaseRegistry
{
    public static Task7Case Case(string suite, string name, Func<Task> execute) =>
        new($"{suite}.{name}", Task7CaseDisposition.Executable,
            execute ?? throw new ArgumentNullException(nameof(execute)));

    public static Task7Case Case(string suite, string name, Action execute) =>
        Case(suite, name, () =>
        {
            execute();
            return Task.CompletedTask;
        });

    public static Task7Case Retired(string suite, string name) =>
        new($"{suite}.{name}", Task7CaseDisposition.Retired, null);

    public static async Task<Task7CaseRun> Execute(IReadOnlyList<Task7Case> cases)
    {
        if (cases == null) throw new ArgumentNullException(nameof(cases));
        var all = new HashSet<string>(StringComparer.Ordinal);
        var executable = new HashSet<string>(StringComparer.Ordinal);
        var retired = new HashSet<string>(StringComparer.Ordinal);
        foreach (Task7Case item in cases)
        {
            TestAssert.True(item != null && !string.IsNullOrWhiteSpace(item.Name),
                "every Task7 case has a non-empty explicit name");
            TestAssert.True(all.Add(item.Name), $"Task7 registry rejects duplicate case: {item.Name}");
            switch (item.Disposition)
            {
                case Task7CaseDisposition.Executable:
                    TestAssert.True(item.Execute != null,
                        $"executable Task7 case has a delegate: {item.Name}");
                    executable.Add(item.Name);
                    break;
                case Task7CaseDisposition.Retired:
                    TestAssert.True(item.Execute == null,
                        $"retired Task7 case has no delegate: {item.Name}");
                    retired.Add(item.Name);
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Task7 case is unclassified: {item.Name}");
            }
        }
        var classified = new HashSet<string>(executable, StringComparer.Ordinal);
        classified.UnionWith(retired);
        TestAssert.Equal(Join(all), Join(classified),
            "Task7 registry classifies every named case exactly once");

        var started = new HashSet<string>(StringComparer.Ordinal);
        var completed = new HashSet<string>(StringComparer.Ordinal);
        foreach (Task7Case item in cases.Where(item => item.Disposition ==
                     Task7CaseDisposition.Executable))
        {
            TestAssert.True(started.Add(item.Name), $"case starts once: {item.Name}");
            await item.Execute();
            TestAssert.True(completed.Add(item.Name), $"case completes once: {item.Name}");
        }
        TestAssert.Equal(Join(executable), Join(started),
            "executable case registry and started delegates are identical");
        TestAssert.Equal(Join(executable), Join(completed),
            "executable case registry and completed delegates are identical");
        return new Task7CaseRun(all, executable, retired, started, completed);
    }

    private static string Join(IEnumerable<string> names) =>
        string.Join("|", names.OrderBy(name => name, StringComparer.Ordinal));
}
