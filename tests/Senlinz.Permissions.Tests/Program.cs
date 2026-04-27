using System.Reflection;

var failures = 0;
var tests = Assembly.GetExecutingAssembly()
    .GetTypes()
    .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        .Where(method => method.GetCustomAttribute<Xunit.FactAttribute>() is not null)
        .Select(method => new { Type = type, Method = method }))
    .OrderBy(test => test.Type.FullName, StringComparer.Ordinal)
    .ThenBy(test => test.Method.Name, StringComparer.Ordinal)
    .ToArray();

foreach (var test in tests)
{
    try
    {
        var instance = Activator.CreateInstance(test.Type);
        test.Method.Invoke(instance, Array.Empty<object>());
        Console.WriteLine("PASS " + test.Type.Name + "." + test.Method.Name);
    }
    catch (TargetInvocationException exception) when (exception.InnerException is not null)
    {
        failures++;
        Console.WriteLine("FAIL " + test.Type.Name + "." + test.Method.Name);
        Console.WriteLine(exception.InnerException.GetType().Name + ": " + exception.InnerException.Message);
    }
    catch (Exception exception)
    {
        failures++;
        Console.WriteLine("FAIL " + test.Type.Name + "." + test.Method.Name);
        Console.WriteLine(exception.GetType().Name + ": " + exception.Message);
    }
}

Console.WriteLine();
Console.WriteLine($"{tests.Length - failures}/{tests.Length} tests passed.");
return failures == 0 ? 0 : 1;
