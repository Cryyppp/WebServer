namespace NegozioVestiti.Models;

public record CategoriaInfo(string Slug, string Nome);

public static class CatalogoMenu
{
    private static readonly Dictionary<(Genere, Reparto), CategoriaInfo[]> _menu = new()
    {
        // ---------------- UOMO ----------------
        [(Genere.Uomo, Reparto.Abbigliamento)] = new CategoriaInfo[]
        {
            new("abiti", "Abiti"),
            new("giacche-e-blazer", "Giacche e Blazer"),
            new("cappotti-e-capispalla", "Cappotti e Capispalla"),
            new("camicie", "Camicie"),
            new("maglieria", "Maglieria"),
            new("t-shirt-e-polo", "T-shirt e Polo"),
            new("pantaloni", "Pantaloni"),
            new("jeans", "Jeans"),
        },
        [(Genere.Uomo, Reparto.Calzature)] = new CategoriaInfo[]
        {
            new("sneakers", "Sneakers"),
            new("mocassini", "Mocassini"),
            new("stringate", "Stringate"),
            new("stivali", "Stivali"),
        },
        [(Genere.Uomo, Reparto.Accessori)] = new CategoriaInfo[]
        {
            new("cravatte-e-pochette", "Cravatte e Pochette"),
            new("sciarpe-e-foulard", "Sciarpe e Foulard"),
            new("cinture", "Cinture"),
            new("borse-e-valigeria", "Borse e Valigeria"),
            new("piccola-pelletteria", "Piccola Pelletteria"),
            new("cappelli-e-guanti", "Cappelli e Guanti"),
        },

        // ---------------- DONNA ----------------
        [(Genere.Donna, Reparto.Abbigliamento)] = new CategoriaInfo[]
        {
            new("abiti", "Abiti"),
            new("giacche-e-blazer", "Giacche e Blazer"),
            new("cappotti-e-capispalla", "Cappotti e Capispalla"),
            new("camicie-e-bluse", "Camicie e Bluse"),
            new("maglieria", "Maglieria"),
            new("gonne", "Gonne"),
            new("pantaloni", "Pantaloni"),
            new("t-shirt-e-top", "T-shirt e Top"),
        },
        [(Genere.Donna, Reparto.Calzature)] = new CategoriaInfo[]
        {
            new("sneakers", "Sneakers"),
            new("mocassini-e-ballerine", "Mocassini e Ballerine"),
            new("sandali", "Sandali"),
            new("stivali", "Stivali"),
        },
        [(Genere.Donna, Reparto.Accessori)] = new CategoriaInfo[]
        {
            new("borse", "Borse"),
            new("sciarpe-e-foulard", "Sciarpe e Foulard"),
            new("cinture", "Cinture"),
            new("piccola-pelletteria", "Piccola Pelletteria"),
            new("cappelli-e-guanti", "Cappelli e Guanti"),
        },
    };

    public static CategoriaInfo[] GetCategorie(Genere genere, Reparto reparto) =>
        _menu.TryGetValue((genere, reparto), out var categorie) ? categorie : Array.Empty<CategoriaInfo>();

    public static bool Esiste(Genere genere, Reparto reparto, string slug) =>
        GetCategorie(genere, reparto).Any(c => c.Slug == slug);
}