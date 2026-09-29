using Credito.Domain;
using Xunit;

namespace Credito.Tests;

public sealed class CustomerReferenceTests
{
    [Fact]
    public void Create_NormalizesReference()
    {
        var reference = CustomerReference.Create("  cliente123  ");

        Assert.Equal("CLIENTE123", reference.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("AB")]
    [InlineData("CLIENTE-001")]
    [InlineData("CLIENTE 001")]
    public void Create_RejectsInvalidReference(string value)
    {
        Assert.Throws<ArgumentException>(
            () => CustomerReference.Create(value));
    }

    [Fact]
    public void SameValue_HasValueEquality()
    {
        var first = CustomerReference.Create("cliente001");
        var second = CustomerReference.Create("CLIENTE001");

        Assert.Equal(first, second);
    }
}
