# Karware.Mediator

Karware.Mediator is a small mediator implementation for .NET applications. It sends requests to their corresponding handlers through `IServiceProvider`, keeping callers independent from the classes that handle those requests.

The library supports:

- Requests that do not return a value
- Requests that return a response
- Cancellation tokens on every request
- Handler resolution through the application's dependency-injection container

## Requirements

- .NET 10.0

## Installation

Reference the `Karware.Mediator` project or package from your application. The library itself does not add a dependency-injection container; use the container already configured by your application, such as `Microsoft.Extensions.DependencyInjection`.

To install the package from NuGet, run:

```bash
dotnet add package Karware.Mediator
```

## Usage

Define a request and its handler. A request without a response implements `IRequest`:

```csharp
using Karware.Mediator.Requests;

public sealed record SendWelcomeEmailCommand(string EmailAddress) : IRequest;

public sealed class SendWelcomeEmailCommandHandler : IRequestHandler<SendWelcomeEmailCommand>
{
    public Task HandleAsync(SendWelcomeEmailCommand request, CancellationToken cancellationToken = default)
    {
        // Send the email here.
        return Task.CompletedTask;
    }
}
```

For a request with a return value, implement `IRequest<TResponse>` and `IRequestHandler<TRequest, TResponse>`:

```csharp
using Karware.Mediator.Requests;

public sealed record GetUserNameQuery(Guid UserId) : IRequest<string>;

public sealed class GetUserNameQueryHandler : IRequestHandler<GetUserNameQuery, string>
{
    public Task<string> HandleAsync(GetUserNameQuery request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"User {request.UserId}");
    }
}
```

## Dependency injection

Register the mediator and each handler with the application's service collection. Handler registrations are explicit; Karware.Mediator does not scan assemblies automatically.

```csharp
using Karware.Mediator;
using Karware.Mediator.Requests;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddSingleton<IMediator, Mediator>();
services.AddTransient<IRequestHandler<SendWelcomeEmailCommand>, SendWelcomeEmailCommandHandler>();
services.AddTransient<IRequestHandler<GetUserNameQuery, string>, GetUserNameQueryHandler>();

using var serviceProvider = services.BuildServiceProvider();
var mediator = serviceProvider.GetRequiredService<IMediator>();
```

Choose handler lifetimes that match the dependencies used by your handlers. For example, `AddTransient` is commonly appropriate when handlers depend on scoped services, while handlers used within a web request can be registered as scoped:

```csharp
services.AddScoped<IRequestHandler<GetUserNameQuery, string>, GetUserNameQueryHandler>();
```

## Sending requests

Inject `IMediator` into an application service, controller, endpoint, or other consumer:

```csharp
public sealed class UserService(IMediator mediator)
{
    public Task SendWelcomeEmailAsync(string emailAddress, CancellationToken cancellationToken = default)
    {
        return mediator.Send(new SendWelcomeEmailCommand(emailAddress), cancellationToken);
    }

    public Task<string> GetUserNameAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return mediator.Send(new GetUserNameQuery(userId), cancellationToken);
    }
}
```

When a request is sent, the mediator resolves the matching handler from `IServiceProvider` and invokes its `HandleAsync` method. If no matching handler is registered, `Send` throws `InvalidOperationException`.