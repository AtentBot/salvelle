namespace Models.Pharmacy;

/// <summary>
/// Dados específicos da etapa de SEPARAÇÃO
/// </summary>
public class SeparacaoStepData
{
    public List<ItemSeparado> Items { get; set; } = new();
    public string? AreaSeparacao { get; set; }
    public DateTime DataSeparacao { get; set; }
    public bool TodosItensConferidos { get; set; }
}

public class ItemSeparado
{
    public Guid RawMaterialId { get; set; }
    public Guid BatchId { get; set; }
    public string LoteInsumo { get; set; } = default!;
    public decimal QuantidadeNecessaria { get; set; }
    public decimal QuantidadeSeparada { get; set; }
    public string Unidade { get; set; } = default!;
    public string? LocalArmazenagem { get; set; }
    public bool RequerRefrigeracao { get; set; }
    public bool Controlado { get; set; }
}

/// <summary>
/// Dados específicos da etapa de PESAGEM
/// </summary>
public class PesagemStepData
{
    public List<ComponentPesado> Components { get; set; } = new();
    public string? BalancaId { get; set; }
    public string? BalancaNome { get; set; }
    public decimal? AmbienteTemperatura { get; set; }
    public decimal? AmbienteUmidade { get; set; }
}

public class ComponentPesado
{
    public Guid RawMaterialId { get; set; }
    public Guid BatchId { get; set; }
    public decimal QuantidadeEsperada { get; set; }
    public decimal QuantidadePesada { get; set; }
    public string Unidade { get; set; } = default!;
    public string? LoteInsumo { get; set; }
    public decimal DesvioPercentual => QuantidadeEsperada > 0
        ? Math.Abs(((QuantidadePesada - QuantidadeEsperada) / QuantidadeEsperada) * 100)
        : 0;
}

/// <summary>
/// Dados específicos da etapa de MISTURA
/// </summary>
public class MisturaStepData
{
    public string MetodoMistura { get; set; } = default!;
    public string? EquipamentoUtilizado { get; set; }
    public int? TempoMistura { get; set; } // em minutos
    public string? VelocidadeMistura { get; set; } // BAIXA, MEDIA, ALTA
    public decimal? Temperatura { get; set; }
    public decimal? Umidade { get; set; }
    public string? Observacoes { get; set; }
    public DateTime? InicioMistura { get; set; }
    public DateTime? FimMistura { get; set; }
}

/// <summary>
/// Dados específicos da etapa de ENVASE
/// </summary>
public class EnvaseStepData
{
    public string TipoEmbalagem { get; set; } = default!;
    public decimal QuantidadeEnvasada { get; set; }
    public string NumeroLote { get; set; } = default!;
    public DateTime DataFabricacao { get; set; }
    public DateTime DataValidade { get; set; }
    public decimal? PesoTotal { get; set; }
    public int? NumeroUnidades { get; set; }
    public decimal? Rendimento { get; set; } // percentual
    public string? Observacoes { get; set; }
}

/// <summary>
/// Dados específicos da etapa de ROTULAGEM
/// </summary>
public class RotulagemStepData
{
    public string NumeroLote { get; set; } = default!;
    public DateTime DataFabricacao { get; set; }
    public DateTime DataValidade { get; set; }
    public string? InformacoesAdicionais { get; set; }
    public string? CodigoBarras { get; set; }
    public int QuantidadeEtiquetasImpressas { get; set; }
    public string? Observacoes { get; set; }
}

/// <summary>
/// Dados específicos da etapa de CONFERÊNCIA
/// </summary>
public class ConferenciaStepData
{
    public string AspectosVisuais { get; set; } = default!;
    public bool EmbalagemIntegra { get; set; }
    public bool RotuloCorreto { get; set; }
    public bool QuantidadeCorreta { get; set; }
    public bool DocumentacaoCompleta { get; set; }
    public bool AprovadoPorFarmaceutico { get; set; }
    public string? FarmaceuticoResponsavel { get; set; }
    public string? CRF { get; set; }
    public string? Observacoes { get; set; }

    // Checklist de qualidade
    public ChecklistQualidade? Checklist { get; set; }
}

public class ChecklistQualidade
{
    public bool AspectoVisual { get; set; }
    public bool Cor { get; set; }
    public bool Odor { get; set; }
    public bool Homogeneidade { get; set; }
    public bool Peso { get; set; }
    public bool Volume { get; set; }
    public bool Rotulagem { get; set; }
    public bool Embalagem { get; set; }
    public bool Lacre { get; set; }
    public bool Documentacao { get; set; }
}

/// <summary>
/// Dados específicos da etapa de APROVAÇÃO FINAL
/// </summary>
public class AprovacaoStepData
{
    // Identificação do farmacêutico
    public Guid FarmaceuticoId { get; set; }
    public string FarmaceuticoNome { get; set; } = default!;
    public string NomeFarmaceutico { get; set; } = default!; // Alias para compatibilidade
    public string CRF { get; set; } = default!;

    // Checklist de aprovação
    public bool InspecaoVisualOk { get; set; }
    public bool DocumentacaoCompleta { get; set; }
    public bool RotulagemCorreta { get; set; }
    public bool EmbalagemIntegra { get; set; }

    // Resultado
    public bool Aprovado { get; set; }
    public string? MotivoReprovacao { get; set; }
    public string? MotivoRejeicao { get; set; } // Alias para compatibilidade
    public string? AcoesCorretivas { get; set; }
    public string? Observacoes { get; set; }

    // Assinatura
    public DateTime DataAprovacao { get; set; }
    public string? AssinaturaDigital { get; set; }
}

/// <summary>
/// Dados específicos da etapa de EXPEDIÇÃO
/// </summary>
public class ExpedicaoStepData
{
    public string MetodoEntrega { get; set; } = default!;
    public string? CodigoRastreio { get; set; }
    public string? NomeEntregador { get; set; }
    public string? TelefoneEntregador { get; set; }
    public string? EnderecoEntrega { get; set; }
    public DateTime? PrevisaoEntrega { get; set; }
    public string? NomeRecebedor { get; set; }
    public string? DocumentoRecebedor { get; set; }
    public bool ClienteNotificado { get; set; }
    public string? MetodoNotificacao { get; set; }
    public DateTime? DataEntregaEfetiva { get; set; }
    public string? AssinaturaRecebedor { get; set; }
    public bool EntregaConfirmada { get; set; }
}