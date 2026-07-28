/* global React, Section, Swatch, TypeRow, TokenCell */

function ApothecaryProposal() {
  return (
    <div className="proposal apothecary">
      <header className="proposal-header">
        <div>
          <div className="brand-mark">
            <div className="brand-glyph">℞</div>
            <div className="brand-name">orc<em>pharm</em></div>
          </div>
          <h1 className="proposal-title">
            Proposta A — <em>Apothecary</em>
          </h1>
          <p className="proposal-tagline">
            Identidade clínico-editorial. Tipografia serifada com itálicos sutis,
            paleta de papel quente e verde-sálvia profundo. Refere a tradição da
            farmácia magistral, com rigor e refinamento.
          </p>
        </div>
        <div className="proposal-meta">
          <div>Direction A · 01</div>
          <div>Sage / Paper</div>
          <div>Fraunces · Söhne</div>
        </div>
      </header>

      <Section eyebrow="01 / Paleta" title="Cor" sub="Uma única primária — sálvia — combinada com neutros de papel quente. Sem gradientes; sinais de status são dessaturados.">
        <div className="grid-4" style={{ marginBottom: 24 }}>
          <Swatch bg="var(--sage-500)" name="Sage 500 — Primária" hex="#4F6B4D" tone="dark" />
          <Swatch bg="var(--sage-700)" name="Sage 700" hex="#344A35" tone="dark" />
          <Swatch bg="var(--ink)" name="Ink" hex="#1B201C" tone="dark" />
          <Swatch bg="var(--terra)" name="Terracotta — Acento" hex="#B45A3C" tone="dark" />
          <Swatch bg="var(--paper)" name="Paper" hex="#F7F4EE" tone="light" />
          <Swatch bg="var(--paper-2)" name="Paper 2" hex="#EFEAE0" tone="light" />
          <Swatch bg="var(--rule)" name="Rule" hex="#DCD6C8" tone="light" />
          <Swatch bg="var(--ink-3)" name="Ink 3" hex="#6E756F" tone="dark" />
        </div>
      </Section>

      <Section eyebrow="02 / Tipografia" title="Type system" sub="Fraunces (display, com itálicos opcionais para tom editorial) + Söhne (sans body) + JetBrains Mono (rótulos técnicos, dosagens, IDs).">
        <TypeRow meta="Display" size="56 / 1.0">
          <span className="type-display">Fórmulas com <em>precisão</em>.</span>
        </TypeRow>
        <TypeRow meta="Heading 1" size="36 / 1.1" klass="type-h1">
          Manipulação magistral
        </TypeRow>
        <TypeRow meta="Heading 2" size="24 / 1.2" klass="type-h2">
          Cápsulas — Lote 2026/04
        </TypeRow>
        <TypeRow meta="Body" size="15 / 1.6" klass="type-body">
          Sistema completo para farmácias de manipulação. Pesagem, rotulagem, controle de estoque e atendimento PDV em um só lugar.
        </TypeRow>
        <TypeRow meta="Small" size="12 / 1.5" klass="type-small">
          Última atualização há 2 minutos · Conferido por J. Lima
        </TypeRow>
        <TypeRow meta="Mono" size="12 / 1.5" klass="type-mono">
          ORD-2026-04-1183 · CAS 50-78-2 · 500MG
        </TypeRow>
      </Section>

      <Section eyebrow="03 / Tokens" title="Sistema" sub="Escala única de raio, sombra e espaçamento. Compartilhada entre área pública e admin.">
        <div className="token-table">
          <TokenCell label="Radius / sm" value="2px" />
          <TokenCell label="Radius / md" value="4px" />
          <TokenCell label="Radius / lg" value="8px" />
          <TokenCell label="Radius / pill" value="999px" />
          <TokenCell label="Shadow / 1" value="0 1px 2px rgba(27,32,28,.04)" />
          <TokenCell label="Shadow / 2" value="0 8px 20px −8px rgba(27,32,28,.10)" />
          <TokenCell label="Spacing base" value="8 · 12 · 16 · 24 · 32 · 48" />
          <TokenCell label="Grid" value="12 col · 24px gutter" />
        </div>
      </Section>

      <Section eyebrow="04 / Componentes" title="Atomos" sub="Botão, input, badge, card, tabela. Mesma linguagem em toda área pública e admin.">
        <div className="grid-2" style={{ marginBottom: 24 }}>
          <div className="card">
            <div className="card-eyebrow">Botões</div>
            <div className="row" style={{ marginTop: 16 }}>
              <button className="btn btn-primary">Confirmar fórmula</button>
              <button className="btn btn-secondary">Salvar rascunho</button>
              <button className="btn btn-ghost">Cancelar</button>
            </div>
          </div>
          <div className="card">
            <div className="card-eyebrow">Status</div>
            <div className="row" style={{ marginTop: 16 }}>
              <span className="badge badge-ok">Liberado</span>
              <span className="badge badge-warn">Em conferência</span>
              <span className="badge badge-danger">Bloqueado</span>
              <span className="badge badge-neutral">Rascunho</span>
            </div>
          </div>
        </div>

        <div className="card" style={{ marginBottom: 24 }}>
          <div className="card-eyebrow">Formulário</div>
          <div className="grid-2" style={{ marginTop: 16, gap: 24 }}>
            <div>
              <div className="input-label">Princípio ativo</div>
              <input className="input" defaultValue="Minoxidil" />
            </div>
            <div>
              <div className="input-label">Concentração</div>
              <input className="input" defaultValue="5%" />
            </div>
            <div>
              <div className="input-label">Forma farmacêutica</div>
              <input className="input" defaultValue="Solução capilar" />
            </div>
            <div>
              <div className="input-label">Volume</div>
              <input className="input" defaultValue="60 mL" />
            </div>
          </div>
        </div>

        <table>
          <thead>
            <tr>
              <th>Pedido</th>
              <th>Cliente</th>
              <th>Fórmula</th>
              <th>Status</th>
              <th className="num">Total</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td className="type-mono">ORD-1183</td>
              <td>Marina Rocha</td>
              <td>Minoxidil 5% · 60mL</td>
              <td><span className="badge badge-ok">Liberado</span></td>
              <td className="num">R$ 86,40</td>
            </tr>
            <tr>
              <td className="type-mono">ORD-1182</td>
              <td>Henrique Sá</td>
              <td>Bupropiona 150mg · 30 cáps</td>
              <td><span className="badge badge-warn">Conferência</span></td>
              <td className="num">R$ 124,00</td>
            </tr>
            <tr>
              <td className="type-mono">ORD-1181</td>
              <td>Clínica Vita</td>
              <td>Tretinoína 0,05% · 30g</td>
              <td><span className="badge badge-neutral">Rascunho</span></td>
              <td className="num">R$ 58,00</td>
            </tr>
          </tbody>
        </table>
      </Section>

      <Section eyebrow="05 / Tela aplicada" title="Pricing" sub="Mesma página que hoje usa pílulas roxas e badge 'MAIS POPULAR' — recomposta com a linguagem do sistema.">
        <div className="pricing-screen">
          <div className="pricing-head">
            <div className="section-eyebrow" style={{ marginBottom: 12 }}>Planos · 2026</div>
            <h3>Escolha como sua farmácia opera.</h3>
            <p>Todos os planos incluem PDV, manipulação, rotulagem e suporte por chat.</p>
          </div>
          <div className="pricing-grid">
            <div className="pricing-plan">
              <div className="pp-name">Essencial</div>
              <div className="pp-price">R$ 189<small>/mês</small></div>
              <div className="pp-desc">Para farmácias começando a digitalizar a manipulação.</div>
              <ul className="pp-features">
                <li>Até 200 fórmulas/mês</li>
                <li>1 ponto de venda</li>
                <li>Rotulagem padrão</li>
                <li>Suporte por chat</li>
              </ul>
              <button className="btn btn-secondary" style={{ marginTop: 'auto' }}>Começar</button>
            </div>
            <div className="pricing-plan featured">
              <div className="pp-name">Profissional</div>
              <div className="pp-price">R$ 389<small>/mês</small></div>
              <div className="pp-desc">Para operações com manipulação controlada e múltiplos farmacêuticos.</div>
              <ul className="pp-features">
                <li>Fórmulas ilimitadas</li>
                <li>3 pontos de venda</li>
                <li>Rotulagem controlada (Anvisa)</li>
                <li>OCR de receitas · pesagem</li>
                <li>Suporte prioritário</li>
              </ul>
              <button className="btn btn-primary" style={{ marginTop: 'auto' }}>Escolher Profissional</button>
            </div>
            <div className="pricing-plan">
              <div className="pp-name">Rede</div>
              <div className="pp-price">Sob consulta</div>
              <div className="pp-desc">Para redes de manipulação com múltiplas unidades.</div>
              <ul className="pp-features">
                <li>Multi-unidade · consolidado</li>
                <li>SLA dedicado</li>
                <li>Integrações sob medida</li>
                <li>Onboarding presencial</li>
              </ul>
              <button className="btn btn-secondary" style={{ marginTop: 'auto' }}>Falar com vendas</button>
            </div>
          </div>
        </div>
      </Section>
    </div>
  );
}

window.ApothecaryProposal = ApothecaryProposal;
