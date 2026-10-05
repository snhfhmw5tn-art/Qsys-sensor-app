using System.Reflection;

namespace Qsys.SensorApp.Core.Tests;

[TestClass]
public sealed class ArchitectureTests
{
    [TestMethod]
    public void TestThat_core_has_no_project_layer_dependencies()
    {
        var projectReferences = Assembly.Load("Qsys.SensorApp.Core")
            .GetReferencedAssemblies()
            .Where(assembly => assembly.Name?.StartsWith("Qsys.SensorApp.", StringComparison.Ordinal) == true)
            .Select(assembly => assembly.Name)
            .ToArray();

        Assert.IsEmpty(projectReferences, string.Join(", ", projectReferences));
    }
}
