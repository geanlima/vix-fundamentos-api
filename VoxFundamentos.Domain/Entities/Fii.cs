namespace VoxFundamentos.Domain.Entities;

public class Fii
{
    public string Papel { get; }
    public string Segmento { get; }

    public decimal Cotacao { get; }
    public decimal FfoYield { get; }
    public decimal DividendYield { get; }
    public decimal Pvp { get; }

    public decimal ValorMercado { get; }
    public decimal Liquidez { get; }

    public int QuantidadeImoveis { get; }

    public decimal PrecoMetroQuadrado { get; }
    public decimal AluguelMetroQuadrado { get; }

    public decimal CapRate { get; }
    public decimal VacanciaMedia { get; }

    public Fii(
        string papel,
        string segmento,
        decimal cotacao,
        decimal ffoYield,
        decimal dividendYield,
        decimal pvp,
        decimal valorMercado,
        decimal liquidez,
        int quantidadeImoveis,
        decimal precoMetroQuadrado,
        decimal aluguelMetroQuadrado,
        decimal capRate,
        decimal vacanciaMedia)
    {
        Papel = papel;
        Segmento = segmento;
        Cotacao = cotacao;
        FfoYield = ffoYield;
        DividendYield = dividendYield;
        Pvp = pvp;
        ValorMercado = valorMercado;
        Liquidez = liquidez;
        QuantidadeImoveis = quantidadeImoveis;
        PrecoMetroQuadrado = precoMetroQuadrado;
        AluguelMetroQuadrado = aluguelMetroQuadrado;
        CapRate = capRate;
        VacanciaMedia = vacanciaMedia;
    }
}
