using Karware.Mediator.Requests;
using NSubstitute;

namespace Karware.Mediator.Tests;

[TestFixture]
public sealed class MediatorTests
{
    private IServiceProvider _serviceProvider = null!;
    private Mediator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _serviceProvider = Substitute.For<IServiceProvider>();
        _sut = new Mediator(_serviceProvider);
    }

    [Test]
    public void Send_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => _sut.Send(null!));
    }

    [Test]
    public void Send_WhenHandlerIsNotRegistered_ThrowsInvalidOperationException()
    {
        var request = new TestRequest();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => _sut.Send(request));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Does.Contain(request.GetType().FullName));
    }

    [Test]
    public async Task Send_WhenHandlerIsRegistered_InvokesHandlerWithRequestAndToken()
    {
        var request = new TestRequest();
        var cancellationToken = new CancellationTokenSource().Token;
        var handler = Substitute.For<IRequestHandler<TestRequest>>();

        _serviceProvider.GetService(typeof(IRequestHandler<TestRequest>)).Returns(handler);

        await _sut.Send(request, cancellationToken);

        _ = _serviceProvider.Received(1).GetService(typeof(IRequestHandler<TestRequest>));
        await handler.Received(1).HandleAsync(request, cancellationToken);
    }

    [Test]
    public void SendOfTResponse_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => _sut.Send((IRequest<int>)null!));
    }

    [Test]
    public void SendOfTResponse_WhenHandlerIsNotRegistered_ThrowsInvalidOperationException()
    {
        var request = new TestResponseRequest();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => _sut.Send<int>(request));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Does.Contain(request.GetType().FullName));
    }

    [Test]
    public async Task SendOfTResponse_WhenHandlerIsRegistered_ReturnsHandlerResult()
    {
        var request = new TestResponseRequest();
        var cancellationToken = new CancellationTokenSource().Token;
        var expectedResponse = 42;
        var handler = Substitute.For<IRequestHandler<TestResponseRequest, int>>();

        handler.HandleAsync(request, cancellationToken).Returns(expectedResponse);
        _serviceProvider.GetService(typeof(IRequestHandler<TestResponseRequest, int>)).Returns(handler);

        var response = await _sut.Send<int>(request, cancellationToken);

        Assert.That(response, Is.EqualTo(expectedResponse));
        _ = _serviceProvider.Received(1).GetService(typeof(IRequestHandler<TestResponseRequest, int>));
        await handler.Received(1).HandleAsync(request, cancellationToken);
    }

    public sealed class TestRequest : IRequest;

    public sealed class TestResponseRequest : IRequest<int>;
}