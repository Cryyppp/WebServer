using System.Text.Json.Serialization;
using NegozioVestiti.Models;
using NegozioVestiti.Services;

const string ErroreGenere = "Genere non valido. Usa 'uomo' o 'donna'.";
const string ErroreReparto = "Reparto non valido. Usa 'abbigliamento', 'calzature' o 'accessori'.";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Gli enum (Uomo, Donna, Abbigliamento...) vengono letti e scritti come testo nel JSON
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<ProdottoStore>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/", () => "Server della maison attivo!");

// ---------------------------------------------------------------
// CATALOGO: Genere -> Reparto -> Categoria
// ---------------------------------------------------------------
var catalogo = app.MapGroup("/catalogo");

// GET /catalogo/uomo
// Menu di navigazione: reparti, categorie e numero di prodotti
catalogo.MapGet("/{genere}", (string genere, ProdottoStore store) =>
{
    if (!TryGenere(genere, out var g)) return Results.BadRequest(ErroreGenere);
    return Results.Ok(store.GetMenu(g));
});

// GET /catalogo/uomo/abbigliamento?collezione=Cashmere&ordina=prezzo-desc
catalogo.MapGet("/{genere}/{reparto}", (string genere, string reparto,
    [AsParameters] FiltroProdotti filtro, ProdottoStore store) =>
{
    if (!TryGenere(genere, out var g)) return Results.BadRequest(ErroreGenere);
    if (!TryReparto(reparto, out var r)) return Results.BadRequest(ErroreReparto);

    return Results.Ok(store.GetTutti(g, r, null, filtro));
});

// GET /catalogo/uomo/abbigliamento/giacche-e-blazer
catalogo.MapGet("/{genere}/{reparto}/{categoria}", (string genere, string reparto, string categoria,
    [AsParameters] FiltroProdotti filtro, ProdottoStore store) =>
{
    if (!TryGenere(genere, out var g)) return Results.BadRequest(ErroreGenere);
    if (!TryReparto(reparto, out var r)) return Results.BadRequest(ErroreReparto);

    var slug = categoria.Trim().ToLowerInvariant();
    if (!CatalogoMenu.Esiste(g, r, slug))
        return Results.NotFound($"La categoria '{categoria}' non esiste in {g}/{r}.");

    return Results.Ok(store.GetTutti(g, r, slug, filtro));
});

// ---------------------------------------------------------------
// COLLEZIONI e NUOVI ARRIVI
// ---------------------------------------------------------------

// GET /collezioni   oppure   /collezioni?genere=donna
app.MapGet("/collezioni", (string? genere, ProdottoStore store) =>
{
    if (genere is null) return Results.Ok(store.GetCollezioni());
    if (!TryGenere(genere, out var g)) return Results.BadRequest(ErroreGenere);
    return Results.Ok(store.GetCollezioni(g));
});

// GET /nuovi-arrivi   oppure   /nuovi-arrivi?genere=uomo
app.MapGet("/nuovi-arrivi", (string? genere, ProdottoStore store) =>
{
    Genere? g = null;
    if (genere is not null)
    {
        if (!TryGenere(genere, out var parsed)) return Results.BadRequest(ErroreGenere);
        g = parsed;
    }

    return Results.Ok(store.GetTutti(g, null, null,
        new FiltroProdotti(null, null, null, null, null, true, "novita")));
});

// ---------------------------------------------------------------
// PRODOTTI: gestione (CRUD)
// ---------------------------------------------------------------
var prodotti = app.MapGroup("/prodotti");

prodotti.MapGet("/", ([AsParameters] FiltroProdotti filtro, ProdottoStore store) =>
    store.GetTutti(null, null, null, filtro));

prodotti.MapGet("/{id:int}", (int id, ProdottoStore store) =>
    store.GetPerId(id) is { } p ? Results.Ok(p) : Results.NotFound());

prodotti.MapPost("/", (ProdottoInput input, ProdottoStore store) =>
{
    var errore = Valida(input);
    if (errore is not null) return Results.BadRequest(errore);

    var creato = store.Aggiungi(input);
    return Results.Created($"/prodotti/{creato.Id}", creato);
});

prodotti.MapPut("/{id:int}", (int id, ProdottoInput input, ProdottoStore store) =>
{
    var errore = Valida(input);
    if (errore is not null) return Results.BadRequest(errore);

    return store.Aggiorna(id, input) is { } p ? Results.Ok(p) : Results.NotFound();
});

prodotti.MapDelete("/{id:int}", (int id, ProdottoStore store) =>
    store.Elimina(id) ? Results.NoContent() : Results.NotFound());

app.Run();

// ---------------------------------------------------------------
// Funzioni di supporto (devono stare dopo app.Run();)
// ---------------------------------------------------------------
static bool TryGenere(string valore, out Genere genere) =>
    Enum.TryParse(valore, ignoreCase: true, out genere) && Enum.IsDefined(genere);

static bool TryReparto(string valore, out Reparto reparto) =>
    Enum.TryParse(valore, ignoreCase: true, out reparto) && Enum.IsDefined(reparto);

static string? Valida(ProdottoInput i)
{
    if (string.IsNullOrWhiteSpace(i.Nome)) return "Il nome è obbligatorio.";
    if (i.Prezzo <= 0) return "Il prezzo deve essere maggiore di zero.";
    if (string.IsNullOrWhiteSpace(i.Categoria)) return "La categoria è obbligatoria.";

    var slug = i.Categoria.Trim().ToLowerInvariant();
    if (!CatalogoMenu.Esiste(i.Genere, i.Reparto, slug))
    {
        var valide = string.Join(", ", CatalogoMenu.GetCategorie(i.Genere, i.Reparto).Select(c => c.Slug));
        return $"Categoria non valida per {i.Genere}/{i.Reparto}. Categorie ammesse: {valide}.";
    }

    return null;
}