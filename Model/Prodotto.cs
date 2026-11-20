namespace NegozioVestiti.Models;

public enum Genere
{
    Uomo,
    Donna
}

public enum Reparto
{
    Abbigliamento,
    Calzature,
    Accessori
}

public class Prodotto
{
    public int Id { get; set; }
    public string Sku { get; set; } = "";            // codice articolo, es. UOM-00001
    public string Nome { get; set; } = "";
    public string Descrizione { get; set; } = "";
    public Genere Genere { get; set; }
    public Reparto Reparto { get; set; }
    public string Categoria { get; set; } = "";      // slug, es. "giacche-e-blazer"
    public string Collezione { get; set; } = "";     // es. "Cashmere", "Lino"
    public string Materiale { get; set; } = "";      // es. "100% cashmere"
    public decimal Prezzo { get; set; }
    public List<string> Colori { get; set; } = new();
    public List<string> Taglie { get; set; } = new();
    public bool NuovoArrivo { get; set; }
}

// Dati che il client invia in POST/PUT (Id e Sku li decide il server)
public record ProdottoInput(
    string Nome,
    string? Descrizione,
    Genere Genere,
    Reparto Reparto,
    string Categoria,
    string? Collezione,
    string? Materiale,
    decimal Prezzo,
    List<string>? Colori,
    List<string>? Taglie,
    bool NuovoArrivo);

// Filtri opzionali che il client può passare nell'URL (?collezione=Cashmere&ordina=prezzo-desc ...)
public record FiltroProdotti(
    string? Collezione,
    string? Colore,
    string? Taglia,
    decimal? PrezzoMin,
    decimal? PrezzoMax,
    bool? Nuovi,
    string? Ordina);

// Risposta del "menu" di navigazione del catalogo
public record CategoriaMenu(string Slug, string Nome, int Prodotti);
public record RepartoMenu(string Reparto, int Prodotti, List<CategoriaMenu> Categorie);