using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;

namespace InventoryX.Common.Tests.AutoFixtureExtensions;

public class AutoDomainDataAttribute() : AutoDataAttribute(() => new Fixture().Customize(new AutoMoqCustomization()));

public class InlineAutoDomainDataAttribute(params object[] values) : InlineAutoDataAttribute(values)
{
    
}