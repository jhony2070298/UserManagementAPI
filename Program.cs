using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using UserManagementAPI.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

if (string.IsNullOrWhiteSpace(builder.Configuration["Authentication:BearerToken"]))
{
    throw new InvalidOperationException(
        "Configure Authentication:BearerToken (for example, with the Authentication__BearerToken environment variable).");
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseMiddleware<BearerTokenAuthenticationMiddleware>();
app.UseMiddleware<RequestResponseLoggingMiddleware>();
app.UseStatusCodePages();
app.UseHttpsRedirection();

var users = new ConcurrentDictionary<Guid, User>();

app.MapGet("/api/users", () => Results.Ok(users.Values))
    .WithName("GetUsers")
    .WithSummary("Retrieve all users")
    .WithDescription("Returns the current users without imposing an ordering.");

app.MapGet("/api/users/{id:guid}", (Guid id) =>
    users.TryGetValue(id, out var user) ? Results.Ok(user) : Results.NotFound())
    .WithName("GetUserById")
    .WithSummary("Retrieve a user by ID");

app.MapPost("/api/users", (UserRequest? request) =>
{
    var (details, errors) = ValidateUserRequest(request);
    if (details is null)
    {
        return Results.ValidationProblem(errors);
    }

    var user = new User(Guid.NewGuid(), details.Name, details.Email, details.Department);
    users[user.Id] = user;

    return Results.Created($"/api/users/{user.Id}", user);
})
    .WithName("CreateUser")
    .WithSummary("Create a user");

app.MapPut("/api/users/{id:guid}", (Guid id, UserRequest? request) =>
{
    if (!users.TryGetValue(id, out var existingUser))
    {
        return Results.NotFound();
    }

    var (details, errors) = ValidateUserRequest(request);
    if (details is null)
    {
        return Results.ValidationProblem(errors);
    }

    var user = new User(id, details.Name, details.Email, details.Department);
    if (!users.TryUpdate(id, user, existingUser))
    {
        return Results.NotFound();
    }

    return Results.Ok(user);
})
    .WithName("UpdateUser")
    .WithSummary("Update an existing user");

app.MapDelete("/api/users/{id:guid}", (Guid id) =>
    users.TryRemove(id, out _) ? Results.NoContent() : Results.NotFound())
    .WithName("DeleteUser")
    .WithSummary("Delete a user");

if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/diagnostics/exception", () =>
        Task.FromException<IResult>(new InvalidOperationException("Diagnostic exception test.")));
}

app.Run();

static (UserDetails? Details, Dictionary<string, string[]> Errors) ValidateUserRequest(UserRequest? request)
{
    var errors = new Dictionary<string, string[]>();

    if (request is null)
    {
        errors["request"] = ["A request body is required."];
        return (null, errors);
    }

    var name = request.Name?.Trim();
    var email = request.Email?.Trim();
    var department = request.Department?.Trim();

    if (string.IsNullOrWhiteSpace(name))
    {
        errors[nameof(request.Name)] = ["Name is required."];
    }
    else if (name.Length > 100)
    {
        errors[nameof(request.Name)] = ["Name cannot exceed 100 characters."];
    }

    if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email))
    {
        errors[nameof(request.Email)] = ["A valid email address is required."];
    }
    else if (email.Length > 254)
    {
        errors[nameof(request.Email)] = ["Email cannot exceed 254 characters."];
    }

    if (string.IsNullOrWhiteSpace(department))
    {
        errors[nameof(request.Department)] = ["Department is required."];
    }
    else if (department.Length > 100)
    {
        errors[nameof(request.Department)] = ["Department cannot exceed 100 characters."];
    }

    return errors.Count == 0
        ? (new UserDetails(name!, email!, department!), errors)
        : (null, errors);
}

record User(Guid Id, string Name, string Email, string Department);

record UserDetails(string Name, string Email, string Department);

record UserRequest(string? Name, string? Email, string? Department);
