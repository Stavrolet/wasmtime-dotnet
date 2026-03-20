using FluentAssertions;
using System.Reflection;
using Xunit;

namespace Wasmtime.Tests.Component;

public class ComponentLoadTests
{
    [Fact]
    public void ItLoadsComponentFromEmbeddedResource()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ComponentLoad.wasm")!;
        stream.Should().NotBeNull();

        using var engine = new Engine();
        Wasmtime.Component.Component.FromStream(engine, stream).Should().NotBeNull();
    }
}