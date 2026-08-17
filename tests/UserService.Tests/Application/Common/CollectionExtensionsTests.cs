using UserService.Application.Common.Extensions;

namespace UserService.Tests.Application.Common;

public sealed class CollectionExtensionsTests
{
    [Fact]
    public void IsNullOrEmpty_NullCollection_ReturnsTrue()
    {
        IReadOnlyCollection<int>? collection = null;

        Assert.True(collection.IsNullOrEmpty());
        Assert.False(collection.IsNotNullOrEmpty());
    }

    [Fact]
    public void IsNullOrEmpty_EmptyCollection_ReturnsTrue()
    {
        IReadOnlyCollection<int> collection = Array.Empty<int>();

        Assert.True(collection.IsNullOrEmpty());
        Assert.False(collection.IsNotNullOrEmpty());
    }

    [Fact]
    public void IsNullOrEmpty_PopulatedCollection_ReturnsFalse()
    {
        IReadOnlyCollection<int> collection = new[] { 1 };

        Assert.False(collection.IsNullOrEmpty());
        Assert.True(collection.IsNotNullOrEmpty());
    }
}
