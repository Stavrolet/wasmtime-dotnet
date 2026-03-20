using FluentAssertions;
using System.IO;
using System.Reflection;
using Xunit;

namespace Wasmtime.Tests.Components
{
    public class ComponentSerializationTests
    {
        [Fact]
        public void ItSerializesAndDeserializesComponent()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ComponentLoad.wasm");
            using var engine = new Engine();

            using var original = Wasmtime.Component.Component.FromStream(engine, stream);

            var bytes = original.Serialize();
            bytes.Should().NotBeNull();
            bytes.Length.Should().NotBe(0);

            using var deserialized = Wasmtime.Component.Component.Deserialize(engine, bytes);
            deserialized.Should().NotBeNull();
        }

        [Fact]
        public void ItDeserializesFromFile()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ComponentLoad.wasm");
            using var engine = new Engine();

            using var original = Wasmtime.Component.Component.FromStream(engine, stream);

            var bytes = original.Serialize();
            bytes.Should().NotBeNull();
            bytes.Length.Should().NotBe(0);

            var path = Path.GetTempFileName();

            File.WriteAllBytes(path, bytes);

            try
            {
                using var deserialized = Wasmtime.Component.Component.DeserializeFile(engine, path);
                deserialized.Should().NotBeNull();
            }
            catch
            {
                throw;
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}