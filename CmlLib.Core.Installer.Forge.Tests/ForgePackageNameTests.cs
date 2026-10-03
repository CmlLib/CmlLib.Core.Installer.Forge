namespace CmlLib.Core.Installer.Forge.Tests;

public class ForgePackageNameTests
{
    [Theory]
    [InlineData(
        "de.oceanlabs.mcp:mcp_config:1.16.2-20200812.004259", 
        "de/oceanlabs/mcp/mcp_config/1.16.2-20200812.004259/mcp_config-1.16.2-20200812.004259.jar")]
    [InlineData("com.google.code.gson:gson:2.10.1@jar", "com/google/code/gson/gson/2.10.1/gson-2.10.1.jar")]
    [InlineData("net.neoforged:neoform:1.21.1-20240808.144430@zip", "net/neoforged/neoform/1.21.1-20240808.144430/neoform-1.21.1-20240808.144430.zip")]
    [InlineData("net.neoforged:neoform:1.21.1-20240808.144430:mappings@txt", "net/neoforged/neoform/1.21.1-20240808.144430/neoform-1.21.1-20240808.144430-mappings.txt")]
    public void test_GetPath(string input, string expected)
    {
        var actual = ForgePackageName.GetPath(input, '/');
        Assert.Equal(expected, actual);
    }
}
