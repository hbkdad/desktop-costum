namespace DesktopRuntime.Core.Tests;

public class ModuleInfoTests
{
    [Fact]
    public void Name_IsDesktopRuntimeCore()
    {
        Assert.Equal("DesktopRuntime.Core", ModuleInfo.Name);
    }
}
