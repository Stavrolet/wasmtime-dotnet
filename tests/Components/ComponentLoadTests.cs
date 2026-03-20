using FluentAssertions;
using System.Reflection;
using Xunit;
using Wasmtime.Components;

namespace Wasmtime.Tests.Components;

public class ComponentLoadTests
{
    [Fact]
    public void ItLoadsComponentFromEmbeddedResource()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ComponentLoad.wasm")!;
        stream.Should().NotBeNull();

        using var engine = new Engine();
        Component.FromStream(engine, stream).Should().NotBeNull();
    }
}