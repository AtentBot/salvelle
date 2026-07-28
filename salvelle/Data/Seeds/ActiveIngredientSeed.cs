using Microsoft.EntityFrameworkCore;
using Models;
using System.Globalization;
using System.Text;

namespace Data.Seeds;

public static class ActiveIngredientSeed
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Set<ActiveIngredient>().AnyAsync())
            return;

        var ingredients = GetIngredients();
        
        foreach (var ingredient in ingredients)
        {
            ingredient.NormalizedName = NormalizeString(ingredient.Name);
        }

        await context.Set<ActiveIngredient>().AddRangeAsync(ingredients);
        await context.SaveChangesAsync();
    }

    private static string NormalizeString(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        
        return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    private static List<ActiveIngredient> GetIngredients()
    {
        return new List<ActiveIngredient>
        {
            // ==================== VITAMINAS ====================
            new() { Name = "Vitamina A (Retinol)", Category = "Vitaminas", DefaultUnit = "UI", MinDosage = 2500, MaxDosage = 10000, Popularity = 95, Synonyms = "Retinol, Acetato de Retinol", Indications = "Visão, pele, imunidade" },
            new() { Name = "Vitamina B1 (Tiamina)", Category = "Vitaminas", DefaultUnit = "mg", MinDosage = 1, MaxDosage = 100, Popularity = 85, Synonyms = "Tiamina, Cloridrato de Tiamina", Indications = "Sistema nervoso, metabolismo energético" },
            new() { Name = "Vitamina B2 (Riboflavina)", Category = "Vitaminas", DefaultUnit = "mg", MinDosage = 1, MaxDosage = 50, Popularity = 80, Synonyms = "Riboflavina", Indications = "Metabolismo, pele, visão" },
            new() { Name = "Vitamina B3 (Niacina)", Category = "Vitaminas", DefaultUnit = "mg", MinDosage = 10, MaxDosage = 500, Popularity = 85, Synonyms = "Niacina, Nicotinamida, Ácido Nicotínico", Indications = "Colesterol, energia, pele" },
            new() { Name = "Vitamina B5 (Ácido Pantotênico)", Category = "Vitaminas", DefaultUnit = "mg", MinDosage = 5, MaxDosage = 100, Popularity = 75, Synonyms = "Pantotenato de Cálcio, D-Pantenol", Indications = "Metabolismo, cabelos, pele" },
            new() { Name = "Vitamina B6 (Piridoxina)", Category = "Vitaminas", DefaultUnit = "mg", MinDosage = 1, MaxDosage = 100, Popularity = 90, Synonyms = "Piridoxina, Cloridrato de Piridoxina", Indications = "Sistema nervoso, TPM, metabolismo proteico" },
            new() { Name = "Vitamina B7 (Biotina)", Category = "Vitaminas", DefaultUnit = "mcg", MinDosage = 30, MaxDosage = 10000, Popularity = 95, Synonyms = "Biotina, Vitamina H", Indications = "Cabelos, unhas, pele" },
            new() { Name = "Vitamina B9 (Ácido Fólico)", Category = "Vitaminas", DefaultUnit = "mcg", MinDosage = 200, MaxDosage = 5000, Popularity = 95, Synonyms = "Folato, Metilfolato, L-Metilfolato", Indications = "Gravidez, anemia, DNA" },
            new() { Name = "Vitamina B12 (Cianocobalamina)", Category = "Vitaminas", DefaultUnit = "mcg", MinDosage = 2, MaxDosage = 5000, Popularity = 98, Synonyms = "Cobalamina, Metilcobalamina, Cianocobalamina", Indications = "Anemia, sistema nervoso, energia" },
            new() { Name = "Metilcobalamina", Category = "Vitaminas", DefaultUnit = "mcg", MinDosage = 500, MaxDosage = 5000, Popularity = 92, Synonyms = "Vitamina B12 Metilada", Indications = "Forma ativa da B12, sistema nervoso" },
            new() { Name = "Vitamina C (Ácido Ascórbico)", Category = "Vitaminas", DefaultUnit = "mg", MinDosage = 60, MaxDosage = 2000, Popularity = 100, Synonyms = "Ácido Ascórbico, Ascorbato de Sódio", Indications = "Imunidade, antioxidante, colágeno" },
            new() { Name = "Vitamina C Lipossomal", Category = "Vitaminas", DefaultUnit = "mg", MinDosage = 500, MaxDosage = 2000, Popularity = 85, Synonyms = "Ácido Ascórbico Lipossomal", Indications = "Alta absorção, imunidade" },
            new() { Name = "Vitamina D3 (Colecalciferol)", Category = "Vitaminas", DefaultUnit = "UI", MinDosage = 400, MaxDosage = 50000, Popularity = 100, Synonyms = "Colecalciferol, Vitamina D", Indications = "Ossos, imunidade, humor" },
            new() { Name = "Vitamina E (Tocoferol)", Category = "Vitaminas", DefaultUnit = "UI", MinDosage = 15, MaxDosage = 800, Popularity = 90, Synonyms = "Tocoferol, Acetato de Tocoferol, Alfa-Tocoferol", Indications = "Antioxidante, pele, fertilidade" },
            new() { Name = "Vitamina K1 (Filoquinona)", Category = "Vitaminas", DefaultUnit = "mcg", MinDosage = 60, MaxDosage = 500, Popularity = 70, Synonyms = "Filoquinona, Fitonadiona", Indications = "Coagulação, ossos" },
            new() { Name = "Vitamina K2 (Menaquinona)", Category = "Vitaminas", DefaultUnit = "mcg", MinDosage = 45, MaxDosage = 200, Popularity = 88, Synonyms = "MK-7, Menaquinona-7", Indications = "Ossos, calcificação arterial" },
            new() { Name = "Complexo B", Category = "Vitaminas", DefaultUnit = "mg", MinDosage = 1, MaxDosage = 100, Popularity = 95, Synonyms = "Vitaminas do Complexo B", Indications = "Energia, sistema nervoso" },
            
            // ==================== MINERAIS ====================
            new() { Name = "Cálcio (Carbonato)", Category = "Minerais", DefaultUnit = "mg", MinDosage = 200, MaxDosage = 1500, Popularity = 95, Synonyms = "Carbonato de Cálcio", Indications = "Ossos, dentes, músculos" },
            new() { Name = "Cálcio (Citrato)", Category = "Minerais", DefaultUnit = "mg", MinDosage = 200, MaxDosage = 1200, Popularity = 88, Synonyms = "Citrato de Cálcio", Indications = "Ossos, melhor absorção" },
            new() { Name = "Cálcio Quelato", Category = "Minerais", DefaultUnit = "mg", MinDosage = 200, MaxDosage = 1000, Popularity = 85, Synonyms = "Cálcio Bisglicinato", Indications = "Ossos, alta biodisponibilidade" },
            new() { Name = "Magnésio (Óxido)", Category = "Minerais", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 500, Popularity = 80, Synonyms = "Óxido de Magnésio", Indications = "Músculos, intestino" },
            new() { Name = "Magnésio (Citrato)", Category = "Minerais", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 400, Popularity = 85, Synonyms = "Citrato de Magnésio", Indications = "Músculos, relaxamento" },
            new() { Name = "Magnésio Quelato", Category = "Minerais", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 400, Popularity = 92, Synonyms = "Magnésio Bisglicinato, Magnésio Dimalato", Indications = "Músculos, sono, ansiedade" },
            new() { Name = "Magnésio Dimalato", Category = "Minerais", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 400, Popularity = 90, Synonyms = "Dimalato de Magnésio", Indications = "Energia, fibromialgia" },
            new() { Name = "Magnésio L-Treonato", Category = "Minerais", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 2000, Popularity = 88, Synonyms = "Treonato de Magnésio, Magtein", Indications = "Cérebro, memória, cognição" },
            new() { Name = "Zinco Quelato", Category = "Minerais", DefaultUnit = "mg", MinDosage = 7, MaxDosage = 50, Popularity = 95, Synonyms = "Zinco Bisglicinato, Zinco Picolinato", Indications = "Imunidade, pele, cabelos" },
            new() { Name = "Zinco Picolinato", Category = "Minerais", DefaultUnit = "mg", MinDosage = 15, MaxDosage = 50, Popularity = 90, Synonyms = "Picolinato de Zinco", Indications = "Imunidade, testosterona" },
            new() { Name = "Selenio Quelato", Category = "Minerais", DefaultUnit = "mcg", MinDosage = 50, MaxDosage = 200, Popularity = 90, Synonyms = "Selênio, Selenometionina", Indications = "Tireoide, antioxidante" },
            new() { Name = "Ferro Quelato", Category = "Minerais", DefaultUnit = "mg", MinDosage = 14, MaxDosage = 60, Popularity = 90, Synonyms = "Ferro Bisglicinato, Ferro Aminoácido Quelato", Indications = "Anemia, energia" },
            new() { Name = "Ferro Lipossomal", Category = "Minerais", DefaultUnit = "mg", MinDosage = 14, MaxDosage = 30, Popularity = 80, Synonyms = "Pirofosfato Férrico Lipossomal", Indications = "Anemia, sem irritação gástrica" },
            new() { Name = "Cobre Quelato", Category = "Minerais", DefaultUnit = "mg", MinDosage = 0.5m, MaxDosage = 2, Popularity = 70, Synonyms = "Cobre Bisglicinato", Indications = "Colágeno, imunidade" },
            new() { Name = "Manganês Quelato", Category = "Minerais", DefaultUnit = "mg", MinDosage = 1, MaxDosage = 10, Popularity = 65, Synonyms = "Manganês Bisglicinato", Indications = "Ossos, metabolismo" },
            new() { Name = "Cromo Quelato", Category = "Minerais", DefaultUnit = "mcg", MinDosage = 50, MaxDosage = 500, Popularity = 85, Synonyms = "Cromo Picolinato, Picolinato de Cromo", Indications = "Glicemia, emagrecimento" },
            new() { Name = "Iodo", Category = "Minerais", DefaultUnit = "mcg", MinDosage = 75, MaxDosage = 300, Popularity = 80, Synonyms = "Iodeto de Potássio", Indications = "Tireoide" },
            new() { Name = "Molibdênio", Category = "Minerais", DefaultUnit = "mcg", MinDosage = 25, MaxDosage = 100, Popularity = 50, Synonyms = "Molibdato de Sódio", Indications = "Enzimas, detox" },
            new() { Name = "Boro", Category = "Minerais", DefaultUnit = "mg", MinDosage = 1, MaxDosage = 10, Popularity = 70, Synonyms = "Borato de Sódio, Ácido Bórico", Indications = "Ossos, hormônios" },
            new() { Name = "Silício Orgânico", Category = "Minerais", DefaultUnit = "mg", MinDosage = 5, MaxDosage = 50, Popularity = 80, Synonyms = "Silício, Ácido Ortosilícico", Indications = "Cabelos, unhas, pele" },
            new() { Name = "Potássio", Category = "Minerais", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 500, Popularity = 75, Synonyms = "Cloreto de Potássio, Citrato de Potássio", Indications = "Pressão arterial, músculos" },

            // ==================== AMINOÁCIDOS ====================
            new() { Name = "L-Triptofano", Category = "Aminoácidos", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 1000, Popularity = 92, Synonyms = "Triptofano", Indications = "Sono, humor, serotonina" },
            new() { Name = "5-HTP", Category = "Aminoácidos", DefaultUnit = "mg", MinDosage = 50, MaxDosage = 300, Popularity = 90, Synonyms = "5-Hidroxitriptofano, Griffonia", Indications = "Sono, humor, ansiedade" },
            new() { Name = "L-Tirosina", Category = "Aminoácidos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 2000, Popularity = 85, Synonyms = "Tirosina", Indications = "Energia, foco, tireoide" },
            new() { Name = "N-Acetil L-Tirosina (NALT)", Category = "Aminoácidos", DefaultUnit = "mg", MinDosage = 150, MaxDosage = 500, Popularity = 78, Synonyms = "NALT", Indications = "Foco, cognição" },
            new() { Name = "L-Teanina", Category = "Aminoácidos", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 400, Popularity = 95, Synonyms = "Teanina, Suntheanine", Indications = "Relaxamento, foco, ansiedade" },
            new() { Name = "L-Glutamina", Category = "Aminoácidos", DefaultUnit = "mg", MinDosage = 500, MaxDosage = 10000, Popularity = 92, Synonyms = "Glutamina", Indications = "Intestino, imunidade, músculos" },
            new() { Name = "L-Arginina", Category = "Aminoácidos", DefaultUnit = "mg", MinDosage = 500, MaxDosage = 6000, Popularity = 88, Synonyms = "Arginina", Indications = "Circulação, hormônio do crescimento" },
            new() { Name = "L-Citrulina", Category = "Aminoácidos", DefaultUnit = "mg", MinDosage = 500, MaxDosage = 6000, Popularity = 82, Synonyms = "Citrulina Malato", Indications = "Circulação, energia, performance" },
            new() { Name = "L-Carnitina", Category = "Aminoácidos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 2000, Popularity = 90, Synonyms = "Carnitina, Acetil L-Carnitina", Indications = "Queima de gordura, energia" },
            new() { Name = "Acetil L-Carnitina (ALCAR)", Category = "Aminoácidos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 2000, Popularity = 85, Synonyms = "ALCAR", Indications = "Cérebro, energia, cognição" },
            new() { Name = "L-Lisina", Category = "Aminoácidos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 3000, Popularity = 80, Synonyms = "Lisina", Indications = "Herpes, colágeno, cálcio" },
            new() { Name = "L-Metionina", Category = "Aminoácidos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1500, Popularity = 70, Synonyms = "Metionina", Indications = "Fígado, cabelos, detox" },
            new() { Name = "L-Cistina", Category = "Aminoácidos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1500, Popularity = 75, Synonyms = "Cistina, Cisteína", Indications = "Cabelos, unhas, detox" },
            new() { Name = "N-Acetil Cisteína (NAC)", Category = "Aminoácidos", DefaultUnit = "mg", MinDosage = 300, MaxDosage = 1800, Popularity = 92, Synonyms = "NAC, Acetilcisteína", Indications = "Antioxidante, fígado, pulmão" },
            new() { Name = "GABA", Category = "Aminoácidos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 750, Popularity = 85, Synonyms = "Ácido Gama-Aminobutírico", Indications = "Ansiedade, sono, relaxamento" },
            new() { Name = "Glicina", Category = "Aminoácidos", DefaultUnit = "mg", MinDosage = 500, MaxDosage = 3000, Popularity = 78, Synonyms = "L-Glicina", Indications = "Sono, colágeno" },
            new() { Name = "L-Prolina", Category = "Aminoácidos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1000, Popularity = 65, Synonyms = "Prolina", Indications = "Colágeno, articulações" },
            new() { Name = "Taurina", Category = "Aminoácidos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 2000, Popularity = 85, Synonyms = "L-Taurina", Indications = "Coração, energia, olhos" },
            new() { Name = "BCAA", Category = "Aminoácidos", DefaultUnit = "mg", MinDosage = 1000, MaxDosage = 10000, Popularity = 88, Synonyms = "Aminoácidos de Cadeia Ramificada", Indications = "Músculos, recuperação" },
            new() { Name = "L-Leucina", Category = "Aminoácidos", DefaultUnit = "mg", MinDosage = 500, MaxDosage = 5000, Popularity = 80, Synonyms = "Leucina", Indications = "Síntese proteica, músculos" },
            new() { Name = "L-Isoleucina", Category = "Aminoácidos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 2000, Popularity = 70, Synonyms = "Isoleucina", Indications = "Energia, músculos" },
            new() { Name = "L-Valina", Category = "Aminoácidos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 2000, Popularity = 70, Synonyms = "Valina", Indications = "Energia, músculos" },
            new() { Name = "Beta-Alanina", Category = "Aminoácidos", DefaultUnit = "mg", MinDosage = 500, MaxDosage = 3200, Popularity = 80, Synonyms = "β-Alanina", Indications = "Performance, resistência" },
            new() { Name = "Creatina", Category = "Aminoácidos", DefaultUnit = "mg", MinDosage = 1000, MaxDosage = 5000, Popularity = 92, Synonyms = "Creatina Monohidratada", Indications = "Força, músculos, cérebro" },
            new() { Name = "L-Ornitina", Category = "Aminoácidos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 2000, Popularity = 65, Synonyms = "Ornitina", Indications = "Detox, hormônio crescimento" },

            // ==================== ÁCIDOS GRAXOS ====================
            new() { Name = "Ômega 3 (EPA/DHA)", Category = "Ácidos Graxos", DefaultUnit = "mg", MinDosage = 500, MaxDosage = 4000, Popularity = 98, Synonyms = "Óleo de Peixe, Fish Oil", Indications = "Coração, cérebro, inflamação" },
            new() { Name = "EPA", Category = "Ácidos Graxos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 2000, Popularity = 85, Synonyms = "Ácido Eicosapentaenoico", Indications = "Anti-inflamatório, coração" },
            new() { Name = "DHA", Category = "Ácidos Graxos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 2000, Popularity = 90, Synonyms = "Ácido Docosahexaenoico", Indications = "Cérebro, visão, gravidez" },
            new() { Name = "Ômega 3 Vegano", Category = "Ácidos Graxos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1000, Popularity = 75, Synonyms = "Óleo de Algas", Indications = "Cérebro, vegano" },
            new() { Name = "Ômega 6 (GLA)", Category = "Ácidos Graxos", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 500, Popularity = 70, Synonyms = "Ácido Gama-Linolênico", Indications = "TPM, pele" },
            new() { Name = "Óleo de Prímula", Category = "Ácidos Graxos", DefaultUnit = "mg", MinDosage = 500, MaxDosage = 2000, Popularity = 82, Synonyms = "Evening Primrose Oil", Indications = "TPM, menopausa, pele" },
            new() { Name = "Óleo de Borragem", Category = "Ácidos Graxos", DefaultUnit = "mg", MinDosage = 500, MaxDosage = 2000, Popularity = 70, Synonyms = "Borage Oil", Indications = "Pele, artrite, TPM" },
            new() { Name = "Óleo de Linhaça", Category = "Ácidos Graxos", DefaultUnit = "mg", MinDosage = 500, MaxDosage = 3000, Popularity = 78, Synonyms = "Flaxseed Oil, ALA", Indications = "Coração, intestino" },
            new() { Name = "Óleo de Coco (TCM)", Category = "Ácidos Graxos", DefaultUnit = "mg", MinDosage = 500, MaxDosage = 5000, Popularity = 85, Synonyms = "MCT Oil, Triglicerídeos de Cadeia Média", Indications = "Energia, cetose" },
            new() { Name = "Ácido Alfa-Lipoico", Category = "Ácidos Graxos", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 600, Popularity = 88, Synonyms = "ALA, R-ALA, Ácido Tióctico", Indications = "Antioxidante, neuropatia, glicemia" },

            // ==================== FITOTERÁPICOS ====================
            new() { Name = "Ashwagandha", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 600, Popularity = 95, Synonyms = "KSM-66, Withania somnifera, Ginseng Indiano", Indications = "Estresse, energia, sono" },
            new() { Name = "Rhodiola Rosea", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 600, Popularity = 88, Synonyms = "Rhodiola, Raiz Dourada", Indications = "Fadiga, estresse, foco" },
            new() { Name = "Ginseng Coreano", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 500, Popularity = 85, Synonyms = "Panax Ginseng", Indications = "Energia, imunidade, libido" },
            new() { Name = "Ginkgo Biloba", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 40, MaxDosage = 240, Popularity = 90, Synonyms = "EGb 761", Indications = "Memória, circulação, cognição" },
            new() { Name = "Bacopa Monnieri", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 150, MaxDosage = 450, Popularity = 85, Synonyms = "Brahmi", Indications = "Memória, aprendizado, ansiedade" },
            new() { Name = "Tribulus Terrestris", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1500, Popularity = 82, Synonyms = "Tribulus", Indications = "Libido, testosterona, energia" },
            new() { Name = "Maca Peruana", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 500, MaxDosage = 3000, Popularity = 92, Synonyms = "Lepidium meyenii", Indications = "Energia, libido, hormônios" },
            new() { Name = "Maca Negra", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 500, MaxDosage = 2000, Popularity = 80, Synonyms = "Black Maca", Indications = "Libido masculina, esperma" },
            new() { Name = "Saw Palmetto", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 160, MaxDosage = 320, Popularity = 80, Synonyms = "Serenoa repens, Palmeto", Indications = "Próstata, queda de cabelo" },
            new() { Name = "Pygeum Africanum", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 50, MaxDosage = 200, Popularity = 70, Synonyms = "Pygeum", Indications = "Próstata" },
            new() { Name = "Dong Quai", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 200, MaxDosage = 600, Popularity = 72, Synonyms = "Angelica sinensis", Indications = "Menopausa, TPM" },
            new() { Name = "Vitex (Agnus Castus)", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 20, MaxDosage = 240, Popularity = 85, Synonyms = "Chasteberry, Pimenteira", Indications = "TPM, ciclo menstrual" },
            new() { Name = "Black Cohosh", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 20, MaxDosage = 80, Popularity = 75, Synonyms = "Cimicifuga racemosa", Indications = "Menopausa, fogachos" },
            new() { Name = "Valeriana", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 200, MaxDosage = 900, Popularity = 88, Synonyms = "Valeriana officinalis", Indications = "Sono, ansiedade" },
            new() { Name = "Passiflora", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 500, Popularity = 85, Synonyms = "Maracujá, Passiflora incarnata", Indications = "Ansiedade, sono" },
            new() { Name = "Camomila", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 200, MaxDosage = 800, Popularity = 82, Synonyms = "Matricaria chamomilla, Apigenina", Indications = "Relaxamento, digestão, sono" },
            new() { Name = "Melissa", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 200, MaxDosage = 600, Popularity = 78, Synonyms = "Erva-Cidreira, Melissa officinalis", Indications = "Ansiedade, sono, digestão" },
            new() { Name = "Magnólia", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 400, Popularity = 75, Synonyms = "Magnolia officinalis, Honokiol", Indications = "Ansiedade, sono, cortisol" },
            new() { Name = "Kava Kava", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 300, Popularity = 70, Synonyms = "Piper methysticum", Indications = "Ansiedade" },
            new() { Name = "Mucuna Pruriens", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 500, Popularity = 80, Synonyms = "L-Dopa Natural", Indications = "Dopamina, humor, libido" },
            new() { Name = "Schisandra", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1000, Popularity = 72, Synonyms = "Schisandra chinensis, Wu Wei Zi", Indications = "Adaptógeno, fígado, estresse" },
            new() { Name = "Eleuthero", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 300, MaxDosage = 1200, Popularity = 70, Synonyms = "Ginseng Siberiano, Eleutherococcus", Indications = "Energia, imunidade" },
            new() { Name = "Astragalus", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1500, Popularity = 75, Synonyms = "Astragalus membranaceus", Indications = "Imunidade, longevidade" },
            new() { Name = "Echinacea", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1000, Popularity = 82, Synonyms = "Equinácea", Indications = "Imunidade, gripes" },
            new() { Name = "Sabugueiro", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1000, Popularity = 78, Synonyms = "Elderberry, Sambucus nigra", Indications = "Imunidade, gripes" },
            new() { Name = "Própolis", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 500, Popularity = 85, Synonyms = "Extrato de Própolis", Indications = "Imunidade, garganta" },
            new() { Name = "Alho Negro", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 600, Popularity = 78, Synonyms = "Black Garlic, Alho Envelhecido", Indications = "Coração, imunidade" },
            new() { Name = "Cúrcuma", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 2000, Popularity = 95, Synonyms = "Açafrão, Curcumina, Turmeric", Indications = "Anti-inflamatório, articulações" },
            new() { Name = "Curcumina", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 1000, Popularity = 92, Synonyms = "95% Curcuminoides", Indications = "Anti-inflamatório" },
            new() { Name = "Gengibre", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1000, Popularity = 85, Synonyms = "Zingiber officinale", Indications = "Digestão, náusea, anti-inflamatório" },
            new() { Name = "Boswellia", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 150, MaxDosage = 500, Popularity = 80, Synonyms = "Incenso, Ácido Boswélico", Indications = "Articulações, inflamação" },
            new() { Name = "Uncaria Tomentosa", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 500, Popularity = 75, Synonyms = "Unha de Gato", Indications = "Imunidade, articulações" },
            new() { Name = "Harpagophytum", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 500, Popularity = 72, Synonyms = "Garra do Diabo", Indications = "Articulações, dor" },
            new() { Name = "Salgueiro Branco", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 120, MaxDosage = 240, Popularity = 65, Synonyms = "Salix alba, Salicina", Indications = "Dor, anti-inflamatório natural" },
            new() { Name = "Berberina", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1500, Popularity = 88, Synonyms = "Berberis", Indications = "Glicemia, metabolismo, intestino" },
            new() { Name = "Gymnema Sylvestre", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 200, MaxDosage = 800, Popularity = 78, Synonyms = "Gymnema", Indications = "Glicemia, doce" },
            new() { Name = "Momordica Charantia", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1000, Popularity = 70, Synonyms = "Melão de São Caetano", Indications = "Glicemia" },
            new() { Name = "Canela de Ceilão", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1500, Popularity = 82, Synonyms = "Cinnamomum verum, Ceylon Cinnamon", Indications = "Glicemia, metabolismo" },
            new() { Name = "Feno Grego", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1000, Popularity = 75, Synonyms = "Trigonella foenum-graecum, Fenugreek", Indications = "Testosterona, amamentação, glicemia" },
            new() { Name = "Garcinia Cambogia", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 500, MaxDosage = 1500, Popularity = 78, Synonyms = "HCA", Indications = "Emagrecimento, apetite" },
            new() { Name = "Café Verde", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 200, MaxDosage = 800, Popularity = 82, Synonyms = "Ácido Clorogênico, Green Coffee", Indications = "Emagrecimento, metabolismo" },
            new() { Name = "Chá Verde", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 200, MaxDosage = 800, Popularity = 88, Synonyms = "Camellia sinensis, EGCG, Catequinas", Indications = "Antioxidante, metabolismo" },
            new() { Name = "Spirulina", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 500, MaxDosage = 3000, Popularity = 85, Synonyms = "Arthrospira", Indications = "Nutrientes, energia, detox" },
            new() { Name = "Chlorella", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 500, MaxDosage = 3000, Popularity = 82, Synonyms = "Chlorella pyrenoidosa", Indications = "Detox, nutrientes" },
            new() { Name = "Cardo Mariano", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 500, Popularity = 85, Synonyms = "Silimarina, Silybum marianum", Indications = "Fígado, detox" },
            new() { Name = "Alcachofra", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1000, Popularity = 78, Synonyms = "Cynara scolymus", Indications = "Fígado, digestão, colesterol" },
            new() { Name = "Boldo", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 500, Popularity = 75, Synonyms = "Peumus boldus", Indications = "Fígado, digestão" },
            new() { Name = "Dente de Leão", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1000, Popularity = 72, Synonyms = "Taraxacum officinale", Indications = "Diurético, fígado" },
            new() { Name = "Cavalinha", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 750, Popularity = 75, Synonyms = "Equisetum arvense", Indications = "Cabelos, unhas, diurético" },
            new() { Name = "Centella Asiática", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 500, Popularity = 82, Synonyms = "Gotu Kola", Indications = "Circulação, cognição, pele" },
            new() { Name = "Castanha da Índia", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 600, Popularity = 78, Synonyms = "Aesculus hippocastanum, Escina", Indications = "Varizes, circulação" },
            new() { Name = "Hamamelis", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 500, Popularity = 68, Synonyms = "Witch Hazel", Indications = "Hemorroidas, varizes" },
            new() { Name = "Cranberry", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1000, Popularity = 85, Synonyms = "Oxicoco, Vaccinium macrocarpon", Indications = "Infecção urinária" },
            new() { Name = "Uva Ursi", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 200, MaxDosage = 500, Popularity = 70, Synonyms = "Arctostaphylos uva-ursi", Indications = "Infecção urinária" },
            new() { Name = "Quebra Pedra", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1000, Popularity = 78, Synonyms = "Phyllanthus niruri", Indications = "Rins, pedras" },
            new() { Name = "Ora-Pro-Nobis", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 500, MaxDosage = 2000, Popularity = 72, Synonyms = "Pereskia aculeata", Indications = "Proteína vegetal, intestino" },
            new() { Name = "Moringa", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1500, Popularity = 80, Synonyms = "Moringa oleifera", Indications = "Nutrientes, energia, anti-inflamatório" },
            new() { Name = "Goji Berry", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1000, Popularity = 78, Synonyms = "Lycium barbarum", Indications = "Antioxidante, imunidade" },
            new() { Name = "Açaí", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 500, MaxDosage = 2000, Popularity = 82, Synonyms = "Euterpe oleracea", Indications = "Antioxidante, energia" },
            new() { Name = "Graviola", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1000, Popularity = 72, Synonyms = "Annona muricata", Indications = "Imunidade, sono" },
            new() { Name = "Cogumelo do Sol", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1500, Popularity = 75, Synonyms = "Agaricus blazei", Indications = "Imunidade" },
            new() { Name = "Reishi", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1500, Popularity = 82, Synonyms = "Ganoderma lucidum, Lingzhi", Indications = "Imunidade, sono, estresse" },
            new() { Name = "Lion's Mane", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1500, Popularity = 85, Synonyms = "Juba de Leão, Hericium erinaceus", Indications = "Cognição, nervos, memória" },
            new() { Name = "Cordyceps", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1500, Popularity = 80, Synonyms = "Cordyceps sinensis", Indications = "Energia, performance, imunidade" },
            new() { Name = "Chaga", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1000, Popularity = 72, Synonyms = "Inonotus obliquus", Indications = "Antioxidante, imunidade" },
            new() { Name = "Turkey Tail", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1500, Popularity = 70, Synonyms = "Trametes versicolor, Coriolus", Indications = "Imunidade" },
            new() { Name = "Shiitake", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 500, MaxDosage = 2000, Popularity = 75, Synonyms = "Lentinula edodes", Indications = "Imunidade, colesterol" },
            new() { Name = "Maitake", Category = "Fitoterápicos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1500, Popularity = 72, Synonyms = "Grifola frondosa", Indications = "Imunidade, glicemia" },

            // ==================== ANTIOXIDANTES ====================
            new() { Name = "Coenzima Q10", Category = "Antioxidantes", DefaultUnit = "mg", MinDosage = 30, MaxDosage = 300, Popularity = 95, Synonyms = "CoQ10, Ubiquinona, Ubiquinol", Indications = "Energia, coração, antioxidante" },
            new() { Name = "Ubiquinol", Category = "Antioxidantes", DefaultUnit = "mg", MinDosage = 50, MaxDosage = 200, Popularity = 88, Synonyms = "CoQ10 Ativa", Indications = "Forma ativa da CoQ10" },
            new() { Name = "PQQ", Category = "Antioxidantes", DefaultUnit = "mg", MinDosage = 10, MaxDosage = 40, Popularity = 78, Synonyms = "Pirroloquinolina Quinona", Indications = "Mitocôndrias, cérebro" },
            new() { Name = "Resveratrol", Category = "Antioxidantes", DefaultUnit = "mg", MinDosage = 50, MaxDosage = 500, Popularity = 88, Synonyms = "Trans-Resveratrol", Indications = "Antienvelhecimento, coração" },
            new() { Name = "Quercetina", Category = "Antioxidantes", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1000, Popularity = 85, Synonyms = "Quercetin", Indications = "Anti-histamínico, antioxidante, imunidade" },
            new() { Name = "Astaxantina", Category = "Antioxidantes", DefaultUnit = "mg", MinDosage = 4, MaxDosage = 12, Popularity = 88, Synonyms = "Astaxanthin", Indications = "Pele, olhos, antioxidante potente" },
            new() { Name = "Luteína", Category = "Antioxidantes", DefaultUnit = "mg", MinDosage = 6, MaxDosage = 20, Popularity = 85, Synonyms = "Luteína + Zeaxantina", Indications = "Olhos, mácula" },
            new() { Name = "Zeaxantina", Category = "Antioxidantes", DefaultUnit = "mg", MinDosage = 2, MaxDosage = 10, Popularity = 80, Synonyms = "Zeaxanthin", Indications = "Olhos, visão" },
            new() { Name = "Licopeno", Category = "Antioxidantes", DefaultUnit = "mg", MinDosage = 5, MaxDosage = 30, Popularity = 82, Synonyms = "Lycopene", Indications = "Próstata, pele, coração" },
            new() { Name = "Beta-Caroteno", Category = "Antioxidantes", DefaultUnit = "mg", MinDosage = 3, MaxDosage = 15, Popularity = 78, Synonyms = "Pró-vitamina A", Indications = "Pele, visão, antioxidante" },
            new() { Name = "Glutationa", Category = "Antioxidantes", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 500, Popularity = 90, Synonyms = "GSH, L-Glutationa Reduzida", Indications = "Master antioxidante, detox, pele" },
            new() { Name = "Glutationa Lipossomal", Category = "Antioxidantes", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 500, Popularity = 85, Synonyms = "Liposomal Glutathione", Indications = "Alta absorção, detox" },
            new() { Name = "SOD (Superóxido Dismutase)", Category = "Antioxidantes", DefaultUnit = "UI", MinDosage = 250, MaxDosage = 1000, Popularity = 70, Synonyms = "GliSODin", Indications = "Antioxidante enzimático" },
            new() { Name = "Extrato de Semente de Uva", Category = "Antioxidantes", DefaultUnit = "mg", MinDosage = 50, MaxDosage = 300, Popularity = 82, Synonyms = "OPC, Proantocianidinas", Indications = "Circulação, antioxidante" },
            new() { Name = "Picnogenol", Category = "Antioxidantes", DefaultUnit = "mg", MinDosage = 25, MaxDosage = 200, Popularity = 80, Synonyms = "Pycnogenol, Extrato de Pinus", Indications = "Circulação, pele, antioxidante" },
            new() { Name = "Pterostilbeno", Category = "Antioxidantes", DefaultUnit = "mg", MinDosage = 25, MaxDosage = 150, Popularity = 72, Synonyms = "Pterostilbene", Indications = "Antienvelhecimento, cognição" },
            new() { Name = "Fisetina", Category = "Antioxidantes", DefaultUnit = "mg", MinDosage = 25, MaxDosage = 100, Popularity = 70, Synonyms = "Fisetin", Indications = "Senolítico, longevidade, cérebro" },

            // ==================== COLÁGENO E PELE ====================
            new() { Name = "Colágeno Hidrolisado", Category = "Colágeno e Pele", DefaultUnit = "mg", MinDosage = 2500, MaxDosage = 10000, Popularity = 98, Synonyms = "Peptídeos de Colágeno", Indications = "Pele, articulações, cabelos" },
            new() { Name = "Colágeno Tipo I", Category = "Colágeno e Pele", DefaultUnit = "mg", MinDosage = 2500, MaxDosage = 10000, Popularity = 88, Synonyms = "Colágeno Bovino", Indications = "Pele, tendões" },
            new() { Name = "Colágeno Tipo II", Category = "Colágeno e Pele", DefaultUnit = "mg", MinDosage = 40, MaxDosage = 100, Popularity = 85, Synonyms = "UC-II, Colágeno Não Desnaturado", Indications = "Articulações, cartilagem" },
            new() { Name = "Colágeno Tipo III", Category = "Colágeno e Pele", DefaultUnit = "mg", MinDosage = 1000, MaxDosage = 5000, Popularity = 75, Synonyms = "Colágeno Reticular", Indications = "Pele, vasos" },
            new() { Name = "Colágeno Verisol", Category = "Colágeno e Pele", DefaultUnit = "mg", MinDosage = 2500, MaxDosage = 5000, Popularity = 92, Synonyms = "Verisol", Indications = "Pele, rugas, celulite" },
            new() { Name = "Colágeno Marinho", Category = "Colágeno e Pele", DefaultUnit = "mg", MinDosage = 2500, MaxDosage = 5000, Popularity = 85, Synonyms = "Colágeno de Peixe", Indications = "Pele, alta absorção" },
            new() { Name = "Ácido Hialurônico", Category = "Colágeno e Pele", DefaultUnit = "mg", MinDosage = 50, MaxDosage = 200, Popularity = 95, Synonyms = "Hialuronato de Sódio", Indications = "Pele, articulações, hidratação" },
            new() { Name = "MSM", Category = "Colágeno e Pele", DefaultUnit = "mg", MinDosage = 500, MaxDosage = 3000, Popularity = 85, Synonyms = "Metilsulfonilmetano, Enxofre Orgânico", Indications = "Articulações, cabelos, pele" },
            new() { Name = "Glucosamina", Category = "Colágeno e Pele", DefaultUnit = "mg", MinDosage = 500, MaxDosage = 1500, Popularity = 88, Synonyms = "Sulfato de Glucosamina", Indications = "Articulações, cartilagem" },
            new() { Name = "Condroitina", Category = "Colágeno e Pele", DefaultUnit = "mg", MinDosage = 400, MaxDosage = 1200, Popularity = 85, Synonyms = "Sulfato de Condroitina", Indications = "Articulações, cartilagem" },
            new() { Name = "Elastina", Category = "Colágeno e Pele", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 500, Popularity = 70, Synonyms = "Peptídeos de Elastina", Indications = "Pele, elasticidade" },
            new() { Name = "Ceramidas", Category = "Colágeno e Pele", DefaultUnit = "mg", MinDosage = 30, MaxDosage = 70, Popularity = 78, Synonyms = "Fitoceramidas", Indications = "Pele, hidratação, barreira" },
            new() { Name = "PABA", Category = "Colágeno e Pele", DefaultUnit = "mg", MinDosage = 30, MaxDosage = 400, Popularity = 55, Synonyms = "Ácido Para-Aminobenzoico", Indications = "Cabelos brancos, pele" },
            new() { Name = "Exsynutriment", Category = "Colágeno e Pele", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 300, Popularity = 80, Synonyms = "Silício Estabilizado em Colágeno", Indications = "Pele, cabelos, unhas" },
            new() { Name = "Bio-Arct", Category = "Colágeno e Pele", DefaultUnit = "mg", MinDosage = 50, MaxDosage = 150, Popularity = 72, Synonyms = "Extrato de Alga do Ártico", Indications = "Energia celular, pele" },
            new() { Name = "Glycoxil", Category = "Colágeno e Pele", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 300, Popularity = 75, Synonyms = "Antiglicante", Indications = "Antiglicação, antienvelhecimento" },
            new() { Name = "In.Cell", Category = "Colágeno e Pele", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 200, Popularity = 70, Synonyms = "Ativo Antiaging", Indications = "Antienvelhecimento celular" },
            new() { Name = "Polypodium Leucotomos", Category = "Colágeno e Pele", DefaultUnit = "mg", MinDosage = 240, MaxDosage = 480, Popularity = 78, Synonyms = "Fernblock", Indications = "Proteção solar oral" },

            // ==================== PROBIÓTICOS ====================
            new() { Name = "Lactobacillus Acidophilus", Category = "Probióticos", DefaultUnit = "UFC", MinDosage = 1000000000, MaxDosage = 10000000000, Popularity = 90, Synonyms = "L. acidophilus", Indications = "Intestino, imunidade" },
            new() { Name = "Lactobacillus Rhamnosus", Category = "Probióticos", DefaultUnit = "UFC", MinDosage = 1000000000, MaxDosage = 10000000000, Popularity = 88, Synonyms = "L. rhamnosus GG, LGG", Indications = "Diarreia, imunidade" },
            new() { Name = "Lactobacillus Casei", Category = "Probióticos", DefaultUnit = "UFC", MinDosage = 1000000000, MaxDosage = 10000000000, Popularity = 82, Synonyms = "L. casei", Indications = "Digestão, imunidade" },
            new() { Name = "Lactobacillus Plantarum", Category = "Probióticos", DefaultUnit = "UFC", MinDosage = 1000000000, MaxDosage = 10000000000, Popularity = 80, Synonyms = "L. plantarum", Indications = "Intestino permeável, IBS" },
            new() { Name = "Lactobacillus Reuteri", Category = "Probióticos", DefaultUnit = "UFC", MinDosage = 100000000, MaxDosage = 5000000000, Popularity = 78, Synonyms = "L. reuteri", Indications = "H. pylori, cólicas" },
            new() { Name = "Bifidobacterium Lactis", Category = "Probióticos", DefaultUnit = "UFC", MinDosage = 1000000000, MaxDosage = 10000000000, Popularity = 85, Synonyms = "B. lactis", Indications = "Imunidade, constipação" },
            new() { Name = "Bifidobacterium Longum", Category = "Probióticos", DefaultUnit = "UFC", MinDosage = 1000000000, MaxDosage = 10000000000, Popularity = 82, Synonyms = "B. longum", Indications = "Ansiedade, intestino" },
            new() { Name = "Bifidobacterium Bifidum", Category = "Probióticos", DefaultUnit = "UFC", MinDosage = 1000000000, MaxDosage = 10000000000, Popularity = 78, Synonyms = "B. bifidum", Indications = "Imunidade, digestão" },
            new() { Name = "Saccharomyces Boulardii", Category = "Probióticos", DefaultUnit = "UFC", MinDosage = 250000000, MaxDosage = 5000000000, Popularity = 88, Synonyms = "S. boulardii, Floratil", Indications = "Diarreia, antibiótico" },
            new() { Name = "Mix Probiótico 10 cepas", Category = "Probióticos", DefaultUnit = "UFC", MinDosage = 5000000000, MaxDosage = 50000000000, Popularity = 90, Synonyms = "Probiótico Multi-cepas", Indications = "Flora intestinal completa" },

            // ==================== PREBIÓTICOS ====================
            new() { Name = "FOS (Frutooligossacarídeos)", Category = "Prebióticos", DefaultUnit = "mg", MinDosage = 2000, MaxDosage = 10000, Popularity = 85, Synonyms = "FOS", Indications = "Alimenta probióticos" },
            new() { Name = "Inulina", Category = "Prebióticos", DefaultUnit = "mg", MinDosage = 2000, MaxDosage = 10000, Popularity = 82, Synonyms = "Fibra de Chicória", Indications = "Prebiótico, saciedade" },
            new() { Name = "GOS (Galactooligossacarídeos)", Category = "Prebióticos", DefaultUnit = "mg", MinDosage = 1000, MaxDosage = 5000, Popularity = 70, Synonyms = "GOS", Indications = "Prebiótico" },
            new() { Name = "Psyllium", Category = "Prebióticos", DefaultUnit = "mg", MinDosage = 1000, MaxDosage = 5000, Popularity = 85, Synonyms = "Plantago ovata", Indications = "Fibra, intestino, colesterol" },
            new() { Name = "Beta-Glucana", Category = "Prebióticos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 750, Popularity = 78, Synonyms = "Beta-Glucan de Aveia", Indications = "Imunidade, colesterol" },

            // ==================== DIGESTÃO ====================
            new() { Name = "Enzimas Digestivas", Category = "Digestão", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 500, Popularity = 88, Synonyms = "Complexo Enzimático", Indications = "Digestão, má absorção" },
            new() { Name = "Bromelina", Category = "Digestão", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1000, Popularity = 82, Synonyms = "Enzima do Abacaxi", Indications = "Digestão, inflamação" },
            new() { Name = "Papaína", Category = "Digestão", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 500, Popularity = 75, Synonyms = "Enzima do Mamão", Indications = "Digestão de proteínas" },
            new() { Name = "Lactase", Category = "Digestão", DefaultUnit = "UI", MinDosage = 3000, MaxDosage = 9000, Popularity = 82, Synonyms = "Enzima da Lactose", Indications = "Intolerância à lactose" },
            new() { Name = "Betaína HCl", Category = "Digestão", DefaultUnit = "mg", MinDosage = 325, MaxDosage = 650, Popularity = 78, Synonyms = "Cloridrato de Betaína", Indications = "Baixa acidez estomacal" },
            new() { Name = "Ox Bile", Category = "Digestão", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 500, Popularity = 70, Synonyms = "Bile Bovina", Indications = "Digestão de gorduras" },
            new() { Name = "DGL (Alcaçuz)", Category = "Digestão", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 750, Popularity = 75, Synonyms = "Alcaçuz Desglicirrizinado", Indications = "Gastrite, úlcera, refluxo" },
            new() { Name = "L-Glutamina (Intestinal)", Category = "Digestão", DefaultUnit = "mg", MinDosage = 2500, MaxDosage = 10000, Popularity = 88, Synonyms = "Glutamina para Intestino", Indications = "Intestino permeável, mucosa" },
            new() { Name = "Aloe Vera", Category = "Digestão", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 500, Popularity = 78, Synonyms = "Babosa, Aloe barbadensis", Indications = "Digestão, intestino, pele" },

            // ==================== HORMONAL E SONO ====================
            new() { Name = "Melatonina", Category = "Sono e Relaxamento", DefaultUnit = "mg", MinDosage = 0.5m, MaxDosage = 10, Popularity = 98, Synonyms = "Hormônio do Sono", Indications = "Sono, jet lag", RequiresPrescription = false },
            new() { Name = "DHEA", Category = "Hormonal", DefaultUnit = "mg", MinDosage = 5, MaxDosage = 50, Popularity = 85, Synonyms = "Dehidroepiandrosterona", Indications = "Hormônios, energia, libido", RequiresPrescription = true },
            new() { Name = "Pregnenolona", Category = "Hormonal", DefaultUnit = "mg", MinDosage = 5, MaxDosage = 50, Popularity = 70, Synonyms = "Hormônio Mãe", Indications = "Memória, hormônios", RequiresPrescription = true },
            new() { Name = "7-Keto DHEA", Category = "Hormonal", DefaultUnit = "mg", MinDosage = 25, MaxDosage = 100, Popularity = 72, Synonyms = "7-Keto", Indications = "Metabolismo, sem conversão hormonal" },
            new() { Name = "Indol-3-Carbinol", Category = "Hormonal", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 400, Popularity = 75, Synonyms = "I3C", Indications = "Metabolismo estrogênio" },
            new() { Name = "DIM", Category = "Hormonal", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 300, Popularity = 80, Synonyms = "Diindolilmetano", Indications = "Equilíbrio hormonal, estrogênio" },
            new() { Name = "Inositol", Category = "Hormonal", DefaultUnit = "mg", MinDosage = 500, MaxDosage = 4000, Popularity = 85, Synonyms = "Myo-Inositol, D-Chiro-Inositol", Indications = "SOP, ansiedade, insulina" },
            new() { Name = "D-Chiro-Inositol", Category = "Hormonal", DefaultUnit = "mg", MinDosage = 50, MaxDosage = 600, Popularity = 78, Synonyms = "DCI", Indications = "SOP, resistência insulínica" },

            // ==================== NOOTROPICOS E COGNIÇÃO ====================
            new() { Name = "Fosfatidilserina", Category = "Nootrópicos", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 300, Popularity = 88, Synonyms = "PS", Indications = "Memória, cognição, cortisol" },
            new() { Name = "Fosfatidilcolina", Category = "Nootrópicos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1200, Popularity = 80, Synonyms = "Lecitina de Soja/Girassol", Indications = "Fígado, cérebro, memória" },
            new() { Name = "CDP-Colina (Citicolina)", Category = "Nootrópicos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1000, Popularity = 85, Synonyms = "Citicolina, Cognizin", Indications = "Memória, foco, neuroproteção" },
            new() { Name = "Alpha-GPC", Category = "Nootrópicos", DefaultUnit = "mg", MinDosage = 150, MaxDosage = 600, Popularity = 82, Synonyms = "Alfa-Glicerofosfocolina", Indications = "Memória, força, HGH" },
            new() { Name = "DMAE", Category = "Nootrópicos", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 400, Popularity = 75, Synonyms = "Dimetilaminoetanol", Indications = "Cognição, humor, pele" },
            new() { Name = "Vinpocetina", Category = "Nootrópicos", DefaultUnit = "mg", MinDosage = 5, MaxDosage = 30, Popularity = 72, Synonyms = "Periwinkle", Indications = "Circulação cerebral, memória" },
            new() { Name = "Huperzina A", Category = "Nootrópicos", DefaultUnit = "mcg", MinDosage = 50, MaxDosage = 200, Popularity = 75, Synonyms = "Huperzia serrata", Indications = "Memória, acetilcolina" },
            new() { Name = "Noopept", Category = "Nootrópicos", DefaultUnit = "mg", MinDosage = 10, MaxDosage = 30, Popularity = 70, Synonyms = "N-fenilacetil-L-prolilglicina", Indications = "Cognição, neuroproteção" },
            new() { Name = "Piracetam", Category = "Nootrópicos", DefaultUnit = "mg", MinDosage = 800, MaxDosage = 4800, Popularity = 75, Synonyms = "Nootropil", Indications = "Cognição, memória", RequiresPrescription = true },
            new() { Name = "Aniracetam", Category = "Nootrópicos", DefaultUnit = "mg", MinDosage = 375, MaxDosage = 1500, Popularity = 68, Indications = "Ansiedade, criatividade", RequiresPrescription = true },
            new() { Name = "Uridina", Category = "Nootrópicos", DefaultUnit = "mg", MinDosage = 150, MaxDosage = 500, Popularity = 65, Synonyms = "Uridina Monofosfato", Indications = "Cognição, humor, sinapse" },
            new() { Name = "Sulbutiamina", Category = "Nootrópicos", DefaultUnit = "mg", MinDosage = 200, MaxDosage = 600, Popularity = 62, Synonyms = "Arcalion", Indications = "Fadiga, memória" },

            // ==================== CARDIOVASCULAR ====================
            new() { Name = "Omega 3 Concentrado", Category = "Cardiovascular", DefaultUnit = "mg", MinDosage = 1000, MaxDosage = 4000, Popularity = 92, Synonyms = "EPA+DHA Concentrado", Indications = "Triglicerídeos, coração" },
            new() { Name = "Niacina (Flush-Free)", Category = "Cardiovascular", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1000, Popularity = 72, Synonyms = "Inositol Hexanicotinato", Indications = "Colesterol HDL" },
            new() { Name = "Berberina (Cardiovascular)", Category = "Cardiovascular", DefaultUnit = "mg", MinDosage = 500, MaxDosage = 1500, Popularity = 85, Synonyms = "Berberin", Indications = "Colesterol, glicemia" },
            new() { Name = "Arroz Vermelho Fermentado", Category = "Cardiovascular", DefaultUnit = "mg", MinDosage = 600, MaxDosage = 2400, Popularity = 78, Synonyms = "Red Yeast Rice, Monacolina K", Indications = "Colesterol LDL" },
            new() { Name = "Policosanol", Category = "Cardiovascular", DefaultUnit = "mg", MinDosage = 5, MaxDosage = 20, Popularity = 72, Synonyms = "Cana de Açúcar", Indications = "Colesterol" },
            new() { Name = "Fitoesteróis", Category = "Cardiovascular", DefaultUnit = "mg", MinDosage = 800, MaxDosage = 2000, Popularity = 75, Synonyms = "Beta-Sitosterol", Indications = "Colesterol, próstata" },
            new() { Name = "Natoquinase", Category = "Cardiovascular", DefaultUnit = "FU", MinDosage = 2000, MaxDosage = 4000, Popularity = 78, Synonyms = "Natto, NSK-SD", Indications = "Circulação, fibrina" },
            new() { Name = "Serrapeptase", Category = "Cardiovascular", DefaultUnit = "SPU", MinDosage = 60000, MaxDosage = 240000, Popularity = 70, Synonyms = "Serratiopeptidase", Indications = "Inflamação, circulação" },
            new() { Name = "L-Arginina (Cardiovascular)", Category = "Cardiovascular", DefaultUnit = "mg", MinDosage = 2000, MaxDosage = 6000, Popularity = 82, Synonyms = "Óxido Nítrico", Indications = "Pressão, circulação, ereção" },
            new() { Name = "Beterraba (Nitrato)", Category = "Cardiovascular", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1000, Popularity = 75, Synonyms = "Beetroot, Nitrato de Beterraba", Indications = "Pressão, performance" },

            // ==================== EMAGRECIMENTO ====================
            new() { Name = "Morosil", Category = "Emagrecimento", DefaultUnit = "mg", MinDosage = 400, MaxDosage = 500, Popularity = 90, Synonyms = "Laranja Moro", Indications = "Gordura abdominal" },
            new() { Name = "Cacti-Nea", Category = "Emagrecimento", DefaultUnit = "mg", MinDosage = 500, MaxDosage = 1000, Popularity = 82, Synonyms = "Extrato de Figo da Índia", Indications = "Drenagem, celulite" },
            new() { Name = "Pholia Negra", Category = "Emagrecimento", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 300, Popularity = 78, Synonyms = "Ilex paraguariensis", Indications = "Celulite, lipólise" },
            new() { Name = "Pholia Magra", Category = "Emagrecimento", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 300, Popularity = 75, Synonyms = "Cordia ecalyculata", Indications = "Saciedade, termogênico" },
            new() { Name = "Faseolamina", Category = "Emagrecimento", DefaultUnit = "mg", MinDosage = 500, MaxDosage = 1500, Popularity = 80, Synonyms = "Feijão Branco", Indications = "Bloqueador de carboidratos" },
            new() { Name = "Quitosana", Category = "Emagrecimento", DefaultUnit = "mg", MinDosage = 500, MaxDosage = 3000, Popularity = 75, Synonyms = "Chitosan", Indications = "Absorção de gordura" },
            new() { Name = "Citrus Aurantium", Category = "Emagrecimento", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 500, Popularity = 78, Synonyms = "Sinefrina, Laranja Amarga", Indications = "Termogênico" },
            new() { Name = "Coleus Forskohlii", Category = "Emagrecimento", DefaultUnit = "mg", MinDosage = 125, MaxDosage = 500, Popularity = 72, Synonyms = "Forskolina", Indications = "Lipólise, testosterona" },
            new() { Name = "Raspberry Ketones", Category = "Emagrecimento", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 400, Popularity = 65, Synonyms = "Cetona de Framboesa", Indications = "Lipólise" },
            new() { Name = "Caralluma Fimbriata", Category = "Emagrecimento", DefaultUnit = "mg", MinDosage = 500, MaxDosage = 1000, Popularity = 68, Synonyms = "Slimaluma", Indications = "Saciedade" },
            new() { Name = "Saffrin", Category = "Emagrecimento", DefaultUnit = "mg", MinDosage = 88, MaxDosage = 176, Popularity = 75, Synonyms = "Extrato de Açafrão", Indications = "Compulsão alimentar" },
            new() { Name = "Capsiate", Category = "Emagrecimento", DefaultUnit = "mg", MinDosage = 3, MaxDosage = 12, Popularity = 70, Synonyms = "Capsimax", Indications = "Termogênico sem ardência" },

            // ==================== PERFORMANCE ESPORTIVA ====================
            new() { Name = "Cafeína Anidra", Category = "Performance", DefaultUnit = "mg", MinDosage = 50, MaxDosage = 400, Popularity = 95, Synonyms = "Cafeína", Indications = "Energia, foco, performance" },
            new() { Name = "Teacrina", Category = "Performance", DefaultUnit = "mg", MinDosage = 50, MaxDosage = 300, Popularity = 75, Synonyms = "TeaCrine", Indications = "Energia sem tolerância" },
            new() { Name = "Dynamine", Category = "Performance", DefaultUnit = "mg", MinDosage = 50, MaxDosage = 200, Popularity = 70, Synonyms = "Metilliberina", Indications = "Energia rápida" },
            new() { Name = "HMB", Category = "Performance", DefaultUnit = "mg", MinDosage = 1000, MaxDosage = 3000, Popularity = 78, Synonyms = "Beta-Hidroxi-Beta-Metilbutirato", Indications = "Músculos, recuperação" },
            new() { Name = "D-Ribose", Category = "Performance", DefaultUnit = "mg", MinDosage = 2500, MaxDosage = 10000, Popularity = 72, Synonyms = "Ribose", Indications = "ATP, energia celular" },
            new() { Name = "Palatinose", Category = "Performance", DefaultUnit = "mg", MinDosage = 5000, MaxDosage = 30000, Popularity = 70, Synonyms = "Isomaltulose", Indications = "Energia sustentada, baixo IG" },
            new() { Name = "Peak ATP", Category = "Performance", DefaultUnit = "mg", MinDosage = 150, MaxDosage = 450, Popularity = 68, Synonyms = "ATP Oral", Indications = "Força, performance" },

            // ==================== OUTROS IMPORTANTES ====================
            new() { Name = "SAMe", Category = "Diversos", DefaultUnit = "mg", MinDosage = 200, MaxDosage = 800, Popularity = 78, Synonyms = "S-Adenosil Metionina", Indications = "Humor, fígado, articulações" },
            new() { Name = "TMG", Category = "Diversos", DefaultUnit = "mg", MinDosage = 500, MaxDosage = 2000, Popularity = 70, Synonyms = "Betaína, Trimetilglicina", Indications = "Metilação, homocisteína" },
            new() { Name = "NMN", Category = "Diversos", DefaultUnit = "mg", MinDosage = 125, MaxDosage = 500, Popularity = 80, Synonyms = "Nicotinamida Mononucleotídeo", Indications = "Longevidade, NAD+" },
            new() { Name = "NR (Niagen)", Category = "Diversos", DefaultUnit = "mg", MinDosage = 125, MaxDosage = 500, Popularity = 75, Synonyms = "Nicotinamida Ribosídeo", Indications = "NAD+, energia celular" },
            new() { Name = "Apigenina", Category = "Diversos", DefaultUnit = "mg", MinDosage = 25, MaxDosage = 100, Popularity = 72, Synonyms = "Flavonoide de Camomila", Indications = "Sono, ansiedade, testosterona" },
            new() { Name = "Rutina", Category = "Diversos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1000, Popularity = 70, Synonyms = "Vitamina P", Indications = "Vasos, hemorroidas" },
            new() { Name = "Hesperidina", Category = "Diversos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1000, Popularity = 72, Synonyms = "Bioflavonoide Cítrico", Indications = "Circulação, varizes" },
            new() { Name = "Diosmina", Category = "Diversos", DefaultUnit = "mg", MinDosage = 450, MaxDosage = 900, Popularity = 78, Synonyms = "Daflon", Indications = "Varizes, hemorroidas" },
            new() { Name = "Espermidina", Category = "Diversos", DefaultUnit = "mg", MinDosage = 1, MaxDosage = 5, Popularity = 65, Synonyms = "Spermidine", Indications = "Autofagia, longevidade" },
            new() { Name = "Carnosina", Category = "Diversos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1000, Popularity = 70, Synonyms = "L-Carnosina", Indications = "Antiglicação, antioxidante" },
            new() { Name = "Shilajit", Category = "Diversos", DefaultUnit = "mg", MinDosage = 100, MaxDosage = 500, Popularity = 72, Synonyms = "Ácido Fúlvico, Mumijo", Indications = "Energia, testosterona, minerais" },
            new() { Name = "Óleo de Krill", Category = "Diversos", DefaultUnit = "mg", MinDosage = 500, MaxDosage = 2000, Popularity = 82, Synonyms = "Krill Oil, Fosfolipídios", Indications = "Ômega 3 fosfolipídico" },
            new() { Name = "Cissus Quadrangularis", Category = "Diversos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1000, Popularity = 68, Synonyms = "Cissus", Indications = "Ossos, articulações, tendões" },
            new() { Name = "Astrágalo", Category = "Diversos", DefaultUnit = "mg", MinDosage = 250, MaxDosage = 1000, Popularity = 72, Synonyms = "TA-65, Astragaloside IV", Indications = "Telômeros, longevidade" }
        };
    }
}
