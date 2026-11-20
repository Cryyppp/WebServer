using System.Text.Json;
using System.Text.Json.Serialization;
using NegozioVestiti.Models;

namespace NegozioVestiti.Services;

public class ProdottoStore
{
    private readonly string _percorso;
    private readonly object _lock = new();
    private List<Prodotto> _prodotti = new();

    private static readonly JsonSerializerOptions _opzioni = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ProdottoStore(IWebHostEnvironment env)
    {
        var cartella = Path.Combine(env.ContentRootPath, "Data");
        Directory.CreateDirectory(cartella);
        _percorso = Path.Combine(cartella, "prodotti.json");
        Carica();
    }

    // ---------- Lettura ----------

    public List<Prodotto> GetTutti(Genere? genere = null, Reparto? reparto = null,
                                   string? categoria = null, FiltroProdotti? f = null)
    {
        lock (_lock)
        {
            IEnumerable<Prodotto> q = _prodotti;

            if (genere is not null) q = q.Where(p => p.Genere == genere);
            if (reparto is not null) q = q.Where(p => p.Reparto == reparto);
            if (!string.IsNullOrWhiteSpace(categoria))
                q = q.Where(p => p.Categoria == categoria.Trim().ToLowerInvariant());

            if (f is not null)
            {
                if (!string.IsNullOrWhiteSpace(f.Collezione))
                    q = q.Where(p => p.Collezione.Equals(f.Collezione, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(f.Colore))
                    q = q.Where(p => p.Colori.Any(c => c.Equals(f.Colore, StringComparison.OrdinalIgnoreCase)));
                if (!string.IsNullOrWhiteSpace(f.Taglia))
                    q = q.Where(p => p.Taglie.Any(t => t.Equals(f.Taglia, StringComparison.OrdinalIgnoreCase)));
                if (f.PrezzoMin is not null) q = q.Where(p => p.Prezzo >= f.PrezzoMin);
                if (f.PrezzoMax is not null) q = q.Where(p => p.Prezzo <= f.PrezzoMax);
                if (f.Nuovi == true) q = q.Where(p => p.NuovoArrivo);

                q = f.Ordina?.ToLowerInvariant() switch
                {
                    "prezzo-asc" => q.OrderBy(p => p.Prezzo),
                    "prezzo-desc" => q.OrderByDescending(p => p.Prezzo),
                    "nome" => q.OrderBy(p => p.Nome),
                    "novita" => q.OrderByDescending(p => p.Id),
                    _ => q.OrderBy(p => p.Id)
                };
            }

            return q.ToList();
        }
    }

    public Prodotto? GetPerId(int id)
    {
        lock (_lock)
        {
            return _prodotti.FirstOrDefault(p => p.Id == id);
        }
    }

    // Menu di navigazione: reparti e categorie con il numero di prodotti
    public List<RepartoMenu> GetMenu(Genere genere)
    {
        lock (_lock)
        {
            return Enum.GetValues<Reparto>().Select(r =>
            {
                var categorie = CatalogoMenu.GetCategorie(genere, r)
                    .Select(c => new CategoriaMenu(
                        c.Slug,
                        c.Nome,
                        _prodotti.Count(p => p.Genere == genere && p.Reparto == r && p.Categoria == c.Slug)))
                    .ToList();

                return new RepartoMenu(r.ToString(), categorie.Sum(c => c.Prodotti), categorie);
            }).ToList();
        }
    }

    public List<string> GetCollezioni(Genere? genere = null)
    {
        lock (_lock)
        {
            return _prodotti
                .Where(p => genere is null || p.Genere == genere)
                .Select(p => p.Collezione)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }
    }

    // ---------- Scrittura ----------

    public Prodotto Aggiungi(ProdottoInput input)
    {
        lock (_lock)
        {
            var id = _prodotti.Count == 0 ? 1 : _prodotti.Max(p => p.Id) + 1;
            var nuovo = new Prodotto { Id = id, Sku = GeneraSku(input.Genere, id) };
            Applica(nuovo, input);
            _prodotti.Add(nuovo);
            Salva();
            return nuovo;
        }
    }

    public Prodotto? Aggiorna(int id, ProdottoInput input)
    {
        lock (_lock)
        {
            var p = _prodotti.FirstOrDefault(x => x.Id == id);
            if (p is null) return null;

            Applica(p, input);
            Salva();
            return p;
        }
    }

    public bool Elimina(int id)
    {
        lock (_lock)
        {
            var rimossi = _prodotti.RemoveAll(p => p.Id == id);
            if (rimossi == 0) return false;
            Salva();
            return true;
        }
    }

    private static void Applica(Prodotto p, ProdottoInput i)
    {
        p.Nome = i.Nome.Trim();
        p.Descrizione = i.Descrizione?.Trim() ?? "";
        p.Genere = i.Genere;
        p.Reparto = i.Reparto;
        p.Categoria = i.Categoria.Trim().ToLowerInvariant();
        p.Collezione = i.Collezione?.Trim() ?? "";
        p.Materiale = i.Materiale?.Trim() ?? "";
        p.Prezzo = i.Prezzo;
        p.Colori = i.Colori ?? new List<string>();
        p.Taglie = i.Taglie ?? new List<string>();
        p.NuovoArrivo = i.NuovoArrivo;
    }

    private static string GeneraSku(Genere genere, int id) =>
        $"{(genere == Genere.Uomo ? "UOM" : "DON")}-{id:D5}";

    // ---------- File JSON ----------

    private void Carica()
    {
        if (File.Exists(_percorso))
        {
            var json = File.ReadAllText(_percorso);
            _prodotti = JsonSerializer.Deserialize<List<Prodotto>>(json, _opzioni) ?? new();
            return;
        }

        _prodotti = CreaDatiDiEsempio();
        Salva();
    }

    private void Salva()
    {
        // Scrivo su un file temporaneo e poi lo sostituisco:
        // se il server si interrompe a metà, il file originale non si corrompe.
        var json = JsonSerializer.Serialize(_prodotti, _opzioni);
        var temp = _percorso + ".tmp";
        File.WriteAllText(temp, json);
        File.Move(temp, _percorso, overwrite: true);
    }

    private static List<Prodotto> CreaDatiDiEsempio()
    {
        Prodotto P(int id, string nome, string descrizione, Genere g, Reparto r, string categoria,
                   string collezione, string materiale, decimal prezzo,
                   string[] colori, string[] taglie, bool nuovo = false) => new()
        {
            Id = id,
            Sku = GeneraSku(g, id),
            Nome = nome,
            Descrizione = descrizione,
            Genere = g,
            Reparto = r,
            Categoria = categoria,
            Collezione = collezione,
            Materiale = materiale,
            Prezzo = prezzo,
            Colori = colori.ToList(),
            Taglie = taglie.ToList(),
            NuovoArrivo = nuovo
        };

        var tagliUomo = new[] { "46", "48", "50", "52", "54", "56" };
        var tagliDonna = new[] { "38", "40", "42", "44", "46" };
        var unica = new[] { "Unica" };

        return new List<Prodotto>
        {
            // ---------- UOMO ----------
            P(1, "Abito monopetto in lana fresca",
              "Abito sartoriale a due bottoni in lana fresca leggera, ideale per la stagione calda.",
              Genere.Uomo, Reparto.Abbigliamento, "abiti", "Lana Fresca", "100% lana",
              2190m, new[] { "grigio antracite", "blu notte" }, tagliUomo),

            P(2, "Blazer destrutturato in lino",
              "Giacca morbida e senza fodera, in lino naturale con tasche applicate.",
              Genere.Uomo, Reparto.Abbigliamento, "giacche-e-blazer", "Lino", "100% lino",
              1850m, new[] { "sabbia", "blu" }, tagliUomo, nuovo: true),

            P(3, "Cappotto doppiopetto in cashmere",
              "Cappotto lungo doppiopetto, tessuto in puro cashmere dalla mano morbidissima.",
              Genere.Uomo, Reparto.Abbigliamento, "cappotti-e-capispalla", "Cashmere", "100% cashmere",
              4900m, new[] { "cammello", "grigio" }, tagliUomo),

            P(4, "Camicia in cotone popeline",
              "Camicia classica con collo italiano e polsini doppi, in popeline di cotone egiziano.",
              Genere.Uomo, Reparto.Abbigliamento, "camicie", "Essenziali", "100% cotone",
              395m, new[] { "bianco", "azzurro" }, new[] { "38", "39", "40", "41", "42", "43", "44" }),

            P(5, "Dolcevita in cashmere e seta",
              "Maglia a collo alto leggera, in filato misto cashmere e seta.",
              Genere.Uomo, Reparto.Abbigliamento, "maglieria", "Cashmere", "70% cashmere, 30% seta",
              1250m, new[] { "blu notte", "grigio perla" }, tagliUomo),

            P(6, "Pantaloni in flanella di lana",
              "Pantaloni dritti con doppia pince, in flanella di lana pettinata.",
              Genere.Uomo, Reparto.Abbigliamento, "pantaloni", "Lana Fresca", "100% lana",
              690m, new[] { "grigio medio", "antracite" }, tagliUomo),

            P(7, "Mocassino in suede",
              "Mocassino foderato in pelle, in suede di vitello con suola in gomma leggera.",
              Genere.Uomo, Reparto.Calzature, "mocassini", "Pelle e Suede", "Suede di vitello",
              780m, new[] { "tortora", "marrone" }, new[] { "40", "41", "42", "43", "44", "45" }, nuovo: true),

            P(8, "Cravatta in seta jacquard",
              "Cravatta sette pieghe con micro disegno jacquard, tessuta in Italia.",
              Genere.Uomo, Reparto.Accessori, "cravatte-e-pochette", "Seta", "100% seta",
              220m, new[] { "blu", "bordeaux" }, unica),

            // ---------- DONNA ----------
            P(9, "Abito midi in seta",
              "Abito fluido a mezza gamba con scollo a V, in seta pura.",
              Genere.Donna, Reparto.Abbigliamento, "abiti", "Seta", "100% seta",
              2450m, new[] { "avorio", "verde salvia" }, tagliDonna, nuovo: true),

            P(10, "Cappotto in cashmere con cintura",
              "Cappotto avvolgente con cintura in vita, in cashmere double-face.",
              Genere.Donna, Reparto.Abbigliamento, "cappotti-e-capispalla", "Cashmere", "100% cashmere",
              5200m, new[] { "cammello", "grigio chiaro" }, tagliDonna),

            P(11, "Blusa in seta lavata",
              "Blusa morbida con maniche ampie, in seta lavata dalla mano setosa.",
              Genere.Donna, Reparto.Abbigliamento, "camicie-e-bluse", "Seta", "100% seta",
              890m, new[] { "bianco", "cipria" }, tagliDonna),

            P(12, "Cardigan in baby cashmere",
              "Cardigan a trecce con bottoni in madreperla, in baby cashmere.",
              Genere.Donna, Reparto.Abbigliamento, "maglieria", "Cashmere", "100% baby cashmere",
              1450m, new[] { "beige", "rosa antico" }, tagliDonna),

            P(13, "Pantaloni palazzo in lino",
              "Pantaloni a gamba larga e vita alta, in lino leggero.",
              Genere.Donna, Reparto.Abbigliamento, "pantaloni", "Lino", "100% lino",
              720m, new[] { "sabbia", "bianco" }, tagliDonna),

            P(14, "Ballerina in nappa",
              "Ballerina in pelle nappa morbida con suola in cuoio.",
              Genere.Donna, Reparto.Calzature, "mocassini-e-ballerine", "Pelle e Suede", "Pelle nappa",
              590m, new[] { "nero", "cipria" }, new[] { "36", "37", "38", "39", "40", "41" }),

            P(15, "Borsa a mano in pelle",
              "Borsa strutturata con manico rigido, in pelle di vitello a grana fine.",
              Genere.Donna, Reparto.Accessori, "borse", "Pelle e Suede", "Pelle di vitello",
              2900m, new[] { "cognac", "nero" }, unica, nuovo: true),

            P(16, "Sciarpa in cashmere e seta",
              "Sciarpa ampia e leggera, in filato misto cashmere e seta con frange.",
              Genere.Donna, Reparto.Accessori, "sciarpe-e-foulard", "Cashmere", "70% cashmere, 30% seta",
              450m, new[] { "grigio", "blu" }, unica),
        };
    }
}