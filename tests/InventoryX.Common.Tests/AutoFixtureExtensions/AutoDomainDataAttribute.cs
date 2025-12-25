using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;

namespace InventoryX.Common.Tests.AutoFixtureExtensions;

public class AutoDomainDataAttribute() : AutoDataAttribute(() =>
{
    var fixture = new Fixture();
    fixture.Customize(new AutoMoqCustomization());
    fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
        .ForEach(b => fixture.Behaviors.Remove(b));
    fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    return fixture;
});

public class InlineAutoDomainDataAttribute(params object[] values) : InlineAutoDataAttribute(values)
{

}
