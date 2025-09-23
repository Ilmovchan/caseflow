using CaseFlow.BLL.Dto.Client;

namespace CaseFlow.API.Endpoints;

public static class ClientsEndpoints
{
    private const string GetClientEndpoint = "GetClient";

    private static readonly List<ClientDto> Clients =
    [
        new(
            1,
            "Illia",
            "Movchan",
            "Oleksandrovich"
        ),
        new(
            2,
            "Vasya",
            "Filkin",
            "Popovich"
        ),
    ];

    public static RouteGroupBuilder MapClientsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("clients")
            .WithParameterValidation();
        
        group.MapGet("/", () => Clients);

        group.MapGet("/{id}", (int id) =>
            {
                var client = Clients.Find(c => c.Id == id);

                return client is null ? Results.NotFound() : Results.Ok(client);
            })
            .WithName(GetClientEndpoint);

        group.MapPost("/", (CreateClientDto newClient) =>
        {
            ClientDto client = new(
                Clients.Count + 1,
                newClient.FirstName,
                newClient.LastName,
                newClient.FatherName
            );

            Clients.Add(client);

            return Results.CreatedAtRoute(GetClientEndpoint, new { id = client.Id }, client);
        });

        group.MapPut("/{id}", (int id, UpdateClientDto updatedClient) =>
        {
            var index = Clients.FindIndex(c => c.Id == id);

            if (index == -1)
                return Results.NotFound();

            Clients[index] = new ClientDto(
                id,
                updatedClient.FirstName,
                updatedClient.LastName,
                updatedClient.FatherName
            );

            return Results.NoContent();
        });

        group.MapDelete("/{id}", (int id) =>
        {
            Clients.RemoveAll(c => c.Id == id);
            return Results.NoContent();
        });

        return group;
    }
}