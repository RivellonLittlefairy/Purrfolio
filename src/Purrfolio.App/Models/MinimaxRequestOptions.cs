namespace Purrfolio.App.Models;

public sealed record MinimaxRequestOptions(
    string Endpoint,
    string ApiKey,
    string Model,
    bool UseBearerToken = true);
